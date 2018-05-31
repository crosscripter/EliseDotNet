// var books = "GE,EX,LE,NU,DE,JOS,JG,RU,1SA,2SA,1KI,2KI,1CH,2CH,EZR,NE,ES,JOB,PS,PR,EC,SONG,ISA,JER,LA,EZE,DA,HO,JOE,AM,OB,JON,MIC,NA,HAB,ZEP,HAG,ZEC,MAL".Split(',');
using System;
using System.IO;
using System.Collections.Generic;

public static class Builder
{
	public static UTF8Encoding UTF8 = new UTF8Encoding(false);

	public static void Build(string inFile, string outFile, int bookOffset, int startLine, string bookList)
	{
		var books = bookList.Split(',');		
		var text = File.ReadAllText(inFile, UTF8);
		var lines = text.Split(new string[] { "\n" }, StringSplitOptions.None);
		var verses = new List<string>();

		for (var i = startLine; i < lines.Length; i++)
		{
			var line = lines[i];
			if (line.Trim().Length == 0) continue;
			var fields = line.Split('\t');
			int bookIndex = int.Parse(fields[0].Substring(0, 2));
			var book = newBooks[bookIndex - bookOffset];
			int chapter = int.Parse(fields[1].Trim());
			int verse = int.Parse(fields[2].Trim());
			text = fields[5];
	        var result = $"{book} {chapter}:{verse} {text}";
			verses.Add(result);
		}

		var output = string.Join("\n", verses.ToArray());
		File.WriteAllText(outFile, output, UTF8);
	}
}

static class TanachBuilder
{
	protected static abbrevs = new Dictionary<string,string>()
	{
		{ "Genesis", "GEN" }, 
		{ "Exodus", "EXO" },
		{ "Leviticus", "LEV" },
		{ "Deuteronomy", "DEU" },
		{ "Joshua", "JOS" },
		{ "Judges", "JDG" },
		{ "Samuel_1", "1SA" },
		{ "Samuel_2", "2SA" },
		{ "Kings_1", "1KI" },
		{ "Kings_2", "2KI" },
		{ "Isaiah", "ISA" },
		{ "Jeremiah", "JER" },
		{ "Ezekiel", "EZE" },
		{ "Hosea", "HOS" },
		{ "Joel", "JOE" },
		{ "Amos", "AMO" },
		{ "Obadiah", "OBA" },
		{ "Jonah", "JON" },
		{ "Micah", "MIC" },
		{ "Nahum", "NAH" },
		{ "Habakkuk", "HAB" },
		{ "Zephaniah", "ZEP" },
		{ "Haggai", "HAG" },
		{ "Zechariah", "ZEC" },
		{ "Malachi", "MAL" },
		{ "Psalms", "PSA" },
		{ "Proverbs", "PRO" },
		{ "Job", "JOB" },
		{ "Song_of_Songs", "SON" },
		{ "Ruth", "RUT" },
		{ "Lamentations", "LAM" },
		{ "Ecclesiastes", "ECC" },
		{ "Esther", "EST" },
		{ "Daniel", "DAN" },
		{ "Ezra", "EZR" },
		{ "Nehemiah", "NEH" },
		{ "Chronicles_1", "1CH" },
		{ "Chronicles_2", "2CH" }
	};

	static void BuildBook(string bookName)
	{
		var text = File.ReadAllText($"{bookName}.acc.txt");
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
			var verse = parts[0].Replace("‫", string.Empty).Trim();
			text = line.Substring(spaceIndex).Trim();
			text = Regex.Replace(text, @"\[.*\]", string.Empty);
			verses.Add($"{book} {chapter}:{verse} {text}");
		}

		return verses.ToArray();
	}

	public static void Build(string inFile, string outFile, int bookOffset, int startLine, string bookList)
	{
		var books = bookList.Split(',');
		File.WriteAllText(outFile, string.Empty, Builder.UTF8);

		foreach (var book in books)
		{
			var verses = BuildBook(book);
			var abbr = abbrevs[book];
			var text = string.Join("\n", verses) + "\n";
			File.WriteAllText($"{abbr}.txt", text);
			File.AppendAllText(outFile, text, Builder.UTF8);
		}
	}
}

class Builders 
{
	static void Main()
	{
		Builder.Build("WLC.txt", "OT.txt", 8, 1, "GEN,EXO,LEV,NUM,DEU,JOS,JDG,RUT,1SA,2SA,1KI,2KI,1CH,2CH,EZR,NEH,EST,JOB,PSA,PRO,ECC,SON,ISA,JER,LAM,EZE,DAN,HOS,JOE,AMO,OBA,JON,MIC,NAH,HAB,ZEP,HAG,ZEC,MAL");
		Builder.Build("TR.txt", "NT.txt", 8, 40, "MAT,MAR,LUK,JOH,ACT,ROM,1CO,2CO,GAL,EPH,PHI,COL,1TH,2TH,1TI,2TI,TIT,PHL,HEB,JAM,1PE,2PE,1JO,2JO,3JO,JUD,REV");
		TanachBuilder.Build("*.acc.txt", "TNK.txt", 0, 0, "Genesis,Exodus,Leviticus,Deuteronomy,Joshua,Judges,Samuel_1,Samuel_2,Kings_1,Kings_2,Isaiah,Jeremiah,Ezekiel,"
												   + "Hosea,Joel,Amos,Obadiah,Jonah,Micah,Nahum,Habakkuk,Zephaniah,Haggai,Zechariah,Malachi,"
												   + "Psalms,Proverbs,Job,Song_of_Songs,Ruth,Lamentations,Ecclesiastes,Esther,Daniel,Ezra,Nehemiah,"
												   + "Chronicles_1,Chronicles_2");
	}
}
