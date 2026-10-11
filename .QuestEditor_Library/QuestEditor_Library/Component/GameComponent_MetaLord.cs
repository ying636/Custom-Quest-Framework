using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public sealed class GameComponent_MetaLord : GameComponent, ISignalReceiver
    {
        public GameComponent_MetaLord(Game game) { }

        public MetaLordManager Manager => this.manager;

        public override void GameComponentTick()
        {
            this.manager.Tick();
        }

        public override void ExposeData()
        {
            Scribe_Deep.Look(ref this.manager, "CQF_MetaLordManager");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                this.manager ??= new MetaLordManager();
        }

        public override void LoadedGame()
        {
            this.manager.Rebuild();
            Find.SignalManager.RegisterReceiver(this);
        }

        public override void StartedNewGame()
        {
            Find.SignalManager.RegisterReceiver(this);
        }

        public void Notify_SignalReceived(Signal signal)
        {
            signal.args.TryGetArg("SUBJECT", out NamedArgument subject);
            foreach (MetaLord owner in new System.Collections.Generic.List<MetaLord>(this.manager.Lords))
            {
                try
                {
                    owner.Trigger(new LordEventSignal(LordEventSignalType.External)
                    { tag = signal.tag, pawn = subject.arg as Pawn });
                }
                catch (System.Exception error)
                {
                    Log.Error("[CQF] MetaLord external signal failed: " + owner.name + "/" + signal.tag + "\n" + error);
                }
            }
        }

        private MetaLordManager manager = new MetaLordManager();
    }
}
