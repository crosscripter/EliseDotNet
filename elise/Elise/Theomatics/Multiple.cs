namespace Elise.Theomatics
{
    public struct Multiple
    {
        public int MultipleOf { get; }
        public int Factor { get; }
        public int Sum { get; }
        public int Start { get; }
        public int Stop { get; }
        public string Text { get; }
        public int Length { get; }

        public Multiple(int multiple, int sum, int start, int stop, string text)
        {
            MultipleOf = multiple;
            Sum = sum;
            Factor = Sum / MultipleOf;
            Start = start;
            Stop = stop;
            Text = text;
            Length = Text.Length;
        }

        public override string ToString()
        {
            return $@"{Start}-{Stop}({Length})""{Text}""={Sum}({MultipleOf}x{Factor})";
        }
    }
}
