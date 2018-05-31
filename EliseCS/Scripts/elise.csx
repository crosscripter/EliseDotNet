#r "System.Drawing.dll"
#r "System.Windows.Forms.dll"

using System;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

enum Language
{
	English,
	Greek,
	Hebrew
}

struct Hit 
{
	public string Term { get; }
	public int Index { get; }
	public int Start { get; }
	public int Skip { get; }

	public Hit(string term, int index, int start, int skip) 
	{
		Term = term;
		Index = index;
		Start = start;
		Skip = skip;
	}

	public override string ToString()
	{
		return $"Term '{Term}' found at {Index + 1} skipping every {Skip} letter(s)";
	}
}

/* Formatting */

abstract class ELSFormatter
{
	string Pattern { get; }

	public ELSFormatter(string pattern)
	{
		Pattern = pattern;
	}

	public virtual string Format(string text)
	{
		return Regex.Replace(text.ToUpperInvariant(), Pattern, string.Empty).Trim();
	}
}

class LatinELSFormatter : ELSFormatter 
{ 
	public LatinELSFormatter() : base(@"[^A-Z]") { } 
}

class HebrewELSFormatter : ELSFormatter 
{ 
	string StripSofit(string text)
	{
		text = text.Replace('ך', 'כ');
		text = text.Replace('ם', 'מ');
		text = text.Replace('ן', 'נ');
		text = text.Replace('ף', 'פ');
		text = text.Replace('ץ', 'צ');
		return text;
	}

	public HebrewELSFormatter() : base(@"[^א-ת]") { } 

	public override string Format(string text) 
	{
		text = base.Format(text);		
		return StripSofit(text);
	}
}

class GreekELSFormatter : ELSFormatter 
{ 
	public GreekELSFormatter() : base (@"[^Α-Ω]") { } 

	string StripDiacritics(string text)
	{
		// Diacritics
		text = Regex.Replace(text, @"[\u0386\u03AC\u1F00-\u1F0F\u1F70-\u1F71\u1F80-\u1F8F\u1FB0-\u1FBC]", @"Α");
		text = Regex.Replace(text, @"[\u0388\u03AD\u1F10-\u1F1D\u1F72-\u1F73\u1FC8-\u1FC9]", @"Ε");
		text = Regex.Replace(text, @"[\u0389\u03AE\u1F21-\u1F2F\u1F74-\u1F75\u1F90-\u1F9F\u1FC2-\u1FC7\u1FCA-\u1FCC]", @"Η");
		text = Regex.Replace(text, @"[\u038A\u0390\u03AA\u03AF\u03CA\u1F30-\u1F3F\u1F76-\u1F77\u1FD0-\u1FDB]", @"Ι");
		text = Regex.Replace(text, @"[\u038C\u03CC\u1F40-\u1F4D\u1F78-\u1F79]", @"Ο");
		text = Regex.Replace(text, @"[\u038E\u03AB\u03B0\u03CB\03CD\u1F51-\u1F5F\u1F7A-\u1F7B\u1FE0-\u1FE3\u1FE6-\u1FEB]", @"Υ");
		text = Regex.Replace(text, @"[\u038F\u03CE\u1F60-\u1F6F\u1F7C-\u1F7D\u1FA0-\u1FAF\u1FF2-\u1FFC]", @"Ω");
		// Sigma and Rho
		text = Regex.Replace(text, @"[\u03C2]", @"Σ");
		text = Regex.Replace(text, @"[\u1FE4-\u1FE5\u1FEC]", @"Ρ");
		return text;
	}

	public override string Format(string text)
	{
		text = StripDiacritics(text);
		return base.Format(text);
	}
}

/* Sequencing */

class Sequencer
{	
	public readonly UTF8Encoding Encoding = new UTF8Encoding(false);

	public string Path { get; }	
	public string Text { get; private set; }
	public int Start { get; }
	public int Stop { get; }
	public int FromSkip { get; }
	public int ToSkip { get; }
	public int Proximity { get; }
	public ELSFormatter Formatter { get; }

	public List<string> Terms { get; private set; }
	public List<Hit> Hits { get; private set; }
	public string Grid { get; private set; }
	public int Limit { get; private set; }

