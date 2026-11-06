using Verse;

namespace QuestEditor_Library
{
    public readonly struct CQFSymmetryTransform
    {
        public CQFSymmetryTransform(int axes, int axisX, int axisZ)
        {
            this.Axes = axes;
            this.axisX = axisX;
            this.axisZ = axisZ;
        }

        public int Axes { get; }

        public IntVec3 Cell(IntVec3 cell) => new IntVec3(
            (this.Axes & 1) != 0 ? this.axisX - cell.x : cell.x, cell.y,
            (this.Axes & 2) != 0 ? this.axisZ - cell.z : cell.z);

        public Rot4 Rotation(Rot4 rotation)
        {
            if (!rotation.IsValid) return rotation;
            int value = rotation.AsInt;
            if ((this.Axes & 1) != 0 && (value == 1 || value == 3)) value = 4 - value;
            if ((this.Axes & 2) != 0 && (value == 0 || value == 2)) value = 2 - value;
            return new Rot4(value);
        }

        public CellRect Rect(CellRect rect)
        {
            return new CellRect((this.Axes & 1) != 0 ? this.axisX - rect.maxX : rect.minX,
                (this.Axes & 2) != 0 ? this.axisZ - rect.maxZ : rect.minZ, rect.Width, rect.Height);
        }

        public IntVec3 Position(IntVec3 position, Rot4 rotation, ThingDef def)
        {
            CellRect target = this.Rect(GenAdj.OccupiedRect(position, rotation, def.size));
            CellRect origin = GenAdj.OccupiedRect(IntVec3.Zero, this.Rotation(rotation), def.size);
            return new IntVec3(target.minX - origin.minX, position.y, target.minZ - origin.minZ);
        }

        private readonly int axisX;
        private readonly int axisZ;
    }
}
