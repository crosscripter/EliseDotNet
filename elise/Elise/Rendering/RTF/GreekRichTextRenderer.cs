using System.Collections.Generic;
using Elise.Sequencing;

namespace Elise.Rendering
{
    public class GreekRichTextRenderer : RichTextRenderer
    {
        protected override string Footer => "\\cf1\\f1\\lang9\\par\n}\n\u0000";
        protected override string Header => @"{\rtf1\ansi\ansicpg1252\deff0\nouicompat\deflang1033{\fonttbl{\f0\fnil\fcharset161 Courier New;}{\f1\fnil\fcharset0 Courier New;}}
{\colortbl ;" + ColorTable + @";}
{\*\generator Riched20 10.0.14393}\viewkind4\uc1 
\pard\sl240\slmult1\f0\fs16\lang1032";

        protected override Dictionary<char, string> EncodingTable => new Dictionary<char, string>
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

        public GreekRichTextRenderer(string text, params Hit[] hits) : base(text, hits) { }
    }
}