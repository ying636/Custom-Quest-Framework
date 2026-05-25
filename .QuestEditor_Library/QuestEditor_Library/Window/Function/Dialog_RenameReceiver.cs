using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_RenameReceiver : Dialog_Rename<Building_TransmitReceiver>
    {
        public Dialog_RenameReceiver(Building_TransmitReceiver renaming) : base(renaming)
        {
        }
    }
}
