using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp2
{
    public class IfCounters
    {
        public int IfEnd { get; set; }
        public int IfElse { get; set; }
        public int IfElsifElse { get; set; }
        public int IfElsifEnd { get; set; }

        public void Add(IfCounters other)
        {
            IfEnd += other.IfEnd;
            IfElse += other.IfElse;
            IfElsifElse += other.IfElsifElse;
            IfElsifEnd += other.IfElsifEnd;
        }
    }

    class IfCounterParser
    {
        public List<string> tokens;
        public int pos;

        public IfCounterParser(List<string> token)
        {
            tokens = token;
            pos = 0;
        }

        private string Current => pos < tokens.Count ? tokens[pos] : null;

        private static bool KwEquals(string a, string b) =>
            a != null && a.Equals(b, StringComparison.OrdinalIgnoreCase);

        private bool Is(string kw) => KwEquals(Current, kw);

        private bool IsAny(params string[] kws)
        {
            var cur = Current;
            foreach (var k in kws) if (KwEquals(cur, k)) return true;
            return false;
        }

        private string Eat()
        {
            return tokens[pos++];
        }

        private void Expect(string expected)
        {
            pos++;
        }

        public IfCounters ParseProgram()
        {
            var total = new IfCounters();

            while (!(pos >= tokens.Count))
            {
                if (Is("if")) total.Add(ParseIf());
                else Eat();
            }

            return total;
        }

        private IfCounters ParseBlock()
        {
            var acc = new IfCounters();

            while (!(pos >= tokens.Count) && !IsAny("elsif", "else", "end"))
            {
                if (Is("if")) acc.Add(ParseIf());
                else Eat();
            }

            return acc;
        }

        // if then if if then else end end else if end end
        private IfCounters ParseIf()
        {
            Expect("if");
            var counters = new IfCounters();

            // then
            counters.Add(ParseBlock());

            // elsif
            int elsifCount = 0;
            while (Is("elsif"))
            {
                Eat();
                counters.Add(ParseBlock());
                elsifCount++;
            }

            //  else
            bool hasElse = false;
            if (Is("else"))
            {
                hasElse = true;
                Eat();

                counters.Add(ParseBlock());
            }

            if (Is("end"))
            {
                Eat(); 

                if (elsifCount == 0 && !hasElse)
                    counters.IfEnd++;
                else if (elsifCount == 0 && hasElse)
                    counters.IfElse++;
                else if (elsifCount >= 1 && hasElse)
                    counters.IfElsifElse++;
                else if (elsifCount >= 1 && !hasElse)
                    counters.IfElsifEnd++;
            }

            return counters;
        }
    }
}
