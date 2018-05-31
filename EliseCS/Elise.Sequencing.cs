using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Elise.Sequencing
{
	using Elise.Formatting;

	public struct Hit 
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

	public class ProgressUpdatedEventArgs : EventArgs 
	{
	   public int Progress { get; }
	   public bool Cancel { get; set; }

	   public ProgressUpdatedEventArgs(int progress)
	   {
		  Progress = progress;
		  Cancel = false;
	   }
	}

	public class Sequencer
	{	
		public readonly UTF8Encoding Encoding = new UTF8Encoding(false);

		public string Text { get; private set; }
		public int Start { get; }
		public int Stop { get; }
		public int FromSkip { get; }
		public int ToSkip { get; }
		public int Proximity { get; }
		public Formatter Formatter { get; }

		public List<string> Terms { get; private set; }
		public List<Hit> Hits { get; private set; }
		public string Grid { get; private set; }
		public int Limit { get; private set; }

		public Sequencer(Formatter formatter, string text, int start=0, int stop=-1, int fromSkip=2, int toSkip=-1, int proximity=-1)
		{
			FromSkip = fromSkip;
			Hits = new List<Hit>();
			Text = text;
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
			var sequence = new StringBuilder(capacity, capacity);

			for (var i = 0; i < length; i += skip) 
			{
				sequence.Append(text[i]);
			}

			return sequence.ToString();
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

		public delegate void ProgressUpdatedEventHandler(object sender, ProgressUpdatedEventArgs e);
		public event ProgressUpdatedEventHandler ProgressUpdated;

		protected virtual void OnProgressUpdated(ProgressUpdatedEventArgs e)
	    {
	        if (ProgressUpdated != null) ProgressUpdated(this, e);
	    }

		public IEnumerable<Hit> Search(params string[] terms)
		{
			var locker = new Object();
			Terms = terms.Select(t => Formatter.Format(t)).ToList();
			var revList = Terms.Select(t => Reverse(t)).ToArray();
			Terms.AddRange(revList);		
			Limit = Terms.OrderByDescending(t => t.Length).FirstOrDefault().Length;
			var hitCache = new List<int>();

			for (var pos = Start; pos < Stop; pos++)
			{
				var args = new ProgressUpdatedEventArgs(pos + 1);
				OnProgressUpdated(args);
				if (args.Cancel) break;
				var length = Stop - pos;

				Parallel.For(FromSkip, ToSkip, new ParallelOptions 
				{ MaxDegreeOfParallelism = ToSkip - FromSkip }, skip =>
				{
					var text = Range(Text, pos, Stop);
					var size = (int)Math.Ceiling((double)(length / skip));
			        if (size < Limit) return;
					var sequence = Sequence(text, skip);

					foreach (var term in Terms)
					{
						var indices = IndicesOf(sequence, term);
						if (indices.Length == 0) continue;

						foreach (var index in indices) 
						{
							var hindex = ((index * skip) + pos);

							if (!hitCache.Contains(hindex))
							{
								lock (locker)
								{
									hitCache.Add(hindex);
									Hits.Add(new Hit(term, hindex, pos, skip));
								}
							}
						}
					}
				});
			}

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
			return $@"Searching text 
starting at position {Start + 1} until position {Stop + 1}
skipping every {FromSkip} to {ToSkip} letter(s) 
searching for {string.Join(",", Terms ?? new List<string>())}
within a proximity of {Proximity} letter(s).

{Hits.Count()} total hit(s) found:

{string.Join("\n", Hits ?? new List<Hit>())}

	";
		}
	}
}
