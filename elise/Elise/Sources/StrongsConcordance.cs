using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Elise.Sources
{
    public class StrongsConcordance
    {
        public string[] Records { get; }

        public StrongsConcordance()
        {
            var text = File.ReadAllText(@"Sources/strongs.txt", new UTF8Encoding(false));
            Records = text.Split(new string[] { "\n" }, StringSplitOptions.None);
        }

        public string[] Entries(string number) => Records.Where(rec => rec.StartsWith($@"""{number}")).ToArray();
        public string Entry(string number) => Entries(number).First();
        public string Greek(int number) => Entry($"G{number}");
        public string Hebrew(int number) => Entry($"H{number}");
        public string[] Greek() => Entries("G");
        public string[] Hebrew() => Entries("H");

        public string[] Fields(string record) => record.Split(new string[] { @""",""" }, StringSplitOptions.None);

        public string Field(string record, int field)
        {
            var fields = Fields(record);
            return field < fields.Length ? fields[field].Trim(new char[] { '\r', '\n', '"', ' ' }) : string.Empty;
        }

        public string[] this[char language] { get { return Entries(language.ToString()); } }
        public string this[string number] { get { return Text(number); } }
        public string this[string number, int field] { get { return Field(Entry(number), field); } }

        public string Number(string number) => Field(Entry(number), 0);
        public string Lemma(string number) => Field(Entry(number), 1);
        public string Transliteration(string number) => Field(Entry(number), 2);
        public string Pronounciation(string number) => Field(Entry(number), 3);
        public string Description(string number) => Field(Entry(number), 4);
        public string PartOfSpeech(string number) => Field(Entry(number), 5);
        public string Language(string number) => Field(Entry(number), 6);

        public string Text(string number)
        {
            return $@"
	{Number(number)}: {Lemma(number)} - {Transliteration(number)} ({Pronounciation(number)})

	{PartOfSpeech(number)} - {Language(number)}
	{Description(number)}

			";
        }
    }
}
