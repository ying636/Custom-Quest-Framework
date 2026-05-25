using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class CaravanActionDef : Def
    {
        public Texture2D Icon 
        {
            get 
            {
                return this.icon;
            }
        }

        public override void PostLoad()
        {
            base.PostLoad();
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                this.icon = ContentFinder<Texture2D>.Get(this.iconPath);
            });
        }
        public string iconPath;
        private Texture2D icon;

        public int CD;
        public List<WorldAction> actions = new List<WorldAction>();
        public List<WorldCondition> conditions = new List<WorldCondition>();
    }
}
