using System;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_CQFTargetKey : Window
    {
        public Dialog_CQFTargetKey(Thing target, Action<string> assign)
        {
            this.target = target;
            this.assign = assign;
            this.key = "CQF_Target_" + target.thingIDNumber;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.doCloseX = true;
            this.closeOnAccept = false;
        }

        public override Vector2 InitialSize => new Vector2(520f, 250f);

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(new Rect(0f, 0f, inRect.width - 30f, 32f), "CQF_TargetRegisterKey".Translate());
            Widgets.Label(new Rect(0f, 40f, inRect.width, 30f), this.target.LabelCap + " " + this.target.Position);
            this.key = Widgets.TextField(new Rect(0f, 78f, inRect.width, 30f), this.key);
            if (!this.error.NullOrEmpty())
            {
                Widgets.Label(new Rect(0f, 115f, inRect.width, 46f), this.error.Colorize(Color.red));
            }
            if (Widgets.ButtonText(new Rect(0f, inRect.height - 35f, inRect.width, 35f), "AcceptButton".Translate()))
            {
                if (!CQFTargetSelectionSession.CanPersistThing(this.target))
                {
                    this.error = "CQF_TargetNotFound".Translate();
                    return;
                }
                TargetWithKey existing = CQFTargetSelectionSession.GetAvailableTargets(this.target.Map)
                    .FirstOrDefault(entry => entry.key == this.key && entry.target.Thing != this.target);
                if (existing != null)
                {
                    this.error = "CQF_TargetKeyConflict".Translate() + ": " + this.key;
                    return;
                }
                if (this.target.Map.GetComponent<MapComponent_CQFTargets>().TryRegister(this.key, this.target))
                {
                    this.assign(this.key);
                    this.Close();
                }
                else
                {
                    this.error = "CQF_TargetKeyInvalid".Translate();
                }
            }
        }

        private readonly Thing target;
        private readonly Action<string> assign;
        private string key;
        private string error;
    }
}
