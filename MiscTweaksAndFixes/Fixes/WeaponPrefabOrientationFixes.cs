using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Utility;
using Kingmaker.View.Equipment;

using MicroWrath;
using MicroWrath.Util;
using MicroWrath.Util.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

using UnityEngine;

using static MiscTweaksAndFixes.Fixes.WeaponPrefabCorrectionConfig;
using static MiscTweaksAndFixes.Fixes.WeaponPrefabRotationConfig;

namespace MiscTweaksAndFixes.Fixes;

public class WeaponPrefabCorrectionConfig
{
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? Comment;

    [JsonIgnore]
    public string? SourceFileName;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum RotationAlignmentSource
    {
        None,
        Sheath,
        Weapon
    }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? WeaponModelAssetId;

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? SheathOverrideModelAssetId;

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? BeltOverrideModelAssetId;

    public class SlotConfig
    {
        public RotationAlignmentSource WeaponRotationSource;
        public Vector3 WeaponRotation;
        public bool ShouldSerializeWeaponRotation() => WeaponRotationSource is RotationAlignmentSource.Weapon;

        public bool HideSheath = false;
        public RotationAlignmentSource SheathRotationSource;
        public Vector3 SheathRotation;
        public bool ShouldSerializeSheathRotation() => !HideSheath && SheathRotationSource is RotationAlignmentSource.Sheath;

        public override string ToString()
        {
            var weaponRotationString = this.WeaponRotationSource == RotationAlignmentSource.Weapon ? this.WeaponRotation.ToString() : "";
            var sheathRotationString = this.SheathRotationSource == RotationAlignmentSource.Weapon ? this.SheathRotation.ToString() : "";

            return $"Weapon rotation: {this.WeaponRotationSource} {weaponRotationString}. " +
                $"Sheath rotation: {this.SheathRotationSource} {sheathRotationString}. " +
                $"Hide sheath? {this.HideSheath}";
        }
    }

    public Dictionary<UnitEquipmentVisualSlotType, SlotConfig> Slots = [];

    public bool MirrorOffHand = false;

    public bool SetMainHandRotation;
    public Vector3 MainHandRotation;

    public bool SetOffHandRotation;
    public Vector3 OffHandRotation;
    public bool ShouldSerializeMainHandRotation() => SetMainHandRotation;
    public bool ShouldSerializeOffHandRotation() => SetOffHandRotation;

    public RotationAlignmentSource DefaultRotation = RotationAlignmentSource.Sheath;

    public override string ToString()
    {
        var sb = new StringBuilder();
        
        sb = sb
            .AppendLine($"{nameof(WeaponPrefabCorrectionConfig)}")
            .AppendLine($"Source file: {this.SourceFileName}")
            .AppendLine($"Comment: {this.Comment}")
            .AppendLine($"Weapon assetId: {this.WeaponModelAssetId}")
            .AppendLine($"Sheath override assetId: {this.SheathOverrideModelAssetId}")
            .AppendLine($"Belt override assetId: {this.BeltOverrideModelAssetId}")
            .AppendLine($"Main Hand rotation: {this.MainHandRotation}")
            .AppendLine($"Off Hand rotation: {this.OffHandRotation}")
            .AppendLine($"Mirror off hand: {this.MirrorOffHand}");

        sb = sb.Append("Slots config:");

        foreach (var slot in this.Slots)
        {
            sb = sb.AppendLine()
                .Append($"  {slot}");
        }

        return sb.ToString();
    }
}

static class WeaponPrefabConfig
{
    static WeaponVisualParameters? GetVisualSourceWeaponVisualParams(this UnitViewHandSlotData slotData) =>
        (slotData.VisibleItem.VisualSourceItemBlueprint as BlueprintItemWeapon)?.VisualParameters;

    static WeaponVisualParameters? GetVisualSourceWeaponTypeVisualParams(this UnitViewHandSlotData slotData) =>
        (slotData.VisibleItem.VisualSourceItemBlueprint as BlueprintItemWeapon)?.Type.VisualParameters;

    static WeaponVisualParameters? GetWeaponVisualParams(this UnitViewHandSlotData slotData) =>
        (slotData.VisibleItem.Blueprint as BlueprintItemWeapon)?.VisualParameters;

    static WeaponVisualParameters? GetWeaponTypeVisualParams(this UnitViewHandSlotData slotData) =>
        (slotData.VisibleItem.Blueprint as BlueprintItemWeapon)?.Type.VisualParameters;