	public Sequencer(ELSFormatter formatter, string path, int start=0, int stop=-1, int fromSkip=2, int toSkip=-1, int proximity=-1)
	{
		Path = path;
		FromSkip = fromSkip;
		Hits = new List<Hit>();
		Text = File.ReadAllText(Path, Encoding);
		Formatter = formatter;
		Text = Formatter.Format(Text);
		Stop = stop == -1 || stop >= Text.Length ? Text.Length - 1 : stop;
		Start = start <= 0 || start > Stop ? 0 : start;
		Text = Range(Text, Start, Stop);
		ToSkip = toSkip == -1 ? Text.Length - 1 : toSkip;
		Proximity = proximity == -1 ? -1 : proximity;
	}

	string Reverse(string text) => new String(text.Reverse().ToArray());

	string Range(string text, int start, int stop) 
	{
		var length = (stop == -1 || stop >= text.Length ? text.Length - 1 : stop) - start + 1;
		return text.Substring(start, length);
	}

	string Sequence(string text, int skip)
	{
		var length = text.Length;
		var capacity = (length / skip) + 1;
		var sequence = new StringBuilder(capacity);

		for (var i = 0; i < length; i += skip) 
		{
			sequence.Append(text[i]);
		}

		var result = sequence.ToString();
		sequence.Clear();
		return result;
		// return new String(text.Where((_, i) => i % skip == 0).ToArray());
	}

	int[] IndicesOf(string text, string term)
	{
		var indices = new List<int>();
		int index = 0;
		index = text.IndexOf(term, index);

		do 
		{
			if (index != -1) indices.Add(index);
			if (index + 1 >= text.Length) break;
			index = text.IndexOf(term, index + 1);
		} while (index > -1);

		return indices.ToArray();
	}

	IEnumerable<Hit> GetProximalHits(int proximity)
	{
		Hits = Hits.OrderByDescending(h => h.Index).ThenByDescending(h => h.Skip).ToList();
		var proximalHits = new List<Hit>();

		for (var i = 0; i < Hits.Count(); i++)
		{
			var current = Hits[i];
			var j = i + 1 < Hits.Count() ? i + 1 : i;
			var next = Hits[j];
			var currentIndex = current.Index;
			var nextIndex = next.Index;
			var currentSkip = current.Skip;
			var nextSkip = next.Skip;

			if (Math.Abs(currentSkip - nextSkip) <= proximity || Math.Abs(currentIndex - nextIndex) <= proximity)
			{
				if (!proximalHits.Contains(current)) proximalHits.Add(current);
				if (!proximalHits.Contains(next)) proximalHits.Add(next);
			}
		}

		Hits = Hits.OrderBy(h => h.Index).ThenBy(h => h.Skip).ToList();
		return proximalHits;
	}

	string OffsetHitsToGrid(List<Hit> hits)
	{
		Hits = Hits.OrderBy(h => h.Index).ThenBy(h => h.Skip).ToList();
		var low = Hits.FirstOrDefault().Index;

		var last = Hits.LastOrDefault();
		var high = last.Index;
		high += (last.Term?.Length ?? 1) * last.Skip;

		Hits = Hits.Select(h => new Hit(h.Term, (h.Index - low), h.Start, h.Skip)).ToList();
		low = low - Start >= 0 ? low - Start : low;
		high = high >= Text.Length ? Text.Length - 1 : high;
		return Range(Text, low, high);		
	}

