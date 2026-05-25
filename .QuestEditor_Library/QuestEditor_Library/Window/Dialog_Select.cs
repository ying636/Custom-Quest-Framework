using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library
{
    public class Dialog_Select<T> : Window
    {
        public Dialog_Select(List<T> ts, Func<T, Texture2D> getTexture, Func<T, string> getText,string title,Action<T> acceptAction,Func<T, Color> getColor = null
            ,Action<T,Rect> drawAction = null, Func<T, string> getTip = null,Func<T,int> getPriority = null,
            List<ExtraOption> extraOptions = null,Func<T, string> getRawText = null)
        {
            this.ts = ts;
            this.getTexture = getTexture;
            this.getText = getText;
            this.acceptAction = acceptAction;
            this.title = title;
            this.getColor = getColor;
            this.drawAction = drawAction;
            this.getTip = getTip;
            this.getPriority = getPriority;
            this.extraOptions = extraOptions;
            this.getRawText = getRawText;
            this.forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
            this.closeOnAccept = false;
            this.closeOnCancel = false; 
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = false;
            this.doCloseX = true;
        }
        public void UpdateTs()
        {
            List<T> items = new List<T>();
            if (this.terms == "")
            {
                items = this.ts;
            }
            else
            {
                foreach (T t in this.ts)
                {
                    string text = this.getText(t);
                    bool canShow = !string.IsNullOrEmpty(text) &&
                                   text.IndexOf(terms, StringComparison.OrdinalIgnoreCase) >= 0;

                    if (this.getRawText != null)
                    {
                        var raw = this.getRawText(t);
                        if (!string.IsNullOrEmpty(raw) &&
                            raw.IndexOf(this.terms, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            canShow = true;
                        }
                    }
                    if (canShow)
                    {
                        items.Add(t);
                    }
                }
            }     
            if (this.getPriority != null) 
            {
                items.SortBy(this.getPriority);
            }
            this.items = items;
        }
        public override void DoWindowContents(Rect inRect)
        {         
            List<T> items = this.items;
            if (!this.items.Any()) 
            {
                this.UpdateTs();
            } 
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f,this.InitialSize.x,35f), this.title);
            Text.Font = GameFont.Small;
            string t0 = Widgets.TextField(new Rect(0f, this.Margin + 17f,300f, 25f),this.terms);
            if (t0 != terms) 
            {
                this.terms = t0;
                this.UpdateTs();
            }
            float y = this.Margin + 43f;
            float x = 0f;
            Rect rect;
            Widgets.BeginScrollView(new Rect(0f,y,this.InitialSize.x,this.InitialSize.y - y),
                ref this.pos,new Rect(0f,0f,this.InitialSize.x,
                    height - y));
            y = 0f;
            foreach (T t in items)
            {
                rect = new Rect(x, y, this.getTexture != null ? 30f : 750f, 25f);
                if (this.getTexture != null)
                {
                    if (this.getColor != null && this.getColor(t) is Color color)
                    {
                        GUI.color = color;
                    }
                    if (this.drawAction != null)
                    {
                        this.drawAction(t, rect);
                        if (Widgets.ButtonInvisible(rect))
                        {
                            this.acceptAction(t);
                            this.Close();
                        }
                    }
                    else if (this.getTexture(t) is Texture2D tex)
                    {
                        if (Widgets.ButtonImage(rect, tex, GUI.color))
                        {
                            this.acceptAction(t);
                            this.Close();
                        }
                    }
                    else 
                    {
                        if (Widgets.ButtonInvisible(rect))
                        {
                            this.acceptAction(t);
                            this.Close();
                        }
                    }
                    GUI.color = Color.white;
                    x += 35f;
                    if (x + 70f > this.InitialSize.x)
                    {
                        x = 0f;
                        y += 30f;
                    }
                }
                else
                {
                    var label = this.getText(t);
                    rect.height = Text.CalcHeight(label, rect.width);
                    if (Widgets.ButtonText(rect,label , false,true,this.getColor == null || this.getColor(t) == null ? Widgets.NormalOptionColor : this.getColor(t)))
                    {
                        this.acceptAction(t);
                        this.Close();
                    }
                    y += rect.height + 5f;
                }
                StringBuilder tip = new StringBuilder();
                tip.AppendLine(this.getText(t));
                if (this.getTip != null)
                {
                    tip.AppendLine(this.getTip(t));
                }
                TooltipHandler.TipRegion(rect, tip.ToString().Trim());
            }
            if (this.extraOptions != null) 
            {
                foreach (var option in this.extraOptions)
                {
                    rect = new Rect(x, y, this.getTexture != null ? 30f : 750f, 25f);
                    if (Widgets.ButtonText(rect, option.text, false, true,option.color))
                    {
                        option.action();
                        this.Close();
                    }
                    y += 30f;
                    StringBuilder tip = new StringBuilder();
                    tip.AppendLine(option.text);
                    if (option.tip != null)
                    {
                        tip.AppendLine(option.tip);
                    }
                    TooltipHandler.TipRegion(rect, tip.ToString().Trim());
                }
            }
            Widgets.EndScrollView();
            height = y;
        }

        public string title;
        public string terms = "";
        public List<T> items = new List<T>();
        public List<T> ts;
        public Action<T> acceptAction;
        public Func<T, Texture2D> getTexture;
        public Func<T, Color> getColor;
        public Func<T, string> getText;
        public Func<T, string>  getRawText;
        public Func<T, string> getTip;
        public List<ExtraOption> extraOptions = new List<ExtraOption>();
        Func<T, int> getPriority = null;
        public Action<T, Rect> drawAction;
        public Vector2 pos = Vector2.zero;

        public float height;
    }

    public class ExtraOption 
    {
        public ExtraOption(string text, string tip , Action action, Texture2D icon = null
            ,Color? color = null)
        {
            this.text = text;
            this.tip = tip;
            if (icon != null)
            {
                this.color = color.Value;
            }
            this.icon = icon;
            this.action = action;
        }
        public string text;
        public string tip;
        public Color color = Color.white;
        public Texture2D icon;
        public Action action;
    }
}
