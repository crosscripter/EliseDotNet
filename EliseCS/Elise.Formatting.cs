using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Elise.Formatting
{
	public enum Language { English, Greek, Hebrew }

	public abstract class Formatter
	{
		public static readonly Dictionary<Language, Formatter> Formatters = new Dictionary<Language, Formatter>
		{
			{ Language.Hebrew, new HebrewFormatter() },
			{ Language.Greek, new GreekFormatter() },
			{ Language.English, new LatinFormatter() }
		};

		public string Pattern { get; }

		public Formatter(string pattern)
		{
			Pattern = pattern;
		}

		public static Formatter GetFormatterByLanguage(Language language)
		{
			return Formatters[language];
		}

		public virtual string Format(string text)
		{
			return Regex.Replace(text.ToUpperInvariant(), $@"[^{Pattern}]", string.Empty).Trim();
		}
	}

	public class LatinFormatter : Formatter 
	{ 
		public LatinFormatter() : base(@"A-Z") { } 
	}

	public class HebrewFormatter : Formatter 
	{ 
		string StripSofit(string text)
		{
			text = text.Replace('ך', 'כ');
			text = text.Replace('ם', 'מ');
			text = text.Replace('ן', 'נ');
			text = text.Replace('ף', 'פ');
			return text.Replace('ץ', 'צ');
		}

		public HebrewFormatter() : base(@"א-ת") { } 

		public override string Format(string text) 
		{
			text = base.Format(text);		
			return StripSofit(text);
		}
	}

	public class GreekFormatter : Formatter 
	{ 
		public GreekFormatter() : base (@"Α-Ω") { } 

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
			return Regex.Replace(text, @"[\u1FE4-\u1FE5\u1FEC]", @"Ρ");
		}

		public override string Format(string text)
		{
			text = StripDiacritics(text);
			return base.Format(text);
		}
	}
}
