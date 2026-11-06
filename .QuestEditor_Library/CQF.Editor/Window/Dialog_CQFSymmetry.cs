using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public sealed class Dialog_CQFSymmetry : Window
    {
        public Dialog_CQFSymmetry(Map map)
        {
            this.symmetry = map.GetComponent<MapComponent_CQFSymmetrySettings>();
            this.doCloseX = true;
            this.closeOnClickedOutside = false;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.draggable = true;
            this.x = this.symmetry.AxisX * 0.5f;
            this.z = this.symmetry.AxisZ * 0.5f;
            this.xBuffer = this.x.ToString();
            this.zBuffer = this.z.ToString();
        }

        public override Vector2 InitialSize => new Vector2(440f, 350f);

        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope scope = new CQFUIScope(inRect.width, inRect.height);
            Widgets.Label(new Rect(0f, 0f, inRect.width - 30f, 30f), "CQF_SymmetryTitle".Translate().Colorize(CQFUIStyle.Accent));
            Widgets.CheckboxLabeled(new Rect(0f, 40f, inRect.width, 28f), "CQF_SymmetryEnabled".Translate(), ref this.symmetry.enabled);
            TooltipHandler.TipRegion(new Rect(0f, 40f, inRect.width, 28f), "CQF_SymmetryDescription".Translate());
            Widgets.CheckboxLabeled(new Rect(0f, 76f, inRect.width, 28f), "CQF_SymmetryMirrorX".Translate(), ref this.symmetry.mirrorX);
            Widgets.CheckboxLabeled(new Rect(0f, 112f, inRect.width, 28f), "CQF_SymmetryMirrorZ".Translate(), ref this.symmetry.mirrorZ);
            Widgets.CheckboxLabeled(new Rect(0f, 148f, inRect.width, 28f), "CQF_SymmetryShowAxes".Translate(), ref this.symmetry.showAxes);
            this.DrawAxis(new Rect(0f, 188f, inRect.width, 28f), "CQF_SymmetryAxisX", ref this.x, ref this.xBuffer, this.symmetry.map.Size.x);
            this.DrawAxis(new Rect(0f, 224f, inRect.width, 28f), "CQF_SymmetryAxisZ", ref this.z, ref this.zBuffer, this.symmetry.map.Size.z);
            this.symmetry.axisX = Mathf.RoundToInt(this.x * 2f);
            this.symmetry.axisZ = Mathf.RoundToInt(this.z * 2f);
            if (CQFUIStyle.ButtonText(new Rect(0f, 270f, inRect.width, 32f), "CQF_SymmetryCenter".Translate()))
            {
                this.symmetry.axisX = this.symmetry.map.Size.x - 1;
                this.symmetry.axisZ = this.symmetry.map.Size.z - 1;
                this.x = this.symmetry.axisX * 0.5f;
                this.z = this.symmetry.axisZ * 0.5f;
                this.xBuffer = this.x.ToString();
                this.zBuffer = this.z.ToString();
            }
        }

        private void DrawAxis(Rect rect, string key, ref float value, ref string buffer, int size)
        {
            Widgets.Label(new Rect(rect.x, rect.y, rect.width - 140f, rect.height), key.Translate());
            Widgets.TextFieldNumeric(new Rect(rect.xMax - 130f, rect.y, 130f, rect.height), ref value, ref buffer, 0f, size - 1f);
            TooltipHandler.TipRegion(rect, "CQF_SymmetryAxisTip".Translate());
        }

        private readonly MapComponent_CQFSymmetrySettings symmetry;
        private float x;
        private float z;
        private string xBuffer;
        private string zBuffer;
    }
}
