using System.Text.RegularExpressions;

// Law: Gen - Deu
// The Twelve: Hos - Mal
// Prophets: Jos - Mal
// Writings: Psa - Chr

var books = ("Genesis,Exodus,Leviticus,Deuteronomy,Joshua,Judges,Samuel_1,Samuel_2,Kings_1,Kings_2,Isaiah,Jeremiah,Ezekiel,"
+ "Hosea,Joel,Amos,Obadiah,Jonah,Micah,Nahum,Habakkuk,Zephaniah,Haggai,Zechariah,Malachi,"
+ "Psalms,Proverbs,Job,Song_of_Songs,Ruth,Lamentations,Ecclesiastes,Esther,Daniel,Ezra,Nehemiah,"
+ "Chronicles_1,Chronicles_2").Split(',');

var abbrevs = new Dictionary<string,string>()
{
	{ "Genesis", "Ge" }, 
	{ "Exodus", "Ex" },
	{ "Leviticus", "Le" },
	{ "Deuteronomy", "De" },
	{ "Joshua", "Jos" },
	{ "Judges", "Jg" },
	{ "Samuel_1", "1Sa" },
	{ "Samuel_2", "2Sa" },
	{ "Kings_1", "1Ki" },
	{ "Kings_2", "2Ki" },
	{ "Isaiah", "Isa" },
	{ "Jeremiah", "Jer" },
	{ "Ezekiel", "Eze" },
	{ "Hosea", "Ho" },
	{ "Joel", "Joe" },
	{ "Amos", "Am" },
	{ "Obadiah", "Ob" },
	{ "Jonah", "Jon" },
	{ "Micah", "Mic" },
	{ "Nahum", "Na" },
	{ "Habakkuk", "Hab" },
	{ "Zephaniah", "Zep" },
	{ "Haggai", "Hag" },
	{ "Zechariah", "Zec" },
	{ "Malachi", "Mal" },
	{ "Psalms", "Ps" },
	{ "Proverbs", "Pr" },
	{ "Job", "Job" },
	{ "Song_of_Songs", "Song" },
	{ "Ruth", "Ru" },
	{ "Lamentations", "La" },
	{ "Ecclesiastes", "Ec" },
	{ "Esther", "Es" },
	{ "Daniel", "Da" },
	{ "Ezra", "Ezr" },
	{ "Nehemiah", "Ne" },
	{ "Chronicles_1", "1Ch" },
	{ "Chronicles_2", "2Ch" }
};

string[] Read(string bookName)
{
	var text = File.ReadAllText($@"../Docs/Tanach.acc.txt/{bookName}.acc.txt");
	var lines = text.Split('\n');	
	var verses = new List<string>();
	var book = abbrevs[bookName];

	for (var i = 0; i < lines.Length; i++)
	{
		var line = lines[i];
		if (line.StartsWith("‪xxxx")) continue;

		string colon = "׃";
		var colonIndex = line.IndexOf(colon, 0);
		var spaceIndex = line.IndexOf(" ", colonIndex);
		var reference = line.Substring(0, spaceIndex);
		var parts = reference.Split(new string[] { colon }, StringSplitOptions.None);
		var chapter = parts[1].Trim();
		var verse = parts[0].Replace(" ", string.Empty).Trim();
		text = line.Substring(spaceIndex).Trim();
		text = Regex.Replace(text, @"\[.*\]", string.Empty);
		verses.Add($"{book} {chapter}:{verse} {text}");
	}

	return verses.ToArray();
}

foreach (var book in books)
{
	Console.Write($"Formatting {book}...");
	var verses = Read(book);
	var abbr = abbrevs[book];
	var text = string.Join("\n", verses);
	File.WriteAllText($"Tanach/{abbr}.txt", text);	
	File.AppendAllText($"Tanach/tanach.txt", text);
	Console.WriteLine("DONE");
}
