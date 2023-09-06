using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Blueprints;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;

namespace MiscTweaksAndFixes.Fixes
{
    //[HarmonyPatch(typeof(EntityFact))]
    //static class EntityFactPatches
    //{
    //    [HarmonyPatch(nameof(EntityFact.Dispose))]
    //    [HarmonyPrefix]
    //    static void Dispose_Prefix(EntityFact __instance)
    //    {
    //        MicroLogger.Debug(() => $"{nameof(EntityFactPatches)}.{nameof(Dispose_Prefix)}");
    //        MicroLogger.Debug(() => $"{__instance}");
    //        MicroLogger.Debug(() => $"Is attached? {__instance.IsAttached}");

    //        if (__instance.IsAttached)
    //            MicroLogger.Warning("Disposing an attached Fact");
    //    }

    //    [HarmonyPatch(nameof(EntityFact.Detach))]
    //    [HarmonyPrefix]
    //    static void Detach_Prefix(EntityFact __instance)
    //    {
    //        MicroLogger.Debug(() => $"{nameof(EntityFactPatches)}.{nameof(Detach_Prefix)}");
    //        MicroLogger.Debug(() => $"{__instance}");
    //    }
    //}

    internal static class RemoveSwarmBuffOnCasterDeath
    {
        [HarmonyPatch]
        internal static class Patch
        {
            //[HarmonyPatch(typeof(UnitEntityData), nameof(UnitEntityData.OnDispose))]
            //[HarmonyPrefix]
            //static void UnitEntityData_OnDispose_Prefix(UnitEntityData __instance)
            //{
            //    MicroLogger.Debug(() => $"{nameof(UnitEntityData_OnDispose_Prefix)}");
            //    MicroLogger.Debug(() => $"{__instance}");
            //}

            //[HarmonyPatch(typeof(EntityFactsManager), nameof(EntityFactsManager.Dispose))]
            //[HarmonyPrefix]
            //static void EntityFactsManager_Dispose_Prefix(EntityFactsManager __instance)
            //{
            //    MicroLogger.Debug(() => nameof(EntityFactsManager_Dispose_Prefix));
            //    MicroLogger.Debug(() => $"Owner: {__instance.Owner}");
            //}

            //[HarmonyPatch(typeof(EntityFactsManager), nameof(EntityFactsManager.Dispose))]
            //[HarmonyPostfix]
            //static void EntityFactsManager_Dispose_Postfix(EntityFactsManager __instance)
            //{
            //    MicroLogger.Debug(() => nameof(EntityFactsManager_Dispose_Postfix));
            //    MicroLogger.Debug(() => $"Owner: {__instance.Owner}");
            //}

            //#if DEBUG
            //[HarmonyPatch(typeof(AreaEffectsController), nameof(AreaEffectsController.Deactivate))]
            //[HarmonyPrefix]
            //static void AreaEffectsController_Deactivate_Prefix(AreaEffectsController __instance)
            //{
            //    MicroLogger.Debug(() => nameof(AreaEffectsController_Deactivate_Prefix));
            //    MicroLogger.Debug(() =>
            //    {
            //        var sb = new StringBuilder();

            //        foreach (var e in __instance.m_EffectsToTick.m_EveryFrameUpdates)
            //        {
            //            sb.AppendLine($"{e?.ToString() ?? "NULL"}");
            //        }

            //        return sb.ToString();
            //    });
            //}
            //#endif

            [HarmonyPatch(
                typeof(DropLootAndDestroyOnDeactivate),
                nameof(DropLootAndDestroyOnDeactivate.OnDeactivate))]
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> DropLootAndDestroyOnDeactivate_OnDeactivate_Patch(
                IEnumerable<CodeInstruction> instructions)
            {
                var callIndex = instructions.FindIndex(ci =>
                    ci.Calls(AccessTools.Method(
                        typeof(EntityDataBase),
                        nameof(EntityDataBase.MarkForDestroy))));

                if (callIndex < 0)
                    return instructions;

                var iList = instructions.ToList();

                iList.InsertRange(callIndex, new[]
                {
                    new CodeInstruction(OpCodes.Dup),
                    new CodeInstruction(OpCodes.Ldc_I4_0),
                    new CodeInstruction(OpCodes.Call,
                        AccessTools.PropertySetter(
                            typeof(EntityDataBase),
                            nameof(EntityDataBase.IsInGame)))
                });

                return iList;
            }

            //[HarmonyPatch(typeof(SummonedUnitBuff), nameof(SummonedUnitBuff.OnRemoved))]
            //[HarmonyPrefix]
            //static void SummonedUnitBuff_OnRemoved()
            //{
            //    #if DEBUG
            //    Debugger.Break();
            //    #endif
            //}

            //[HarmonyPatch(typeof(Buff), nameof(Buff.OnDetach))]
            //[HarmonyPrefix]
            //static void Buff_OnDetach_Prefix(Buff __instance)
            //{
            //    MicroLogger.Debug(() => nameof(Buff_OnDetach_Prefix));
            //    MicroLogger.Debug(() => $"{__instance}");
            //}
        }
    }
}
