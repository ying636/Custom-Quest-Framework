using QuestEditor_Library;
using RimWorld;
using RSH;
using UnityEngine;
using Verse;

namespace CQF.DomainRuntimeChecks;

public sealed class DomainChecksDriver : MonoBehaviour
{
    private string ResultPath => Path.Combine(GenFilePaths.SaveDataFolderPath, "DomainChecks.txt");

    private void Update()
    {
        if (finished || LongEventHandler.AnyEventNowOrWaiting) return;
        try
        {
            if (Time.realtimeSinceStartup > 240f) throw new TimeoutException("Domain runtime stage " + stage);
            if (stage == 0)
            {
                if (Current.Game?.CurrentMap == null) return;
                source = Find.CurrentMap;
                Check(Current.Game.components.Count(component => component is GameComponent_MapManager) == 1,
                    "one map manager handles both domains and hibernation");
                caster = source.mapPawns.FreeColonistsSpawned.First();
                casterId = caster.thingIDNumber;
                casterPosition = caster.Position;
                caster.drafter.Drafted = true;
                Pawn enemy = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfAncientsHostile);
                GenSpawn.Spawn(enemy, source.Center, source);
                enemyId = enemy.thingIDNumber;
                realm = DomainWorker_BloodSea.Create(caster, new[] { enemy });
                MapComponent_Domain domain = realm.GetComponent<MapComponent_Domain>();
                Check(caster.Map == realm && enemy.Map == realm && caster.Drafted, "participants enter with drafted state");
                Check(domain.Definition == DomainWorker_BloodSea.BloodSeaDefinition && domain.MaximumHealth == 100000f, "blood sea uses CQF domain definition");
                Check(domain.Anchor?.Domain == domain, "external blood sea anchor links to generic domain");
                Check(GameComponent_MapManager.Instance!.DomainMaps.Contains(realm), "unified manager registers active domains");
                Check(!realm.terrainGrid.TerrainAt(realm.Center).temporary && realm.terrainGrid.BaseTerrainAt(realm.Center).defName == "RSH_BloodSeaFloor", "blood sea has permanent base terrain");
                domain.DrainHealth(123f);
                Check(domain.Health == 99877f && domain.Anchor!.HitPoints == 99877, "domain health synchronizes external anchor");
                Thing item = ThingMaker.MakeThing(ThingDefOf.Steel);
                item.stackCount = 7;
                GenSpawn.Spawn(item, realm.Center, realm);
                itemId = item.thingIDNumber;
                enemy.Kill(null);
                Check(enemy.Corpse?.Map == realm, "participant corpse remains in domain until return");
                GameDataSaveLoader.SaveGame("CQFDomainChecks");
                stage = 1;
                GameDataSaveLoader.LoadGame("CQFDomainChecks");
            }
            else if (stage == 1)
            {
                realm = GameComponent_MapManager.Instance!.GetMaps(DomainWorker_BloodSea.BloodSeaDefinition)
                    .FirstOrDefault(map => map.GetComponent<MapComponent_Domain>().Active);
                if (realm == null) return;
                MapComponent_Domain domain = realm.GetComponent<MapComponent_Domain>();
                caster = domain.Caster!;
                source = domain.SourceMap!;
                Check(caster!.thingIDNumber == casterId && domain.Health == 99877f, "active domain caster and health survive save/load");
                Check(domain.Worker is DomainWorker_BloodSea && domain.Participants.Count == 2 && domain.Anchor?.Domain == domain,
                    "worker, participants and anchor survive save/load");
                Check(caster.health.hediffSet.hediffs.OfType<Hediff_DomainProtection>().Any(hediff => hediff.Domain == domain), "domain protection survives save/load");
                Current.Game.CurrentMap = source;
                domain.RequestReturn("domain runtime checks");
                stage = 2;
            }
            else if (stage == 2)
            {
                if (caster?.Map != source || GameComponent_MapManager.Instance?.IsHibernating(realm!) != true) return;
                Check(caster!.Position == casterPosition && caster.Drafted, "caster returns to original position and draft state");
                Check(source!.listerThings.AllThings.OfType<Corpse>().Any(corpse => corpse.InnerPawn.thingIDNumber == enemyId), "participant corpse returns to source");
                Check(source.listerThings.AllThings.Any(thing => thing.thingIDNumber == itemId), "domain loot returns to source");
                Check(!caster.health.hediffSet.hediffs.OfType<Hediff_DomainProtection>().Any(), "normal exit removes shared damage protection");
                Check(caster.abilities.GetAbility(DefDatabase<AbilityDef>.GetNamed("RSH_BloodSeaTraverse")) == null, "normal exit removes blood sea temporary abilities");
                Map previous = realm!;
                realm = DomainWorker_BloodSea.Create(caster, Array.Empty<Pawn>());
                Check(realm == previous && !GameComponent_MapManager.Instance!.IsHibernating(realm), "second opening reuses and activates the same map");
                MapComponent_Domain domain = realm.GetComponent<MapComponent_Domain>();
                Check(domain.Health == domain.MaximumHealth, "reused domain begins at maximum health");
                Current.Game.CurrentMap = source;
                domain.DrainHealth(domain.MaximumHealth);
                Check(caster.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("RSH_BloodSeaExhaustion")), "blood sea retains its collapse penalty");
                stage = 3;
            }
            else if (stage == 3)
            {
                if (caster?.Map != source || GameComponent_MapManager.Instance?.IsHibernating(realm!) != true) return;
                Check(!realm!.GetComponent<MapComponent_Domain>().Active, "collapsed domain returns occupants and hibernates");
                Map removed = realm;
                PocketMapUtility.DestroyPocketMap(removed);
                Check(removed.Disposed && !GameComponent_MapManager.Instance!.DomainMaps.Contains(removed),
                    "destroyed domain leaves the unified map pool");
                stage = 4;
            }
            else if (stage == 4)
            {
                GameComponent_MapManager manager = GameComponent_MapManager.Instance!;
                Map? replacement = manager.GetMaps(DomainWorker_BloodSea.BloodSeaDefinition).FirstOrDefault();
                if (replacement == null || !manager.IsHibernating(replacement)) return;
                Check(replacement != realm && replacement.GetComponent<MapComponent_Domain>().Available,
                    "removing the last domain schedules one replacement preload and hibernates it");
                Finish(true);
            }
        }
        catch (Exception error)
        {
            File.AppendAllText(ResultPath, "FAIL " + error + "\n");
            Log.Error("CQF domain runtime checks: " + error);
            Finish(false);
        }
    }

    private void Check(bool passed, string name)
    {
        if (!passed) throw new InvalidOperationException(name);
        File.AppendAllText(ResultPath, "PASS " + name + "\n");
    }

    private void Finish(bool success)
    {
        finished = true;
        File.AppendAllText(ResultPath, success ? "COMPLETE\n" : "FAILED\n");
        Application.Quit(success ? 0 : 1);
    }

    private Map? source;
    private Map? realm;
    private Pawn? caster;
    private IntVec3 casterPosition;
    private int casterId;
    private int enemyId;
    private int itemId;
    private int stage;
    private bool finished;
}
