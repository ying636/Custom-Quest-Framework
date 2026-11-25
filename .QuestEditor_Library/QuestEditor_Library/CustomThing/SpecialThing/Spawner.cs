using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace QuestEditor_Library
{
    public class Spawner : ThingWithComps, IDrawTabable
    {
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            yield return new Command_Action()
            {
                defaultLabel = "Spawn",
                action = () =>
                {
                    this.pawns.ForEach(x =>
                    {
                        Lord lord = null;
                        if (x is not PawnSpawnData_Group)
                        {
                            if (!x.lordDataName.NullOrEmpty())
                            {
                                if (MapComponent_CustomMapData.GetComp(this.Map)?.TryGetLord(x.lordDataName, out lord) != true || lord == null)
                                {
                                    Log.Error("[CQF] Missing destination group for generator: " + x.lordDataName);
                                    return;
                                }
                            }
                            else
                            {
                                lord = LordMaker.MakeNewLord(x.faction == null ? null : GameTools.GetFaction(x.faction, this.Map), new LordJob_Custom(), this.Map);
                            }
                        }
                        Quest quest = x is PawnSpawnData_Group || !x.lordDataName.NullOrEmpty() ? GameTools.GetQuestFromMap(this.Map) : null;
                        x.Spawn(this.Position, this.Map, quest == null ? "null" : "Quest" + quest.id, quest, lord);
                    });
                }
            };
            yield break;
        }

        public void DrawTab()
        {
            using CQFUIScope scope = new CQFUIScope();
            float width = CQFUIScope.ContentWidth;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, width - 36f, 34f), "CQF_Spawn_Title".Translate().Colorize(CQFUIStyle.Accent));
            Text.Font = GameFont.Small;
            float actionWidth = (width - 16f) / 3f;
            if (CQFUIStyle.ButtonText(new Rect(0f, 40f, actionWidth, 30f), "AddNewPawns".Translate()))
            {
                var types = new List<Type> { typeof(PawnSpawnData) };
                types.AddRange(typeof(PawnSpawnData).AllSubclassesNonAbstract());
                CQFEditorTools.DrawFloatMenu(types, type => this.pawns.Add((PawnSpawnData)Activator.CreateInstance(type)), type => type.Name.Translate());
            }
            if (CQFUIStyle.ButtonText(new Rect(actionWidth + 8f, 40f, actionWidth, 30f), "CQF_GroupBook_FromBook".Translate()))
                CQFGroupBookBridge.Open(template => this.pawns.Add(new PawnSpawnData_Group { group = template, dataName = template.defName }));
            if (CQFUIStyle.ButtonText(new Rect((actionWidth + 8f) * 2, 40f, actionWidth, 30f), "PastePawns".Translate(), active: CQFEditorTools.data != null))
                this.pawns.Add(CQFEditorTools.data.Copy());

            Rect panel = new(0f, 81f, width, Mathf.Max(50f, CQFUIScope.ContentHeight - 81f));
            CQFUIStyle.DrawMenuSection(panel);
            Rect viewport = panel.ContractedBy(8f);
            if (this.pawns.Count == 0)
            {
                Widgets.Label(viewport, "CQF_Spawn_Empty".Translate().Colorize(CQFUIStyle.Muted));
                return;
            }
            const float rowHeight = 56f;
            Rect content = new Rect(0f, 0f, viewport.width - 18f, this.pawns.Count * rowHeight);
            Widgets.BeginScrollView(viewport, ref this.scrollPos, content);
            try
            {
                int first = Mathf.Max(0, Mathf.FloorToInt(this.scrollPos.y / rowHeight));
                int last = Mathf.Min(this.pawns.Count, Mathf.CeilToInt((this.scrollPos.y + viewport.height) / rowHeight));
                for (int i = first; i < last; i++)
                {
                    PawnSpawnData recipe = this.pawns[i];
                    Rect row = new(0f, i * rowHeight, content.width, rowHeight - 4f);
                    CQFUIStyle.DrawBackground(row, Mouse.IsOver(row));
                    Rect delete = new(row.xMax - 28f, row.y + 12f, 25f, 26f);
                    Rect edit = new(delete.x - 61f, row.y + 11f, 54f, 28f);
                    float textWidth = edit.x - row.x - 15f;
                    string typeName = recipe.GetType().Name.Translate().ToString();
                    string title = recipe.dataName.NullOrEmpty() || recipe.dataName == "undefined" ? typeName : recipe.dataName;
                    string source = recipe is PawnSpawnData_Group { group: { } template } ? template.DisplayName : typeName;
                    string summary = recipe.lordDataName.NullOrEmpty() ? source :
                        source + " · " + "CQF_Spawn_JoinSummary".Translate(recipe.lordDataName);
                    Widgets.Label(new Rect(row.x + 8f, row.y + 5f, textWidth, 22f), title.Truncate(textWidth));
                    Widgets.Label(new Rect(row.x + 8f, row.y + 27f, textWidth, 21f), summary.Truncate(textWidth).Colorize(CQFUIStyle.Muted));
                    TooltipHandler.TipRegion(new Rect(row.x, row.y, edit.x - row.x, row.height), title + "\n" + summary);
                    if (CQFUIStyle.ButtonText(edit, "Edit".Translate()))
                        Find.WindowStack.Add(CQFEditorBridge.Editor.CreateDrawableWindow(recipe));
                    if (CQFUIStyle.ButtonImage(delete, TexButton.Delete, tooltip: "Delete".Translate()))
                    {
                        this.pawns.RemoveAt(i);
                        break;
                    }
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.pawns, "QE_Spawner_Pawns", LookMode.Deep);
        }

        public float height = 0f;
        public Vector2 scrollPos;
        public List<PawnSpawnData> pawns = new List<PawnSpawnData>();
    }
}
