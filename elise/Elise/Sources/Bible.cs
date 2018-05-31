using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Elise.Sources
{
    public class Bible
    {
        public string FormatReference(string book, int chapter = 0, int verse = 0)
        {
            var chapterString = chapter > 0 ? $" {chapter}" : "";
            var verseString = verse > 0 ? $":{verse}" : "";
            return $"{book}{chapterString}{verseString}".ToUpper();
        }

        public Reference[] Search(string phrase)
        {
            var records = Records.Where(rec => rec.ToUpper().Contains(phrase.ToUpper()));
            if (records.Count() == 0) return new Reference[] { };
            return records.Select(GetReference).ToArray();
        }

        public Reference GetReference(string record)
        {
            var reference = record.Substring(0, record.IndexOf(' ', record.IndexOf(':'))).Trim();
            var book = reference.Split(':')[0].Split(' ')[0];
            var chapter = int.Parse(reference.Split(':')[0].Split(' ')[1]);
            var verse = int.Parse(reference.Split(':')[1]);
            return new Reference(book.ToUpper(), chapter, verse);
        }

        public string Book(string record) => GetReference(record).Book;
        public int Chapter(string record) => GetReference(record).Chapter;
        public int Verse(string record) => GetReference(record).Verse;

        public string Text(string record)
        {
            if (string.IsNullOrEmpty(record)) return string.Empty;
            record = Regex.Replace(record, @"[\[\]]", string.Empty);
            return record.Substring(record.IndexOf(' ', record.IndexOf(':'))).Trim();
        }

        public string Record(string book, int chapter, int verse)
        {
            var reference = FormatReference(book, chapter, verse);
            return Records.Where(rec => rec.ToUpper().StartsWith(reference)).FirstOrDefault();
        }

        public string Verse(Reference reference)
        {
            return Verse(reference.Book, reference.Chapter, reference.Verse);
        }

        public string Verse(string book, int chapter, int verse)
        {
            var record = Record(book, chapter, verse);
            return Text(record);
        }

        public int Verses(string book, int chapter)
        {
            var record = Record(book, chapter, 1);
            int recordID = Array.IndexOf(Records, record);

            int verses = 0;

            for (var id = recordID; id < Records.Length; id++)
            {
                record = Records[id];
                if (Book(record) != book.ToUpper()) break;
                if (Chapter(record) != chapter) break;
                verses++;
            }

            return verses;
        }

        public int Chapters(string book)
        {
            var record = Record(book, 1, 1);
            int recordID = Array.IndexOf(Records, record);
            int chapters = 0;

            for (var id = recordID; id < Records.Length; id++)
            {
                record = Records[id];
                if (Book(record) != book.ToUpper()) break;
                chapters = Chapter(record);
            }

            return chapters;
        }

        public Reference ParseReference(string reference)
        {
            reference = reference.Replace("  ", " ");
            reference = reference.ToUpper().Trim();

            // "Ge", "Ge 1", "Gen 2:1", "Ge 2-Ge 3", "Ge 2:1-Ge 3", "Ge 2:1-3:10", "Ge 1:1-Ex 1:2"
            const string ReferencePattern = @"^\s*(\d)?[a-zA-Z]{2,4}(\s+\d{1,3}(\s*\:\s*\d{1,3})?)?(\s*\-\s*(\d)?[a-zA-Z]{2,4}(\s+\d{1,3}(\s*\:\s*\d{1,3})?)?)?\s*$";

            if (!Regex.IsMatch(reference, ReferencePattern))
            {
                throw new ArgumentException($"Invalid Reference Format: '{reference}'");
            }

            var parts = reference.Split('-');
            var fromPart = parts[0].Trim();
            var toPart = string.Empty;

            if (parts.Length > 1)
            {
                toPart = parts[1].Trim();
            }

            Func<string, Reference> parsePart = (part) =>
            {
                var subParts = part.Split(':');
                var first = subParts[0];
                var firstParts = first.Split(' ');
                var book = firstParts[0].ToUpper();
                var chapter = 0;
                var verse = 0;

                if (firstParts.Length > 1)
                {
                    chapter = int.Parse(firstParts[1]);
                }

                if (subParts.Length > 1)
                {
                    var last = subParts[1];
                    verse = int.Parse(last);
                }

                return new Reference(book, chapter, verse);
            };

            var fromRef = parsePart(fromPart);
            var toRef = new Reference(fromRef.Book, fromRef.Chapter, fromRef.Verse);

            if (!string.IsNullOrEmpty(toPart))
            {
                toRef = parsePart(toPart);
            }

            // Validate(fromRef.Book, fromRef.Chapter, fromRef.Verse);
            // Validate(toRef.Book, toRef.Chapter, toRef.Verse);

            return new Reference(fromRef.Book, fromRef.Chapter, fromRef.Verse,
                                 toRef.Book, toRef.Chapter, toRef.Verse);
        }

        public bool TryParseReference(string reference, out Reference result)
        {
            result = new Reference(null, 0, 0);

            try
            {
                result = ParseReference(reference);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Validate(string book, int chapter, int verse)
        {
            if (Array.IndexOf(Books, book.ToUpper()) == -1)
            {
                throw new ArgumentException($"Invalid book '{book}'");
            }

            if (chapter > Chapters(book)) throw new ArgumentException($"Chapter {chapter} not found in {book}");
            if (verse > Verses(book, chapter)) throw new ArgumentException($"Verse {verse} not found in {book} chapter {chapter}");
        }

        public string Passage(string fromBook, int fromChapter, int fromVerse, string toBook, int toChapter, int toVerse)
        {
            var passage = new StringBuilder();
            var fromRecord = Record(fromBook, fromChapter, fromVerse);
            int fromID = Array.IndexOf(Records, fromRecord);

            var toRecord = Record(toBook, toChapter, toVerse);
            int toID = Array.IndexOf(Records, toRecord);

            Validate(fromBook, fromChapter, fromVerse);
            for (var id = fromID; id <= toID; id++)
            {
                var record = Records[id];
                var text = Text(record);
                passage.Append(text + " ");
            }

            return passage.ToString();
        }

        public Dictionary<string, string> Aliases { get; protected set; }
        public string[] Records { get; protected set; }
        public string[] Books { get; protected set; }

        public Bible(Versions version = Versions.KJV)
        {
            var text = Sources.Load($"Elise.Resources.Sources.{version}.src");
            Records = text.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            var bookList = ("Ge,Ex,Le,Nu,De,Jos,Jg,Ru,1Sa,2Sa,1Ki,2Ki,1Ch,2Ch,Ezr,Ne,Es,Job,Ps,Pr,Ec,Song,Isa,Jer," +
                "La,Eze,Da,Ho,Joe,Am,Ob,Jon,Mic,Na,Hab,Zep,Hag,Zec,Mal,Mt,Mr,Lu,Joh,Ac,Ro,1Co,2Co,Ga," +
                "Eph,Php,Col,1Th,2Th,1Ti,2Ti,Tit,Phm,Heb,Jas,1Pe,2Pe,1Jo,2Jo,3Jo,Jude,Re").ToUpper();

            Books = bookList.Split(',');

            Aliases = new Dictionary<string, string>
            {
                { "NT", this["Mt", "Re"] },
                { "OT", this["Ge", "Mal"] },
                { "LAW", this["Ge", "De"] },
                { "HISTORY", this["Jos", "Es"] },
                { "MINOR", this["Ho", "Mal"] },
                { "MAJOR", this["Isa", "Da"] },
                { "PROPHETS", this["Isa", "Mal"] },
                { "WISDOM", this["Ps", "Song"] },
                { "GOSPELS", this["Mt", "Joh"] },
                { "SYNOPTIC", this["Mt", "Lu"] },
                { "EPISTLES", this["Ro", "Jude"] },
                { "PAULINE", this["Ro", "Phm"] },
                { "GENERAL", this["Heb", "Jude"] },
                { "BIBLE", this["Ge", "Re"] }
            };
        }

        // Record ID: Bible[int recordID]
        public string this[int recordID]
        {
            get { return Records[recordID]; }
        }

        // Multiple Books: Bible[fromBook, toBook]
        public string this[string fromBook, string toBook]
        {
            get
            {
                int chapters = Chapters(toBook);
                return Passage(fromBook, 1, 1, toBook, chapters, Verses(toBook, chapters));
            }
        }

        // Chapters: Bible[fromBook, fromChapter, toBook, toChapter]
        public string this[string fromBook, int fromChapter, string toBook, int toChapter]
        {
            get { return Passage(fromBook, fromChapter, 1, toBook, toChapter, Verses(toBook, toChapter)); }
        }

        // Chapter: Bible[book, chapter]	
        public string this[string book, int chapter]
        {
            get { return Passage(book, chapter, 1, book, chapter, Verses(book, chapter)); }
        }

        // Verse: Bible[book, chapter, verse]
        public string this[string book, int chapter, int verse]
        {
            get { return Verse(book, chapter, verse); }
        }

        // Single chapter passage: Bible[book, chapter, fromVerse, toVerse]
        public string this[string book, int chapter, int fromVerse, int toVerse]
        {
            get { return Passage(book, chapter, fromVerse, book, chapter, toVerse); }
        }

        // Single book, multiple chapter passage: Bible[book, fromChapter, fromVerse, toChapter, toVerse]
        public string this[string book, int fromChapter, int fromVerse, int toChapter, int toVerse]
        {
            get { return Passage(book, fromChapter, fromVerse, book, toChapter, toVerse); }
        }

        // Multiple book, chapter passage: Bible[fromBook, fromChapter, fromVerse, toBook, toChapter, toVerse]
        public string this[string fromBook, int fromChapter, int fromVerse, string toBook, int toChapter, int toVerse]
        {
            get { return Passage(fromBook, fromChapter, fromVerse, toBook, toChapter, toVerse); }
        }

        // Alias|Reference: Bible[alias|reference]
        public string this[string value]
        {
            get
            {
                var alias = value.ToUpper();
                if (Aliases.ContainsKey(alias)) return Aliases[alias];

                var reference = ParseReference(value);
                var toBook = reference.ToBook != null ? reference.ToBook : reference.Book;
                var toChapter = reference.ToChapter > 0 ? reference.ToChapter : Chapters(toBook);

                return Passage(
                    reference.Book,
                    reference.Chapter > 0 ? reference.Chapter : 1,
                    reference.Verse > 0 ? reference.Verse : 1,
                    toBook,
                    toChapter,
                    reference.ToVerse > 0 ? reference.ToVerse : Verses(toBook, toChapter)
                );
            }
        }
    }
}