	public IEnumerable<Hit> Search(params string[] terms)
	{
		var locker = new Object();
		var backspaces = new String('\b', 8 * 8 + 1) + "Sequencing...";

		Terms = terms.Select(t => Formatter.Format(t)).ToList();
		var revList = Terms.Select(t => Reverse(t)).ToArray();
		Terms.AddRange(revList);		
		Limit = Terms.OrderByDescending(t => t.Length).FirstOrDefault().Length;
		var hitCache = new List<int>();

		// Parallel.For(Start, Stop, pos =>
		for (var pos = Start; pos < Stop; pos++)
		{
			Console.Write(backspaces + $"{pos + 1}/{Stop} ");
			var length = Stop - pos;

			// Parallel.For(FromSkip, ToSkip + 1, skip => 
			for (var skip = FromSkip; skip <= ToSkip; skip++)
			{
				var text = Range(Text, pos, Stop);
				var size = (int)Math.Ceiling((double)(length / skip));
		        if (Limit > 0 && size < Limit) break;
				var sequence = Sequence(text, skip);

				// Parallel.ForEach(Terms, term =>
				foreach (var term in Terms)
				{
					var indices = IndicesOf(sequence, term);
					if (indices.Length == 0) continue;

					foreach (var index in indices) 
					{
						var hindex = ((index * skip) + pos);

						if (!hitCache.Contains(hindex))
						{
							hitCache.Add(hindex);
							Hits.Add(new Hit(term, hindex, pos, skip));
						}
					}
				}//);
			} //);
		} //);

		hitCache.Clear();

		if (Proximity != -1)
		{
			Hits = GetProximalHits(Proximity).ToList();
		}

		Grid = OffsetHitsToGrid(Hits);
		return Hits;
	}

	public override string ToString()
	{
		return $@"Searching text of file '{Path}'
starting at position {Start + 1} until position {Stop + 1}
skipping every {FromSkip} to {ToSkip} letter(s) 
searching for {string.Join(",", Terms ?? new List<string>())}
within a proximity of {Proximity} letter(s).

{Hits.Count()} total hit(s) found:

{string.Join("\n", Hits ?? new List<Hit>())}

";
	}
}

/* Rendering */

abstract class GridRenderer
{
	public readonly UTF8Encoding Encoding = new UTF8Encoding(false);
	protected List<string> Colors { get; private set; } = new List<string>();
	protected Dictionary<string, int> TermColors { get; private set; } = new Dictionary<string, int>();
	Dictionary<string, List<int>> HitIndices { get; } = new Dictionary<string, List<int>>();
	Random Random { get; } = new Random();

	public string Text { get; }
	public StringBuilder Grid { get; }
	public List<string> Terms { get; }
	public List<Hit> Hits { get; }

	public GridRenderer(string text, params Hit[] hits)
	{
		Hits = new List<Hit>(hits);
		Text = text;
		Grid = new StringBuilder(Text.Length);
		Terms = Hits.Select(h => h.Term).Distinct().ToList();
		HitIndices = GetHitIndices(Hits);
		BuildColorTable(Terms);
	}

	string RandomColor()
	{	
		int bits = 200;
		int R = Random.Next(bits);
		int G = Random.Next(bits);
		int B = Random.Next(bits);		
		var color = $"\\red{R}\\green{G}\\blue{B}";
		if (Colors.Contains(color)) return RandomColor();
		return color;
	}

	protected Dictionary<string, int> BuildColorTable(List<string> terms)
	{
		Colors = new List<string>();
		TermColors = new Dictionary<string, int>();

		foreach (var term in terms)
		{
			Colors.Add(RandomColor());
			TermColors.Add(term, Colors.Count + 1);
		}

		return TermColors;
	}

	Dictionary<string, List<int>> GetHitIndices(List<Hit> hits)
	{
		var hitIndices = new Dictionary<string, List<int>>();

		foreach (var hit in hits)
		{	
			if (!hitIndices.ContainsKey(hit.Term))
			{
				hitIndices[hit.Term] = new List<int>();
			}

			for (var i = hit.Index; i < hit.Index + (hit.Term.Length * hit.Skip); i += hit.Skip)
			{
				hitIndices[hit.Term].Add(i);
			}
		}

		return hitIndices;
	}

	protected bool IsTermIndex(int index, out string term)
	{
		term = null;

		foreach (var hitIndex in HitIndices)
		{
			var indices = hitIndex.Value;

			if (indices.Contains(index)) {
				term = hitIndex.Key;
				return true;
			}
		}

		return false;
	}

	public abstract string Render();
}

class RTFGridRenderer : GridRenderer
{
	public new string DefaultColor = @"\red204\green204\blue204";
	protected Dictionary<char, string> EncodingTable;

	public RTFGridRenderer(string text, params Hit[] hits) : base(text, hits) 
	{
		BuildColorTable(Terms);
		Colors.Insert(0, DefaultColor);
	}

