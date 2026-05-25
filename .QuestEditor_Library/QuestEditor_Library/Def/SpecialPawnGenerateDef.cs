using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace QuestEditor_Library
{
    public class SpecialPawnGenerateDef : Def
    {
        public SpecialPawnGenerator generator;
        public float commonality = 0.5f;
    }

    public abstract class SpecialPawnGenerator 
    {
        public abstract void Work(List<Pawn> pawns);
    }

    public class SpecialPawnGenerator_AddDialog : SpecialPawnGenerator
    {
        public override void Work(List<Pawn> pawns)
        {
            Dictionary<DialogManagerDef, float> dialogs = new Dictionary<DialogManagerDef, float>();
            this.dialogs.ForEach(d =>
            {
                if (Prefs.DevMode)
                {
                    Log.Message(d.ToString());
                }
                if (d.dialog != null) 
                {
                    dialogs.Add(d.dialog,d.commonality);
                }
                if (d.tag != null) 
                {
                    DefDatabase<DialogManagerDef>.AllDefsListForReading.FindAll(d2 => d2.tags.Contains(d.tag)).ForEach(d2 => dialogs.Add(d2,d.commonality));
                }
            }); if (Prefs.DevMode)
            {
                dialogs.ToList().ForEach(d => Log.Message(d.Key.defName));
            }
            if (!dialogs.Any()) 
            {
                return;
            }
            Dictionary<string, TargetInfo> targets = new Dictionary<string, TargetInfo>();
            pawns.ForEach(p =>
            {
                targets.Clear();
                targets.Add("Interviewee", p);
                var ds = dialogs.ToList().FindAll(d => 
                    (d.Key.genrationConditions == null 
                     || !d.Key.genrationConditions.Exists(c => 
                         !c.Satisfied(targets,out string reason,null))));
                if (Prefs.DevMode)
                {
                    Log.Message(p.Label);
                }
                if (p.trader?.traderKind == null && p.RaceProps.Humanlike && ds.Any())
                {
                    KeyValuePair<DialogManagerDef, float> d = ds.
                        RandomElementByWeight(d2 => d2.Value);
                    if (Rand.Chance(d.Value)) 
                    {
                        Current.Game.GetComponent<GameComponent_Editor>().AddDialog(p, d.Key);
                        dialogs.Remove(d.Key);
                    }
                }
            });
        }

        public List<DialogWithComonality> dialogs = new List<DialogWithComonality>();
    }

    public class DialogWithComonality 
    {
        public DialogManagerDef dialog;
        public string tag;
        public float commonality;
    }
}
