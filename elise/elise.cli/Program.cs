using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

using Elise.Formatting;
using Elise.Sequencing;
using Elise.Rendering;
using Elise.Sources;

class Program
{
    static string Path;
    static int Start;
    static int Stop;
    static int FromSkip;
    static int ToSkip;
    static int Proximity;
    static List<string> Terms;
    static Language Language;

    static Dictionary<Language, dynamic> Settings = new Dictionary<Language, dynamic>()
    {
        {Language.English, new { Formatter = typeof(LatinFormatter), Renderer = typeof(RichTextRenderer) }},
        {Language.Greek, new { Formatter = typeof(GreekFormatter), Renderer = typeof(GreekRichTextRenderer) }},
        {Language.Hebrew, new { Formatter = typeof(HebrewFormatter), Renderer = typeof(HebrewRichTextRenderer) }}
    };

    static bool Usage()
    {
        Console.WriteLine(@"Usage: 

elise.exe <path> [<start> <stop> <fromSkip> <toSkip> <proximity> <terms..>]

	path:	   Path to the source input text file. (Required)
	start:	   Starting index of the search range. (Defaults to 0)
	stop:	   Ending index of the search range. (Defaults to -1, the end of the text)
	fromSkip:  Starting skip interval. (Defaults to 2, minimal skip interval)
	toSkip:    Ending skip interval. (Defaults to maximal skip interval of text length)
	proximity: Proximity of text in characters to keep as valid hits.  (Defaults to -1, or text length)
	terms:	   A comma separated list of search terms to search for.  (Required)

	Example:	elise ""Genesis.txt"" 0 -1 2 10 50 ""God,Adam,Eve""

	This would search the text of Genesis from beginning to end skipping
	every 2 to 10 letters looking for the terms God, Adam or Eve within
	50 characters of each other.
        ");

        return false;
    }

    static bool ParseArgs(string[] args)
    {
        Func<string, bool> Error = (message) =>
        {
            Console.WriteLine(message);
            return Usage();
        };

        if (args.Length != 7) return Error("Not enough arguments");

        Path = args[0];
        if (!File.Exists(Path)) return Error($"File '{Path}' does not exist!");

        Language = Path.Contains("Tanach") ? Language.Hebrew
                 : Path.Contains("SBLGNT") ? Language.Greek
                 : Language.English;

        Func<string, bool> NotNumeric = (arg) => Error($"Argument '{arg}' was not numeric");
        if (!int.TryParse(args[1], out Start)) return NotNumeric(args[1]);
        if (!int.TryParse(args[2], out Stop)) return NotNumeric(args[2]);
        if (!int.TryParse(args[3], out FromSkip)) return NotNumeric(args[3]);
        if (!int.TryParse(args[4], out ToSkip)) return NotNumeric(args[4]);
        if (!int.TryParse(args[5], out Proximity)) return NotNumeric(args[5]);

        var termList = args[6];
        var terms = termList.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
        Terms = new List<string>(terms);
        if (Terms.Count == 0) return Error("At least one term is required for searching.");

        return true;
    }

    public static void Main(string[] args)
    {
        Console.Write("Parsing args...");
        if (!ParseArgs(args)) return;
        Console.WriteLine("DONE");

        Console.Write("Sequencing...");
        Formatter formatter = (Formatter)Activator.CreateInstance(Settings[Language].Formatter);
        var sequencer = new Sequencer(formatter, Path, Start, Stop, FromSkip, ToSkip, Proximity);
        var seqTimer = Stopwatch.StartNew();
        var hits = sequencer.Search(Terms.ToArray());
        seqTimer.Stop();
        Console.WriteLine($"DONE ({seqTimer.ElapsedMilliseconds} ms)");

        Console.WriteLine(sequencer);
        if (hits.Count() == 0) return;

        Console.Write("Rendering...");
        Type TRenderer = Settings[Language].Renderer;

        RichTextRenderer renderer = (RichTextRenderer)Activator.CreateInstance(TRenderer, new object[] { sequencer.Grid, hits.ToArray() });

        var rendTimer = Stopwatch.StartNew();
        var grid = renderer.Render();
        rendTimer.Stop();

        Console.WriteLine($"DONE ({rendTimer.ElapsedMilliseconds} ms)");

        Console.Write("Writing to file...");
        File.WriteAllText(@"out.rtf", grid, sequencer.Encoding);
        Console.WriteLine("DONE");
        Process.Start("WORDPAD.EXE", @"out.rtf");
    }
}
