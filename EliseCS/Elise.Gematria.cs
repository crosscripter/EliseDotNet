using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Elise.Gematria
{
	public class Alphabet
	{
		public int Begin { get; }
		public int End { get; }
		public Dictionary<char,int> Letters { get; set; }

		public virtual int Calculate(int begin, int end, int value)
		{
			return value - begin;
		}

		protected virtual void MapCharacterValues()
		{
			Letters = new Dictionary<char,int>();

			for (var i = Begin; i <= End; i++) 
			{
				Letters.Add((char)i, Calculate(Begin, End, i));
			}
		}

		public Alphabet(int begin, int end)
		{
			Begin = begin;
			End = end;
			MapCharacterValues();
		}

		public Alphabet(Dictionary<char,int> letters)
		{
			Letters = letters;
		}

		public int this[char letter] => Letters.ContainsKey(letter) ? Letters[letter] : 0;
	}

	public class LatinAlphabet : Alphabet
	{
		public bool UseAgrippaKey { get; }

		static int[] Values = {
			1, 2, 3, 4, 5, 6, 7, 8, 9, 600,
			10, 20, 30, 40, 50, 60, 70, 80, 90,
			100, 200, 700, 900, 300, 400, 500
		};

		public override int Calculate(int begin, int end, int value)
		{
			return UseAgrippaKey ? Values[value - begin] : value - (begin - 1);
		}

		public LatinAlphabet(bool useAgrippaKey=false) : base(65, 65 + 26 - 1) 
		{ 
			UseAgrippaKey = useAgrippaKey;
			if (UseAgrippaKey) MapCharacterValues();
		}
	}

	public class HebrewAlphabet : Alphabet
	{
		public bool UseFinalValues { get; }

		static int[] Values = { 
			1, 2, 3, 4, 5, 6, 7, 8, 9, 
			10, 500, 20, 30, 600, 40, 700, 50, 60, 70, 800, 80, 900, 90, 
			100, 200, 300, 400 
		};
		
		public override int Calculate(int begin, int end, int value) 
		{
			var index = value - begin;
			return index < Values.Length ? Values[index] : 0;
		}

		public HebrewAlphabet(bool useFinalValues=true) : base(1488, 1488 + 27 - 1)
		{
			UseFinalValues = useFinalValues;

			if (!UseFinalValues)
			{
				for (var i = 0; i < Values.Length; i++) 
				{
					if (Values[i] > 400) Values[i] = Values[i + 1];
				}
		
				MapCharacterValues();
			}
		}
	}

	public class GreekAlphabet : Alphabet
	{
		static int[] Values = {
			1, 2, 3, 4, 5, 7, 8, 9, 
			10, 20, 30, 40, 50, 60, 70, 80,
			100, 0, 200, 300, 400, 500, 600, 700, 800
		};

		public override int Calculate(int begin, int end, int value)
		{
			var index = value - begin;			
			return index < Values.Length ? Values[index] : 0;
		}

		public GreekAlphabet() : base(913, 913 + 24) { }
	}


	public class GematriaCalculator
	{
		Alphabet Alphabet;

		public GematriaCalculator(Alphabet alphabet)
		{
			Alphabet = alphabet;
		}

		public int Add(char letter) => Alphabet[letter];
		public int this[char letter] => Add(letter);
		public int Add(string word) => word.Sum(Add);
		public int this[string word] => Add(word);
	}
}
