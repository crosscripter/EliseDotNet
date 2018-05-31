#r "elise.dll"
using Elise;

void Log(string output)
{
	File.WriteAllText("NT/NT.txt", output, new UTF8Encoding(false));
}

var books = "MT,MR,LU,JOH,AC,RO,1CO,2CO,GA,EPH,PHP,COL,1TH,2TH,1TI,2TI,TIT,PHM,HEB,JAS,1PE,2PE,1JO,2JO,3JO,JUDE,RE".Split(',');
var newBooks = "MAT,MAR,LUK,JOH,ACT,ROM,1CO,2CO,GAL,EPH,PHI,COL,1TH,2TH,1TI,2TI,TIT,PHL,HEB,JAM,1PE,2PE,1JO,2JO,3JO,JUD,REV".Split(',');

string Read(string path)
{
	var text = File.ReadAllText(path, new UTF8Encoding(false));
	var lines = text.Split(new string[] { "\n" }, StringSplitOptions.None);
	var verses = new List<string>();

	// 40N	1	1		10	βιβλος γενεσεως ιησου χριστου υιου δαβιδ υιου αβρααμ
	for (var i = 8; i < lines.Length; i++)
	{
		var line = lines[i];
		if (line.Trim().Length == 0) continue;
		var fields = line.Split('\t');
		int bookIndex = int.Parse(fields[0].Substring(0, 2));
		var book = books[bookIndex - 40];
		int chapter = int.Parse(fields[1].Trim());
		int verse = int.Parse(fields[2].Trim());
		text = fields[5];
        var newBook = newBooks[bookIndex - 40];
        var result = $"{newBook} {chapter}:{verse} {text}";
		verses.Add(result);
	}

	return string.Join("\n", verses.ToArray());
}

Log(Read(@"NT/TR.txt"));

