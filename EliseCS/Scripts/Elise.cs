using System;
using System.Text.RegularExpressions;

namespace Elise {
    
    class Sequence {
        string _text;
        
        public string Text { get; private set; }
        public int Length { get { return Text.Length; } }
        public int Skip { get; private set; }
        public int Start { get; private set; }
        
        public Sequence(string text, int skip=1, int start=0) {
            _text = Clean(text);
            Text = _text.Substring(start);
            Skip = skip;
            Start = start;
        }
        
        static string Clean(string text) {
            var re = new Regex(@"[^A-Z]");
            return re.Replace(text.ToUpper().Trim(), string.Empty);
        }
    }
}

class Program {
    static void Main() {
        var seq = new Elise.Sequence("Abcdefg", 10);
        Console.WriteLine("{0} ({1}) at {2}", seq.Text, seq.Length, seq.Start);
    }
}
