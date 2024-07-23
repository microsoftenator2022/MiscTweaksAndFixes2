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
using MiscTweaksAndFixes.AddedContent.RipAndTear;
using MiscTweaksAndFixes.Tweaks.MythicSuperiorSummoning;

namespace MiscTweaksAndFixes
{
    //[EnableReloading]
    internal static partial class Settings
    {
        // Proof of concept. UMM can have multiple entry points to one assembly
        //internal static void EnableDebugLogging(UnityModManager.ModEntry modEntry)
        //{
        //    modEntry.OnUnload = _ => DisableDebugLogging();

        //    modEntry.OnToggle = (modEntry, state) =>
        //    {
        //        if (state)

        //        {
        //            modEntry.Logger.Log("Enabling debug logging");
                
        //            DebugLogging = true;
        //        }
        //        else
        //        {
        //            modEntry.Logger.Log("Disabling debug logging");

        //            DebugLogging = false;
        //        }

        //        return true;
        //    };
            
        //}

        //internal static bool DisableDebugLogging() => true;

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
                MicroLogger.SetLogLevel(value ? MicroLogger.Severity.Debug : MicroLogger.Severity.Info);
            }
        }

        private static string SettingsRootKey => Main.Instance?.ModEntry?.Info?.Id?.ToLower()!;
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
                description: Localized.BookOfDreamsToggleDescription,
                longDescription: Localized.BookOfDreamsToggleLongDescription,
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
                description: Localized.ReformedFiendDRToggleDescription,
                longDescription: Localized.ReformedFiendDRToggleLongDescription,
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
                description: Localized.StrengthBlessingFixToggleDescription,
                longDescription: Localized.StrengthBlessingFixToggleLongDescription,
                onChanged: value => StrengthBlessingMajorHeavyArmor.Enabled = value);

        [LocalizedString]
        public const string DebugLoggingDescription = "Debug Logging";
        private static Toggle DebugLogToggle =>
            CreateSettingToggle(
                nameof(DebugLogging),
                defaultValue: false,
                description: Localized.DebugLoggingDescription,
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
                description: Localized.NaturalWeaponStackingDescription,
                longDescription: Localized.NaturalWeaponStackingLongDescription,
                onChanged: value => NaturalWeaponStacking.Enabled = value);

        [LocalizedString]
        public const string DollRoomColorAdjustmentsFilterToggleDescription = "\"Color Adjustments\"";
        private static Toggle DollRoomColorAdjustmentsFilterToggle =>
            CreateSettingToggle(
                nameof(DollRoomFilters.ColorAdjustmentsFilter),
                defaultValue: false,
                description: Localized.DollRoomColorAdjustmentsFilterToggleDescription,
                onChanged: value => DollRoomFilters.ColorAdjustmentsFilter = value);

        [LocalizedString]
        public const string DollRoomSlopePowerOffsetFilterToggleDescription = "\"Slope Power Offset\"";
        private static Toggle DollRoomSlopePowerOffsetFilterToggle =>
            CreateSettingToggle(
                nameof(DollRoomFilters.SlopePowerOffsetFilter),
                defaultValue: true,
                description: Localized.DollRoomSlopePowerOffsetFilterToggleDescription,
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
                description: Localized.RipAndTearDescription,
                longDescription: Localized.RipAndTearLongDescription,
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

        [LocalizedString]
        public const string IdentifyNaturalWeaponsDescription = "Identify natural weapons";
        [LocalizedString]
        public const string IdentifyNaturalWeaponsLongDescription =
            "In some cases, enchanted natural weapons require a Knowledge (Arcana) check to identify." +
            " Instead, automatically identify natural weapons on equip.";

        private static Toggle IdentifyNaturalWeaponsToggle =>
            CreateSettingToggle(
                nameof(IdentifyNaturalWeaponsToggle),
                defaultValue: true,
                description: Localized.IdentifyNaturalWeaponsDescription,
                longDescription: Localized.IdentifyNaturalWeaponsLongDescription,
                onChanged: value => NaturalWeapons.IdentifyNaturalWeapons = value);

        [LocalizedString]
        public const string EquipBestEmptyHandWeaponDescription = "Equip best empty hand weapon";
        [LocalizedString]
        public const string EquipBestEmptyHandWeaponLongDescription =
            "When multiple features or buffs provide empty hand reqlacements (eg. claws)" +
            " the game doesn't always select the best empty hand weapon, using the most recent instead." +
            " Force selection of the \"best\" empty hand replacement weapon.\n" +
            "May only be needed if using Natural Weapon Stacking.";

        private static Toggle EquipBestEmptyHandWeaponToggle =>
            CreateSettingToggle(
                nameof(EquipBestEmptyHandWeaponToggle),
                defaultValue: true,
                description: Localized.EquipBestEmptyHandWeaponDescription,
                longDescription: Localized.EquipBestEmptyHandWeaponLongDescription,
                onChanged: value => NaturalWeapons.EquipBestEmptyHandWeapon = value);

        [LocalizedString]
        public const string ZippyMagicBlastsDescription = "Zippy Magic Blasts";
        [LocalizedString]
        public const string ZippyMagicBlastsLongDescription = "Makes Zippy Magic also apply to kineticists' (ranged) energy blasts.\nRequires restart.";

        private static Toggle ZippyMagicBlastsToggle =>
            CreateSettingToggle(nameof(ZippyMagicBlastsToggle),
                defaultValue: false,
                description: Localized.ZippyMagicBlastsDescription,
                longDescription: Localized.ZippyMagicBlastsLongDescription,
                onChanged: value => ZippyMagicBlasts.Enabled = value);

        [LocalizedString]
        public const string BasicBlastsDescription = "Extra basic blasts";
        [LocalizedString]
        public const string BasicBlastsLongDescription =
            "Whenever a kineticist selects an element focus that has more than one blast type (eg. water or air)," +
            " they also gain a weak version of the other basic blast. This blast does not advance in die count and cannot have form" +
            " infusions applied to it. It also cannot qualify as a prerequisite for feats, infusions, wild talents, etc.\nRequires restart.";

        private static Toggle BasicBlastsToggle =>
            CreateSettingToggle(
                nameof(BasicBlastsToggle),
                defaultValue: false,
                description: Localized.BasicBlastsDescription,
                longDescription: Localized.BasicBlastsLongDescription,
                onChanged: value => BasicBlasts.Enabled = value);

        [LocalizedString]
        public const string MythicSuperiorSummoningDescription = Tweaks.MythicSuperiorSummoning.MythicSuperiorSummoning.DisplayName;

        [LocalizedString]
        public const string MythicSuperiorSummoningLongDescription = "A mythic variant of Superior Summoning that adds +1 to any summon ability";

        private static Toggle MythicSuperiorSummoningToggle =>
            CreateSettingToggle(
                nameof(MythicSuperiorSummoningToggle),
                defaultValue: true,
                description: LocalizedStrings.Settings_MythicSuperiorSummoningDescription,
                longDescription: LocalizedStrings.Settings_MythicSuperiorSummoningLongDescription,
                onChanged: value => MythicSuperiorSummoning.Enabled = value
            );

        [LocalizedString]
        public const string WeaponPrefabFixesDescription = "Weapon Prefab Fixes";
        
        [LocalizedString]
        public const string WeaponPrefabFixesToggleDescription = "Enabled";
        private static Toggle WeaponPrefabFixesToggle =>
            CreateSettingToggle(
                nameof(WeaponPrefabFixesToggle),
                defaultValue: true,
                description: Localized.WeaponPrefabFixesToggleDescription,
                onChanged: value => WeaponPrefabOrientationFixes.Enabled = value);

        [LocalizedString]
        public const string WeaponPrefabFixesEditModeToggleDescription = "Edit mode";

        private static Toggle WeaponPrefabFixesEditModeToggle =>
            CreateSettingToggle(
                nameof(WeaponPrefabFixesEditModeToggle),
                defaultValue: false,
                description: Localized.WeaponPrefabFixesEditModeToggleDescription,
                onChanged: value => WeaponPrefabOrientationFixes.EditMode = value);

        internal static void SettingsInit()
        {
            var settings = SettingsBuilder
                .New(SettingsRootKey, LocalizedStrings.Settings_Title)
                
                .AddSubHeader(LocalizedStrings.Settings_FixesSubHeading)
                .AddToggle(BookOfDreamsToggle)
                .AddToggle(StrengthBlessingMajorFixToggle)
                .AddToggle(IdentifyNaturalWeaponsToggle)
                .AddToggle(EquipBestEmptyHandWeaponToggle)
                
                .AddSubHeader(LocalizedStrings.Settings_TweaksSubHeading)
                .AddToggle(NaturalWeaponStackingToggle)
                .AddToggle(ReformedFiendDRToggle)
                .AddToggle(ZippyMagicBlastsToggle)
                .AddToggle(BasicBlastsToggle)
                .AddToggle(MythicSuperiorSummoningToggle)

                .AddSubHeader(LocalizedStrings.Settings_DollroomFilters)
                .AddToggle(DollRoomColorAdjustmentsFilterToggle)
                .AddToggle(DollRoomSlopePowerOffsetFilterToggle)

                .AddSubHeader(LocalizedStrings.Settings_RipAndTearHeader)
                .AddToggle(RipAndTearToggle)

                .AddSubHeader(Localized.WeaponPrefabFixesDescription)
                .AddToggle(WeaponPrefabFixesToggle)
                .AddToggle(WeaponPrefabFixesEditModeToggle)

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
            Triggers.BlueprintsCache_Init_Early.Subscribe(_ => SettingsInit());
    }
}
