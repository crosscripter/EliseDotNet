namespace Elise.Sequencing
{
    public struct Hit
    {
        public string Term { get; }
        public int Index { get; }
        public int Start { get; }
        public int Skip { get; }

        public Hit(string term, int index, int start, int skip)
        {
            Term = term;
            Index = index;
            Start = start;
            Skip = skip;
        }

        public override string ToString()
        {
            return $"Term '{Term}' found at {Index + 1} skipping every {Skip} letter(s)";
        }
    }
}
