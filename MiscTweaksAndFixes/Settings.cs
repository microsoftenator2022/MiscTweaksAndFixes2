using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Localization;

using UniRx;

using ModMenu;
using ModMenu.Settings;

using MicroWrath;
using MicroWrath.Localization;

using MiscTweaksAndFixes.Fixes;
using MiscTweaksAndFixes.Tweaks;
using MiscTweaksAndFixes.Tweaks.NaturalWeaponStacking;

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

        [LocalizedString]
        public const string BookOfDreamsToggleDescription = "Book of Dreams upgrade fix";
        [LocalizedString]
        public const string BookOfDreamsToggleLongDescription =
            "The Book of Dreams item is supposed to upgrade at certain points in the story, " +
            "but this has never reliably worked (at least in my experience).\n" +
            "Enabling this forces the upgrade script to check if it should run on every Etude update.";
        private static Toggle BookOfDreamsToggle =>
            CreateSettingToggle(
                nameof(BookOfDreams),
                defaultValue: true,
                description: LocalizedStrings.Settings_BookOfDreamsToggleDescription,
                longDescription: LocalizedStrings.Settings_BookOfDreamsToggleLongDescription)
            .OnValueChanged(newValue => BookOfDreams.Enabled = newValue);


        [LocalizedString]
        public const string ReformedFiendDRToggleDescription = "Reformed Fiend DR/good";
        [LocalizedString]
        public const string ReformedFiendDRToggleLongDescription =
            "Changes the damage reduction for the Reformed Fiend Bloodrager archetype from DR/evil to " +
            "DR/good.\n" +
            "Requires restart.";
        private static Toggle ReformedFiendDRToggle =>
            CreateSettingToggle(
                nameof(ReformedFiendDRGood),
                defaultValue: false,
                description: LocalizedStrings.Settings_ReformedFiendDRToggleDescription,
                longDescription: LocalizedStrings.Settings_ReformedFiendDRToggleLongDescription)
            .OnValueChanged(newValue => ReformedFiendDRGood.Enabled = newValue);

        [LocalizedString]
        public const string StrengthBlessingFixToggleDescription = "Major Strength Blessing armor speed fix";
        [LocalizedString]
        public const string StrengthBlessingFixToggleLongDescription =
            "Allows Strength domain Warpriests' Major Blessing to apply to heavy armor in addition to " +
            "medium armor.\n" +
            "Requires restart.";
        private static Toggle StrengthBlessingMajorFixToggle =>
            CreateSettingToggle(
                nameof(StrengthBlessingMajorHeavyArmor),
                defaultValue: true,
                description: LocalizedStrings.Settings_StrengthBlessingFixToggleDescription,
                longDescription: LocalizedStrings.Settings_StrengthBlessingFixToggleLongDescription)
            .OnValueChanged(newValue => StrengthBlessingMajorHeavyArmor.Enabled = true);

        [LocalizedString]
        public const string DebugLoggingDescription = "Debug Logging";
        private static Toggle DebugLogToggle =>
            CreateSettingToggle(
                nameof(DebugLogging),
                defaultValue: false,
                description: LocalizedStrings.Settings_DebugLoggingDescription)
            .OnValueChanged(newValue => DebugLogging = newValue);

        [LocalizedString]
        public const string NaturalWeaponStackingDescription = "Natural weapon stacking";
        [LocalizedString]
        public const string NaturalWeaponStackingLongDescription =
            "Previously, if you got multiple natural attacks of the same type from different " +
            "features/buffs/etc. you would get extra attacks per round. This was 'fixed' by Owlcat at " +
            "some point so now extra natural attacks give no benefit to PCs.\n" +
            "With this enabled, vanilla behaviour is replaced with an approximation of the tabletop rules:\n" +
            "Addtional natural attacks of the same kind gives a stacking increase to the effective size " +
            "of the 'weapon' (eg. 2 pairs of Medium claw attacks effectively grant 1 pair of Large claw " +
            "attacks instead).\n" +
            "You get all 'enchantment' effects (eg. fire damage/DR penetration) but multiple enchants " +
            "of the same type do not stack.";
        private static Toggle NaturalWeaponStackingToggle =>
            CreateSettingToggle(
                nameof(NaturalWeaponStacking),
                defaultValue: true,
                description: LocalizedStrings.Settings_NaturalWeaponStackingDescription,
                longDescription: LocalizedStrings.Settings_NaturalWeaponStackingLongDescription)
            .OnValueChanged(newValue => NaturalWeaponStacking.Enabled = newValue);

        [LocalizedString]
        public const string DollRoomColorAdjustmentsFilterToggleDescription = "\"Color Adjustments\"";
        private static Toggle DollRoomColorAdjustmentsFilterToggle =>
            CreateSettingToggle(
                nameof(DollRoomFilters.ColorAdjustmentsFilter),
                defaultValue: false,
                description: LocalizedStrings.Settings_DollRoomColorAdjustmentsFilterToggleDescription)
            .OnValueChanged(newValue => DollRoomFilters.ColorAdjustmentsFilter = newValue);

        [LocalizedString]
        public const string DollRoomSlopePowerOffsetFilterToggleDescription = "\"Slope Power Offset\"";
        private static Toggle DollRoomSlopePowerOffsetFilterToggle =>
            CreateSettingToggle(
                nameof(DollRoomFilters.SlopePowerOffsetFilter),
                defaultValue: true,
                description: LocalizedStrings.Settings_DollRoomSlopePowerOffsetFilterToggleDescription)
            .OnValueChanged(newValue => DollRoomFilters.SlopePowerOffsetFilter = newValue);

        [LocalizedString]
        public const string Title = "Miscellaneous Tweaks & Fixes";
        [LocalizedString]
        public const string FixesSubHeading = "Fixes";
        [LocalizedString]
        public const string TweaksSubHeading = "Tweaks";
        [LocalizedString]
        public const string DollroomFilters = "Dollroom post-processing filters";
        [LocalizedString]
        public const string DebugSubHeading = "Debug options";

        internal static void SettingsInit()
        {
            var settings = SettingsBuilder
                .New(SettingsRootKey, LocalizedStrings.Settings_Title)
                
                .AddSubHeader(LocalizedStrings.Settings_FixesSubHeading)
                .AddToggle(BookOfDreamsToggle)
                .AddToggle(StrengthBlessingMajorFixToggle)
                
                .AddSubHeader(LocalizedStrings.Settings_TweaksSubHeading)
                .AddToggle(NaturalWeaponStackingToggle)
                .AddToggle(ReformedFiendDRToggle)

                .AddSubHeader(LocalizedStrings.Settings_DollroomFilters)
                .AddToggle(DollRoomColorAdjustmentsFilterToggle)
                .AddToggle(DollRoomSlopePowerOffsetFilterToggle)

                .AddSubHeader(LocalizedStrings.Settings_DebugSubHeading)
                .AddToggle(DebugLogToggle);

            ModMenu.ModMenu.AddSettings(settings);
        }

        [Init]
        internal static void Init() => 
            Triggers.BlueprintsCache_Init_Early.Take(1).Subscribe(_ => SettingsInit());
    }
}

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

//            }
//        } 
//    }
//}
