using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.UI.ServiceWindow;

using MicroWrath;

using Owlcat.Runtime.Visual.RenderPipeline.PostProcess;

namespace MiscTweaksAndFixes.Tweaks
{
    internal static class DollRoomFilters
    {
        public static bool ColorAdjustmentsFilter = false;
        public static bool SlopePowerOffsetFilter = true;
    }

    [HarmonyPatch(typeof(DollCamera), nameof(DollCamera.OnEnable))]
    internal static class DollCamera_OnEnable
    {
        static void Postfix(DollCamera __instance)
        {
            var postProcessingVolume = __instance.GetComponentInChildren<UnityEngine.Rendering.Volume>();

            if (postProcessingVolume == null)
            {
                MicroLogger.Warning($"{nameof(UnityEngine.Rendering.Volume)} component not found");
                return;
            }

            if (postProcessingVolume.profile == null || postProcessingVolume.profile.components == null)
            {
                MicroLogger.Warning($"$missing {nameof(postProcessingVolume)} profile or components");
                return;
            }

            if (postProcessingVolume.profile.components.FirstOrDefault(c => c is ColorAdjustments) is { } ca)
                ca.active = DollRoomFilters.ColorAdjustmentsFilter;
            else
                MicroLogger.Warning($"{nameof(ColorAdjustments)} component not found");

            if (postProcessingVolume.profile.components.FirstOrDefault(c => c is SlopePowerOffset) is { } spo)
                spo.active = DollRoomFilters.SlopePowerOffsetFilter;
            else
                MicroLogger.Warning($"{nameof(SlopePowerOffset)} component not found");
        }
    }
}
