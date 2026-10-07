namespace QuestEditor_Library
{
    public static class CQFAILiveFeatureValidation
    {
        public static void Validate(CQFAILiveThingConfiguration value)
        {
            if (value.loot is CQFAILootConfiguration loot)
            {
                if (loot.tickToOpen < 0 || loot.lootBoxName == null || loot.openReport == null || loot.useLootDef && loot.lootDef == null)
                    throw new InvalidDataException("CQF_AI_InvalidValue: loot configuration");
                ValidateLoot(loot.loots);
                if (loot.lootDef != null) ValidateLoot(loot.lootDef.loots);
            }
            if (value.trap is CQFAITrapConfiguration trap)
            {
                if (trap.trapName == null || trap.trapComps == null || trap.trapComps.Any(comp => comp == null
                    || !Enum.IsDefined(typeof(ActionTriggerMode), comp.mode) || comp.mode == ActionTriggerMode.Tick && comp.tick <= 0
                    || comp.mode == ActionTriggerMode.Signal && string.IsNullOrWhiteSpace(comp.inSignal)
                    || comp.mode is not (ActionTriggerMode.None or ActionTriggerMode.Signal or ActionTriggerMode.Tick or ActionTriggerMode.StepOn)
                    || comp.actions == null || comp.actions.Any(action => action == null))) throw new InvalidDataException("CQF_AI_InvalidValue: trap configuration");
                if (trap.capture is CQFAICaptureTrapConfiguration capture)
                {
                    if (capture.tickToDisarm < 0 || capture.disarmReport == null) throw new InvalidDataException("CQF_AI_InvalidValue: disarm configuration");
                    ValidateActions(capture.disarmActions);
                }
            }
            if (value.door is CQFAIDoorConfiguration door) { ValidateActions(door.openingActions); ValidateConditions(door.openingConditions); }
            if (value.container is CQFAIContainerConfiguration container)
            {
                if (container.tickToOpen < 0) throw new InvalidDataException("CQF_AI_InvalidValue: container configuration");
                ValidateLoot(container.innerThings); ValidateActions(container.openingActions); ValidateConditions(container.openingConditions);
            }
            if (value.spawner != null) ValidatePawns(value.spawner.pawns);
            if (value.generation != null) ValidateActions(value.generation.actions);
            if (value.extraInteractions == null || value.extraInteractions.Any(operation => operation == null) || value.customTexts == null
                || value.customTexts.Any(text => text == null || text.useCustomName && string.IsNullOrWhiteSpace(text.customName)
                    || text.useCustomDescription && string.IsNullOrWhiteSpace(text.customDescription)
                    || text.useCustomInspectText && string.IsNullOrWhiteSpace(text.customInspectText)))
                throw new InvalidDataException("CQF_AI_InvalidValue: object text or extra interactions");
            if (value.zone is CQFAIZoneConfiguration zone)
            {
                if (zone.size == null || zone.size.minX < 0 || zone.size.minZ < 0 || zone.size.maxX < 0 || zone.size.maxZ < 0
                    || !zone.isCenter && !zone.coreRotation.IsValid || zone.conditions == null || zone.conditions.Any(condition => condition == null)
                    || zone.coreTags == null || zone.coreTags.Any(string.IsNullOrWhiteSpace) || zone.coreTags.Distinct().Count() != zone.coreTags.Count
                    || zone.reserveThing != null && zone.reserveThing.def == null) throw new InvalidDataException("CQF_AI_InvalidValue: zone configuration");
            }
            if (value.actionWorkers == null || value.actionWorkers.Any(worker => worker == null || worker.comps == null))
                throw new InvalidDataException("CQF_AI_InvalidValue: action workers");
            foreach (CQFAIActionWorkerConfiguration worker in value.actionWorkers)
                foreach (ActionComp comp in worker.comps)
                {
                    if (comp == null || !Enum.IsDefined(typeof(ActionTriggerMode), comp.mode) || !comp.HasValidTickInterval
                        || comp.mode == ActionTriggerMode.Signal && string.IsNullOrWhiteSpace(comp.signal)) throw new InvalidDataException("CQF_AI_InvalidValue: action trigger");
                    ValidateActions(comp.actions);
                }
        }
        private static void ValidateLoot(List<LootData>? loots)
        {
            if (loots == null || loots.Count > 0 && !loots.Any(loot => loot != null && loot.chance > 0)
                || loots.Any(loot => loot == null || float.IsNaN(loot.chance) || float.IsInfinity(loot.chance) || loot.chance < 0
                || loot.things == null || loot.things.Any(item => item == null || item.thing == null || item.count.min < 0 || item.count.max < item.count.min)
                || loot.categorys == null || loot.categorys.Any(item => item == null || item.category == null || item.count.min < 0 || item.count.max < item.count.min)
                || loot.specialThingDatas == null || loot.specialThingDatas.Any(item => item == null))) throw new InvalidDataException("CQF_AI_InvalidValue: loot entries");
            foreach (LootData loot in loots) ValidatePawns(loot.pawnDatas);
        }
        private static void ValidatePawns(List<PawnSpawnData>? pawns)
        {
            if (pawns == null || pawns.Any(pawn => pawn == null || pawn.count.min < 0 || pawn.count.max < pawn.count.min || pawn.timeToSpawn < 0
                || float.IsNaN(pawn.generationChance) || float.IsInfinity(pawn.generationChance) || pawn.generationChance < 0 || pawn.generationChance > 1
                || !pawn.CanSaveToMap() || pawn.actions == null || pawn.actions.Any(action => action == null))) throw new InvalidDataException("CQF_AI_InvalidValue: pawn spawn configuration");
            foreach (PawnSpawnData pawn in pawns)
                if (pawn is PawnSpawnData_Random random) ValidatePawns(random.datas);
        }
        private static void ValidateActions(List<CQFAction>? actions)
        { if (actions == null || actions.Any(action => action == null)) throw new InvalidDataException("CQF_AI_InvalidValue: actions"); }
        private static void ValidateConditions(List<DialogCondition>? conditions)
        { if (conditions == null || conditions.Any(condition => condition == null)) throw new InvalidDataException("CQF_AI_InvalidValue: conditions"); }
    }
}
