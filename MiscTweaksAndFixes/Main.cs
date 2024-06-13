#if false
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.JsonSystem.Converters;
using Kingmaker.SharedTypes;

using MicroWrath;
using MicroWrath.BlueprintsDb;

using UniRx;

namespace MiscTweaksAndFixes
{
    [HarmonyPatch]
    static class TestPatch
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Load))]
        public static SimpleBlueprint LoadBlueprint(BlueprintsCache instance, BlueprintGuid guid) => throw new NotImplementedException("STUB");
    }

    internal partial class Main
    {
        [Init]
        static void Init()
        {
            Triggers.BlueprintsCache_Init.Take(1).Subscribe(_ =>
            {
                MicroLogger.Debug(() => $"TEST BLUEPRINT GET");

                try
                {
                    var blueprint = TestPatch.LoadBlueprint(ResourcesLibrary.BlueprintsCache, BlueprintsDb.Owlcat.BlueprintRace.HumanRace.BlueprintGuid);

                    MicroLogger.Debug(() => $"{blueprint}");
                }
                catch (Exception ex)
                {
                    MicroLogger.Error("Exception!", ex);
                }
            });
        }
    }
}
#endif