using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using System.Xml;
using System.Xml.Linq;
using RimWorld.QuestGen;
using Verse.Grammar;
using System.Reflection;
using UnityEngine;
using System.Collections;
using Verse.AI;
using Verse.AI.Group;
using System.IO;
using Unity.Collections;
using RimWorld.Planet;
using System.Net.NetworkInformation;
using System.Text;

namespace QuestEditor_Library
{
    public abstract class DialogCondition_Target : DialogCondition
    {
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("targetText", this.targetText));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawSelectableText(y, "DialogueTarget".Translate(), ref this.targetText, () => CQFEditorTools.DrawFloatMenu(CQFEditorTools.TargetTexts,
            t =>
            {
                this.targetText = t;
            }, t => t.Translate()), x, 150f);
            y += 30f;
        }
        public virtual bool GetTarget(Dictionary<string, TargetInfo> targets, out Thing targetResult, out string reason, Quest quest)
        {
            string[] ts = this.targetText.Split(new char[] { '.' });
            if (ts.Count() >= 2 && int.TryParse(ts.Last(), out int index0) && GameTools.GetTargetWithIndex(quest, ts.First(), index0) is TargetInfo target && target.Thing is Thing targetP)
            {
                targetResult = targetP;
                reason = null;
                return true;
            }
            else if (GameTools.GetTargetsFromGroup(quest, this.targetText) is List<TargetInfo> ps && ps.Any() && ps.Find(p => p.Thing != null) is TargetInfo t3)
            {
                targetResult = t3.Thing;
                reason = null;
                return true;
            }
            if (targets.TryGetValue(this.targetText, out TargetInfo t4))
            {
                reason = null;
                targetResult = t4.Thing;
                return true;
            }
            else
            {
                reason = "TargetIsntPawn".Translate();
                targetResult = null;
                return false;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.targetText, "DialogCondition_Target_targetText");
        }

        public string targetText;
    }
    //public class DialogCondition_Quality : DialogCondition_Target_Pawn
    //{
    //    public override XElement SaveToXElement(string nodeName)
    //    {
    //        XElement result = base.SaveToXElement(nodeName);
    //        result.Add(new XElement("skill", this.skill.defName));
    //        result.Add(new XElement("level", this.level));
    //        result.Add(new XElement("needToBeGreater", this.needToBeGreater));
    //        return result;
    //    }
    //    public override void Draw(ref float y, Rect inRect, float x)
    //    {
    //        base.Draw(ref y, inRect, x);
    //        CQFEditorTools.DrawLabelAndText_Line(y, "RequiredQualiy".Translate(), ref this.level, ref this.buffer, x);
    //        y += 30f;
    //        Widgets.CheckboxLabeled(new Rect(x, y, 325f, 20f), "NeedToBeGreater".Translate(), ref this.needToBeGreater);
    //        y += 25f;
    //    }
    //    public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
    //    {
    //        if (base.GetTarget(targets, out Thing target, out reason, quest) && target.TryGetQuality(out QualityCategory quality))
    //        {
    //        }
    //        else
    //        {
    //            reason = null;
    //            return false;
    //        }
    //    }

    //    public override void ExposeData()
    //    {
    //        base.ExposeData();
    //        Scribe_Values.Look(ref this.value, "DialogCondition_Quality_value");
    //        Scribe_Values.Look(ref this.needToBeGreater, "DialogCondition_Quality_needToBeGreater");
    //    }

    //    public float value;
    //    public bool needToBeGreater = true;
    //}
    public class DialogCondition_QuestState : DialogCondition
    {
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            if (Widgets.ButtonText(new Rect(x,y,inRect.width,25f),"QuestState".Translate(this.state.ToString().Translate()),false)) 
            {
                CQFEditorTools.DrawFloatMenu(new List<QuestState>() {QuestState.EndedFailed, QuestState.EndedSuccess , QuestState.Ongoing
                ,QuestState.EndedOfferExpired},q => this.state = q,q => q.ToString().Translate());         
            }
            y += 30f; 
            if (Widgets.ButtonText(new Rect(x, y, 450f, 25f), "CQFQuestDef".Translate(this.quest?.defName), false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<QuestScriptDef>.AllDefsListForReading, (d) => this.quest = d, (d) => d.defName);
            }
            y += 30f;
        }
        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            Quest q = quest;
            if (this.quest != null && Find.QuestManager.QuestsListForReading.Find(qu => qu.root == this.quest) is Quest q2) 
            {
                q = q2;
            }
            if (q != null && q.State == this.state)
            {
                reason = null;
                return true;
            }
            else
            {
                reason = this.failReason?.Translate();
                return false;
            }
        }
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("quest", this.quest.defName));
            result.Add(new XElement("state", this.state.ToString()));
            return result;
        }
        public override void ExposeData()
        {
            base.ExposeData(); 
            Scribe_Values.Look(ref this.state, "state");
            Scribe_Defs.Look(ref this.quest, "quest");
        }
        public QuestScriptDef quest;
        public QuestState state = QuestState.Ongoing;
    }
    public class DialogCondition_CapturedPawn: DialogCondition_Target
    {
        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            if (base.GetTarget(targets, out Thing target1, out string reason2, quest) && target1 is CustomTrap_Capture trap && trap.HasPawn)
            {
                reason = null;
                return true;
            }
            else
            {
                reason = reason2;
                return false;
            }
        }
    }
    public class DialogCondition_ContainerIsFull : DialogCondition_Target
    {
        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            if (base.GetTarget(targets, out Thing target1, out string reason2, quest) && target1 is IThingHolder holder && holder.GetDirectlyHeldThings().Any())
            {
                reason = null;
                return true;
            }
            else
            {
                reason = reason2;
                return false;
            }
        }
    }
    public class DialogCondition_SkillCheck : DialogCondition_Target
    {
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("skill", this.skill.defName)); 
            result.Add(new XElement("checkModifier", this.checkModifier));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            Rect rect = new Rect(x, y, 150f, 25f);
            rect = new Rect(x, y, 150f, 25f); 
            if (Widgets.ButtonText(rect, "RequiredSkill".Translate() + this.skill?.label, false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<SkillDef>.AllDefsListForReading, s => this.skill = s, s => s.label);
            }
            y += 30f; 
            CQFEditorTools.DrawLabelAndText_Line(y, "CheckModifier".Translate(), ref this.checkModifier, ref this.buffer2, x);
            y += 30f;
        }
        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            if (base.GetTarget(targets, out Thing targetPawn, out reason, quest) && targetPawn is Pawn p)
            {
                if (p.skills is Pawn_SkillTracker skill)
                {
                    var point = skill.GetSkill(this.skill).Level + this.checkModifier;
               
                    if (Rand.Range(0,20) < point)
                    {
                        reason = null;
                        return true;
                    }
                    else
                    {
                        reason = this.failReason.Translate(this.skill);
                        return false;
                    }
                }
                else
                {
                    reason = "TargetIsntLikeHumanOrTargetIsNull".Translate();
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData(); 
            Scribe_Values.Look(ref this.checkModifier, "checkModifier");
            Scribe_Defs.Look(ref this.skill, "DialogCondition_Target_skill");
        }

        public string buffer;
        public string buffer2;
        public SkillDef skill; 
        public int checkModifier;
    }
    public class DialogCondition_Faction : DialogCondition_Target
    {
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("faction", this.faction.defName));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            Rect rect = new Rect(x, y, 150f, 25f);
            if (Widgets.ButtonText(rect, "RequiredFaction".Translate(this.faction?.label), false))
            {
                Find.WindowStack.Add(new Dialog_Select<FactionDef>(DefDatabase<FactionDef>.AllDefsListForReading, null, t => t.label, "Select".Translate(), t =>
                {
                    this.faction = t;
                }));
            }
            y += 30f;
        }
        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            if (base.GetTarget(targets, out Thing target, out reason, quest) && target is Thing targetPawn)
            {
                if (targetPawn != null)
                {
                    if (targetPawn.Faction.def == this.faction)
                    {
                        reason = null;
                        return true;
                    }
                    else
                    {
                        reason = "TargetIsntNumberOfFaction".Translate(this.faction?.label);
                        return false;
                    }
                }
                else
                {
                    reason = "TargetIsntLikeHumanOrTargetIsNull".Translate();
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref this.faction, "DialogCondition_Faction_faction");
        }

        public FactionDef faction;
    }
    public class DialogCondition_ThingInPosition : DialogCondition_Target
    {
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("positionName", this.positionName));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawSelectableText(y, "PositionName".Translate(), ref this.positionName, () => CQFEditorTools.DrawFloatMenu(CQFEditorTools.TargetTexts,
            t =>
            {
                this.positionName = t;
            }, t => t.Translate()), x, 150f);
            y += 30f;
        }
        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            if (base.GetTarget(targets, out Thing target, out reason, quest) && GameTools.GetTarget(targets,quest,this.positionName) is TargetInfo position)
            {
                if ((position.Map == null || target.Map == position.Map) && target.Position == position.Cell)
                {
                    reason = null;
                    return true;
                }
                else 
                {
                    reason = this.failReason.Translate();
                    return false;
                }
            }
            else
            {
                reason = null;
                return false;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.positionName, "positionName");
        }

        public string positionName;
    }
    public abstract class DialogCondition_Target_Pawn : DialogCondition_Target
    {
        public override bool GetTarget(Dictionary<string, TargetInfo> targets, out Thing targetPawn, out string reason, Quest quest)
        {
            if (base.GetTarget(targets, out Thing target, out string reason2, quest))
            {
                if (!(target is Pawn))
                {
                    reason = "TargetIsntPawn".Translate();
                    targetPawn = null;
                    return false;
                }
                reason = null;
                targetPawn = target;
                return true;
            }
            else
            {
                targetPawn = null;
                reason = reason2;
                return false;
            }
        }
    }
    public class DialogCondition_Skill : DialogCondition_Target_Pawn
    {
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("skill", this.skill.defName));
            result.Add(new XElement("level", this.level));
            result.Add(new XElement("needToBeGreater", this.needToBeGreater));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            Rect rect = new Rect(x, y, 150f, 25f);
            rect = new Rect(x, y, 150f, 25f);
            if (Widgets.ButtonText(rect, "RequiredSkill".Translate() + this.skill?.label, false))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<SkillDef>.AllDefsListForReading, s => this.skill = s, s => s.label);
            }
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line(y, "RequiredLevel".Translate(), ref this.level, ref this.buffer, x);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 325f, 20f), "NeedToBeGreater".Translate(), ref this.needToBeGreater);
            y += 25f;
        }
        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            if (base.GetTarget(targets, out Thing targetPawn, out reason, quest) && targetPawn is Pawn p)
            {
                if (p.skills is Pawn_SkillTracker skill)
                {
                    if (this.needToBeGreater ? skill.GetSkill(this.skill).Level > this.level : skill.GetSkill(this.skill).Level < this.level)
                    {
                        reason = null;
                        return true;
                    }
                    else
                    {
                        reason = "SkillIsntsatisfied".Translate(this.skill);
                        return false;
                    }
                }
                else
                {
                    reason = "TargetIsntLikeHumanOrTargetIsNull".Translate();
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.level, "DialogCondition_Target_level");
            Scribe_Values.Look(ref this.needToBeGreater, "DialogCondition_Target_needToBeGreater");
            Scribe_Defs.Look(ref this.skill, "DialogCondition_Target_skill");
        }

        public string buffer;
        public SkillDef skill;
        public int level;
        public bool needToBeGreater = true;
    }
    public class DialogCondition_Hediff : DialogCondition_Target_Pawn
    {
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("hediff", this.hediff.defName));
            result.Add(new XElement("severity", this.severity));
            result.Add(new XElement("needToBeGreater", this.needToBeGreater));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            Rect rect = new Rect(x, y, 150f, 25f);
            rect = new Rect(x, y, 150f, 25f);
            Widgets.Label(rect, "RequiredHediff".Translate() + this.hediff?.label);
            rect.x = 160f;
            if (Widgets.ButtonText(rect, "Select".Translate()))
            {
                CQFEditorTools.DrawFloatMenu(DefDatabase<HediffDef>.AllDefsListForReading, h => this.hediff = h, h => h.label);
            }
            y += 30f;
            CQFEditorTools.DrawLabelAndText_Line<float>(y, "RequiredSeverity".Translate(), ref this.severity, ref this.buffer, x);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 325f, 20f), "NeedToBeGreater".Translate(), ref this.needToBeGreater);
            y += 25f;
        }
        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            if (base.GetTarget(targets, out Thing targetPawn, out reason, quest) && targetPawn is Pawn p)
            {
                if (targetPawn != null && p.health is Pawn_HealthTracker health)
                {
                    if (health.hediffSet.GetFirstHediffOfDef(this.hediff) is Hediff hediff)
                    {
                        if (this.needToBeGreater ? hediff.Severity > this.severity : hediff.Severity < this.severity)
                        {
                            reason = null;
                            return true;
                        }
                        else
                        {
                            reason = this.failReason.Translate(this.hediff);
                            return false;
                        }
                    }
                    else
                    {
                        reason = "TargetHasntRequired".Translate(this.hediff);
                        return false;
                    }
                }
                else
                {
                    reason = "TargetIsntLikeHumanOrTargetIsNull".Translate();
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.severity, "DialogCondition_Hediff_severity");
            Scribe_Values.Look(ref this.needToBeGreater, "DialogCondition_Hediff_needToBeGreater");
            Scribe_Defs.Look(ref this.hediff, "DialogCondition_Hediff_hediff");
        }

        public string buffer;
        public HediffDef hediff;
        public float severity;
        public bool needToBeGreater = true;
    }
    public class DialogCondition_Trait : DialogCondition_Target_Pawn
    {
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("trait", this.trait.defName));
            result.Add(new XElement("degree", this.degree));
            result.Add(new XElement("needToBeGreater", this.needToBeGreater));
            result.Add(new XElement("accurate", this.accurate));
            return result; 
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            Rect rect = new Rect(x, y, 150f, 25f);
            List<KeyValuePair<TraitDef, TraitDegreeData>> stagets = new List<KeyValuePair<TraitDef, TraitDegreeData>>();
            DefDatabase<TraitDef>.AllDefsListForReading.ForEach(t =>
            {
                t.degreeDatas.ForEach(s =>
                {
                    stagets.Add(new KeyValuePair<TraitDef, TraitDegreeData>(t, s));
                });
            });
            if (Widgets.ButtonText(rect, "RequiredTrait".Translate(this.trait?.degreeDatas.Find(d => d.degree == this.degree)?.label), false))
            {
                Find.WindowStack.Add(new Dialog_Select<KeyValuePair<TraitDef, TraitDegreeData>>(stagets, null, t => t.Value.label, "Select".Translate(), t =>
                {
                    this.trait = t.Key;
                    this.degree = t.Value.degree;
                }));
            }
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 325f, 20f), "NeedToBeGreater".Translate(), ref this.needToBeGreater);
            y += 25f;
            Rect r = new Rect(x, y, 325f, 20f);
            Widgets.CheckboxLabeled(r, "Accurate".Translate(), ref this.accurate);
            TooltipHandler.TipRegion(r, "Accurate_Tip".Translate());
            y += 25f;
        }
        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            if (base.GetTarget(targets, out Thing targetPawn, out reason, quest) && targetPawn is Pawn p)
            {
                if (targetPawn != null && p.story is Pawn_StoryTracker story)
                {
                    if (story.traits?.GetTrait(this.trait) is Trait trait)
                    {
                        if (this.accurate ? trait.Degree == this.degree : this.needToBeGreater ? trait.Degree >= this.degree : trait.Degree <= this.degree)
                        {
                            reason = null;
                            return true;
                        }
                        else
                        {
                            reason = "TraitIsntsatisfied".Translate(this.trait.DataAtDegree(this.degree)?.label);
                            return false;
                        }
                    }
                    else
                    {
                        reason = "TargetHasntRequiredTrait".Translate(this.trait.DataAtDegree(this.degree)?.label);
                        return false;
                    }
                }
                else
                {
                    reason = "TargetIsntLikeHumanOrTargetIsNull".Translate();
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.degree, "DialogCondition_Trait_severity");
            Scribe_Values.Look(ref this.accurate, "accurate");
            Scribe_Values.Look(ref this.needToBeGreater, "DialogCondition_Trait_needToBeGreater");
            Scribe_Defs.Look(ref this.trait, "DialogCondition_Trait_hediff");
        }

        public string buffer;
        public TraitDef trait;
        public int degree = 0;
        public bool needToBeGreater = true;
        public bool accurate = false;
    }
    public class DialogCondition_Age : DialogCondition_Target_Pawn
    {
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("age", this.age));
            result.Add(new XElement("needToBeGreater", this.needToBeGreater));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawLabelAndText_Line(y, "CQFAge".Translate(), ref this.age, ref this.buffer, x);
            y += 30f;
            Widgets.CheckboxLabeled(new Rect(x, y, 325f, 25f), "NeedToBeGreater".Translate(), ref this.needToBeGreater);
            y += 30f;
        }
        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            if (base.GetTarget(targets, out Thing target, out reason, quest) && target is Pawn targetPawn)
            {
                if (targetPawn != null)
                {
                    int age = targetPawn.ageTracker.AgeBiologicalYears;
                    if (this.needToBeGreater ? age > this.age : age <= this.age)
                    {
                        reason = null;
                        return true;
                    }
                    else
                    {
                        reason = this.failReason.Translate(this.age);
                        return false;
                    }
                }
                else
                {
                    reason = "TargetIsntLikeHumanOrTargetIsNull".Translate();
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.age, "DialogCondition_Age_age");
            Scribe_Values.Look(ref this.needToBeGreater, "DialogCondition_Age_needToBeGreater");
        }

        public string buffer;
        public int age;
        public bool needToBeGreater = true;
    }
    public class DialogCondition_PrisonerOrSlave : DialogCondition_Target_Pawn
    {
        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            if (base.GetTarget(targets, out Thing target, out reason, quest) && target is Pawn targetPawn)
            {
                if (targetPawn != null)
                {
                    if (targetPawn.IsPrisoner || targetPawn.IsSlave)
                    {
                        reason = null;
                        return true;
                    }
                    else
                    {
                        reason = this.failReason.Translate(targetPawn.Name.ToString());
                        return false;
                    }
                }
                else
                {
                    reason = "TargetIsntLikeHumanOrTargetIsNull".Translate();
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

    }
    public class DialogCondition_Thought : DialogCondition_Target_Pawn
    {
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(new XElement("thought", this.thought.defName));
            result.Add(new XElement("untranslatedLabel", this.untranslatedLabel));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);

            Rect rect = new Rect(x, y, 350f, 25f);
            List<KeyValuePair<ThoughtDef, ThoughtStage>> stagets = new List<KeyValuePair<ThoughtDef, ThoughtStage>>();
            DefDatabase<ThoughtDef>.AllDefsListForReading.ForEach(t =>
            {
                t.stages.ForEach(s =>
                {
                    stagets.Add(new KeyValuePair<ThoughtDef, ThoughtStage>(t, s));
                });
            });
            if (Widgets.ButtonText(rect, "CQF_ThoughtDef".Translate(this.thought?.stages.Find(s => s.untranslatedLabel == this.untranslatedLabel)?.label), false))
            {
                Find.WindowStack.Add(new Dialog_Select<KeyValuePair<ThoughtDef, ThoughtStage>>(stagets, null, t => t.Value?.label, "Select".Translate(), t =>
                {
                    this.thought = t.Key;
                    if (t.Key.stages.Contains(t.Value))
                    {
                        this.untranslatedLabel = t.Value.untranslatedLabel;
                    }
                    else
                    {
                        Log.Message("CQF Action Gain Mood Error:A thoughtstage without thought");
                    }
                }));
            }
            y += 30f;
        }
        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            if (base.GetTarget(targets, out Thing target, out reason, quest) && target is Pawn targetPawn)
            {
                if (targetPawn != null)
                {
                    List<Thought_Situational> ts = (List<Thought_Situational>)targetPawn.needs.mood.thoughts.situational.GetType().GetField("cachedThoughts", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(targetPawn.needs.mood.thoughts.situational);
                    if ((targetPawn.needs.mood.thoughts.memories.GetFirstMemoryOfDef(this.thought) is Thought t
                        && t.CurStage.untranslatedLabel == this.untranslatedLabel)
                        || (ts.Find(t0 => t0.def == this.thought) is Thought t2 && t2.CurStage.untranslatedLabel == this.untranslatedLabel ))
                    {
                        reason = null;
                        return true;
                    }
                    else
                    {
                        reason = this.failReason.Translate(targetPawn.Name.ToString());
                        return false;
                    }
                }
                else
                {
                    reason = "TargetIsntLikeHumanOrTargetIsNull".Translate();
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.untranslatedLabel, "untranslatedLabel");
            Scribe_Defs.Look(ref this.thought, "thought");
        }
        public ThoughtDef thought;
        public string untranslatedLabel;
    }
    public class DialogCondition_Inventory : DialogCondition_Target_Pawn
    {
        public override XElement SaveToXElement(string nodeName)
        {
            XElement result = base.SaveToXElement(nodeName);
            result.Add(CQFEditorTools.SaveList_Saveable(this.requirations, "requirations"));
            return result;
        }
        public override void Draw(ref float y, Rect inRect, float x)
        {
            base.Draw(ref y, inRect, x);
            CQFEditorTools.DrawIDrawList(ref y, x, this.requirations, inRect, "RequiredThings".Translate(), () =>
CQFEditorTools.DrawFloatMenu(new List<Type>() {typeof(CQFThingDefCount)}, t =>
{
    CQFThingData.OpenSelectWindow(t, d => this.requirations.Add(d));
}, t => t.Name.Translate()), t => t.ToString(), (t, y2, rect, x2) =>
{
    t.DrawWithSingleCount(ref y2, rect, x2);
    return y2;
});
            y += 5f;
        }
        public override bool Satisfied(Dictionary<string, TargetInfo> targets, out string reason, Quest quest)
        {
            if (base.GetTarget(targets, out Thing target, out reason, quest) && target is Pawn targetPawn)
            {
                if (targetPawn != null && targetPawn.inventory?.innerContainer?.InnerListForReading is List<Thing> list)
                {
                    Dictionary<ThingDef, int> requirations = new Dictionary<ThingDef, int>();
                    this.requirations.ForEach(r => requirations.Add((r as CQFThingDefCount).thing,r.count.RandomInRange));
                    list.ForEach(t =>
                    {
                        if (requirations.ContainsKey(t.def)) 
                        {
                            requirations[t.def] -= t.stackCount;
                        }
                    });
                    if (!requirations.ToList().Exists(r => r.Value > 0))
                    {
                        reason = null;
                        return true;
                    }
                    else
                    {
                        reason = this.failReason.Translate(targetPawn.Name.ToString());
                        return false;
                    }
                }
                else
                {
                    reason = "TargetIsntLikeHumanOrTargetIsNull".Translate();
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.requirations, "requirations",LookMode.Deep);
        }
        public List<CQFThingData> requirations = new List<CQFThingData>();
    }
}
