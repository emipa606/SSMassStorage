using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace StockpileAugmentations;

[StaticConstructorOnStartup]
public static class OnStartup
{
    static OnStartup()
    {
        LongEventHandler.QueueLongEvent(Patcher, "Running patches", false, null);
    }

    private static void Patcher()
    {
        var harmony = new Harmony("net.spdskatr.factoryframework.patches");
        harmony.Patch(typeof(ResourceCounter).GetMethod(nameof(ResourceCounter.UpdateResourceCounts)), null,
            new HarmonyMethod(typeof(OnStartup), nameof(resourceCounterPostfix)));
    }

    private static void resourceCounterPostfix(Map ___map, ref Dictionary<ThingDef, int> ___countedAmounts)
    {
        var countedAmounts = ___countedAmounts;

        try
        {
            ___map.listerBuildings.allBuildingsColonist.OfType<Building_MassStorageDevice>().ToList()
                .FindAll(b => b.internalStoredDef != null && b.ThingCount > 0)
                .ForEach(storage =>
                {
                    if (storage.internalStoredDef.CountAsResource)
                    {
                        countedAmounts[storage.internalStoredDef] += storage.ThingCount;
                    }
                });
        }
        catch (Exception ex)
        {
            Log.Error($"SS Mass Storage caught exception while editing resource counts: {ex}");
        }
        finally
        {
            ___countedAmounts = countedAmounts;
        }
    }
}