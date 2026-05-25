using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace QuestEditor_Library
{
    public class MetaLordEventDef : Def
    {
        public List<Event> events = new List<Event>();
    }
    public class Event 
    {
        public EvenetTrigger trigger;
        public EventAction action;
    }
    public abstract class EvenetTrigger 
    {
        public abstract bool Active(MetaLord lord, LordEventSignal signal);
    }
    public abstract class EventAction 
    {
        public abstract void Do(MetaLord lord);
    }
    public struct LordEventSignal 
    {
        public LordEventSignal(LordEventSignalType type) 
        {
            this.type = type;
        }
        public LordEventSignalType type;
    }
    public enum LordEventSignalType
    {
        Tick
    }
}
