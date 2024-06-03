using Kingmaker.Blueprints.Classes;

using MicroWrath;
using MicroWrath.BlueprintsDb;
using MicroWrath.Internal.InitContext;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.Localization;

using MiscTweaksAndFixes.AddedContent;
using MicroWrath.Util;
using Kingmaker.UnitLogic.FactLogic;

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

            //var context = new BlueprintInitializationContext(Triggers.BlueprintsCache_Init);

            var bp =
                InitContext.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get(nameof(MythicSuperiorSummoning)))
                    .Map(bp =>
                    {
                        MicroLogger.Debug(() => $"Add {nameof(MythicSuperiorSummoning)}");

                        //bp.AddComponent<ExtraSummonCount>();
                        bp.AddComponent<BookOfDreamsSummonUnitsCountLogic>();
                        bp.AddPrerequisiteFeature(BlueprintsDb.Owlcat.BlueprintFeature.SuperiorSummoning);

                        bp.m_DisplayName = LocalizedStrings.Tweaks_MythicSuperiorSummoning_MythicSuperiorSummoning_DisplayName;
                        bp.m_Description = LocalizedStrings.Tweaks_MythicSuperiorSummoning_MythicSuperiorSummoning_Description;

                        bp.m_Icon = AssetUtils.Direct.GetSprite("3defc1db47c477348b05c11450b9d5be", 21300000);

                        bp.Groups = [FeatureGroup.MythicFeat];

                        return bp;
                    })
                    .RegisterBlueprint(GeneratedGuid.MythicSuperiorSummoning);

            InitContext.GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeatureSelection.MythicFeatSelection)
                .Combine(bp)
                .Map(bps =>
                {
                    MicroLogger.Debug(() => "MythicFeatSelection");

                    var (selection, bp) = bps;
                    selection.AddFeatures(bp);

                    return selection;
                })
                .RegisterBlueprint(BlueprintsDb.Owlcat.BlueprintFeatureSelection.MythicFeatSelection.BlueprintGuid);
        }
    }
}