	protected virtual string WriteHeader(string colorTable)
	{
		return @"{\rtf1\ansi\ansicpg1252\deff0\nouicompat\deflang1033{\fonttbl{\f0\fnil\fcharset0 Consolas;}}
{\colortbl ;" + colorTable + @";}
{\*\generator Riched20 10.0.14393}\viewkind4\uc1 
\pard\sl240\slmult1\cf1\f0\fs16\lang9";
	}

	protected virtual string WriteFooter() { return "\\par\n}\n\u0000"; }

	public override string Render()
	{
		var colorTable = string.Join(";", Colors);
		Grid.Append(WriteHeader(colorTable)); 
		bool encoded = EncodingTable != null;

		for (var i = 0; i < Text.Length; i++)
		{
			string term = null;
			var letter = string.Empty;
			var chr = string.Empty;

			if (IsTermIndex(i, out term))
			{
				var cindex = TermColors[term];
				chr = encoded ? $@"\'{EncodingTable[Text[i]]}" : Text[i].ToString();
				letter += $"\\cf{cindex}\\b {chr} \\cf1\\b0";
			}
			else
			{
				term = null;
				chr = encoded ? $@"\'{EncodingTable[Text[i]]}" : Text[i].ToString();
				letter += chr + " ";
			}

			Grid.Append(letter);
		}

		Grid.Append(WriteFooter());
		return Grid.ToString();
	}
}

class HebrewRTFGridRenderer : RTFGridRenderer
{
	public HebrewRTFGridRenderer(string text, params Hit[] hits) : base(text, hits) 
	{ 
		base.EncodingTable = new Dictionary<char, string>()
		{
			{'א', "e0"},
			{'ב', "e1"},
			{'ג', "e2"},
			{'ד', "e3"},
			{'ה', "e4"},
			{'ו', "e5"},
			{'ז', "e6"},
			{'ח', "e7"},
			{'ט', "e8"},
			{'י', "e9"},
			{'כ', "eb"},
			{'ל', "ec"},
			{'מ', "ee"},
			{'נ', "f0"},
			{'ס', "f1"},
			{'ע', "f2"},
			{'פ', "f4"},
			{'צ', "f6"},
			{'ק', "f7"},
			{'ר', "f8"},
			{'ש', "f9"},
			{'ת', "fa"}
		};
	}

	protected override string WriteHeader(string colorTable)
	{
		return @"{\rtf1\fbidis\ansi\ansicpg1252\deff0\nouicompat\deflang1033{\fonttbl{\f0\fnil\fcharset177 Courier New;}{\f1\fnil\fcharset0 Consolas;}}
{\colortbl ;" + colorTable + @";}
{\*\generator Riched20 10.0.14393}\viewkind4\uc1 
\pard\ltrpar\sl240\slmult1\f0\rtlch\fs22\lang1037";
	}

	protected override string WriteFooter() { return "\\f1\\ltrch\\lang1033  \\lang9\\par\n}\n\u0000"; }
}


class GreekRTFGridRenderer : RTFGridRenderer
{
	public GreekRTFGridRenderer(string text, params Hit[] hits) : base(text, hits) 
	{ 
		base.EncodingTable = new Dictionary<char, string>()
		{
			{'Α', "c1"},
			{'Β', "c2"},
			{'Γ', "c3"},
			{'Δ', "c4"},
			{'Ε', "c5"},
			{'Ζ', "c6"},
			{'Η', "c7"},
			{'Θ', "c8"},
			{'Ι', "c9"},
			{'Κ', "ca"},
			{'Λ', "cb"},
			{'Μ', "cc"},
			{'Ν', "cd"},
			{'Ξ', "ce"},
			{'Ο', "cf"},
			{'Π', "d0"},
			{'Ρ', "d1"},
			{'Σ', "d3"},
			{'Τ', "d4"},
			{'Υ', "d5"},
			{'Φ', "d6"},
			{'Χ', "d7"},
			{'Ψ', "d8"},
			{'Ω', "d9"}		
		};
	}

	protected override string WriteHeader(string colorTable)
	{
		return @"{\rtf1\ansi\ansicpg1252\deff0\nouicompat\deflang1033{\fonttbl{\f0\fnil\fcharset161 Consolas;}{\f1\fnil\fcharset0 Consolas;}}
{\colortbl ;" + colorTable + @";}
{\*\generator Riched20 10.0.14393}\viewkind4\uc1 
\pard\sl240\slmult1\f0\fs20\lang1032";
	}

