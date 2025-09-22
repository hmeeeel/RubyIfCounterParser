using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp2
{
    class RubyLexer
    {
        public static List<string> ExtractKeywords(string source)
        {
            var result = new List<string>();
            int i = 0, n = source.Length;
            bool inSingle = false, inDouble = false, inComment = false;

            bool IsIdentStart(char c) => char.IsLetter(c) || c == '_';
            bool IsIdentChar(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '?' || c == '!';

            while (i < n)
            {
                char c = source[i];

                if (inComment)
                {
                    if (c == '\n') inComment = false;
                    i++;
                    continue;
                }

                if (inSingle)
                {
                    if (c == '\\') { i += Math.Min(2, n - i); continue; }
                    if (c == '\'') { inSingle = false; i++; continue; }
                    i++;
                    continue;
                }

                if (inDouble)
                {
                    if (c == '\\') { i += Math.Min(2, n - i); continue; }
                    if (c == '"') { inDouble = false; i++; continue; }
                    i++;
                    continue;
                }

                if (c == '#') { inComment = true; i++; continue; }
                if (c == '\'') { inSingle = true; i++; continue; }
                if (c == '"') { inDouble = true; i++; continue; }

                if (IsIdentStart(c))
                {
                    int start = i;
                    i++;
                    while (i < n && IsIdentChar(source[i])) i++;
                    var w = source.Substring(start, i - start).ToLowerInvariant();
                    if (w == "if" || w == "elsif" || w == "else" || w == "then" || w == "end")
                        result.Add(w);
                    continue;
                }

                i++;
            }

            return result;
        }
    }
}
