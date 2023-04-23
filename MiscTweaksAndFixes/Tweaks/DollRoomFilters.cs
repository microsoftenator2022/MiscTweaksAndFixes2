using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.UI.ServiceWindow;

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

            postProcessingVolume.profile.components.FirstOrDefault(c => c is ColorAdjustments).active = DollRoomFilters.ColorAdjustmentsFilter;
            postProcessingVolume.profile.components.FirstOrDefault(c => c is SlopePowerOffset).active = DollRoomFilters.SlopePowerOffsetFilter;
        }
    }
}