	protected override string WriteFooter() { return "\\f1\\lang9\\par\n}\n\u0000"; }
}

/* Main Program */

class Program
{
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

	Example:	elise ""Genesis.txt"" 0 1000 2 10 50 ""God,Adam,Eve""

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

		Func<string,bool> NotNumeric = (arg) => Error($"Argument '{arg}' was not numeric");
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
		{Language.English, new { Formatter = typeof(LatinELSFormatter), Renderer = typeof(RTFGridRenderer) }},
		{Language.Greek, new { Formatter = typeof(GreekELSFormatter), Renderer = typeof(GreekRTFGridRenderer) }},
		{Language.Hebrew, new { Formatter = typeof(HebrewELSFormatter), Renderer = typeof(HebrewRTFGridRenderer) }}
	};
	
	public static void Main(string[] args)
	{
		Console.Write("Parsing args...");
		if (!ParseArgs(args)) return;
		Console.WriteLine("DONE");

		Console.Write("Sequencing...");
		ELSFormatter formatter = (ELSFormatter)Activator.CreateInstance(Settings[Language].Formatter);
		var sequencer = new Sequencer(formatter, Path, Start, Stop, FromSkip, ToSkip, Proximity);		
		var seqTimer = Stopwatch.StartNew();
		var hits = sequencer.Search(Terms.ToArray());
		seqTimer.Stop();
		Console.WriteLine($"DONE ({seqTimer.ElapsedMilliseconds} ms)");
		
		Console.WriteLine(sequencer);
		if (hits.Count() == 0) return;

		Console.Write("Rendering...");
		Type TRenderer = Settings[Language].Renderer;

		RTFGridRenderer renderer = (RTFGridRenderer)Activator.CreateInstance(TRenderer, new object[] 
		{ 
			sequencer.Grid, hits.ToArray() 
		});

		var rendTimer = Stopwatch.StartNew();
		var grid = renderer.Render();
		rendTimer.Stop();

		Console.WriteLine($"DONE ({rendTimer.ElapsedMilliseconds} ms)");

		Console.Write("Writing to file...");
		File.WriteAllText(@"../Docs/ELS/out.rtf", grid, sequencer.Encoding);
		Console.WriteLine("DONE");
		Process.Start("WORDPAD.EXE", @"../Docs/ELS/out.rtf");
	}
}

class EliseUI : Form
{
	Panel Panel(DockStyle dock) => new Panel
	{
		Dock = dock,
		BackColor = SystemColors.Control
	};

	GroupBox GroupBox(string text) => new GroupBox
	{
		Dock = DockStyle.Fill,
		AutoSize = false,
		Text = text
	};

	SplitContainer SplitPanel(Orientation orientation) => new SplitContainer
	{
		Dock = DockStyle.Fill,
		Orientation = orientation,
		BackColor = Color.WhiteSmoke
	};
	
	Label Label(string text) => new Label
	{
		Dock = DockStyle.Left,
		AutoSize = true,
		TextAlign = ContentAlignment.MiddleLeft,
		Text = text + ":"
	};

	ComboBox List(params string[] items) => new ComboBox
	{
		Dock = DockStyle.Left,
		AutoSize = false,
		Width = 140,
		DataSource = items,
		DropDownStyle = ComboBoxStyle.DropDownList		
	};

	NumericUpDown Spinner(int min, int max) => new NumericUpDown
	{
		Dock = DockStyle.Left,
		Minimum = min,
		Maximum = max,
		Value = min,
		AutoSize = true,
	};

	Control[] DockAll(DockStyle dock, params Control[] controls)
	{
		Array.ForEach(controls, c => c.Dock = dock);
		return controls;
	}

	TContainer Container<TContainer>(DockStyle dock, params Control[] controls) where TContainer : Control
	{
		var container = Activator.CreateInstance(typeof(TContainer)) as TContainer;
		container.Dock = dock;
		container.AutoSize = false;
		controls = controls.Reverse().ToArray();
		container.Controls.AddRange(controls);
		return container;
	}