    static WeaponVisualParameters? VisualParametersSource(this UnitViewHandSlotData slotData)
    {
        bool check(WeaponVisualParameters? wvp) => !(wvp?.m_WeaponModel?.AssetId.IsNullOrEmpty() ?? true);

        var vsw = slotData.GetVisualSourceWeaponVisualParams();
        MicroLogger.Debug(() => $"VisualSourceWeapon: {vsw?.m_WeaponModel?.AssetId}");

        var vswt = slotData.GetVisualSourceWeaponTypeVisualParams();
        MicroLogger.Debug(() => $"VisualSourceWeaponType: {vswt?.m_WeaponModel?.AssetId}");

        var w = slotData.GetWeaponVisualParams();
        MicroLogger.Debug(() => $"Weapon: {w?.m_WeaponModel?.AssetId}");

        var wt = slotData.GetWeaponTypeVisualParams();
        MicroLogger.Debug(() => $"WeaponType: {wt?.m_WeaponModel?.AssetId}");

        if (check(vsw))
            return vsw;
        
        if (check(vswt))
            return vswt;

        if (check(w))
            return w;
        
        if (check(wt))
            return wt;

        return null;
    }

    static string? NullIfEmpty(this string? s) => string.IsNullOrEmpty(s) ? null : s;

    static bool IsMainHand(this UnitViewHandSlotData slotData) =>
        slotData.VisualModel.transform.parent == slotData.MainHandTransform;

    static bool IsOffHand(this UnitViewHandSlotData slotData) =>
        slotData.VisualModel.transform.parent == slotData.OffHandTransform;
    static MeshRenderer? GetWeaponRenderer(this UnitViewHandSlotData slotData) => slotData.VisualModel.GetComponentInChildren<MeshRenderer>();

    static void ApplyHandsCorrection(UnitViewHandSlotData slotData, WeaponPrefabCorrectionConfig config)
    {

        var weaponRenderer = slotData.GetWeaponRenderer();

        if (weaponRenderer == null)
            return;

        if (config.SetMainHandRotation && slotData.IsMainHand())
        {
            MicroLogger.Debug(() => $"Setting main hand rotation {config.MainHandRotation}");
            weaponRenderer.transform.localEulerAngles = config.MainHandRotation;
        }

        if (slotData.IsOffHand())
        {
            if (!slotData.Owner.Descriptor.IsLeftHanded && config.MirrorOffHand)
            {
                var s1 = slotData.VisualModel.transform.localScale;
                var s2 = new Vector3(-s1.x, s1.y, s1.z);

                MicroLogger.Debug(() => $"Setting off hand mirror: {s1} -> {s2}");
                slotData.VisualModel.transform.localScale = s2;
            }

            if (config.SetOffHandRotation)
            {
                MicroLogger.Debug(() => $"Setting off hand rotation {config.OffHandRotation}");
                weaponRenderer.transform.localEulerAngles = config.OffHandRotation;
            }
        }
    }

    static List<WeaponPrefabCorrectionConfig> Configs = [];

    enum PrefabType
    {
        Weapon,
        SheathOverride,
        BeltOverride
    }

    static WeaponPrefabCorrectionConfig? TryGetConfig(
        PrefabType type,
        string? weaponAssetId,
        string? sheathOverrideAssetId,
        string? beltOverrideAssetId) =>
        Configs.Select(config =>
        {
            switch (type)
            {
                case PrefabType.Weapon:
                    if (config.WeaponModelAssetId.IsNullOrEmpty())
                        return (config, count: 0);
                    break;

                case PrefabType.SheathOverride:
                    if (config.SheathOverrideModelAssetId.IsNullOrEmpty())
                        return (config, count: 0);
                    break;

                case PrefabType.BeltOverride:
                    if (!config.BeltOverrideModelAssetId.IsNullOrEmpty())
                        return (config, count: 0);
                    break;
            }

            var count = 0;

            if (!weaponAssetId.IsNullOrEmpty() && config.WeaponModelAssetId == weaponAssetId)
                count++;

            if (!sheathOverrideAssetId.IsNullOrEmpty() && config.SheathOverrideModelAssetId == sheathOverrideAssetId)
                count++;

            if (!beltOverrideAssetId.IsNullOrEmpty() && config.BeltOverrideModelAssetId == beltOverrideAssetId)
                count++;

            return (config, count);
        }).Where(pair => pair.count > 0)
        .OrderByDescending(pair => pair.count)
        .Select(pair => pair.config)
        .FirstOrDefault();

