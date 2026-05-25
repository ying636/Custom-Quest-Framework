using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace QuestEditor_Library
{
    public class ModExtension_LandMark : DefModExtension
    {
        public CustomMapGenerationSet maps;
        public IntRange count = new IntRange(3,15);
    }
}
