using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Elise.Formatting
{
    public abstract class Formatter
    {
        public static readonly Dictionary<Language, Formatter> Formatters = new Dictionary<Language, Formatter>
        {
            { Language.Hebrew, new HebrewFormatter() },
            { Language.Greek, new GreekFormatter() },
            { Language.English, new LatinFormatter() }
        };

        public string Pattern { get; }

        public Formatter(string pattern)
        {
            Pattern = pattern;
        }

        protected virtual string Clean(string text) => text;

        public string Format(string text)
        {
            return Regex.Replace(text.ToUpperInvariant(), $@"[^{Pattern}]", string.Empty).Trim();
        }
    }
}