    internal static void ApplyCorrections(UnitViewHandSlotData slotData)
    {
        if (slotData.VisualModel is null)
            return;

        var visualParams = slotData.VisualParametersSource();

        if (visualParams is null)
        {
            MicroLogger.Warning($"Could not find {nameof(WeaponVisualParameters)} source for {slotData.VisibleItem}");
            return;
        }

        var weaponRenderer = slotData.GetWeaponRenderer();

        if (weaponRenderer == null)
        {
            MicroLogger.Warning($"Could not get renderer for {slotData.VisualModel}");
            return;
        }

        var weaponModelAssetId = visualParams.m_WeaponModel?.AssetId.NullIfEmpty();
        var beltModelOverrideAssetId = visualParams.m_WeaponBeltModelOverride?.AssetId.NullIfEmpty();
        var sheathModelOverrideAssetId = visualParams.m_WeaponSheathModelOverride?.AssetId.NullIfEmpty();

        MicroLogger.Debug(() =>
            $"Weapon: {weaponModelAssetId ?? "NULL"} " +
            $"Sheath override: {sheathModelOverrideAssetId ?? "NULL"} " +
            $"Belt override: {beltModelOverrideAssetId ?? "NULL"}");

        var weaponConfig = TryGetConfig(PrefabType.Weapon, weaponModelAssetId, sheathModelOverrideAssetId, beltModelOverrideAssetId);
        MicroLogger.Debug(() =>
        {
            var sb = new StringBuilder();

            sb.Append($"Weapon config: {weaponConfig?.SourceFileName} {weaponConfig?.WeaponModelAssetId}");
            if (weaponConfig?.Comment is string comment && comment != "")
            {
                sb.AppendLine()
                    .Append($"  \"{comment}\"");
            }

            return sb.ToString();
        });

        var beltConfig = TryGetConfig(PrefabType.BeltOverride, weaponModelAssetId, sheathModelOverrideAssetId, beltModelOverrideAssetId);
        MicroLogger.Debug(() =>
        {
            var sb = new StringBuilder();

            sb.Append($"Belt config: {beltConfig?.SourceFileName} {beltConfig?.BeltOverrideModelAssetId}");
            if (beltConfig?.Comment is string comment && comment != "")
            {
                sb.AppendLine()
                    .Append($"  \"{comment}\"");
            }

            return sb.ToString();
        });

        var sheathConfig = TryGetConfig(PrefabType.SheathOverride, weaponModelAssetId, sheathModelOverrideAssetId, beltModelOverrideAssetId);
        MicroLogger.Debug(() =>
        {
            var sb = new StringBuilder();

            sb.Append($"Sheath config: {sheathConfig?.SourceFileName} {sheathConfig?.SheathOverrideModelAssetId}");
            if (sheathConfig?.Comment is string comment && comment != "")
            {
                sb.AppendLine()
                    .Append($"  \"{comment}\"");
            }

            return sb.ToString();
        });

        var weaponSlotConfig = (weaponConfig?.Slots.TryGetValue(slotData.VisualSlot, out var wsConfig) ?? false) ? wsConfig : null;

        var beltSlotConfig = (beltConfig?.Slots.TryGetValue(slotData.VisualSlot, out var bsConfig) ?? false) ? bsConfig : weaponSlotConfig;

        if (weaponConfig is not null && slotData.VisualModel.transform.parent == slotData.HandTransform)
        {
            ApplyHandsCorrection(slotData, weaponConfig);
        }
        else if (beltSlotConfig is not null && beltSlotConfig.WeaponRotationSource is RotationAlignmentSource.Weapon)
        {
            MicroLogger.Debug(() => $"Setting {slotData.VisualSlot} weapon rotation: {beltSlotConfig.WeaponRotation}");
            weaponRenderer.transform.localEulerAngles = beltSlotConfig.WeaponRotation;
        }
        else if (beltConfig is not null && beltSlotConfig is null && beltConfig.DefaultRotation is RotationAlignmentSource.Sheath)
        {
            beltSlotConfig = new() { WeaponRotationSource = RotationAlignmentSource.Sheath };
        }
        else if (weaponConfig is not null && beltSlotConfig is null && weaponConfig.DefaultRotation is RotationAlignmentSource.Sheath)
        {
            beltSlotConfig = new() { WeaponRotationSource = RotationAlignmentSource.Sheath };
        }

        if (slotData.SheathVisualModel == null)
            return;

        var sheathSlotConfig = (sheathConfig?.Slots.TryGetValue(slotData.VisualSlot, out var ssConfig) ?? false) ? ssConfig : weaponSlotConfig;

        var sheathRenderer = slotData.SheathVisualModel.GetComponentInChildren<MeshRenderer>();

        if (sheathSlotConfig is not null)
        {
            if (sheathSlotConfig.SheathRotationSource is RotationAlignmentSource.Sheath)
            {
                MicroLogger.Debug(() => $"Setting {slotData.VisualSlot} sheath rotation: {sheathSlotConfig.SheathRotation}");
                sheathRenderer.transform.localEulerAngles = sheathSlotConfig.SheathRotation;
            }

            if (sheathSlotConfig.HideSheath || (beltSlotConfig?.HideSheath ?? false))
            {
                MicroLogger.Debug(() => $"Hiding sheath {slotData.SheathVisualModel}");
                sheathRenderer.enabled = false;
            }
        }
        else if (sheathConfig is not null && sheathConfig.DefaultRotation is RotationAlignmentSource.Weapon)
        {
            sheathSlotConfig = new() { SheathRotationSource = RotationAlignmentSource.Weapon };
        }

        if (sheathSlotConfig is not null && sheathSlotConfig.SheathRotationSource is RotationAlignmentSource.Weapon)
        {
            MicroLogger.Debug(() => $"Setting sheath rotation from weapon: {weaponRenderer.transform.localEulerAngles}");
            sheathRenderer.transform.localEulerAngles = weaponRenderer.transform.localEulerAngles;
        }

        if (beltSlotConfig is not null && beltSlotConfig.WeaponRotationSource is RotationAlignmentSource.Sheath &&
            slotData.VisualModel.transform.parent != slotData.HandTransform)
        {
            MicroLogger.Debug(() => $"Setting weapon rotation from sheath: {sheathRenderer.transform.localEulerAngles}");
            weaponRenderer.transform.localEulerAngles = sheathRenderer.transform.localEulerAngles;
        }
    }

