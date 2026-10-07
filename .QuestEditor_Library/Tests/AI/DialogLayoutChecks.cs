using QuestEditor_Library;

internal static class DialogLayoutChecks
{
    public static void Run()
    {
        DialogNode node = new() { text = "CQF_Check_Text" };
        DialogOption simple = new() { text = "CQF_Check_Simple" };
        DialogOption complex = new() { text = "CQF_Check_Complex" };
        complex.results[0].conditions.Add(new DialogCondition_Chance());
        complex.results.Add(new DialogResult());
        DialogOption empty = new() { text = "CQF_Check_Empty" }; empty.results.Clear();
        node.options.AddRange(new[] { simple, complex, empty });
        CQFDialogNodeLayout layout = new(node, 80f);
        Check(layout.Results.Count == 0 && layout.Options.Count == 3, "dialogue cards contain choice references without embedded result rows");
        CQFDialogNodeLayout choice = new(complex, 80f);
        Check(choice.Results.Count == 2 && choice.Results[complex.results[1]] - choice.Results[complex.results[0]] == CQFDialogNodeLayout.ResultHeight,
            "standalone choices expose one connection row per result");
        Check(new CQFDialogNodeLayout(empty, 80f).Height == 48f + 80f + CQFDialogNodeLayout.ResultHeight + 10f,
            "unconnected choices reserve a readable end area");
        Check(new CQFDialogNodeLayout(node, 1f).BodyHeight == 48f && new CQFDialogNodeLayout(node, 1000f).BodyHeight == 120f, "dialogue text height is bounded independently of choice references");
        Check(node.text == "CQF_Check_Text" && node.options.Count == 3 && complex.results[0].conditions.Count == 1 && complex.results.Count == 2, "compact layout preserves original dialogue content, conditions and branches");
        simple.results[0].actions.Add(new CQFAction_Loop());
        Check(new CQFDialogNodeLayout(simple, 80f).Results.ContainsKey(simple.results[0]), "action-bearing choices retain a selectable output");
    }

    private static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
}
