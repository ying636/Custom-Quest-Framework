using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class CQFDialogTreeWindow : Window
{
    public CQFDialogTreeWindow(string title,Thing interviewee, Thing interviewer,Quest quest
    , DialogTreeDef tree)
    {
        this.title = title; 
        this.forcePause = true;
        this.absorbInputAroundWindow = true;
        this.closeOnAccept = false;
        this.closeOnCancel = false; 
        this.soundAppear = SoundDefOf.CommsWindow_Open;
        this.soundClose = SoundDefOf.CommsWindow_Close; 
        this.interviewee = interviewee;
        this.interviewer = interviewer;
        this.quest = quest;
        this.tree = tree;
        this.GoToNode(0);
    }

    protected override float Margin => 15f;
    public override Vector2 InitialSize => new Vector2(620 + Margin, 620 + Margin);
    public float CharacterWidth => 0f;
    public override void DoWindowContents(Rect inRect)
    {
        if (this.title != null)
        {
            Text.Font = GameFont.Medium; 
            Rect titleRect = new Rect(CharacterWidth, 0f, inRect.width - CharacterWidth*2f, 40f);
            Widgets.DrawBoxSolid(titleRect,Color.black);
            Widgets.DrawBox(titleRect);
            titleRect.y += 5f;
            titleRect.x += this.InitialSize.x/2f - (CharacterWidth) - this.title.GetWidthCached() / 2f;
            Widgets.Label(titleRect, this.title);
            Text.Font = GameFont.Small; 
        }
        // if (this.interviewee != null)
        // {
        //     this.DrawCharacter(0f,this.interviewee);
        // } 
        // if (this.interviewer != null)
        // {
        //     this.DrawCharacter(rightLine,this.interviewer);
        // }
  
        Rect dialogRect = new Rect(CharacterWidth,40f,inRect.width - CharacterWidth * 2f,inRect.height - 40f );
        Widgets.DrawBox(dialogRect);
        Widgets.DrawTitleBG(dialogRect);
        Rect curRect = new Rect(20f, 20f,dialogRect.width - 40f,dialogRect.height);
        float y = 15f;
        Widgets.BeginScrollView(dialogRect,ref pos,new Rect(0,0,dialogRect.width - 16f,height));
        foreach (var dialogElement in this.elements)
        {
            dialogElement.Draw(ref y,curRect);
        }

        y += 20f;
        foreach (var dialogElement in this.options)
        {
            dialogElement.Draw(ref y,curRect);
        }

        if (!nextOptions.NullOrEmpty())
        {
            this.options.Clear();
            this.options.AddRange(nextOptions);
            this.nextOptions.Clear();
        }

        y += 10f;
        this.height = y;
        Widgets.EndScrollView();
    }

    public void DrawCharacter(float x,Thing thing)
    {
        Widgets.ThingIcon(new Rect(x,220f,CharacterWidth * (3f/4f),CharacterWidth),thing);
        string name = thing.Label;
        if (thing is Pawn pawn)
        {
            name = pawn.Name.ToStringShort;
        }
        Widgets.Label(new Rect(x + (CharacterWidth - name.GetWidthCached())/2f
            ,220f+ CharacterWidth + 5f,CharacterWidth,25f),name);
    } 
    public void GoToNode(int index)
    {
        if (this.tree.nodeMoulds.TryGetValue(index,out var node))
        {
            this.curNode = node;
            this.nextOptions.Clear();
            this.elements.Add(this.curNode.Get(interviewer,interviewee,tree,this.quest));
            foreach (var curNodeOption in this.curNode.options)
            {
                foreach (var op in curNodeOption.GetDEOptions(interviewer,interviewee,tree,this.quest))
                {
                    if (op.nextIndex != null)
                    {
                        op.action += () =>
                        {
                            this.elements.Add(new DialogElement_Text("----" + op.text));
                            this.GoToNode(op.nextIndex.Value);
                        };   
                    }
                    else
                    {
                        op.action += () =>
                        {
                            this.Close();
                        };   
                    }
                    this.nextOptions.Add(op);
                }
            }
        }
    }

    public override void Close(bool doCloseSound = true)
    {
        base.Close(doCloseSound);
    }

    public Vector2 pos;
    public float height;
    public List<IDialogElement> elements = new List<IDialogElement>();
    public List<DialogElement_Option> options = new List<DialogElement_Option>();
    public List<DialogElement_Option> nextOptions = new List<DialogElement_Option>();
    public DialogNode curNode;
    public Thing interviewee;
    public Thing interviewer;
    private Quest quest;
    public DialogTreeDef tree;

    private string title;
    
    
    private static readonly Rect DefaultTexCoords = new Rect(0f, 0f, 1f, 1f);
    private static readonly Rect LinkedTexCoords = new Rect(0f, 0.5f, 0.25f, 0.25f);
}