    static string UpgradeConfigsFile(string fileName, IEnumerable<WeaponPrefabRotationConfig> configs)
    {
        WeaponPrefabCorrectionConfig upgradeConfig(WeaponPrefabRotationConfig oldConfig)
        {
            var newConfig = new WeaponPrefabCorrectionConfig()
            {
                SourceFileName = fileName,
                Comment = AccessTools.Field(typeof(WeaponPrefabRotationConfig), "Comment").GetValue(oldConfig) as string,
            };

            switch (oldConfig.Type)
            {
                case ConfigType.Weapon:
                    newConfig.WeaponModelAssetId = oldConfig.AssetId;
                    break;
                case ConfigType.SheathOverride:
                    newConfig.SheathOverrideModelAssetId = oldConfig.AssetId;
                    break;
                case ConfigType.BeltOverride:
                    newConfig.BeltOverrideModelAssetId = oldConfig.AssetId;
                    break;
            }

            foreach (var beltRotation in oldConfig.BeltModelRotations)
            {
                if (!newConfig.Slots.ContainsKey(beltRotation.Key))
                    newConfig.Slots[beltRotation.Key] = new();

                var slotConfig = newConfig.Slots[beltRotation.Key];

                slotConfig.WeaponRotationSource = RotationAlignmentSource.Weapon;
                slotConfig.WeaponRotation = beltRotation.Value;

                slotConfig.HideSheath = oldConfig.RemoveSheath;
            }

            foreach (var sheathRotation in oldConfig.SheathModelRotations)
            {
                if (!newConfig.Slots.ContainsKey(sheathRotation.Key))
                    newConfig.Slots[sheathRotation.Key] = new();

                var slotConfig = newConfig.Slots[sheathRotation.Key];

                slotConfig.SheathRotationSource = RotationAlignmentSource.Sheath;
                slotConfig.SheathRotation = sheathRotation.Value;

                slotConfig.HideSheath = oldConfig.RemoveSheath;
            }

            if (oldConfig.UseHandRotation)
            {
                if (oldConfig.EnableMainHandRotation)
                {
                    newConfig.SetMainHandRotation = true;
                    newConfig.MainHandRotation = oldConfig.MainHandRotation;
                }

                if (oldConfig.EnableOffHandRotation)
                {
                    newConfig.SetOffHandRotation = true;
                    newConfig.OffHandRotation = oldConfig.OffHandRotation;
                }
            }

            newConfig.MirrorOffHand = oldConfig.MirrorOffHand;

            return newConfig;
        }

        var newConfigs = configs.Select(upgradeConfig);

        return JsonConvert.SerializeObject(newConfigs, Formatting.Indented);
    }

