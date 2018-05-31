#r "elise.dll"
using Elise;

enum NTSources
{
	TR,
	BYZ
}

class GreekNT : Bible
{
	public GreekNT(NTSources source=NTSources.TR)
	{
		var text = ResourceManager.LoadResource($"Elise.Sources.{source}");
		Records = text.Split(new string[] { "\n" }, StringSplitOptions.None);
		Books = "MT,MR,LU,JOH,AC,RO,1CO,2CO,GA,EPH,PHP,COL,1TH,2TH,1TI,2TI,TIT,PHM,HEB,JAS,1PE,2PE,1JO,2JO,3JO,JUDE,RE".ToUpper().Split(',');
		
		Aliases = new Dictionary<string,string>
		{
			{ "NT", this["MT", "RE"]},
			{ "GOSPELS", this["Mt", "Joh"] },
			{ "SYNOPTIC", this["Mt", "Lu"] },
			{ "EPISTLES", this["Ro", "Jude"] },
			{ "PAULINE", this["Ro", "Phm"] },
			{ "GENERAL", this["Heb", "Jude"] }
		};
	}
}

void Log(string output)
{
	File.WriteAllText("out.log", output, new UTF8Encoding(false));
	Process.Start("out.log");
}

var nt = new GreekNT();
Log(nt["Mt"]);
