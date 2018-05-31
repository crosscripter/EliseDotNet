using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Elise.Rendering
{
	using Elise.Formatting;
	using Elise.Sequencing;

	public abstract class GridRenderer
	{
		static Dictionary<Language, Type> Renderers = new Dictionary<Language, Type>
		{
			{ Language.Hebrew, typeof(HebrewRTFGridRenderer) },
			{ Language.Greek, typeof(GreekRTFGridRenderer) },
			{ Language.English, typeof(RTFGridRenderer) }
		};

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

		public static GridRenderer GetRendererByLanguage(Language language, params object[] args)
		{
			Type TRenderer = GridRenderer.Renderers[language];
			return Activator.CreateInstance(TRenderer, args) as GridRenderer;
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

	public class RTFGridRenderer : GridRenderer
	{
		public string DefaultColor = @"\red204\green204\blue204";
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

		protected virtual string WriteFooter() { return "\\cf1\\par\n}\n\u0000"; }

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

	public class HebrewRTFGridRenderer : RTFGridRenderer
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
			return @"{\rtf1\fbidis\ansi\ansicpg1252\deff0\nouicompat\deflang1033{\fonttbl{\f0\fnil\fcharset177 Courier New;}{\f1\fnil\fcharset0 Courier New;}}
{\colortbl ;" + colorTable + @";}
{\*\generator Riched20 10.0.14393}\viewkind4\uc1 
\pard\ltrpar\sl240\slmult1\f0\rtlch\fs16\lang1037";
		}

		protected override string WriteFooter() { return "\\cf1\\f1\\ltrch\\lang1033  \\lang9\\par\n}\n\u0000"; }
	}

	public class GreekRTFGridRenderer : RTFGridRenderer
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
			return @"{\rtf1\ansi\ansicpg1252\deff0\nouicompat\deflang1033{\fonttbl{\f0\fnil\fcharset161 Courier New;}{\f1\fnil\fcharset0 Courier New;}}
{\colortbl ;" + colorTable + @";}
{\*\generator Riched20 10.0.14393}\viewkind4\uc1 
\pard\sl240\slmult1\f0\fs16\lang1032";
		}

		protected override string WriteFooter() { return "\\cf1\\f1\\lang9\\par\n}\n\u0000"; }
	}
}
