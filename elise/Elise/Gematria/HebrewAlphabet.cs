namespace Elise.Gematria
{
    public class HebrewAlphabet : Alphabet
    {
        public bool UseFinalValues { get; }

        static int[] Values = {
            1, 2, 3, 4, 5, 6, 7, 8, 9,
            10, 500, 20, 30, 600, 40, 700, 50, 60, 70, 800, 80, 900, 90,
            100, 200, 300, 400
        };

        public override int Calculate(int begin, int end, int value)
        {
            var index = value - begin;
            return index < Values.Length ? Values[index] : 0;
        }

        public HebrewAlphabet(bool useFinalValues = true) : base(1488, 1488 + 27 - 1)
        {
            UseFinalValues = useFinalValues;

            if (!UseFinalValues)
            {
                for (var i = 0; i < Values.Length; i++)
                {
                    if (Values[i] > 400) Values[i] = Values[i + 1];
                }

                MapCharacterValues();
            }
        }
    }
}
