namespace Elise.Formatting
{
    public class HebrewFormatter : Formatter
    {
        protected override string Clean(string text)
        {
            text = text.Replace('ך', 'כ');
            text = text.Replace('ם', 'מ');
            text = text.Replace('ן', 'נ');
            text = text.Replace('ף', 'פ');
            return text.Replace('ץ', 'צ');
        }

        public HebrewFormatter() : base(@"א-ת") { }

        //public override string Format(string text)
        //{
        //    text = base.Format(text);
        //    return StripFinals(text);
        //}
    }
}