	Panel ListPanel(string text, ComboBox list) 
	{
		var container = Container<Panel>(DockStyle.Left, Label(text), list);
		container.AutoSize = false;
		container.Height = 50;
		container.Width = 80;
		container.Padding = new Padding(0);
		container.Margin = new Padding(0);
		return container;
	}

	Panel ListPanel(string text, params string[] items)
	{
		return ListPanel(text, List(items));
	}

	Panel SpinPanel(string text, NumericUpDown spinner)
	{
	 	var container = Container<Panel>(DockStyle.Left, Label(text), spinner);
		container.AutoSize = false;
		container.Width = 90;
		container.Padding = new Padding(0);
		container.Margin = new Padding(0);
	 	return container;
	}

	Panel SpinPanel(string text, int min, int max)
	{
		return SpinPanel(text, Spinner(min, max));
 	}
	
	SplitContainer Splitter(Control left, Control right, Orientation orientation = Orientation.Vertical) 
	{
		var container = SplitPanel(orientation);
		container.Panel1.Controls.Add(left);
		container.Panel2.Controls.Add(right);
		return container;
	}

	RichTextBox RTFGrid() => new RichTextBox
	{
		Dock = DockStyle.Fill,
		BorderStyle = BorderStyle.None,
		ReadOnly = true,
		WordWrap = true,
		TabStop = false,
		ScrollBars = RichTextBoxScrollBars.ForcedBoth,
		BackColor = Color.White,
		ForeColor = Color.LightGray,
		Font = new Font("Courier New", 8),
	};

	Tuple<string, EventHandler> Command(string item, EventHandler command)
	{
		return new Tuple<string, EventHandler>(item, command);
	}

	ToolStripMenuItem SubMenu(string name, params Tuple<string, EventHandler>[] items)
	{
		var subMenu = new ToolStripMenuItem(name);
		subMenu.DropDownItems.AddRange(items.Select(i => new ToolStripMenuItem(i.Item1, null, i.Item2)).ToArray());
		return subMenu;
	}

	MenuStrip Menu(params ToolStripMenuItem[] menus)
	{
		var menu = new MenuStrip();
		menu.Dock = DockStyle.Top;
		menu.Items.AddRange(menus);		
		return menu;
	}

	BackgroundWorker Worker<T,U>(Func<T,U> work, Action<U> completed)
	{
		var worker = new BackgroundWorker();
		worker.DoWork += (_, e) => { e.Result = (U)work((T)e.Argument); };
		worker.RunWorkerCompleted += (_, e) => { completed((U)e.Result); };
		return worker;
	}

	public StatusBar StatusBar { get; }
	public RichTextBox Grid { get; private set; }
	public UTF8Encoding Encoding = new UTF8Encoding(false);
	ComboBox SourceList;
	NumericUpDown StartSpinner, StopSpinner, FromSkip, ToSkip;
	ListBox TermListBox;

	public string GridText 
	{ 
		get { return Grid.Text;  }
		set { if (Grid != null) Grid.Text = value; }
	}

	public string Source 
	{ 
		get { return SourceList.Text; } 
	}

	public string[] Terms
	{
		get 
		{
			var terms = new List<string>();

			foreach (var term in TermListBox.Items)
			{
				terms.Add(term.ToString());
			}

			return terms.ToArray();
		}
	}

	public int Start { get { return (int)StartSpinner.Value; } }
	public int Stop { get { return (int)StopSpinner.Value; } }
	public int From { get { return (int)FromSkip.Value; } }
	public int To { get { return (int)ToSkip.Value; } } 
	
	ListBox OutputListBox;
	ProgressBar ProgressBar;
	BackgroundWorker GridLoader;
	BackgroundWorker Searcher;

