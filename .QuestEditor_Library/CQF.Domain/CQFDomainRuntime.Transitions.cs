using Verse;

namespace QuestEditor_Library;

public sealed partial class CQFDomainRuntime
{
    public DomainTransition? Transition => transition;

    public void QueueOpening(DomainOpening opening)
    {
        if (opening.Fusion?.Active != true && !openings.Contains(opening)) openings.Add(opening);
    }

    public void SetTransition(DomainTransition? value)
    {
        transition?.Dispose();
        transition = value;
    }

    public void FinishTransitionFor(Map map)
    {
        transition?.DisposeIfMap(map);
        if (transition?.Finished == true) SetTransition(null);
    }

    private void UpdateDomainTransition()
    {
        DomainTransition? current = transition;
        if (current == null) return;
#if RIMWORLD_1_7
        current.Update();
#endif
        if (ReferenceEquals(transition, current) && current.Finished) SetTransition(null);
    }

    private void ProcessDomainOpenings()
    {
        if (transition != null) return;
        if (CQFPresentationGate.For(game).IsBusy) return;
        while (openings.Count > 0)
        {
            DomainOpening opening = openings[0];
            openings.RemoveAt(0);
            if (opening.Destroyed || !opening.Spawned) continue;
            if (opening is DomainOpening_Fusion fusion && !fusion.Prepared)
            {
                LongEventHandler.QueueLongEvent(() =>
                {
                    try
                    {
                        PrepareFusion(fusion);
                        LongEventHandler.ExecuteWhenFinished(() => { if (Current.Game == game) QueueOpening(fusion); });
                    }
                    catch (Exception error)
                    {
                        Log.Error("[CQF] Fused domain map preparation failed: " + error);
                        if (!fusion.Destroyed) fusion.Destroy();
                    }
                }, "GeneratingMap", false, null);
                return;
            }
            if (opening is DomainOpening_Fusion)
            {
                opening.CompleteOpening();
                return;
            }
            bool counter = opening.Caster?.MapHeld?.GetComponent<MapComponent_Domain>().HasActiveDomain == true;
            if (counter ? HasTerrainTemplate(opening.Definition)
                && HasAvailableBattlefield(opening.Map.GetComponent<MapComponent_Domain>(), opening.Definition, opening.Caster!) : HasAvailable(opening.Definition))
            {
#if RIMWORLD_1_7
                if (counter && Find.CurrentMap == opening.Map && RimWorld.Planet.WorldRendererUtility.DrawingMap
                    && UnityEngine.Screen.width > 0 && UnityEngine.Screen.height > 0)
                {
                    DomainCounterTransition counterTransition = new(opening);
                    SetTransition(counterTransition);
                    counterTransition.BindOpening(opening);
                }
                else
#endif
                    opening.CompleteOpening();
            }
            else LongEventHandler.QueueLongEvent(() =>
            {
                try
                {
                    if (Current.Game != game || opening.Destroyed || !opening.Active) return;
                    if (counter)
                    {
                        GetMapTemplate(opening.Definition, opening.Map);
                        PrepareBattlefield(opening.Map.GetComponent<MapComponent_Domain>(), opening.Definition, opening.Caster!);
                    }
                    else if (!HasAvailable(opening.Definition)) host.Hibernate(GenerateDomain(opening.Definition, opening.Map));
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        if (Current.Game == game && !opening.Destroyed && opening.Active) QueueOpening(opening);
                    });
                }
                catch (Exception error)
                {
                    Log.Error("[CQF] Domain opening preparation failed: " + error);
                    if (!opening.Destroyed) opening.Destroy();
                }
            }, "GeneratingMap", false, null);
            return;
        }
    }

    private readonly List<DomainOpening> openings = new();
    private DomainTransition? transition;
}
