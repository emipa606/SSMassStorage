using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace StockpileAugmentations;

public class Building_MassStorageDevice : Building, IStoreSettingsParent
{
    public float rotProgressInt;
    public StorageSettings settings;
    public ThingDef storedDef;
    public int ThingCount;

    public Zone_Stockpile ResidingZone => Position.GetZone(Map) != null && Position.GetZone(Map) is Zone_Stockpile
        ? Position.GetZone(Map) as Zone_Stockpile
        : null;

    public ThingDef internalStoredDef => storedDef;

    public int maxCount => 2147483647;

    public CompPowerTrader PowerTraderComp => GetComp<CompPowerTrader>();

    public int ItemsStoredExternally
    {
        get
        {
            var i = 0;
            GenAdj.CellsOccupiedBy(this).ToList().ForEach(c =>
                c.GetThingList(Map).FindAll(t => t.def.category == ThingCategory.Item)
                    .ForEach(t => i += t.stackCount));
            return i;
        }
    }

    public bool StoredIsRottable => storedDef?.GetCompProperties<CompProperties_Rottable>() != null;

    public int TicksUntilRotAtCurrentTemp
    {
        get
        {
            var num = GenTemperature.GetTemperatureForCell(Position, Map);
            num = Mathf.RoundToInt(num);
            var num2 = GenTemperature.RotRateAtTemperature(num);
            if (num2 <= 0f)
            {
                return 2147483647;
            }

            var num3 = storedDef.GetCompProperties<CompProperties_Rottable>().TicksToRotStart - rotProgressInt;
            return num3 <= 0f ? 0 : Mathf.RoundToInt(num3 / num2);
        }
    }

    public void Notify_SettingsChanged()
    {
    }

    public bool StorageTabVisible => true;

    public StorageSettings GetStoreSettings()
    {
        return settings;
    }

    public StorageSettings GetParentStoreSettings()
    {
        return def.building.fixedStorageSettings;
    }