	public EliseUI()
	{
		Text = "ELISE :: Equidistant Letter Interval Sequencing Engine";
		Size = new Size(800, 540);
		ShowIcon = false;
		SuspendLayout();

		var menu = Menu(
			SubMenu("&File", Command("E&xit", Exit)), 
			SubMenu("&Help", Command("&About", About))
		);

		SourceList = List("Genesis", "John", "ABCs");
		var SourceListPanel = ListPanel("&Source", SourceList);
		const int GroupBoxHeight = 50;

		StartSpinner = Spinner(0, 1000);
		StartSpinner.Value = 0;

		var StartSpinPanel = SpinPanel("&Start", StartSpinner);

		StopSpinner = Spinner(0, 1000);
		StopSpinner.Value = 1000;

		var StopSpinPanel = SpinPanel("&Stop", StopSpinner);
		var RangeOptions = Container<GroupBox>(DockStyle.Top, StartSpinPanel, StopSpinPanel);
		RangeOptions.Text = "Range &Options";
		RangeOptions.Height = GroupBoxHeight;

		FromSkip = Spinner(2, 1000);
		FromSkip.Value = 2;

		var FromSkipPanel = SpinPanel("&From", FromSkip);
		ToSkip = Spinner(2, 1000);
		ToSkip.Value = 20;

		var ToSkipPanel = SpinPanel("&To", ToSkip);
		var SkipOptions = Container<GroupBox>(DockStyle.Top, FromSkipPanel, ToSkipPanel);
		SkipOptions.Text = "Skip &Interval";
		SkipOptions.Height = GroupBoxHeight;

		var TermLabel = Label("&Term");
		var TermTextBox = new TextBox { Dock = DockStyle.Top, AutoSize = false, Width = 80 };
		var AddTermButton = new Button { Dock = DockStyle.Left, Text = "Add" };
		var RemoveTermButton = new Button { Dock = DockStyle.Left, Text = "Remove" };
		var TermButtons = Container<Panel>(DockStyle.Top, AddTermButton, RemoveTermButton);
		TermButtons.Height = 20;

		var TermPanel = Container<Panel>(DockStyle.Top, TermLabel, TermTextBox, TermButtons);
		TermPanel.AutoSize = true;

		var SearchButton = new Button { Dock = DockStyle.Top, Text = "&Search" };
		TermListBox = new ListBox { Dock = DockStyle.Top };
		TermListBox.Items.AddRange(new [] { "GOD", "MAN", "SIN" });

		var TermOptions = Container<GroupBox>(DockStyle.Top, TermPanel, TermListBox, SearchButton);
		TermOptions.Text = "Search &Terms";
		TermOptions.AutoSize = true;

		var SearchOptions = Container<GroupBox>(DockStyle.Fill, 
			DockAll(DockStyle.Top, SourceListPanel, RangeOptions, SkipOptions, TermOptions)
		);

		SearchOptions.Text = "&Search Options";

		var LeftPanel = Container<Panel>(DockStyle.Fill, SearchOptions);
		Grid = RTFGrid();

		ProgressBar ProgressBar = new ProgressBar 
		{
			Dock = DockStyle.Right,
			Style = ProgressBarStyle.Marquee
		};

		StatusBar = Container<StatusBar>(DockStyle.Bottom, ProgressBar);
		StatusBar.Font = new Font("MS Sans Serif", 8);
		var VSplitter = Splitter(LeftPanel, Grid);
		var OutputLabel = Label("&Output");
		OutputLabel.Dock = DockStyle.Top;
		OutputLabel.AutoSize = false;
		OutputLabel.Font = new Font("MS Sans Serif", 8);
		OutputLabel.BackColor = SystemColors.ControlLight;

		OutputListBox = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None };		
		var OutputConsole = Container<Panel>(DockStyle.Fill, OutputLabel, OutputListBox);
		var HSplitter = Splitter(VSplitter, OutputConsole, Orientation.Horizontal);

		Controls.Add(Container<Panel>(DockStyle.Fill, menu, HSplitter, StatusBar));
		ResumeLayout();

		VSplitter.FixedPanel = FixedPanel.Panel1;
		var splitterDistance = 200;
		VSplitter.Panel1MinSize = splitterDistance;
		VSplitter.SplitterDistance = splitterDistance;
		VSplitter.Update();
		
		HSplitter.FixedPanel = FixedPanel.Panel2;
		HSplitter.Panel2MinSize = 100;
		HSplitter.SplitterDistance = 400;
		HSplitter.Update();
		
		// Workers
		GridLoader = Worker<string,string>(path => 
		{
			StatusBar.Text = "Loading grid, please wait...";
			var text = File.ReadAllText(path, Encoding);
			return string.Join(" ", text.ToCharArray());
		}, 
		result => 
		{
			GridText = result;
			StatusBar.Text = "Grid loaded";
			ProgressBar.Style = ProgressBarStyle.Continuous;
		});

