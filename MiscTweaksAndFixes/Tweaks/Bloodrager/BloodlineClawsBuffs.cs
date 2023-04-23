using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Buffs.Blueprints;

using MicroWrath;
using MicroWrath.BlueprintsDb;
using MicroWrath.Util;
using MicroWrath.Util.Linq;

namespace MiscTweaksAndFixes.Tweaks.Bloodrager
{
    public static class BloodlineClawsBuffs
    {
        private static readonly Lazy<BlueprintFeature[]> clawFeatures = new(() => new[]
        {
            BlueprintsDb.Owlcat.BlueprintFeature.BloodragerDraconicClawFeature1.GetBlueprint()!,
            BlueprintsDb.Owlcat.BlueprintFeature.BloodragerDraconiclClawFeature4.GetBlueprint()!,
            BlueprintsDb.Owlcat.BlueprintFeature.BloodragerDraconicClawFeature8.GetBlueprint()!,
            BlueprintsDb.Owlcat.BlueprintFeature.BloodragerDraconicClawFeature12Acid.GetBlueprint()!,
            BlueprintsDb.Owlcat.BlueprintFeature.BloodragerDraconicClawFeature12Cold.GetBlueprint()!,
            BlueprintsDb.Owlcat.BlueprintFeature.BloodragerDraconicClawFeature12Electricity.GetBlueprint()!,
            BlueprintsDb.Owlcat.BlueprintFeature.BloodragerDraconicClawFeature12Fire.GetBlueprint()!
        });
        internal static IReadOnlyCollection<BlueprintFeature> ClawFeatures => clawFeatures.Value;

        private static readonly Lazy<BlueprintBuff[]> clawBuffs = new(() => new[]
        {
            BlueprintsDb.Owlcat.BlueprintBuff.BloodragerDraconicClawBuff1.GetBlueprint()!,
            BlueprintsDb.Owlcat.BlueprintBuff.BloodragerDraconicClawBuff4.GetBlueprint()!,
            BlueprintsDb.Owlcat.BlueprintBuff.BloodragerDraconicClawBuff8.GetBlueprint()!,
            BlueprintsDb.Owlcat.BlueprintBuff.BloodragerDraconicClawBuff12Acid.GetBlueprint()!,
            BlueprintsDb.Owlcat.BlueprintBuff.BloodragerDraconicClawBuff12Cold.GetBlueprint()!,
            BlueprintsDb.Owlcat.BlueprintBuff.BloodragerDraconicClawBuff12Electricity.GetBlueprint()!,
            BlueprintsDb.Owlcat.BlueprintBuff.BloodragerDraconicClawBuff12Fire.GetBlueprint()!,
        });

        internal static IReadOnlyCollection<BlueprintBuff> ClawBuffs => clawBuffs.Value;

        private static readonly Lazy<Dictionary<BlueprintFeature, BlueprintBuff>> featureBuffMap = new(() => new()
        {
            { BlueprintsDb.Owlcat.BlueprintFeature.BloodragerDraconicClawFeature1.GetBlueprint()!,
                BlueprintsDb.Owlcat.BlueprintBuff.BloodragerDraconicClawBuff1.GetBlueprint()! },

            { BlueprintsDb.Owlcat.BlueprintFeature.BloodragerDraconiclClawFeature4.GetBlueprint()!,
                BlueprintsDb.Owlcat.BlueprintBuff.BloodragerDraconicClawBuff4.GetBlueprint()! },

            { BlueprintsDb.Owlcat.BlueprintFeature.BloodragerDraconicClawFeature8.GetBlueprint()!,
                BlueprintsDb.Owlcat.BlueprintBuff.BloodragerDraconicClawBuff8.GetBlueprint()! },

            { BlueprintsDb.Owlcat.BlueprintFeature.BloodragerDraconicClawFeature12Acid.GetBlueprint()!,
                BlueprintsDb.Owlcat.BlueprintBuff.BloodragerDraconicClawBuff12Acid.GetBlueprint()! },

            { BlueprintsDb.Owlcat.BlueprintFeature.BloodragerDraconicClawFeature12Cold.GetBlueprint()!,
                BlueprintsDb.Owlcat.BlueprintBuff.BloodragerDraconicClawBuff12Cold.GetBlueprint()! },

            {BlueprintsDb.Owlcat.BlueprintFeature.BloodragerDraconicClawFeature12Electricity.GetBlueprint()!,
                BlueprintsDb.Owlcat.BlueprintBuff.BloodragerDraconicClawBuff12Electricity.GetBlueprint()! },

            { BlueprintsDb.Owlcat.BlueprintFeature.BloodragerDraconicClawFeature12Fire.GetBlueprint()!,
                BlueprintsDb.Owlcat.BlueprintBuff.BloodragerDraconicClawBuff12Fire.GetBlueprint()! },
        });
        internal static IReadOnlyDictionary<BlueprintFeature, BlueprintBuff> FeatureBuffMap => featureBuffMap.Value;

        public static IDictionary<int, BlueprintFeature> GetBloodragerDraconicClawFeaturesByLevel(BlueprintProgression dragonBloodline) =>
            dragonBloodline.LevelEntries.SelectMany(le =>
                ClawFeatures
                    .Where(cf => le.Features.Contains(cf))
                    .Select(f => (le.Level, f)))
                .ToDictionary();

        public static IEnumerable<BlueprintFeature> GetBloodragerDragonClawFeaturesFor(UnitEntityData unit)
        {
            static bool IsBloodragerDraconicBloodline(BlueprintProgression progression)
            {
                var levelEntries = progression.LevelEntries.FirstOrDefault(le => le.Level == 1);

                if (levelEntries is null) return false;

                return levelEntries.Features
                        .Contains(BlueprintsDb.Owlcat.BlueprintFeature.BloodragerDraconicBaseFeature.GetBlueprint()!);
            }

            var progressions = unit.Progression.m_Progressions.Values;

            if (Settings.DebugLogging)
            {
                MicroLogger.Debug(() => $"Progressions:");
                foreach (var p in progressions)
                    MicroLogger.Debug(() => $"    {p.Blueprint?.name} -  {p.Blueprint?.AssetGuid}");
            }

            var features = progressions
                .Where(p => p.Blueprint is not null && IsBloodragerDraconicBloodline(p.Blueprint))
                .Select(p =>
                {
                    var bloodlineClawFeatures = GetBloodragerDraconicClawFeaturesByLevel(p.Blueprint);
                    return bloodlineClawFeatures[bloodlineClawFeatures.Keys
                        .OrderByDescending(Functional.Identity)
                        .First(level => level <= p.Level)];
                });

            return features;
        }
    }
}
