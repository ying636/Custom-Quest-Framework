using Verse;

namespace QuestEditor_Library
{
    public class MapComponent_CQFLord : MapComponent
    {
        public MapComponent_CQFLord(Map map) : base(map) { }

        public MetaLordManager Manager => Current.Game.GetComponent<GameComponent_MetaLord>().Manager;

        public override void MapRemoved()
        {
            base.MapRemoved();
            this.Manager.NotifyMapRemoved(this.map);
        }
    }
}