    static void BackupOldConfig(string path)
    {
        var directory = Path.Combine(Path.GetDirectoryName(path), "OldConfigsFormat");

        if (!Directory.Exists(directory))
            _ = Directory.CreateDirectory(directory);

        File.Move(path, Path.Combine(directory,
            (Path.GetFileNameWithoutExtension(path) +
            "_" +
            DateTimeOffset.Now.ToString("s").Replace(":", "_") +
            Path.GetExtension(path))));
    }

    static IEnumerable<WeaponPrefabCorrectionConfig> LoadConfigsFromFile(string path)
    {
        MicroLogger.Debug(() => $"Load from {path}");

        try
        {
            string text = File.ReadAllText(path);

            var ja = JArray.Parse(text);

            if (ja[0] is JObject obj && obj.Properties().Any(p => p.Name == "WeaponSheathAutoAlignment"))
            {
                BackupOldConfig(path);
                text = UpgradeConfigsFile(Path.GetFileName(path), JsonConvert.DeserializeObject<List<WeaponPrefabRotationConfig>>(text));
                File.WriteAllText(path, text);
            }

            return JsonConvert.DeserializeObject<List<WeaponPrefabCorrectionConfig>>(text)
                .Select(c => { c.SourceFileName = Path.GetFileName(path); return c; });
        }
        catch (Exception ex)
        {
            MicroLogger.Error($"Error loading config file {path}", ex);

            return [];
        }
    }

    internal static List<WeaponPrefabCorrectionConfig> LoadConfigs(string configsDirectory)
    {
        if (!WeaponPrefabOrientationFixes.EditMode && Configs.Any())
            return Configs;


        if (!Directory.Exists(configsDirectory))
        {
            //Directory.CreateDirectory(configsDirectory);

            _ = WeaponPrefabOrientationFixes.Configs;
        }

        foreach (var f in Directory.EnumerateFiles(configsDirectory, "*.json"))
        {
            Configs.AddRange(LoadConfigsFromFile(f));
        }

        return Configs;
    }
}

public class WeaponPrefabRotationConfig
{
    public enum ConfigType
    {
        Weapon,
        SheathOverride,
        BeltOverride
    }

    public enum AutoAlignType
    {
        None,
        WeaponPriority,
        SheathPriority
    }

    [JsonProperty]
    string Comment = "";

    public string AssetId = "";
    public ConfigType Type = ConfigType.Weapon;

    public Dictionary<UnitEquipmentVisualSlotType, Vector3> BeltModelRotations = [];
    public Dictionary<UnitEquipmentVisualSlotType, Vector3> SheathModelRotations = [];
    
    public bool UseHandRotation = false;
    
    public bool EnableMainHandRotation = false;
    public Vector3 MainHandRotation = default;

    public bool EnableOffHandRotation = false;
    public Vector3 OffHandRotation = default;

    public AutoAlignType WeaponSheathAutoAlignment = AutoAlignType.SheathPriority;

    public bool RemoveSheath = false;

    public bool MirrorOffHand = false;

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb = sb
            .AppendLine($"{nameof(WeaponPrefabRotationConfig)} {this.AssetId} {this.Type}")
            .AppendLine($"Comment: {this.Comment}")
            .AppendLine($"Main Hand rotation: {this.MainHandRotation}")
            .AppendLine($"Off Hand rotation: {this.OffHandRotation}")
            .AppendLine($"Remove sheath: {this.RemoveSheath}")
            .AppendLine($"Mirror off hand: {this.MirrorOffHand}");

        sb = sb.Append("Belt rotations:");
        foreach (var (key, value) in this.BeltModelRotations.Select(pair => (pair.Key, pair.Value)))
        {
            sb = sb
                .AppendLine()
                .Append($"{key}: {value}");
        }

        sb = sb.AppendLine();

        sb = sb.Append("Sheath rotations:");
        foreach (var (key, value) in this.SheathModelRotations.Select(pair => (pair.Key, pair.Value)))
        {
            sb = sb
                .AppendLine()
                .Append($"{key}: {value}");
        }

