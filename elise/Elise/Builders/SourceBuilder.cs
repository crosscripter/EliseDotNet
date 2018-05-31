using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Elise.Builders
{
    public static class SourceBuilder
    {
        public static Encoding encoding = new UTF8Encoding(false);

        public static void BuildWLC() => Build("WLC.txt", "GE,EX,LE,NU,DE,JOS,JG,RU,1SA,2SA,1KI,2KI,1CH,2CH,EZR,NE,ES,JOB,PS,PR,EC,SONG,ISA,JER,LA,EZE,DA,HO,JOE,AM,OB,JON,MIC,NA,HAB,ZEP,HAG,ZEC,MAL", 1);
        public static void BuildBYZ() => Build("BYZ.txt", "MT,MR,LU,JOH,AC,RO,1CO,2CO,GA,EPH,PHP,COL,1TH,2TH,1TI,2TI,TIT,PHM,HEB,JAS,1PE,2PE,1JO,2JO,3JO,JUDE,RE", 40);

        static void Build(string path, string bookList, int bookOffset)
        {   
            var books = bookList.Split(',');
            var verses = new List<string>();
            var text = File.ReadAllText(path, encoding);
            var lines = text.Split(new string[] { "\n" }, StringSplitOptions.None);

            for (var i = 8; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Trim().Length == 0) continue;
                var fields = line.Split('\t');
                int bookIndex = int.Parse(fields[0].Substring(0, 2));
                var book = books[bookIndex - bookOffset];
                int chapter = int.Parse(fields[1].Trim());
                int verse = int.Parse(fields[2].Trim());
                text = fields[5];
                var result = $"{book} {chapter}:{verse} {text}";
                verses.Add(result);
            }

            var output = string.Join("\n", verses.ToArray());
            File.WriteAllText(path.Replace(".txt", ".src"), output, encoding);
        }

        public static void BuildTanach()
        {
            var abbrevs = new Dictionary<string, string>()
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

            var books = abbrevs.Keys;
            File.WriteAllText($"Tanach/tanach.src", string.Empty);

            foreach (var bookName in books)
            {
                Console.Write($"Formatting {bookName}...");

                var text = File.ReadAllText($@"Tanach.acc.txt/{bookName}.acc.txt");
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

                var abbr = abbrevs[book];
                var bookText = string.Join("\n", verses) + "\n";
                File.WriteAllText($"Tanach/{abbr}.src", bookText);
                File.AppendAllText($"Tanach/tanach.src", bookText);
            }

            Console.WriteLine("DONE");
        }
    }
}
