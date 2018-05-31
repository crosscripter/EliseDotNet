using System.Linq;
using Elise.Gematria;
using Elise.Formatting;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Elise.Theomatics
{
    public class Multiplier
    {
        public Calculator Calculator { get; set; }
        public Formatter Formatter { get; set; }

        public Multiplier(Calculator calculator, Formatter formatter)
        {
            Calculator = calculator;
            Formatter = formatter;
        }

        public string[] SplitWords(string text)
        {
            return Regex.Split(text, @"\b").Where(p => p.Trim().Length > 0).ToArray();
        }

        public bool IsMultiple(int sum, int multiple) => sum > 0 && sum % multiple == 0;

        public Multiple[] Calculate(string text, int multiple)
        {
            var multiples = new List<Multiple>();
            var phrase = new List<string>();
            var words = SplitWords(text);
            var index = 0;

            foreach (var word in words)
            {
                index = text.IndexOf(word, index);
                var fword = Formatter.Format(word);
                phrase.Add(word);
                var sum = Calculator.Calculate(fword);

                if (IsMultiple(sum, multiple))
                {
                    var mul = new Multiple(multiple, sum, index, index + fword.Length, word);
                    if (!multiples.Contains(mul)) multiples.Add(mul);
                }
                else
                {
                    for (var i = 0; i < phrase.Count(); i++)
                    {
                        for (var j = 0; j < phrase.Count() - i; j++)
                        {
                            var pwords = string.Join(" ", phrase.Where((w, n) => n >= i && n <= j + 1));
                            if (pwords.Trim().Length == 0) continue;
                            var fwords = Formatter.Format(pwords);
                            sum = Calculator.Calculate(fwords);

                            if (IsMultiple(sum, multiple))
                            {
                                var pindex = text.IndexOf(pwords, 0);
                                var mul = new Multiple(multiple, sum, pindex, pindex + pwords.Length, pwords);
                                if (!multiples.Contains(mul)) multiples.Add(mul);
                            }
                        }
                    }
                }
            }

            return multiples.OrderBy(m => m.Sum)
                            .ThenBy(m => m.Length)
                            .ThenBy(m => m.Start)
                            .ToArray();
        }
    }
}
