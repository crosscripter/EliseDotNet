using System;
using System.Collections.Generic;

namespace Elise.Sources
{
    public class OldTestament : Bible
    {
        public OldTestament(HebrewSources source = HebrewSources.WLC)
        {
            var text = Sources.Load($"Elise.Resources.Sources.{source}.src");
            Records = text.Split(new string[] { "\n" }, StringSplitOptions.None);
            Books = "Ge,Ex,Le,De,Jos,Jg,1Sa,2Sa,1Ki,2Ki,Isa,Jer,Eze,Ho,Joe,Am,Ob,Jon,Mic,Na,Hab,Zep,Hag,Zec,Mal,Ps,Pr,Job,Song,Ru,La,Ec,Es,Da,Ezr,Ne,1Ch,2Ch".ToUpper().Split(',');

            Aliases = new Dictionary<string, string>
            {
                { "OT", this["Ge", "2Ch"]},
                { "LAW", this["Ge", "De"]},
                { "TWELVE", this["Ho","Mal"]},
                { "PROPHETS", this["Jos","Mal"]},
                { "WRITINGS", this["Ps", "2Ch"]}
            };
        }
    }
}