        return sb.ToString();
    }
}

[HarmonyPatch]
internal static class WeaponPrefabOrientationFixes
{
    const string ConfigDirectoryName = "WeaponPrefabCorrections";
    internal static bool Enabled = true;
    internal static bool EditMode =
#if DEBUG
        true;
#else
        false;
#endif

    internal static readonly WeaponPrefabRotationConfig ExampleFalcata;
    internal static readonly WeaponPrefabRotationConfig ExampleFalcataSheath;

    static WeaponPrefabOrientationFixes()
    {
        var sheathSlots = new[]
        {
            UnitEquipmentVisualSlotType.LeftBack01,
            UnitEquipmentVisualSlotType.LeftBack02,
            UnitEquipmentVisualSlotType.RightBack01,
            UnitEquipmentVisualSlotType.RightBack02,
            UnitEquipmentVisualSlotType.LeftFront01
        }
        .Select(slot => (slot, new Vector3(0, 90, 0)))
        .Append((UnitEquipmentVisualSlotType.RightFront01, new Vector3(90, 90, 0)));

        ExampleFalcata = new()
        {
            AssetId = "d26b2020e3ab8674cbf002c91b7d97a2",
            UseHandRotation = true,
            EnableMainHandRotation = true,
            MainHandRotation = new(0, 90, 0),
            EnableOffHandRotation = true,
            OffHandRotation = new(0, 90, 0),
            MirrorOffHand = true
        };

        ExampleFalcataSheath = new()
        {
            Type = ConfigType.SheathOverride,
            AssetId = "d59747b0b894180468a17bb445772e92",
        };

        foreach (var (slot, r) in sheathSlots)
        {
            ExampleFalcataSheath.SheathModelRotations[slot] = r;
        }

        ExampleFalcata.BeltModelRotations = ExampleFalcataSheath.SheathModelRotations;
    }

    static List<WeaponPrefabRotationConfig>? configs = null;

    static List<WeaponPrefabRotationConfig> LoadConfigs(string path) =>
        JsonConvert.DeserializeObject<List<WeaponPrefabRotationConfig>>(File.ReadAllText(path));

    internal static IEnumerable<WeaponPrefabRotationConfig> LoadConfigsFromDirectory(string dir)
    {
        foreach (var f in Directory.EnumerateFiles(dir, "*.json"))
        {
            List<WeaponPrefabRotationConfig> loadConfigsSafe()
            {
                try
                {
                    return LoadConfigs(f);
                }
                catch(Exception ex)
                {
                    MicroLogger.Error($"Failed to read config from {f}", ex);
                    return [];
                }
            }

            foreach (var config in loadConfigsSafe())
            {
                yield return config;
            }
        }
    }

    //static void SaveConfigs(string path) =>
    //    File.WriteAllText(path, JsonConvert.SerializeObject(Configs, Formatting.Indented));

    static readonly Lazy<string> ConfigPath = new(() =>
    {
        var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ConfigDirectoryName);
        MicroLogger.Debug(() => $"{nameof(WeaponPrefabOrientationFixes)} config path: {path}");

        return path;
    });

    internal static IEnumerable<WeaponPrefabRotationConfig> Configs
    {
        get
        {
            if (configs is not null && !EditMode)
                return configs;

            var path = ConfigPath.Value;

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);

                MicroLogger.Debug(() => $"Creating sample config");

                File.WriteAllText(Path.Combine(path, "CeremonialFalcataSample.json"), JsonConvert.SerializeObject(
                    new List<WeaponPrefabRotationConfig>()
                    {
                        ExampleFalcata,
                        ExampleFalcataSheath
                    }, Formatting.Indented));
            }

            return LoadConfigsFromDirectory(path);
        }
    }

    [HarmonyPatch(typeof(UnitViewHandSlotData), nameof(UnitViewHandSlotData.AttachModel), [])]
    [HarmonyPostfix]
    static void AttachModel_Postfix(UnitViewHandSlotData __instance)
    {
        if (!Enabled)
            return;

        if (__instance.VisibleItemVisualParameters is null)
            return;

        MicroLogger.Debug(() => $"Visual model is {__instance.VisualModel}. Sheath model is {__instance.SheathVisualModel}. Slot is {__instance.VisualSlot}.");

        WeaponPrefabConfig.LoadConfigs(ConfigPath.Value);

        WeaponPrefabConfig.ApplyCorrections(__instance);
    }
}
