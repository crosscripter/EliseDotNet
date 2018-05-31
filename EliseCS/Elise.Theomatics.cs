using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Elise.Theomatics
{
	using Elise.Formatting;
	using Elise.Gematria;

	public struct TheomaticMultiple
	{
		public int Multiple { get; }
		public int Factor { get; }
		public int Sum { get; }
		public int Start { get; }
		public int Stop { get; }
		public string Text { get; }
		public int Length { get; }

		public TheomaticMultiple(int multiple, int sum, int start, int stop, string text)
		{
			Multiple = multiple;
			Sum = sum;
			Factor = Sum / Multiple;
			Start = start;
			Stop = stop;
			Text = text;
			Length = Text.Length;
		}

		public override string ToString() 
		{
			return $@"{Start}-{Stop}({Length})""{Text}""={Sum}({Multiple}x{Factor})";
		}
	}

	public class TheomaticMultiplier
	{
		public GematriaCalculator Calculator { get; set; }
		public Formatter Formatter { get; set; }

		public TheomaticMultiplier(GematriaCalculator calculator, Formatter formatter)
		{
			Calculator = calculator;
			Formatter = formatter;
		}

		public string[] SplitWords(string text)
		{
			return  Regex.Split(text, @"\b").Where(p => p.Trim().Length > 0).ToArray();
		}

		public TheomaticMultiple[] CalculateMultiples(string text, int multiple)
		{
			Func<int,bool> isMultiple = sum => sum > 0 && sum % multiple == 0;

			var multiples = new List<TheomaticMultiple>();
			var phrase = new List<string>();		
			var words = SplitWords(text);	
			var index = 0;

			foreach (var word in words)
			{
				index = text.IndexOf(word, index);
				var fword = Formatter.Format(word);
				phrase.Add(word);
				var sum = Calculator.Add(fword);

				if (isMultiple(sum))
				{				
					var mul = new TheomaticMultiple(multiple, sum, index, index + fword.Length, word);
			 		if (!multiples.Contains(mul)) multiples.Add(mul);
				}
				else
				{
					for (var i = 0; i < phrase.Count(); i++)
					{
						for (var j = 0; j < phrase.Count() - i; j++)
						{
							var pwords = string.Join(" ", phrase.Where((w, n) => n >= i && n <= j + 1));
							if (pwords.Trim().Length == 0) continue;
							var fwords = Formatter.Format(pwords);
							sum = Calculator.Add(fwords);
							
							if (isMultiple(sum))
							{
								var pindex = text.IndexOf(pwords, 0);							
								var mul = new TheomaticMultiple(multiple, sum, pindex, pindex + pwords.Length, pwords);
						 		if (!multiples.Contains(mul)) multiples.Add(mul);
							}
						}
					}
				}
			}

			return multiples.OrderBy(m => m.Sum).ThenBy(m => m.Length).ThenBy(m => m.Start).ToArray();
		}
	}
}
