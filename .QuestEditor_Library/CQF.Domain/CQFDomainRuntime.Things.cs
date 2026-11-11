using System.Reflection;
using UnityEngine;
using Verse;

namespace QuestEditor_Library;

public sealed partial class CQFDomainRuntime
{
    private void RelocateBattlefieldThing(Thing thing, Map destination, IntVec3 cell, Rot4 rotation)
    {
        Map previous = thing.Map;
        IntVec3 offset = cell - thing.Position;
        Vector3 delta = offset.ToVector3();
        Vector3 exact = thing is Mote mote ? mote.exactPosition : default;
        Projectile? projectile = thing as Projectile;
        Vector3 origin = default, target = default;
        if (projectile != null)
        {
            origin = (Vector3)ProjectileOrigin.GetValue(projectile)!;
            target = (Vector3)ProjectileDestination.GetValue(projectile)!;
        }
        thing.DeSpawn();
        GenSpawn.Spawn(thing, cell, destination, rotation, respawningAfterLoad: true);
        if (thing is Mote movedMote) movedMote.exactPosition = exact + delta;
        if (projectile != null)
        {
            ProjectileOrigin.SetValue(projectile, origin + delta);
            ProjectileDestination.SetValue(projectile, target + delta);
            if (projectile.usedTarget.IsValid && !projectile.usedTarget.HasThing) projectile.usedTarget = new LocalTargetInfo(projectile.usedTarget.Cell + offset);
            if (projectile.intendedTarget.IsValid && !projectile.intendedTarget.HasThing) projectile.intendedTarget = new LocalTargetInfo(projectile.intendedTarget.Cell + offset);
        }
        if (thing is IDomainMapTransfer participant) participant.DomainMapTransferred(previous, offset);
    }

    private static readonly FieldInfo ProjectileOrigin = typeof(Projectile).GetField("origin", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(Projectile).FullName, "origin");
    private static readonly FieldInfo ProjectileDestination = typeof(Projectile).GetField("destination", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(Projectile).FullName, "destination");
}
