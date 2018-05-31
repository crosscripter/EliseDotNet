#r "elise.dll"
using Elise;

void Log(string output)
{
	File.WriteAllText("Tanach/WLC.txt", output, new UTF8Encoding(false));
}

var books = "GE,EX,LE,NU,DE,JOS,JG,RU,1SA,2SA,1KI,2KI,1CH,2CH,EZR,NE,ES,JOB,PS,PR,EC,SONG,ISA,JER,LA,EZE,DA,HO,JOE,AM,OB,JON,MIC,NA,HAB,ZEP,HAG,ZEC,MAL".Split(',');

string Read(string path)
{
	var text = File.ReadAllText(path, new UTF8Encoding(false));
	var lines = text.Split(new string[] { "\n" }, StringSplitOptions.None);
	var verses = new List<string>();

	for (var i = 8; i < lines.Length; i++)
	{
		var line = lines[i];
		if (line.Trim().Length == 0) continue;
		var fields = line.Split('\t');
		int bookIndex = int.Parse(fields[0].Substring(0, 2));
		var book = books[bookIndex - 1];
		int chapter = int.Parse(fields[1].Trim());
		int verse = int.Parse(fields[2].Trim());
		text = fields[5];
		var result = $"{book} {chapter}:{verse} {text}";
		verses.Add(result);
	}

	return string.Join("\n", verses.ToArray());
}

Log(Read(@"WLC.txt"));

