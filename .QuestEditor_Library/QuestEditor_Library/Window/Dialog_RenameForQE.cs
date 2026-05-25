using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_RenameForQE : Window
    {
        public Dialog_RenameForQE()
        {
        }
        public Dialog_RenameForQE(Action<string> rename,string tile = "Rename") 
        {
            this.rename = rename;
			this.optionalTitle = tile.Translate();

		}
        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(280f, 205f);
            }
		}
		protected virtual int MaxNameLength
		{
			get
			{
				return 28;
			}
		}
		public override void DoWindowContents(Rect inRect)
		{
			Text.Font = GameFont.Small;
			bool flag = false;
			if (UnityEngine.Event.current.type == EventType.KeyDown && (UnityEngine.Event.current.keyCode == KeyCode.Return || UnityEngine.Event.current.keyCode == KeyCode.KeypadEnter))
			{
				flag = true;
                UnityEngine.Event.current.Use();
			}
			GUI.SetNextControlName("RenameField");
			string text = Widgets.TextField(new Rect(0f,10f, inRect.width, 35f), this.curName);
			if (text.Length < this.MaxNameLength)
			{
				this.curName = text;
			}
			if (!this.focusedRenameField)
			{
				UI.FocusControl("RenameField", this);
				this.focusedRenameField = true;
			}
			if (Widgets.ButtonText(new Rect(15f, inRect.height - 35f - 10f, inRect.width - 15f - 15f, 35f), "OK", true, true, true, null) || flag)
			{
				AcceptanceReport acceptanceReport = this.NameIsValid(this.curName);
				if (!acceptanceReport.Accepted)
				{
					if (acceptanceReport.Reason.NullOrEmpty())
					{
						Messages.Message("NameIsInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
						return;
					}
					Messages.Message(acceptanceReport.Reason, MessageTypeDefOf.RejectInput, false);
					return;
				}
				else
				{
					this.SetName(this.curName);
					Find.WindowStack.TryRemove(this, true);
				}
			}
		}
		protected virtual AcceptanceReport NameIsValid(string name)
		{
			return name.Length != 0;
		}
		protected void SetName(string name)
		{
			this.rename(name);
		}
        public Action<string> rename; 
		protected string curName;
		private bool focusedRenameField;
	}
}
