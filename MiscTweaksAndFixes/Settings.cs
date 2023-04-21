using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.UI.SettingsUI;
using Kingmaker.Settings;
using Kingmaker.Localization;

using ModMenu.Settings;

namespace MiscTweaksAndFixes
{
    internal static partial class Settings
    {
        private static bool debugLogging;
        public static bool DebugLogging
        {
            get =>
#if DEBUG
                true;
#else
                debugLogging;
#endif
            private set
            {
                debugLogging = value;
                MicroLogger.SetUmmLogLevel(value ? MicroLogger.Severity.Debug : MicroLogger.Severity.Info);
            }
        }

        private static string SettingsRootKey => $"{Main.Instance.ModEntry.Info.Id}".ToLower();
        private static string SettingKey(string key) => $"{SettingsRootKey}.{key}".ToLower();

        private static Toggle CreateSettingToggle(string name, LocalizedString description, bool defaultValue = true, LocalizedString? longDescription = null)
        {
            var key = SettingKey(name);

            MicroLogger.Debug(() => $"New toggle: key = \"{key}\"");

            var toggle = Toggle.New(key, defaultValue, description);

            if (longDescription is not null)
                toggle = toggle.WithLongDescription(longDescription);

            return toggle;
        }

        internal static void SettingsInit()
        {

        }
    }
}

//{
//    internal partial class MiscTweaksMod
//    {
//        internal class ModSettings
//        {
//            private bool debugLogging;

//            internal bool DebugLogging
//            {
//                get =>
//#if DEBUG
//                    true;
//#else
//                    debugLogging;
//#endif

//                set => debugLogging = value;
//            }

//            private static string SettingsRootKey => $"{Main.Mod.ModEntry.Info.Id}".ToLower();

//            private static string SettingsKey(string key) => $"{SettingsRootKey}.{key}".ToLower();

//            private static Toggle CreateSettingToggle(string name, string description, bool defaultValue = true, string? longDescription = null)
//            {
//                var nameKey = SettingsKey(name);

//                Main.Log.Debug($"New toggle key: \"{nameKey}\"");

//                var toggle = Toggle.New(nameKey, defaultValue, Localization.CreateString($"{nameKey}.Toggle.Description", description));

//                if (longDescription is not null)
//                    toggle = toggle.WithLongDescription(Localization.CreateString($"{nameKey}.Toggle.LongDescription", longDescription));

//                return toggle;
//            }

//            internal static void SettingsInit()
//            {
//                //var primalistToggle =
//                //    CreateSettingToggle(
//                //        $"{nameof(PrimalistBloodlineFixes)}",
//                //        "Primalist bloodline selection fix",
//                //        longDescription:
//                //            "Primalist bloodline selections are now per-bloodline and should function correctly when "
//                //            + "combined with Dragon Disciple and/or Second Bloodline (still two rage powers per 4 "
//                //            + "levels, but you can choose which bloodline's power to trade)\n"
//                //            + "Requires restart.")
//                //    .OnValueChanged(newValue => PrimalistBloodlineFixes.Enabled = newValue);

//                var bookOfDreamsToggle =
//                    CreateSettingToggle(
//                        $"{nameof(BookOfDreams.BookOfDreamsFix)}",
//                        "Book of Dreams upgrade fix",
//                        defaultValue: false,
//                        longDescription:
//                            "The Book of Dreams item is supposed to upgrade at certain points in the story, "
//                            + "but this has never reliably worked (at least in my experience).\n"
//                            + "Enabling this forces the upgrade script to run on every Etude update.")
//                    .OnValueChanged(newValue => BookOfDreams.BookOfDreamsFix.Enabled = newValue);

//                var naturalWeaponStacking =
//                    CreateSettingToggle(
//                        $"{nameof(NaturalWeaponStacking.NaturalWeaponStacking)}",
//                        "Natural weapon stacking",
//                        longDescription:
//                            "Previously, if you got multiple natural attacks of the same type from different "
//                            + "features/buffs/etc. you would get extra attacks per round. This was 'fixed' by Owlcat at "
//                            + "some point so now extra natural attacks give no benefit to PCs.\n"
//                            + "With this enabled, vanilla behaviour is replaced with an approximation of the tabletop rules:\n"
//                            + "Addtional natural attacks of the same kind gives a stacking increase to the effective size "
//                            + "of the 'weapon' (eg. 2 pairs of Medium claw attacks effectively grant 1 pair of Large claw "
//                            + "attacks instead).\n"
//                            + "You get all 'enchantment' effects (eg. fire damage/DR penetration) but multiple enchants "
//                            + "of the same type do not stack.")
//                    .OnValueChanged(newValue => NaturalWeaponStacking.NaturalWeaponStacking.Enabled = newValue);

