using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Elise.Formatting;
using Elise.Sequencing;

namespace Elise.Rendering
{
    public abstract class Renderer
    {
        public readonly UTF8Encoding Encoding = new UTF8Encoding(false);

        static Dictionary<Language, Type> Renderers = new Dictionary<Language, Type>
        {
            { Language.Hebrew, typeof(HebrewRichTextRenderer) },
            { Language.Greek, typeof(GreekRichTextRenderer) },
            { Language.English, typeof(RichTextRenderer) }
        };
        
        Dictionary<string, List<int>> HitIndices { get; } = new Dictionary<string, List<int>>();
        public string Text { get; }
        public StringBuilder Grid { get; }
        public List<string> Terms { get; }
        public List<Hit> Hits { get; }

        public Renderer(string text, params Hit[] hits)
        {
            Hits = new List<Hit>(hits);
            Text = text;
            Grid = new StringBuilder(Text.Length);
            Terms = Hits.Select(h => h.Term).Distinct().ToList();
            HitIndices = GetHitIndices(Hits);
        }

        public static Renderer GetRendererByLanguage(Language language, params object[] args)
        {
            Type TRenderer = Renderers[language];
            return Activator.CreateInstance(TRenderer, args) as Renderer;
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

                if (indices.Contains(index))
                {
                    term = hitIndex.Key;
                    return true;
                }
            }

            return false;
        }

        public abstract string Render();
    }
}
