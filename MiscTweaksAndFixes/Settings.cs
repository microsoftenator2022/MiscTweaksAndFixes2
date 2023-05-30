using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Localization;

using UniRx;

using UnityModManagerNet;

using ModMenu;
using ModMenu.Settings;

using MicroWrath;
using MicroWrath.Localization;

using MiscTweaksAndFixes.Fixes;
using MiscTweaksAndFixes.Tweaks;
using MiscTweaksAndFixes.Tweaks.NaturalWeaponStacking;
using MiscTweaksAndFixes.AddedContent.RipAndTear;

namespace MiscTweaksAndFixes
{
    //[EnableReloading]
    internal static partial class Settings
    {
        // Proof of concept. UMM can have multiple entry points to one assembly
        internal static void EnableDebugLogging(UnityModManager.ModEntry modEntry)
        {
            modEntry.OnUnload = _ => DisableDebugLogging();

            modEntry.OnToggle = (modEntry, state) =>
            {
                if (state)

                {
                    modEntry.Logger.Log("Enabling debug logging");
                
                    DebugLogging = true;
                }
                else
                {
                    modEntry.Logger.Log("Disabling debug logging");

                    DebugLogging = false;
                }

                return true;
            };
            
        }

        internal static bool DisableDebugLogging() => true;

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

        private static Toggle CreateSettingToggle(string name, LocalizedString description,
            bool defaultValue = true, LocalizedString? longDescription = null, Action<bool>? onChanged = null)
        {
            var key = SettingKey(name);

            MicroLogger.Debug(() => $"New toggle: key = \"{key}\"");

            var toggle = Toggle.New(key, defaultValue, description);

            if (longDescription is not null)
                toggle = toggle.WithLongDescription(longDescription);

            if (onChanged is not null)
            {
                SettingsLoaded.Take(1).Subscribe(_ => onChanged(ModMenu.ModMenu.GetSettingValue<bool>(key)));
                toggle = toggle.OnValueChanged(onChanged);
            }

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
                longDescription: LocalizedStrings.Settings_BookOfDreamsToggleLongDescription,
                onChanged: value => BookOfDreams.Enabled = value);

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
                longDescription: LocalizedStrings.Settings_ReformedFiendDRToggleLongDescription,
                onChanged: value => ReformedFiendDRGood.Enabled = value);

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
                longDescription: LocalizedStrings.Settings_StrengthBlessingFixToggleLongDescription,
                onChanged: value => StrengthBlessingMajorHeavyArmor.Enabled = value);

        [LocalizedString]
        public const string DebugLoggingDescription = "Debug Logging";
        private static Toggle DebugLogToggle =>
            CreateSettingToggle(
                nameof(DebugLogging),
                defaultValue: false,
                description: LocalizedStrings.Settings_DebugLoggingDescription,
                onChanged: value => DebugLogging = value)
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
                longDescription: LocalizedStrings.Settings_NaturalWeaponStackingLongDescription,
                onChanged: value => NaturalWeaponStacking.Enabled = value);

        [LocalizedString]
        public const string DollRoomColorAdjustmentsFilterToggleDescription = "\"Color Adjustments\"";
        private static Toggle DollRoomColorAdjustmentsFilterToggle =>
            CreateSettingToggle(
                nameof(DollRoomFilters.ColorAdjustmentsFilter),
                defaultValue: false,
                description: LocalizedStrings.Settings_DollRoomColorAdjustmentsFilterToggleDescription,
                onChanged: value => DollRoomFilters.ColorAdjustmentsFilter = value);

        [LocalizedString]
        public const string DollRoomSlopePowerOffsetFilterToggleDescription = "\"Slope Power Offset\"";
        private static Toggle DollRoomSlopePowerOffsetFilterToggle =>
            CreateSettingToggle(
                nameof(DollRoomFilters.SlopePowerOffsetFilter),
                defaultValue: true,
                description: LocalizedStrings.Settings_DollRoomSlopePowerOffsetFilterToggleDescription,
                onChanged: value => DollRoomFilters.SlopePowerOffsetFilter = value);

        [LocalizedString]
        public const string RipAndTearHeader = "Rip and tear";
        [LocalizedString]
        public const string RipAndTearDescription = "Rip and Tear";
        [LocalizedString]
        public const string RipAndTearLongDescription = "RIP AND TEAR";
        private static Toggle RipAndTearToggle =>
            CreateSettingToggle(
                nameof(RipAndTear),
                defaultValue: true,
                description: LocalizedStrings.Settings_RipAndTearDescription,
                longDescription: LocalizedStrings.Settings_RipAndTearLongDescription,
                onChanged: value => RipAndTear.Enabled = value);

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

                .AddSubHeader(LocalizedStrings.Settings_RipAndTearHeader)
                .AddToggle(RipAndTearToggle)

                .AddSubHeader(LocalizedStrings.Settings_DebugSubHeading)
                .AddToggle(DebugLogToggle);
            
            ModMenu.ModMenu.AddSettings(settings);

            SettingsLoadedEvent();
        }

        private static event Action SettingsLoadedEvent = () => { };

        private static readonly IObservable<Unit> SettingsLoaded =
            Observable.FromEvent(action => SettingsLoadedEvent += action, action => SettingsLoadedEvent -= action);

        [Init]
        internal static void Init() =>
            Triggers.BlueprintsCache_Init_Early.Take(1).Subscribe(_ => SettingsInit());
    }
}
