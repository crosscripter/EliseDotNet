using System.Collections.Generic;

namespace Elise.Gematria
{
    public class Alphabet
    {
        public int Begin { get; }
        public int End { get; }
        public Dictionary<char, int> Letters { get; set; }
        public int this[char letter] => Letters.ContainsKey(letter) ? Letters[letter] : 0;

        public virtual int Calculate(int begin, int end, int value)
        {
            return value - begin;
        }

        protected virtual void MapCharacterValues()
        {
            Letters = new Dictionary<char, int>();

            for (var i = Begin; i <= End; i++)
            {
                Letters.Add((char)i, Calculate(Begin, End, i));
            }
        }

        public Alphabet(int begin, int end)
        {
            Begin = begin;
            End = end;
            MapCharacterValues();
        }

        public Alphabet(Dictionary<char, int> letters)
        {
            Letters = letters;
        }
    }
}
