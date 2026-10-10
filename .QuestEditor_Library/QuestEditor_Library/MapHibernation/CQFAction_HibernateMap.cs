using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace QuestEditor_Library
{
    public sealed class CQFAction_HibernateMap : CQFAction_Target
    {
        public override CQFActionCategory ActionCategory => CQFActionCategory.MapAction;

        public override void RealWork(Dictionary<string, TargetInfo> targets, Quest quest)
        {
            GameComponent_MapHibernation component = GameComponent_MapHibernation.Instance
                ?? throw new InvalidOperationException("[CQF] Map hibernation component is unavailable.");
            HashSet<Map> maps = new HashSet<Map>();
            foreach (KeyValuePair<string, TargetInfo> target in targets)
            {
                if (target.Value.Map == null)
                {
                    throw new InvalidOperationException("[CQF] HibernateMap target has no map: " + target.Key);
                }
                maps.Add(target.Value.Map);
            }
            if (maps.Count == 0)
            {
                throw new InvalidOperationException("[CQF] HibernateMap requires at least one map target.");
            }
            foreach (Map map in maps)
            {
                component.Hibernate(map);
            }
        }
    }
}
