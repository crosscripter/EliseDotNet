namespace Elise.Gematria
{
    public class GreekAlphabet : Alphabet
    {
        static int[] Values = {
            1, 2, 3, 4, 5, 7, 8, 9,
            10, 20, 30, 40, 50, 60, 70, 80,
            100, 0, 200, 300, 400, 500, 600, 700, 800
        };

        public override int Calculate(int begin, int end, int value)
        {
            var index = value - begin;
            return index < Values.Length ? Values[index] : 0;
        }

        public GreekAlphabet() : base(913, 913 + 24) { }
    }
}
