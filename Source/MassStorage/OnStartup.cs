using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace StockpileAugmentations
{
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
                new HarmonyMethod(typeof(OnStartup), nameof(ResourceCounterPostfix)));
        }

        private static void ResourceCounterPostfix(ResourceCounter __instance)
        {
            var countedAmounts = Traverse.Create(__instance).Field("countedAmounts")
                .GetValue<Dictionary<ThingDef, int>>();
            var map = Traverse.Create(__instance).Field("map").GetValue<Map>();

            try
            {
                map.listerBuildings.allBuildingsColonist.OfType<Building_MassStorageDevice>().ToList()
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
                Log.Error("SS Mass Storage caught exception while editing resource counts: " + ex);
            }
            finally
            {
                Traverse.Create(__instance).Field("countedAmounts").SetValue(countedAmounts);
            }
        }
    }
}