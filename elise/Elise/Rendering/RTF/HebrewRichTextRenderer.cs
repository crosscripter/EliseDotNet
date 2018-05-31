using Elise.Sequencing;
using System.Collections.Generic;

namespace Elise.Rendering
{
    public class HebrewRichTextRenderer : RichTextRenderer
    {
        protected override string Footer => "\\cf1\\f1\\ltrch\\lang1033  \\lang9\\par\n}\n\u0000";
        protected override string Header => @"{\rtf1\fbidis\ansi\ansicpg1252\deff0\nouicompat\deflang1033{\fonttbl{\f0\fnil\fcharset177 Courier New;}{\f1\fnil\fcharset0 Courier New;}}
{\colortbl ;" + ColorTable + @";}
{\*\generator Riched20 10.0.14393}\viewkind4\uc1 
\pard\ltrpar\sl240\slmult1\f0\rtlch\fs16\lang1037";

        protected override Dictionary<char,string> EncodingTable => new Dictionary<char, string>
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

        public HebrewRichTextRenderer(string text, params Hit[] hits) : base(text, hits) { }
    }
}
