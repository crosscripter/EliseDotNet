using System;
using Elise.Sequencing;
using System.Collections.Generic;

namespace Elise.Rendering
{
    public class RichTextRenderer : Renderer
    {
        public string DefaultColor = @"\red204\green204\blue204";

        Random Random { get; } = new Random();
        protected List<string> Colors { get; private set; } = new List<string>();
        protected string ColorTable { get { return string.Join(";", Colors); } }
        protected Dictionary<string, int> TermColors { get; private set; } = new Dictionary<string, int>();

        protected virtual Dictionary<char, string> EncodingTable { get; }
        protected virtual string Footer { get; } = "\\cf1\\par\n}\n\u0000";
        protected virtual string Header { get { return @"{\rtf1\ansi\ansicpg1252\deff0\nouicompat\deflang1033{\fonttbl{\f0\fnil\fcharset0 Consolas;}}
{\colortbl ;" + ColorTable + @";}
{\*\generator Riched20 10.0.14393}\viewkind4\uc1 
\pard\sl240\slmult1\cf1\f0\fs16\lang9"; } }

        public RichTextRenderer(string text, params Hit[] hits) : base(text, hits)
        {
            BuildColorTable(Terms);
            Colors.Insert(0, DefaultColor);
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

        public override string Render()
        {
            var colorTable = string.Join(";", Colors);
            Grid.Append(Header);
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

            Grid.Append(Footer);
            return Grid.ToString();
        }
    }

}
