using Kingmaker.Blueprints.Classes;

using MicroWrath;
using MicroWrath.BlueprintsDb;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.Localization;

using MiscTweaksAndFixes.AddedContent;
using MicroWrath.Util;

namespace MiscTweaksAndFixes.Tweaks.MythicSuperiorSummoning
{
    internal static class MythicSuperiorSummoning
    {
        internal static bool Enabled = true;

        [LocalizedString]
        internal const string DisplayName = "Mythic Superior Summoning";

        [LocalizedString]
        internal const string Description = "Each time you use an ability that lets you summon multiple creatures, their number is increased by one.";

        [Init]
        internal static void Init()
        {
            if (!Enabled) return;

            var context = new BlueprintInitializationContext(Triggers.BlueprintsCache_Init);

            var bp =
                context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get(nameof(MythicSuperiorSummoning)))
                    .Map(bp =>
                    {
                        bp.AddComponent<ExtraSummonCount>();
                        bp.AddPrerequisiteFeature(BlueprintsDb.Owlcat.BlueprintFeature.SuperiorSummoning);

                        bp.m_DisplayName = LocalizedStrings.Tweaks_MythicSuperiorSummoning_MythicSuperiorSummoning_DisplayName;
                        bp.m_Description = LocalizedStrings.Tweaks_MythicSuperiorSummoning_MythicSuperiorSummoning_Description;

                        bp.m_Icon = AssetUtils.Direct.GetSprite("3defc1db47c477348b05c11450b9d5be", 21300000);

                        bp.Groups = [FeatureGroup.MythicFeat];

                        return bp;
                    });

            context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeatureSelection.MythicFeatSelection)
                .Combine(bp)
                .Map(bps =>
                {
                    var (selection, bp) = bps;
                    selection.AddFeatures(bp);
                })
                .Register();
        }
    }
}
