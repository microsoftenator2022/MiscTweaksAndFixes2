using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Blueprints.Classes;
using Kingmaker.Enums.Damage;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.FactLogic;

using MicroWrath;
using MicroWrath.BlueprintsDb;
//using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Constructors;
using MicroWrath.Deferred;
using MicroWrath.Util;

using UniRx;

namespace MiscTweaksAndFixes.Tweaks
{
    internal static class ReformedFiendDRGood
    {
        internal static bool Enabled = true;

        //private static readonly BlueprintInitializationContext PatchContext = new (Triggers.BlueprintsCache_Init);

        [Init]
        public static void Init()
        {
            _ = Deferred.GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeature.ReformedFiendDamageReductionFeature)
                .Map(bp =>
                {
                    if (!Enabled) return bp;

                    MicroLogger.Debug(() => $"{nameof(ReformedFiendDRGood)}");

                    var description = bp.Description;

                    if (description is null)
                    {
                        MicroLogger.Error($"{nameof(ReformedFiendDRGood)}: Could not get blueprint or description for feature");
                        return bp;
                    }

                    var damageReductionComponent = bp.Components.OfType<AddDamageResistancePhysical>().FirstOrDefault();

                    if (damageReductionComponent is null)
                    {
                        MicroLogger.Error($"{nameof(ReformedFiendDRGood)}: Could not get damage reduction component");
                        return bp;
                    }

                    damageReductionComponent.Alignment = DamageAlignment.Good;
                    damageReductionComponent.BypassedByAlignment = true;

                    description = description
                        .Replace("Evil", "Good")
                        .Replace("evil", "good");

                    LocalizationManager.CurrentPack.PutString(bp.m_Description.Key, description);

                    return bp;
                })
                .AddOnTrigger(
                    BlueprintsDb.Owlcat.BlueprintFeature.ReformedFiendDamageReductionFeature.BlueprintGuid,
                    Triggers.BlueprintsCache_Init);
        }
    }
}
