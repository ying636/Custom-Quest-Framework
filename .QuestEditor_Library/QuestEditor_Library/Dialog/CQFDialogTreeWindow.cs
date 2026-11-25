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
    public override Vector2 InitialSize => this.presentationRenderer?.GetInitialSize(new Vector2(UI.screenWidth, UI.screenHeight))
        ?? new Vector2(Mathf.Min(688f, UI.screenWidth - 24f), Mathf.Min(616f, UI.screenHeight - 24f));
    public float CharacterWidth => 0f;
    public int CurrentNodeIndex { get; private set; } = -1;
    public bool IsClosed { get; private set; }
    public IReadOnlyList<IDialogElement> CurrentElements => this.currentElements;
    public override void DoWindowContents(Rect inRect)
    {
        using CQFUIScope cqfUIScope = new CQFUIScope(inRect.width, inRect.height);
        if (this.optionsPending)
        {
            this.options.Clear();
            this.options.AddRange(this.nextOptions);
            this.nextOptions.Clear();
            this.optionsPending = false;
        }

        if (this.presentationRenderer != null)
        {
            this.presentationRenderer.Draw(inRect, new CQFDialogFrame(this.tree, this.curNode, this.title,
                this.currentElements, this.options, this.interviewer, this.interviewee, () => this.Close()));
            return;
        }
        Text.Font = GameFont.Small;
        float bodyTop = 0f;
        if (!string.IsNullOrWhiteSpace(this.title))
        {
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 37f);
            Widgets.DrawBoxSolid(titleRect, CQFUIStyle.Header);
            CQFUIStyle.DrawBox(titleRect);
            Text.Anchor = TextAnchor.MiddleCenter;
            Rect titleContent = titleRect.ContractedBy(12f, 0f);
            if (Widgets.LabelFit(titleContent, this.title)) TooltipHandler.TipRegion(titleContent, this.title);
            Text.Anchor = TextAnchor.UpperLeft;
            bodyTop = titleRect.height + 8f;
        }
        float bodyHeight = inRect.height - bodyTop;
        float choiceWidth = Mathf.Max(22f, inRect.width - 36f);
        float choicesHeight = this.options.Count == 0 ? 42f : this.options.Sum(option => option.HeightForWidth(choiceWidth)) - 8f;
        float choicePanelHeight = Mathf.Min(choicesHeight + 20f, bodyHeight * .46f);
        Rect choices = new Rect(inRect.x, inRect.yMax - choicePanelHeight, inRect.width, choicePanelHeight);
        Rect history = new Rect(inRect.x, inRect.y + bodyTop, inRect.width, bodyHeight - choicePanelHeight - 8f);
        this.DrawClassicHistory(history);
        this.DrawClassicOptions(choices, choiceWidth, choicesHeight);
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
            this.followLatest = this.CurrentNodeIndex >= 0;
            this.choiceScroll = Vector2.zero;
            this.latestElement = this.elements.Count;
            this.CurrentNodeIndex = index;
            this.curNode = node;
            this.nextOptions.Clear();
            this.currentElements = this.curNode.Get(interviewer,interviewee,tree,this.quest);
            this.elements.AddRange(this.currentElements);
            this.optionsPending = true;
            foreach (var curNodeOption in this.curNode.options)
            {
                foreach (var op in curNodeOption.GetDEOptions(interviewer,interviewee,tree,this.quest))
                {
                    if (op.disabled && curNodeOption.hideWhenDisabled) continue;
                    if (curNodeOption.hideFailReason) op.disableReason = string.Empty;

                    if (op.nextIndex != null)
                    {
                        op.action += () =>
                        {
                            this.elements.Add(new DialogElement_Text(("› " + op.text).Colorize(CQFUIStyle.Muted)));
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

    private void DrawClassicHistory(Rect rect)
    {
        CQFUIStyle.DrawMenuSection(rect);
        Rect scroll = rect.ContractedBy(10f);
        float width = Mathf.Max(22f, scroll.width - 16f);
        float latestY = 0f;
        float y = 0f;
        Widgets.BeginScrollView(scroll, ref this.pos, new Rect(0f, 0f, width, Mathf.Max(scroll.height, this.height)));
        try
        {
            using CQFUIScope scope = new CQFUIScope(width, scroll.height);
            var content = new Rect(0f, 0f, width, scroll.height);
            for (int index = 0; index < this.elements.Count; index++)
            {
                if (index == this.latestElement) latestY = y;
                this.elements[index].Draw(ref y, content);
                y += 7f;
            }
        }
        finally { Widgets.EndScrollView(); }
        this.height = y;
        if (this.followLatest)
        {
            this.pos.y = Mathf.Clamp(latestY, 0f, Mathf.Max(0f, y - scroll.height));
            this.followLatest = false;
        }
    }

    private void DrawClassicOptions(Rect rect, float width, float height)
    {
        CQFUIStyle.DrawMenuSection(rect);
        Rect scroll = rect.ContractedBy(10f);
        Widgets.BeginScrollView(scroll, ref this.choiceScroll, new Rect(0f, 0f, width, Mathf.Max(scroll.height, height)));
        try
        {
            float y = 0f;
            Rect content = new Rect(0f, 0f, width, scroll.height);
            foreach (DialogElement_Option option in this.options) option.Draw(ref y, content);
            if (this.options.Count == 0 && CQFUIStyle.ButtonText(new Rect(0f, 0f, width, 42f), "Close".Translate())) this.Close();
        }
        finally { Widgets.EndScrollView(); }
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
    private bool optionsPending;
    private bool followLatest;
    private int latestElement;
    private Vector2 choiceScroll;


    private static readonly Rect DefaultTexCoords = new Rect(0f, 0f, 1f, 1f);
    private static readonly Rect LinkedTexCoords = new Rect(0f, 0.5f, 0.25f, 0.25f);
}
