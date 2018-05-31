namespace Elise.Gematria
{
    public class LatinAlphabet : Alphabet
    {
        public bool UseAgrippaKey { get; }

        static int[] Values = {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 600,
            10, 20, 30, 40, 50, 60, 70, 80, 90,
            100, 200, 700, 900, 300, 400, 500
        };

        public override int Calculate(int begin, int end, int value)
        {
            return UseAgrippaKey ? Values[value - begin] : value - (begin - 1);
        }

        public LatinAlphabet(bool useAgrippaKey = false) : base(65, 65 + 26 - 1)
        {
            UseAgrippaKey = useAgrippaKey;
            if (UseAgrippaKey) MapCharacterValues();
        }
    }
}
