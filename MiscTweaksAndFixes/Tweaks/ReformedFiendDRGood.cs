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
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Constructors;
using MicroWrath.Util;

using UniRx;

namespace MiscTweaksAndFixes.Tweaks
{
    internal static class ReformedFiendDRGood
    {
        internal static bool Enabled = true;

        private static readonly BlueprintInitializationContext PatchContext = new (Triggers.BlueprintsCache_Init);

        [Init]
        public static void Init()
        {
            PatchContext.GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeature.ReformedFiendDamageReductionFeature)
                .Select(bp =>
                {
                    if (!Enabled) return;

                    MicroLogger.Debug(() => $"{nameof(ReformedFiendDRGood)}");

                    var description = bp?.Description;

                    if (bp is null || description is null)
                    {
                        MicroLogger.Error($"{nameof(ReformedFiendDRGood)}: Could not get blueprint or description for feature");
                        return;
                    }

                    var damageReductionComponent = bp.Components.OfType<AddDamageResistancePhysical>().FirstOrDefault();

                    if (damageReductionComponent is null)
                    {
                        MicroLogger.Error($"{nameof(ReformedFiendDRGood)}: Could not get damage reduction component");
                        return;
                    }

                    damageReductionComponent.Alignment = DamageAlignment.Good;
                    damageReductionComponent.BypassedByAlignment = true;

                    description = description
                        .Replace("Evil", "Good")
                        .Replace("evil", "good");

                    LocalizationManager.CurrentPack.PutString(bp.m_Description.Key, description);
                })
                .Register();
        }
    }
}
