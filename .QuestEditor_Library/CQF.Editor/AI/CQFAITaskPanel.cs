using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public static class CQFAITaskPanel
    {
        public static float Height(CQFAITaskState state, float width)
        {
            GameFont previous = Text.Font;
            try { Text.Font = GameFont.Tiny; return Rows(state).Sum(row => Text.CalcHeight(row.text, width) + 8f); }
            finally { Text.Font = previous; }
        }
        public static float Draw(CQFAITaskState state, Rect rect)
        {
            GameFont font = Text.Font;
            Color color = GUI.color;
            TextAnchor anchor = Text.Anchor;
            bool wrap = Text.WordWrap;
            float y = rect.y;
            try
            {
                Text.Font = GameFont.Tiny; Text.Anchor = TextAnchor.UpperLeft; Text.WordWrap = true;
                foreach (var row in Rows(state))
                {
                    float height = Text.CalcHeight(row.text, rect.width);
                    Rect line = new Rect(rect.x, y, rect.width, height);
                    GUI.color = row.failed ? ColorLibrary.RedReadable : CQFEditorPalette.Muted;
                    Widgets.Label(line, row.text);
                    if (row.tip.Length > 0) TooltipHandler.TipRegion(line, row.tip);
                    if (row.agent != null && Widgets.ButtonInvisible(line)) row.agent.Expanded = !row.agent.Expanded;
                    y += height + 8f;
                }
            }
            finally { Text.Font = font; Text.Anchor = anchor; Text.WordWrap = wrap; GUI.color = color; }
            return y - rect.y;
        }
        private static IEnumerable<(string text, string tip, bool failed, CQFAIAgentRecord? agent)> Rows(CQFAITaskState state)
        {
            if (state.Goal.Length > 0)
            {
                yield return ("CQF_AI_GoalLine".Translate(state.Goal, Status(state.Status)), state.Evidence, false, null);
                foreach (string criterion in state.Criteria) yield return ("CQF_AI_CriterionLine".Translate(criterion), "", false, null);
            }
            foreach (CQFAIPlanStep step in state.Steps)
                yield return ("CQF_AI_PlanLine".Translate(Status(step.Status), step.Text), step.Evidence, step.Status == "blocked", null);
            foreach (CQFAIAgentRecord agent in state.Agents)
            {
                yield return ((agent.Expanded ? "▾ " : "▸ ") + "CQF_AI_AgentLine".Translate(agent.Name, Status(agent.Status), agent.Seconds.ToString("0")),
                    "CQF_AI_AgentDetailsHint".Translate(), agent.Status == "failed", agent);
                if (agent.Expanded)
                {
                    yield return (agent.Assignment, "", false, null);
                    if (agent.Result.Length > 0 || agent.Progress.Length > 0) yield return (agent.Result.Length > 0 ? agent.Result : agent.Progress, "", agent.Status == "failed", null);
                }
            }
        }
        private static string Status(string value) => ("CQF_AI_TaskStatus_" + value).Translate();
    }
}
