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
        }

        public override Vector2 InitialSize => new Vector2(440f, 310f);

        public override void DoWindowContents(Rect inRect)
        {
            using CQFUIScope scope = new CQFUIScope(inRect.width, inRect.height);
            Widgets.Label(new Rect(0f, 0f, inRect.width - 30f, 30f), "CQF_SymmetryTitle".Translate().Colorize(CQFUIStyle.Accent));
            Widgets.CheckboxLabeled(new Rect(0f, 40f, inRect.width, 28f), "CQF_SymmetryEnabled".Translate(), ref this.symmetry.enabled);
            TooltipHandler.TipRegion(new Rect(0f, 40f, inRect.width, 28f), "CQF_SymmetryDescription".Translate());
            Widgets.CheckboxLabeled(new Rect(0f, 76f, inRect.width, 28f), "CQF_SymmetryMirrorX".Translate(), ref this.symmetry.mirrorX);
            Widgets.CheckboxLabeled(new Rect(0f, 112f, inRect.width, 28f), "CQF_SymmetryMirrorZ".Translate(), ref this.symmetry.mirrorZ);
            Widgets.CheckboxLabeled(new Rect(0f, 148f, inRect.width, 28f), "CQF_SymmetryShowAxes".Translate(), ref this.symmetry.showAxes);
            if (CQFUIStyle.ButtonText(new Rect(0f, 190f, inRect.width, 32f), "CQF_SymmetryDragAxes".Translate()))
            {
                this.symmetry.enabled = true;
                this.symmetry.showAxes = true;
                if (this.symmetry.Axes == 0) this.symmetry.mirrorX = true;
                Find.DesignatorManager.Deselect();
                this.Close();
            }
            TooltipHandler.TipRegion(new Rect(0f, 190f, inRect.width, 32f), "CQF_SymmetryDragTip".Translate());
            if (CQFUIStyle.ButtonText(new Rect(0f, 230f, inRect.width, 32f), "CQF_SymmetryCenter".Translate()))
            {
                this.symmetry.axisX = this.symmetry.map.Size.x - 1;
                this.symmetry.axisZ = this.symmetry.map.Size.z - 1;
            }
        }

        private readonly MapComponent_CQFSymmetrySettings symmetry;
    }
}