		// ELISE
		Searcher = Worker<string,int>(args =>
		{
			StatusBar.Text = $"Searching {Source} starting from position {Start} to {Stop} skipping every {From} to {To} letter(s) looking for {string.Join(",", Terms)}...";
			ProgressBar.Style = ProgressBarStyle.Marquee;

			// Invoke a new ELISE engine process
			var elise = new Process
		    {
		        StartInfo = new ProcessStartInfo
		        {
		            FileName = "elise.exe",
		            // Arguments = args,
		            UseShellExecute = false,
		            RedirectStandardOutput = true,
		            RedirectStandardError = true,
		        }
		    };
		 
		 	// Redirect output to our UI output console
			elise.OutputDataReceived += (sender, e) => Print(e.Data);
			elise.ErrorDataReceived += (sender, e) => Print(e.Data);		 
			elise.Start();
			UseWaitCursor = true;

			elise.BeginOutputReadLine();
			elise.BeginErrorReadLine();		 
			elise.WaitForExit();
			return 0;
		},
		hits =>
		{
			ProgressBar.Style = ProgressBarStyle.Continuous;
			StatusBar.Text = $"Search completed, {hits} total hit(s) found";
			UseWaitCursor = false;
		});

		AddTermButton.Click += (sender, e) => 
		{
			if (TermTextBox.TextLength <= 0) return;
			var term = TermTextBox.Text.Trim().ToUpper();

			if (!TermListBox.Items.Contains(term)) 
			{
				TermListBox.Items.Add(term);
				TermTextBox.Clear();
			}
		};

		RemoveTermButton.Click += (sender, e) =>
		{
			if (TermListBox.SelectedIndex == -1) return;
			var term = TermListBox.Items[TermListBox.SelectedIndex];
			
			if (TermListBox.Items.Contains(term))
			{
				TermListBox.Items.Remove(term);
			}
		};

		SearchButton.Click += (sender, e) =>
		{
			StatusBar.Text = $"Searching {Source} starting from position {Start} to {Stop} skipping every {From} to {To} letter(s) looking for {string.Join(",", Terms)}...";
			ProgressBar.Style = ProgressBarStyle.Marquee;
			UseWaitCursor = true;

			var formatter = new LatinELSFormatter();
			var sequencer = new Sequencer(formatter, @"../Docs/ELS/GEN-ELS.txt", Start, Stop, From, To, -1);
			var hits = sequencer.Search(Terms);
			var renderer = new RTFGridRenderer(sequencer.Grid, hits.ToArray());
			var grid = renderer.Render();
			Grid.Rtf = grid;

			UseWaitCursor = false;
			ProgressBar.Style = ProgressBarStyle.Continuous;
			StatusBar.Text = $"Search completed, {hits.Count()} hit(s) found";
		};

		Shown += (s, e) => GridLoader.RunWorkerAsync(@"../Docs/ELS/KJV-ELS.txt");
	}
	
	SynchronizationContext Context = SynchronizationContext.Current;
	
	bool Confirm(string question, string title, DialogResult expected=DialogResult.Yes)
	{
		return expected == MessageBox.Show(question, $"Elise - {title}", 
			MessageBoxButtons.YesNo, MessageBoxIcon.Question);
	}

	void Print(string output) 
	{
	    Context.Post(_ => OutputListBox.Items.Add(output), null);
	}

	void About(object sender, EventArgs e)
	{
		MessageBox.Show(@"
Elise :: Equidistant Letter Interval Sequencing Engine 
Version 1.0.0

Copyright (C) Michael Schutt 2016
All Rights Reserved.", "About Elise",
	MessageBoxButtons.OK, MessageBoxIcon.Information);
	}

	void Exit(object sender, EventArgs e)
	{
		if (Confirm("Are you sure you want to exit?", "Confirm Exit"))
		{
			Application.Exit();		
		}
	}
}

class UIProgram
{
	[STAThread]
	public static void Main()
	{
		Application.EnableVisualStyles();
		Application.Run(new EliseUI());
	}
}

UIProgram.Main();
