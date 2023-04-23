using System.Collections.Generic;
using System.Linq;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Utility;

using MicroWrath;
using MicroWrath.BlueprintsDb;
using MicroWrath.Util.Linq;

namespace MiscTweaksAndFixes.Tweaks.Bloodrager
{
    internal static class BloodlinePowerHelpers
    {
        internal class BloodlinePowers
        {
            public Dictionary<int, BlueprintFeature[]> AllPowers { get; } = new();

            public BlueprintFeature[] Level4
            {
                get => AllPowers[4];
                set => AllPowers[4] = value;
            }

            public BlueprintFeature[] Level8
            {
                get => AllPowers[8];
                set => AllPowers[8] = value;
            }

            public BlueprintFeature[] Level12
            {
                get => AllPowers[12];
                set => AllPowers[12] = value;
            }

            public BlueprintFeature[] Level16
            {
                get => AllPowers[16];
                set => AllPowers[16] = value;
            }

            public BlueprintFeature[] Level20
            {
                get => AllPowers[20];
                set => AllPowers[20] = value;
            }
        }

        internal static BlueprintFeature[] BloodlinePowerForLevel(
            BlueprintProgression bloodline, int level) =>
            bloodline.GetLevelEntry(level).Features
                .Where(f =>
                    f.Components
                        .OfType<PrerequisiteNoFeature>()
                        .Where(p => p.Feature.AssetGuid == BlueprintsDb.Owlcat.BlueprintProgression.PrimalistProgression.BlueprintGuid)
                        .Any())
            .OfType<BlueprintFeature>()
            .ToArray();

        internal static IDictionary<BlueprintProgression, BloodlinePowers> GetPowersByBloodline()
        {
            var bloodlineSelection = BlueprintsDb.Owlcat.BlueprintFeatureSelection.BloodragerBloodlineSelection.GetBlueprint()!;
            var bloodlines = bloodlineSelection.m_AllFeatures.Select(f => f.Get()).OfType<BlueprintProgression>();

            MicroLogger.Debug(() => $"{bloodlines.Count()} bloodlines");

            var powers = bloodlines.Select(bloodline =>
                (bloodline, new BloodlinePowers()
                {
                    Level4 = BloodlinePowerForLevel(bloodline, 4),
                    Level8 = BloodlinePowerForLevel(bloodline, 8),
                    Level12 = BloodlinePowerForLevel(bloodline, 12),
                    Level16 = BloodlinePowerForLevel(bloodline, 16),
                    Level20 = BloodlinePowerForLevel(bloodline, 20)
                }));

            return powers.ToDictionary();
        }
    }
}
