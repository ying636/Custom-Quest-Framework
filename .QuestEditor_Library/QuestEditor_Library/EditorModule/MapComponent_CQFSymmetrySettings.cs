using Verse;

namespace QuestEditor_Library
{
    public sealed class MapComponent_CQFSymmetrySettings : MapComponent
    {
        public MapComponent_CQFSymmetrySettings(Map map) : base(map) { }

        public bool Active => this.enabled && DebugSettings.godMode && this.Axes != 0;
        public int Axes => (this.mirrorX ? 1 : 0) | (this.mirrorZ ? 2 : 0);
        public int AxisX => this.axisX < 0 ? this.map.Size.x - 1 : this.axisX;
        public int AxisZ => this.axisZ < 0 ? this.map.Size.z - 1 : this.axisZ;
        public bool EditorSessionOpen { get; set; }

        public override void MapComponentUpdate()
        {
            if (this.Active || this.EditorSessionOpen) CQFEditorBridge.SymmetryUpdate?.Invoke(this.map);
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.enabled, "CQF_symmetryEnabled");
            Scribe_Values.Look(ref this.mirrorX, "CQF_symmetryMirrorX", true);
            Scribe_Values.Look(ref this.mirrorZ, "CQF_symmetryMirrorZ");
            Scribe_Values.Look(ref this.showAxes, "CQF_symmetryShowAxes", true);
            Scribe_Values.Look(ref this.axisX, "CQF_symmetryAxisX", -1);
            Scribe_Values.Look(ref this.axisZ, "CQF_symmetryAxisZ", -1);
        }

        public override void MapComponentOnGUI()
        {
            CQFEditorBridge.SymmetryOnGUI?.Invoke(this.map);
        }

        public bool enabled;
        public bool mirrorX = true;
        public bool mirrorZ;
        public bool showAxes = true;
        public int axisX = -1;
        public int axisZ = -1;
    }
}
