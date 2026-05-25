using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace QuestEditor_Library
{
    public interface ICustomThing
    {
        CustomThingData GetData(IntVec3 pos);
    }
}
