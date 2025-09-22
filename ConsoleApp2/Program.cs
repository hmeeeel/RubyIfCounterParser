using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ConsoleApp2
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = null;
            while (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                Console.Write("> ");

                var input = Console.ReadLine();

                input = input.Trim();
                if (input.Equals("q", StringComparison.OrdinalIgnoreCase) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    return;

                if ((input.StartsWith("\"") && input.EndsWith("\"")) || (input.StartsWith("'") && input.EndsWith("'"))) input = input.Substring(1, input.Length - 2);

                path = input;
            }

            var code = File.ReadAllText(path, Encoding.UTF8);
            var tokens = RubyLexer.ExtractKeywords(code);

            Console.WriteLine("Извлечённые ключевые слова:");
            Console.WriteLine(tokens.Count == 0 ? "(пусто)" : string.Join(" ", tokens));
            Console.WriteLine("--------------------------------------------------------------");

            var parser = new IfCounterParser(tokens);
            var result = parser.ParseProgram();

            Console.WriteLine("Подсчёт:");
            Console.WriteLine($"if-end:             {result.IfEnd}");
            Console.WriteLine($"if-else-end:        {result.IfElse}");
            Console.WriteLine($"if-elsif-else-end:  {result.IfElsifElse}");
            Console.WriteLine($"if-elsif-end:       {result.IfElsifEnd}");
        }
    }
}