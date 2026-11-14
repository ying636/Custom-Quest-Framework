using RimWorld;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public class CQFDialogTreeWindow : Window
{
    public CQFDialogTreeWindow(string title, Thing interviewer, Thing interviewee, Quest quest, DialogTreeDef tree)
        : this(title, interviewer, interviewee, quest, tree, 0)
    {
    }

    public CQFDialogTreeWindow(string title,Thing interviewer, Thing interviewee,Quest quest
    , DialogTreeDef tree, int startIndex)
    {
        this.title = title;
        this.forcePause = true;
        this.absorbInputAroundWindow = true;
        this.closeOnAccept = false;
        this.closeOnCancel = false;
        this.soundAppear = SoundDefOf.CommsWindow_Open;
        this.soundClose = SoundDefOf.CommsWindow_Close;
        this.interviewer = interviewer;
        this.interviewee = interviewee;
        this.quest = quest;
        this.tree = tree;
        this.tree.ResolveOptions();
        if (this.tree.ActivePresentation is CQFDialogPresentation presentation)
        {
            string[] errors = this.tree.PresentationErrors().ToArray();
            if (errors.Length > 0) throw new InvalidOperationException(string.Join("\n", errors));
            this.presentationRenderer = presentation.CreateRenderer();
            this.doWindowBackground = this.presentationRenderer.DrawWindowBackground;
            this.preventCameraMotion = true;
        }
        this.GoToNode(startIndex);
    }

    protected override float Margin => this.presentationRenderer?.Margin ?? 15f;
    public override Vector2 InitialSize => this.presentationRenderer?.GetInitialSize(new Vector2(UI.screenWidth, UI.screenHeight)) ?? new Vector2(620 + Margin, 620 + Margin);
    public float CharacterWidth => 0f;
    public int CurrentNodeIndex { get; private set; } = -1;
    public bool IsClosed { get; private set; }
    public IReadOnlyList<IDialogElement> CurrentElements => this.currentElements;
    public override void DoWindowContents(Rect inRect)
    {
        using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width, inRect.height);
        if (this.presentationRenderer != null)
        {
            if (this.presentationOptionsPending)
            {
                this.options.Clear();
                this.options.AddRange(this.nextOptions);
                this.nextOptions.Clear();
                this.presentationOptionsPending = false;
            }
            this.presentationRenderer.Draw(inRect, new CQFDialogFrame(this.tree, this.curNode, this.title,
                this.currentElements, this.options, this.interviewer, this.interviewee, () => this.Close()));
            return;
        }
        if (this.title != null)
        {
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(CharacterWidth, 0f, inRect.width - CharacterWidth*2f, 40f);
            Widgets.DrawBoxSolid(titleRect, CQFUIStyle.Header);
            CQFUIStyle.DrawBox(titleRect);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(titleRect, this.title);
            Text.Anchor = TextAnchor.UpperLeft;
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
        Widgets.DrawBoxSolid(dialogRect, CQFUIStyle.Panel);
        CQFUIStyle.DrawBox(dialogRect);
        Rect curRect = new Rect(16f, 0f, dialogRect.width - 52f, dialogRect.height);
        float y = 15f;
        Widgets.BeginScrollView(dialogRect,ref pos,new Rect(0,0,dialogRect.width - 16f,Mathf.Max(dialogRect.height, height)));
        using CQFUIScope contentScope = new CQFUIScope(curRect.width);
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
            this.CurrentNodeIndex = index;
            this.curNode = node;
            this.nextOptions.Clear();
            this.currentElements = this.curNode.Get(interviewer,interviewee,tree,this.quest);
            this.elements.AddRange(this.currentElements);
            this.presentationOptionsPending = true;
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
            this.NodeChanged?.Invoke(index);
        }
        else
        {
            Log.Error($"CQF dialog {this.tree.defName} has no node {index}.");
        }
    }

    public override void PostClose()
    {
        base.PostClose();
        if (!this.IsClosed)
        {
            this.IsClosed = true;
            try { this.Closed?.Invoke(); }
            finally { this.presentationRenderer?.Dispose(); }
        }
    }

    public Vector2 pos;
    public float height;
    public List<IDialogElement> elements = new List<IDialogElement>();
    public List<DialogElement_Option> options = new List<DialogElement_Option>();
    public List<DialogElement_Option> nextOptions = new List<DialogElement_Option>();
    public DialogNode curNode;
    public Thing interviewee;
    public Thing interviewer;
    public event Action<int>? NodeChanged;
    public event Action? Closed;
    public DialogTreeDef tree;

    private Quest quest;
    private string title;
    private readonly CQFDialogRenderer? presentationRenderer;
    private List<IDialogElement> currentElements = new();
    private bool presentationOptionsPending;


    private static readonly Rect DefaultTexCoords = new Rect(0f, 0f, 1f, 1f);
    private static readonly Rect LinkedTexCoords = new Rect(0f, 0.5f, 0.25f, 0.25f);
}
