using System.Linq;

namespace Elise.Gematria
{
    public class Calculator
    {
        Alphabet Alphabet;

        public Calculator(Alphabet alphabet)
        {
            Alphabet = alphabet;
        }

        public int Calculate(char letter) => Alphabet[letter];
        public int Calculate(string word) => word.Sum(Calculate);
    }
}
