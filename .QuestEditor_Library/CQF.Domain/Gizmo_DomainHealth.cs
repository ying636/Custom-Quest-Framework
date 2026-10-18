using Verse;

namespace QuestEditor_Library;

public sealed class Gizmo_DomainHealth : Gizmo_Slider
{
    public Gizmo_DomainHealth(MapComponent_Domain domain)
    {
        Domain = domain;
    }

    public MapComponent_Domain Domain { get; }
    public override bool Visible => Domain.Active;
    protected override float Target { get => Domain.HealthFraction; set { } }
    protected override float ValuePercent => Domain.HealthFraction;
    protected override string Title => Domain.Definition.LabelCap;
    protected override string BarLabel => Domain.Health.ToString("N0") + " / " + Domain.MaximumHealth.ToString("N0");
    protected override bool DraggingBar { get; set; }

    public override bool GroupsWith(Gizmo other) => other is Gizmo_DomainHealth health && ReferenceEquals(health.Domain, Domain);

    protected override string GetTooltip() => Domain.Definition.LabelCap + "\n" + BarLabel;
}