    public override string GetInspectString()
    {
        var thingName = storedDef != null ? ThingCount + "x " + storedDef.label.CapitalizeFirst() : "nothing";
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine(base.GetInspectString());
        stringBuilder.AppendFormat("In internal storage: {0} (Item(s) stored externally: {1})", thingName,
            ItemsStoredExternally);
        if (!StoredIsRottable)
        {
            return stringBuilder.ToString();
        }

        stringBuilder.AppendLine();
        stringBuilder.AppendFormat("Spoils in: {0}", TicksUntilRotAtCurrentTemp.ToStringTicksToPeriodVague());

        return stringBuilder.ToString();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ThingCount, "thingCount");
        Scribe_Values.Look(ref rotProgressInt, "rotProgress");
        Scribe_Defs.Look(ref storedDef, "storedDef");
        Scribe_Deep.Look(ref settings, "settings", this);
    }

    public override void DeSpawn(DestroyMode destroyMode = DestroyMode.Vanish)
    {
        DropAll();
        base.DeSpawn(destroyMode);
    }

    public override void PostMake()
    {
        base.PostMake();
        settings = new StorageSettings(this);
        if (def.building.defaultStorageSettings != null)
        {
            settings.CopyFrom(def.building.defaultStorageSettings);
        }
    }

    public override void Tick()
    {
        base.Tick();
        float powerMultiplier = ThingCount > 1 ? (int)Math.Floor(Math.Log10(ThingCount)) : 0;
        PowerTraderComp.powerOutputInt = (float)Math.Pow(2, powerMultiplier) * -1 *
                                         def.GetCompProperties<CompProperties_Power>().PowerConsumption;
        if (!PowerTraderComp.PowerOn)
        {
            return;
        }

        if (Find.TickManager.TicksGame % 40 != 0)
        {
            return;
        }

        //Code executed 40 times less often
        var clist = GenAdj.CellsOccupiedBy(this).ToList();
        CheckIfStorageInvalid(clist);
        TrySpawnItemsAtOutput(clist);
        if (ResidingZone == null)
        {
            return;
        }

        CollectApplicableItems();
        TickRottables();
    }

    public void TickRottables()
    {
        if (storedDef == null || Find.TickManager.TicksAbs % 250 != 0)
        {
            return;
        }

        if (StoredIsRottable)
        {
            _ = rotProgressInt;
            var num = 1f;
            var temperatureForCell = GenTemperature.GetTemperatureForCell(Position, Map);
            num *= GenTemperature.RotRateAtTemperature(temperatureForCell);
            rotProgressInt += Mathf.Round(num * 250f);
            if (!(rotProgressInt >= storedDef.GetCompProperties<CompProperties_Rottable>().TicksToRotStart))
            {
                return;
            }

            Messages.Message("MessageRottedAwayInStorage".Translate(storedDef.label).CapitalizeFirst(),
                MessageTypeDefOf.SilentInput);
            storedDef = null;
            ThingCount = 0;
            rotProgressInt = 1;
        }
        else
        {
            rotProgressInt = 0;
        }
    }

    public void CollectApplicableItems()
    {
        foreach (var cell in ResidingZone.cells)
        {
            if (GenAdj.CellsOccupiedBy(this).Any(c => c == cell))
            {
                continue;
            }

            var thingsAtCell = (from Thing t in cell.GetThingList(Map)
                where t.def.category == ThingCategory.Item && t is not Corpse && t.def.EverHaulable &&
                      !t.TryGetQuality(out _) && !t.def.MadeFromStuff && (t.TryGetComp<CompForbiddable>() == null ||
                                                                          !t.TryGetComp<CompForbiddable>()
                                                                              .Forbidden)
                select t).ToList();
            foreach (var t in thingsAtCell)
            {
                if (!settings.AllowedToAccept(t) || cell.GetThingList(Map).Any(u =>
                        u is Building_MassStorageDevice device && device.storedDef == t.def))
                {
                    continue;
                }

                AcceptItem(t);
            }
        }
    }

    public void TrySpawnItemsAtOutput(List<IntVec3> clist)
    {
        foreach (var cell in clist)
        {
            if (ThingCount <= 0)
            {
                continue;
            }

            var thing = cell.GetThingList(Map).Find(thing1 => thing1.def == storedDef);
            var any = cell.GetThingList(Map).Any(thing1 => thing1.def.category == ThingCategory.Item);
            if (thing != null)
            {
                var potential = thing.def.stackLimit - thing.stackCount;
                if (potential > ThingCount)
                {
                    thing.stackCount += ThingCount;
                    ThingCount = 0;
                    continue;
                }

                ThingCount -= potential;
                thing.stackCount += potential;
                continue;
            }

            if (any || storedDef == null)
            {
                continue;
            }

            var t = ThingMaker.MakeThing(storedDef);
            if (t.def.stackLimit > ThingCount)
            {
                t.stackCount = ThingCount;
                ThingCount = 0;
            }
            else
            {
                ThingCount -= t.def.stackLimit;
                t.stackCount = t.def.stackLimit;
            }

            GenPlace.TryPlaceThing(t, cell, Map, ThingPlaceMode.Direct);
        }
    }

    public void CheckIfStorageInvalid(List<IntVec3> clist)
    {
        if (clist.FindAll(intvec => intvec.GetFirstItem(Map) != null).NullOrEmpty() && storedDef != null &&
            ThingCount <= 0)
        {
            storedDef = null;
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var g in base.GetGizmos())
        {
            yield return g;
        }

        foreach (var g2 in StorageSettingsClipboard.CopyPasteGizmosFor(settings))
        {
            yield return g2;
        }

        if (!Prefs.DevMode)
        {
            yield break;
        }

        yield return new Command_Action
        {
            icon = ContentFinder<Texture2D>.Get("UI/Buttons/Drop"),
            defaultLabel = "DEBUG: Drop all items",
            defaultDesc =
                "Drops all items stored in internal storage and disallows the item in storage. WARNING: Some items will be lost if storage exceeds ~300 stacks.",
            action = () => DropAll(),
            activateSound = SoundDefOf.Click
        };
        yield return new Command_Action
        {
            defaultLabel = "DEBUG: Add 1 million of current item",
            defaultDesc = "If no item stored, adds Steel.",
            action = delegate
            {
                if (storedDef == null)
                {
                    storedDef = ThingDefOf.Steel;
                }

                ThingCount += 1000000;
            },
            activateSound = SoundDefOf.Click
        };
        yield return new Command_Action
        {
            defaultLabel = "DEBUG: Reset without dropping items",
            action = delegate
            {
                storedDef = null;
                ThingCount = 0;
            },
            activateSound = SoundDefOf.Click
        };
    }

    public virtual void AcceptItem(Thing t)
    {
        if (storedDef == null)
        {
            ThingCount = t.stackCount;
            storedDef = t.def;
            if (t.TryGetComp<CompRottable>() != null)
            {
                var ratio = (float)t.stackCount / (ThingCount + t.stackCount);
                rotProgressInt = Mathf.Lerp(rotProgressInt, t.TryGetComp<CompRottable>().RotProgress, ratio);
            }

            t.Destroy();
            t.def.soundDrop.PlayOneShot(SoundInfo.InMap(new TargetInfo(Position, Map)));
            return;
        }

        if (storedDef != t.def)
        {
            return;
        }

        if (StoredIsRottable)
        {
            var ratio = (float)t.stackCount / (ThingCount + t.stackCount);
            rotProgressInt = Mathf.Lerp(rotProgressInt, t.TryGetComp<CompRottable>().RotProgress, ratio);
        }

        ThingCount += t.stackCount;
        t.Destroy();
        t.def.soundDrop.PlayOneShot(SoundInfo.InMap(new TargetInfo(Position, Map)));
    }

    public void DropAll(bool disableItem = false)
    {
        if (storedDef == null)
        {
            ThingCount = 0;
            return;
        }

        while (ThingCount > 0)
        {
            var t = ThingMaker.MakeThing(storedDef);
            t.stackCount = ThingCount > t.stackCount ? t.def.stackLimit : ThingCount;
            ThingCount -= t.stackCount;
            GenPlace.TryPlaceThing(t, Position, Map, ThingPlaceMode.Near);
        }

        if (disableItem)
        {
            settings.filter.SetAllow(storedDef, false);
        }

        storedDef = null;
    }
}