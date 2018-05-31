using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Elise
{
	public struct Reference
	{
		public string Book { get; set; }
		public int Chapter { get; set; }
		public int Verse { get; set; }
		public string ToBook { get; set; }
		public int ToChapter { get; set; }
		public int ToVerse { get; set; }

		public Reference(string book, int chapter, int verse, string toBook=null, int toChapter=0, int toVerse=0)
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

	public class Bible
	{	
		public string FormatReference(string book, int chapter=0, int verse=0)
		{
			var chapterString = chapter > 0 ? $" {chapter}" : "";
			var verseString = verse > 0 ? $":{verse}" : "";
			return $"{book}{chapterString}{verseString}".ToUpper();
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

			Func<string,Reference> parsePart = (part) =>
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

		public string Passage(string fromBook, int fromChapter, int fromVerse, string toBook, int toChapter, int toVerse)
		{
			var passage = new StringBuilder();
			var fromRecord = Record(fromBook, fromChapter, fromVerse);
			int fromID = Array.IndexOf(Records, fromRecord);

			var toRecord = Record(toBook, toChapter, toVerse);
			int toID = Array.IndexOf(Records, toRecord);

			for (var id = fromID; id <= toID; id++)
			{
				var record = Records[id];
				var text = Text(record);
				passage.Append(text + " ");
			}

			return passage.ToString();
		}

		public Dictionary<string,string> Aliases { get; }
		public string[] Records { get; }

		public Bible()
		{
			var text = File.ReadAllText(@"Docs/kjv.txt");
			Records = text.Split(new string[] { "\r\n" }, StringSplitOptions.None);
			
			Aliases = new Dictionary<string,string>
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
				{ "EPISTLES", this["Ro", "Re"] },
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

// var Bible = new BibleDB();

// // Testaments: Bible[testament]
// Console.WriteLine(Bible["NT"]); 
// Console.WriteLine(Bible["OT"]); 

// // Divisions: Bible[division]
// Console.WriteLine(Bible["Law"]); 
// Console.WriteLine(Bible["History"]); 
// Console.WriteLine(Bible["Minor"]); 
// Console.WriteLine(Bible["Major"]); 
// Console.WriteLine(Bible["Prophets"]); 
// Console.WriteLine(Bible["Wisdom"]); 
// Console.WriteLine(Bible["Gospels"]); 
// Console.WriteLine(Bible["Synoptic"]);
// Console.WriteLine(Bible["Epistles"]); 
// Console.WriteLine(Bible["Pauline"]); 
// Console.WriteLine(Bible["General"]); 
// Console.WriteLine(Bible["Bible"]);

// // Books: Bible[book]
// Console.WriteLine(Bible["Ge"]); // GEN (1-50)
// Console.WriteLine(Bible["GE"]);

// // Multiple Books: Bible[fromBook, toBook]
// Console.WriteLine(Bible["Mt", "Joh"]);
// Console.WriteLine(Bible["Mt", "Lu"]);	// The synoptic gospels (MAT-LUK)

// // Chapters: Bible[fromBook, fromChapter, toBook, toChapter]
// Console.WriteLine(Bible["Ge", 6, "Ge", 8]); // The flood (Gen 6-8)

// // Chapter: Bible[book, chapter]
// Console.WriteLine(Bible["Ge", 11]); // Tower of Babel (Gen 11)

// // Verse: Bible[book, chapter, verse]
// Console.WriteLine(Bible["Ge", 1, 3]); // Let there be light (Gen 1:3)

// // Single chapter passage: Bible[book, chapter, fromVerse, toVerse]
// Console.WriteLine(Bible["Ge", 3, 1, 24]); // The fall (Gen 3:1-24)

// // Single book, multiple chapter passage: Bible[book, fromChapter, fromVerse, toChapter, toVerse]
// Console.WriteLine(Bible["Ge", 1, 1, 2, 25]); // Creation story (Gen 1:1-2:25)

// // Multiple book, chapter passage: Bible[fromBook, fromChapter, fromVerse, toBook, toChapter, toVerse]
// Console.WriteLine(Bible["Jer", 1, 1, "La", 5, 22]); // Jeremiah's writings (Jer-Lam)
// Console.WriteLine(Bible["Ge", 1, 1, "Re", 22, 21]);	// Gen-Rev (entire Bible)

// // Reference formatting
// Console.WriteLine(Bible.FormatReference("Ge", 3, 4));

// // Access by recordID
// Console.WriteLine(Bible.GetReference(Bible[0]));
// Console.WriteLine(Bible.Passage("Jude", 1, 1, "Re", 13, 3));
// Console.WriteLine(Bible.Verses("Ps", 119));
// Console.WriteLine(Bible.Chapters("Isa"));
// Console.WriteLine(Bible.ParseReference("Ge"));
// Console.WriteLine(Bible.ParseReference("Ge 1"));
// Console.WriteLine(Bible.ParseReference("Ps 119"));
// Console.WriteLine(Bible.ParseReference("Ge 2:1"));
// Console.WriteLine(Bible.ParseReference("Ge 2-Ge 3"));
// Console.WriteLine(Bible.ParseReference("Ge 2:1-Ge 3"));
// Console.WriteLine(Bible.ParseReference("Ge 2:1-Ge 3:10"));
// Console.WriteLine(Bible.ParseReference("Ge 1:1-Ex 1:2"));
// Console.WriteLine(Bible["Ge"]);
// Console.WriteLine(Bible["Ge 1"]);
// Console.WriteLine(Bible["Ps 119"]);
// Console.WriteLine(Bible["Ge 2:1"]);
// Console.WriteLine(Bible["Ge 2-Ge 3"]);
// Console.WriteLine(Bible["Ge 2:2-Ge 3"]);
// Console.WriteLine(Bible["Ge 2:10-Ge 3:10"]);
// Console.WriteLine(Bible["Ge 1:5-Ex 2:10"]);
