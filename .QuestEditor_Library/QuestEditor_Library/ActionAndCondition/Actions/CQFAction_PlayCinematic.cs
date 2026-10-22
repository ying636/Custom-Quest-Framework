using System;
using System.Collections.Generic;
using System.Xml.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAction_PlayCinematic : CQFAction
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.VisualEffect;

        public override void Draw(ref float y, Rect inRect, float x)
        {
            using CQFUIScope scope = new CQFUIScope(inRect.width);
            Rect rect = new Rect(x, y, Mathf.Max(40f, inRect.width - x - 12f), 30f);
            if (CQFUIStyle.ButtonText(rect, "CQF_Cinematic_PlayCinematic".Translate() + ": " + (this.cinematic?.defName ?? "CQF_Cinematic_None".Translate().ToString()), overrideTextAnchor: TextAnchor.MiddleLeft))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<CQFCinematicDef>.AllDefsListForReading, definition => this.cinematic = definition, definition => definition.defName);
            }

            y += 38f;
        }

        public override void Work(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            if (this.cinematic == null)
            {
                throw new InvalidOperationException("[CQF] CQFAction_PlayCinematic requires a CQFCinematicDef.");
            }

            GameComponent_Cinematics component = GameComponent_Cinematics.Instance ?? throw new InvalidOperationException("[CQF] GameComponent_Cinematics is unavailable.");
            component.Play(this.cinematic, targets ?? new Dictionary<string, TargetInfo>(), quest);
        }

        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            if (this.cinematic != null)
            {
                result.Add(new XElement("cinematic", this.cinematic.defName));
            }

            return result;
        }

        public override void ExposeData()
        {
            Scribe_Defs.Look(ref this.cinematic, "cinematic");
        }

        public CQFCinematicDef? cinematic;
    }
}