//                var reformedFiendDRToggle =
//                    CreateSettingToggle(
//                        $"{nameof(ReformedFiendDamageReductionGood)}",
//                        "Reformed Fiend DR/good",
//                        defaultValue: false,
//                        longDescription:
//                            "Changes the damage reduction for the Reformed Fiend Bloodrager archetype from DR/evil to "
//                            + "DR/good.\n"
//                            + "Requires restart.")
//                    .OnValueChanged(newValue => ReformedFiendDamageReductionGood.Enabled = newValue);

//                var strengthBlessingMajorFixToggle =
//                    CreateSettingToggle(
//                        $"{nameof(StrengthBlessingMajor.StrengthBlessingMajorBuff)}",
//                        "Major Strength Blessing armor speed fix",
//                        defaultValue: true,
//                        longDescription:
//                            "Strength domain Warpriests' Major Blessing now applies to heavy armor in addition to "
//                            + "medium armor.\n"
//                            + "Requires restart.")
//                    .OnValueChanged(newValue => StrengthBlessingMajor.StrengthBlessingMajorBuff.Enabled = newValue);

//                //var bloodragerDraconicClawsFix =
//                //    CreateSettingToggle(
//                //        name: $"{nameof(BloodragerDraconicBaseBuffFixes.FixBloodragerDraconicClawsBuff)}",
//                //        "Bloodrager Draconic Claws fix",
//                //        longDescription:
//                //            $"Fixes claw progression for draconic bloodrager bloodlines:{Environment.NewLine}"
//                //            + $"Correct progression: 1d6, 1d6 (Magic), 1d8 (Magic), 1d8+1d6 elemental (Magic){Environment.NewLine}"
//                //            + "Actual progression (without fix): 1d6, 1d6, 1d6, 1d8+1d6 elemental (Magic)")
//                //    .OnValueChanged(newValue => BloodragerDraconicBaseBuffFixes.Enabled = newValue);

//                var dollRoomPpColorAdjustmentsFilter =
//                    CreateSettingToggle(
//                        $"{nameof(DollRoomFilters.DollRoomFilters.ColorAdjustmentsFilter)}",
//                        "\"Color Adjustments\"",
//                        defaultValue: false,
//                        longDescription: "Enable or disable the \"Color Adjustments\" filter in the \"doll room\".")
//                    .OnValueChanged(newValue => DollRoomFilters.DollRoomFilters.ColorAdjustmentsFilter = newValue);

//                var dollRoomPpSlopePowerOffsetFilter =
//                    CreateSettingToggle(
//                        $"{nameof(DollRoomFilters.DollRoomFilters.SlopePowerOffsetFilter)}",
//                        "\"Slope Power Offset\"",
//                        defaultValue: true,
//                        longDescription: "Enable or disable the \"Slope Power Offset\" filter in the \"doll room\".")
//                    .OnValueChanged(newValue => DollRoomFilters.DollRoomFilters.ColorAdjustmentsFilter = newValue);

//                var debugLoggingToggle =
//                    CreateSettingToggle(
//                        "DebugLogging", "Add more info to logs for debugging", defaultValue: false)
//                    .OnValueChanged(newValue => Main.Mod.Settings.DebugLogging = newValue);

//                var settings =
//                    SettingsBuilder.New(SettingsRootKey,
//                        Localization.CreateString(
//                            $"{nameof(MiscTweaksAndFixes)}.Title",
//                            "Miscellaneous Tweaks and Fixes"))
//                    .AddSubHeader(
//                        Localization.CreateString($"{nameof(Main.Mod.ModEntry.Info.Id)}.Fixes", "Fixes"),
//                        true)
//                    //.AddToggle(primalistToggle)
//                    .AddToggle(bookOfDreamsToggle)
//                    //.AddToggle(bloodragerDraconicClawsFix)
//                    .AddToggle(strengthBlessingMajorFixToggle)

//                    .AddSubHeader(
//                        Localization.CreateString($"{nameof(Main.Mod.ModEntry.Info.Id)}.Tweaks", "Tweaks"),
//                        true)
//                    .AddToggle(naturalWeaponStacking)
//                    .AddToggle(reformedFiendDRToggle)

//                    .AddSubHeader(Localization.CreateString(
//                        $"{nameof(Main.Mod.ModEntry.Info.Id)}.{nameof(DollRoomFilters)}",
//                        "Dollroom post-processing filters"), true)
//                    .AddToggle(dollRoomPpColorAdjustmentsFilter)
//                    .AddToggle(dollRoomPpSlopePowerOffsetFilter)

//                    .AddSubHeader(Localization.CreateString($"{Main.Mod.ModEntry.Info.Id}.Debug", "Debug options"))
//                    .AddToggle(debugLoggingToggle);

//                //var debugSetting = new SettingsEntityBool(SettingsKey("debug"), false);
//                //var debugSettingUI = UnityEngine.ScriptableObject.CreateInstance<UISettingsEntityBool>();
//                ////debugSettingUI.
//                //var settingsGroup = UnityEngine.ScriptableObject.CreateInstance<UISettingsGroup>();

//                ModMenu.ModMenu.AddSettings(settings);
//            }
//        } 
//    }
//}
