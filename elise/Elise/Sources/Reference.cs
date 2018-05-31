namespace Elise.Sources
{
    public struct Reference
    {
        public string Book { get; set; }
        public int Chapter { get; set; }
        public int Verse { get; set; }
        public string ToBook { get; set; }
        public int ToChapter { get; set; }
        public int ToVerse { get; set; }

        public Reference(string book, int chapter, int verse, string toBook = null, int toChapter = 0, int toVerse = 0)
        {
            Book = book;
            Chapter = chapter;
            Verse = verse;
            ToBook = string.IsNullOrEmpty(toBook) ? book : toBook;
            ToChapter = toChapter;
            ToVerse = toVerse;
        }

        public override string ToString()
        {
            return $"{Book} {Chapter}:{Verse}-{ToBook} {ToChapter}:{ToVerse}";
        }
    }
}
