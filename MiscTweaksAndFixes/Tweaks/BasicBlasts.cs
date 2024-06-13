using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Mechanics.Components;

using MicroWrath;
using MicroWrath.BlueprintsDb;
//using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.InitContext;
using MicroWrath.Util;
using MicroWrath.Util.Linq;

namespace MiscTweaksAndFixes.Tweaks
{
    internal static class BasicBlasts
    {
        internal static bool Enabled { get; set; }
        #if DEBUG
            = true;
        #endif

        private static BlueprintAbility? CreateWeakBlast(BlueprintFeature blastFeature)
        {
            if (!blastFeature.name.EndsWith("Feature")) return null;

            var blastName = blastFeature.name.Substring(0, blastFeature.name.Length - 7);

            MicroLogger.Debug(() => blastName);

            var weakBlastName = $"Weak{blastName}Ability";

            var maybeBaseAbility = blastFeature.Components
                .OfType<AddFeatureIfHasFact>()
                .Select(c => c.m_Feature.Get())
                .Where(f => f.name == $"{blastName}Base")
                .FirstOrDefault();

            if (maybeBaseAbility is not BlueprintAbility baseAbility) return null;

            var basicBlastAbility = baseAbility.Components
                .OfType<AbilityVariants>()
                .SelectMany(av => av.m_Variants)
                .FirstOrDefault(a => a.NameSafe() == $"{blastName}Ability")
                ?.Get();

            if (basicBlastAbility is null) return null;

            MicroLogger.Debug(() => $"Creating {weakBlastName}");

            var weakBlastAbility = AssetUtils.CloneBlueprint(basicBlastAbility, GeneratedGuid.Get(weakBlastName), weakBlastName);

            var diceConfig = weakBlastAbility.Components
                .OfType<ContextRankConfig>()
                .FirstOrDefault(c => c.m_Type == Kingmaker.Enums.AbilityRankType.DamageDice);

            if (diceConfig is null) return null;

            diceConfig.m_UseMax = true;
            diceConfig.m_Max = 1;

            weakBlastAbility.m_Parent = null;

            // TODO: Fix this
            var displayName = weakBlastAbility.m_DisplayName.LoadString(LocalizationManager.CurrentPack, LocalizationManager.CurrentLocale);
            var key = $"{weakBlastName}.DisplayName";
            LocalizationManager.CurrentPack.PutString(key, $"{displayName} (Weak)");
            weakBlastAbility.m_DisplayName.m_Key = key;

            return weakBlastAbility;
        }

        [Init]
        internal static void Init()
        {
            var kineticistElementalFocusSelection = BlueprintsDb.Owlcat.BlueprintFeatureSelection.ElementalFocusSelection_1f3a15a3ae8a5524ab8b97f469bf4e3d;
            InitContext.GetBlueprint(kineticistElementalFocusSelection)
                .Map((BlueprintFeatureSelection s) => {
                    if (Enabled)
                    {
                        var elements = s.AllFeatures
                            .OfType<BlueprintProgression>();

                        var blastSelections = elements.Select(p =>
                            (p, p.LevelEntries
                                .Where(e => e.Level == 1)
                                .SelectMany(e => e.Features.OfType<BlueprintFeatureSelection>())));

                        foreach (var (progression, selection) in blastSelections)
                        {
                            if (!selection.Any()) continue;

                            MicroLogger.Debug(() => $"Patching {progression.name}");

                            var blastFeatures = selection
                                .First().AllFeatures
                                    .OfType<BlueprintProgression>()
                                    .Select(p => p.LevelEntries[0].Features.FirstOrDefault())
                                    .Where(f => f is not null);

                            foreach (var f in blastFeatures.OfType<BlueprintFeature>())
                            {
                                var weakBlast = CreateWeakBlast(f);

                                MicroLogger.Debug(() => $"Adding {weakBlast?.name ?? "<null>"}");

                                if (weakBlast is null) continue;

                                ResourcesLibrary.BlueprintsCache.AddCachedBlueprint(weakBlast.AssetGuid, weakBlast);

                                progression.AddAddFeatureIfHasFact(c =>
                                {
                                    c.m_CheckedFact = c.m_Feature = weakBlast.ToReference<BlueprintUnitFactReference>();
                                    c.Not = true;
                                });

                                f.AddRemoveFeatureOnApply(c => c.m_Feature = weakBlast.ToReference<BlueprintUnitFactReference>());
                            }
                        }
                    }

                    return s;
                })
                .AddOnTrigger(
                    kineticistElementalFocusSelection.BlueprintGuid,
                    Triggers.BlueprintsCache_Init);
        }
    }
}
