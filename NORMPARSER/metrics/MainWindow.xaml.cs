﻿using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace metrics
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string lexerPath = @"Test2.exe";


        public MainWindow()
        {
            InitializeComponent();
        }


        private void BtnLoadFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Текстовые файлы|*.txt;*.rb;*.cs;*.cpp|Все файлы|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                TxtCodeInput.Text = File.ReadAllText(dlg.FileName);
                
            }
        }

        static string GetTypeFromToken(string token)
        {
            int commaIndex = token.IndexOf(',');
            return token.Substring(1, commaIndex - 1).Trim();
        }

        static string GetLexemeFromToken(string token)
        {
            int firstQuote = token.IndexOf('"');
            int lastQuote = token.LastIndexOf('"');
            if (firstQuote >= 0 && lastQuote > firstQuote)
                return token.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
            return "";
        }

        static void Inc(Dictionary<string, int> dict, string key)
        {
            if (string.IsNullOrEmpty(key)) key = "<empty>";
            if (dict.TryGetValue(key, out int val))
                dict[key] = val + 1;
            else
                dict[key] = 1;
        }

        static int Sum(Dictionary<string, int> dict)
        {
            int s = 0;
            foreach (var kv in dict) s += kv.Value;
            return s;
        }

        class Block
        {
            public string Kind;
            public bool HasElse;
            public bool HasWhen;
            public bool HasElseIf;
            public bool HasIn;
            public Block(string kind) { Kind = kind; }
        }

        private void BtnAnalyze_Click(object sender, RoutedEventArgs e)
        {
            File.WriteAllText("code.txt", TxtCodeInput.Text);
            Process proc = Process.Start(lexerPath, "");
            proc.WaitForExit(); // блокирует поток до завершения

            if (!File.Exists("tokens.txt"))
            {
                Console.WriteLine($"Файл tokens не найден.");
                return;
            }

            ////////////////////////////////
            var operators = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var operands = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var stack = new Stack<Block>();

            string prevType = null;
            string prevLexeme = null;

            foreach (var line in File.ReadLines("tokens.txt"))
            {
                if (!line.StartsWith("<")) continue;

                string type = GetTypeFromToken(line);
                string lexeme = GetLexemeFromToken(line);

                switch (type.ToUpperInvariant())
                {
                    // открывающие блоки
                    case "IF":
                    case "CLASS":
                    case "DEF":
                    case "FOR":
                    case "MODULE":
                    case "WHILE":
                    case "UNTIL":
                    case "DO":
                    case "BEGIN":
                    case "CASE":
                    case "UNLESS":
                        stack.Push(new Block(type.ToUpperInvariant()));
                        break;


                    case "OPERATOR" when lexeme == "(":
                        if (prevType == "IDENTIFIER" ||
                            (prevType == "OPERATOR" && (prevLexeme == "puts" || prevLexeme == "gets")))
                        {
                            // вызов функции → игнорируем
                            stack.Push(new Block("FUNC_CALL_PAREN"));
                        }
                        else
                        {
                            stack.Push(new Block("EXPR_PAREN"));
                        }
                        break;

                    case "OPERATOR" when lexeme == ")":
                        if (stack.Count > 0)
                        {
                            var blk = stack.Pop();
                            if (blk.Kind == "EXPR_PAREN")
                            {
                                // каждая закрытая пара → отдельный оператор
                                Inc(operators, "()");
                            }
                            // если FUNC_CALL_PAREN → игнорируем
                        }
                        else
                        {
                            Inc(operators, "UNMATCHED_PAREN");
                        }
                        break;

                    case "IN":
                        if (stack.Count > 0 && stack.Peek().Kind == "FOR")
                            stack.Peek().HasIn = true;
                        break;

                    // промежуточные
                    case "ELSE":
                        if (stack.Count > 0)
                            stack.Peek().HasElse = true;
                        break;
                    case "ELSIF":
                        if (stack.Count > 0 && stack.Peek().Kind == "IF")
                            stack.Peek().HasElse = true;
                        break;
                    case "WHEN":
                        if (stack.Count > 0 && stack.Peek().Kind == "CASE")
                            stack.Peek().HasWhen = true;
                        break;

                    // закрывающий
                    case "END":
                        if (stack.Count > 0)
                        {
                            var blk = stack.Pop();
                            string name = blk.Kind switch
                            {
                                "IF" => blk.HasElse ? "IF_ELSE_BLOCK" : "IF_BLOCK",
                                "CASE" => blk.HasWhen ? "CASE_BLOCK" : "CASE_EMPTY_BLOCK",
                                "FOR" => blk.HasIn ? "FOR_IN_BLOCK" : "FOR_BLOCK",
                                _ => blk.Kind + "_BLOCK"
                            };
                            Inc(operators, name);
                        }
                        else
                        {
                            Inc(operators, "END");
                        }
                        break;

                    // игнорируем переносы строк, пробелы и комментарии
                    case "NEWLINE":
                    case "WHITESPACE":
                    case "COMMENT":
                        break;

                    // обычные операторы
                    case "OPERATOR":
                        if (lexeme is "+=" or "-=" or "*=" or "/=" or "GETS" or "PUTS")
                            Inc(operators, lexeme);
                        else if (lexeme != "(" && lexeme != ")") // скобки обрабатываются отдельно
                            Inc(operators, lexeme);
                        break;

                    // всё остальное считаем операндами
                    default:
                        if (type.ToUpperInvariant() == "IDENTIFIER")
                        {
                            // случай объявления функции
                            if (prevType == "DEF")
                            {
                                Inc(operators, lexeme); // имя функции = операнд
                            }
                            else
                            {
                                Inc(operands, lexeme);
                            }
                        }
                        else
                        {
                            Inc(operands, lexeme);
                        }
                        break;
                }
                prevType = type.ToUpperInvariant();
                prevLexeme = lexeme;
            }

            // незакрытые блоки
            while (stack.Count > 0)
            {
                var blk = stack.Pop();
                //Inc(operators, blk.Kind + "_UNCLOSED");
            }

            // Метрики
            int eta1 = operators.Count;
            int eta2 = operands.Count;
            int N1 = Sum(operators);
            int N2 = Sum(operands);
            int eta = eta1 + eta2;
            int N = N1 + N2;

            double V = (eta > 0) ? N * Math.Log(eta, 2) : 0;
            double D = (eta2 > 0) ? (eta1 / 2.0) * (N2 / (double)eta2) : 0;

            //////////////////////////////////////////////////


            // метрики
            TxtMetrics.Text = $"η1 (операторы): {eta1}\n" +
                              $"η2 (операнды): {eta2}\n" +
                              $"N1 (все операторы): {N1}\n" +
                              $"N2 (все операнды): {N2}\n" +
                              $"Словарь: {eta}, Длина: {N}\n" +
                              $"Объём: {V:F3}, Сложность: {D:F3}";

            // объединяем в одну таблицу
            var rows = new List<ResultRow>();
            int max = Math.Max(operators.Count, operands.Count);
            var opKeys = operators.Keys.ToList();
            var opdKeys = operands.Keys.ToList();

            for (int i = 0; i < max; i++)
            {
                rows.Add(new ResultRow
                {
                    Operator = i < opKeys.Count ? opKeys[i] : "",
                    OperatorCount = i < opKeys.Count ? operators[opKeys[i]] : 0,
                    Operand = i < opdKeys.Count ? opdKeys[i] : "",
                    OperandCount = i < opdKeys.Count ? operands[opdKeys[i]] : 0
                });
            }

            GridResults.ItemsSource = null; 
            GridResults.ItemsSource = rows;
        }
    }

    public class ResultRow
    {
        public string Operator { get; set; }
        public int OperatorCount { get; set; }
        public string Operand { get; set; }
        public int OperandCount { get; set; }
    }
}