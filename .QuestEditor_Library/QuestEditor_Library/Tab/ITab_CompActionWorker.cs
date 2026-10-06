using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class ITab_CompActionWorker : ITab
    {
        public ITab_CompActionWorker()
        {
            this.size = new Vector2(500f,500f);
            this.labelKey = "ITab_CompActionWorker";
            this.tutorTag = "CompActionWorker";
        }
        internal object CQFSelectedObject => this.SelObject;
        internal Vector2 CQFSize => this.size;

        public override bool IsVisible => CQFEditorBridge.IsLoaded && DebugSettings.godMode;
        protected override bool StillValid => CQFEditorBridge.IsLoaded && DebugSettings.godMode;
        protected override void FillTab()

        {
            object[] arguments = new object[]
            {
            };
            CQFEditorBridge.Invoke("QuestEditor_Library.ITab_CompActionWorker.FillTab()", this, arguments);
        }
        public Vector2 scrollPos = Vector2.zero;
        public float height =0f;
    }
}
