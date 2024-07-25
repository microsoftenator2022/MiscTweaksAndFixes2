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
using MicroWrath.Util.Linq;

using Newtonsoft.Json;

using UnityEngine;

using static MiscTweaksAndFixes.Fixes.WeaponPrefabRotationConfig;

namespace MiscTweaksAndFixes.Fixes;

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
            .AppendLine($"Main Hand rotation: {this.MainHandRotation}")
            .AppendLine($"Off Hand rotation: {this.OffHandRotation}");

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
    private const string JsonFileName = "WeaponRotationCorrections.json";
    internal static bool Enabled = true;
    internal static bool EditMode =
#if DEBUG
        true;
#else
        false;
#endif

    static readonly WeaponPrefabRotationConfig ExampleFalcata;
    static readonly WeaponPrefabRotationConfig ExampleFalcataSheath;

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

        foreach (var (slot, r) in sheathSlots)
        {
            ExampleFalcata.SheathModelRotations[slot] = r;
        }

        ExampleFalcataSheath = new()
        {
            Type = ConfigType.SheathOverride,
            AssetId = "d59747b0b894180468a17bb445772e92",
            SheathModelRotations = ExampleFalcata.SheathModelRotations
        };
    }

    static List<WeaponPrefabRotationConfig>? configs = null;

    static List<WeaponPrefabRotationConfig> LoadConfigs(string path) =>
        JsonConvert.DeserializeObject<List<WeaponPrefabRotationConfig>>(File.ReadAllText(path));

    static void SaveConfigs(string path) =>
        File.WriteAllText(path, JsonConvert.SerializeObject(Configs, Formatting.Indented));

    static readonly Lazy<string> ConfigPath = new(() =>
    {
        var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), JsonFileName);

        MicroLogger.Debug(() => $"{nameof(WeaponPrefabOrientationFixes)} config path: {path}");

        return path;
    });

    static List<WeaponPrefabRotationConfig> Configs
    {
        get
        {
            if (configs is not null && !EditMode)
                return configs;

            var path = ConfigPath.Value;

            if (!File.Exists(path))
            {
                MicroLogger.Debug(() => $"Creating new {JsonFileName} config");

                File.WriteAllText(path, JsonConvert.SerializeObject(
                    new List<WeaponPrefabRotationConfig>()
                    {
                        ExampleFalcata,
                        ExampleFalcataSheath
                    }, Formatting.Indented));
            }
            try
            {
                configs = LoadConfigs(path);
            }
            catch(Exception ex)
            {
                MicroLogger.Error($"Failed to read config from {path}", ex);
            }

            return configs!;
        }
    }

    static WeaponPrefabRotationConfig? GetWeaponConfig(WeaponVisualParameters wvp) => 
        Configs?.FirstOrDefault(config =>
            config.Type == ConfigType.Weapon &&
            config.AssetId == wvp.m_WeaponModel?.AssetId);

    static WeaponPrefabRotationConfig? GetSheathConfig(WeaponVisualParameters wvp)
    {
        //if ((wvp.m_WeaponSheathModelOverride?.AssetId).IsNullOrEmpty())
        //    return GetWeaponConfig(wvp);

        return Configs?.FirstOrDefault(config =>
            config.Type == ConfigType.SheathOverride &&
            config.AssetId == wvp.m_WeaponSheathModelOverride?.AssetId);
    }

    static WeaponPrefabRotationConfig? GetBeltConfig(WeaponVisualParameters wvp)
    {
        //if ((wvp.m_WeaponBeltModelOverride?.AssetId).IsNullOrEmpty())
        //    return GetWeaponConfig(wvp);

        return Configs?.FirstOrDefault(config =>
            config.Type == ConfigType.BeltOverride &&
            config.AssetId == wvp.m_WeaponBeltModelOverride?.AssetId);
    }

    static void AutoAlignWeaponSheath(UnitViewHandSlotData hsd, AutoAlignType autoAlignType)
    {
        if (hsd.VisualModel == null || hsd.SheathVisualModel == null)
            return;

        var weaponRenderer = hsd.VisualModel.GetComponentInChildren<MeshRenderer>();
        var sheathRenderer = hsd.VisualModel.GetComponentInChildren<MeshRenderer>();

        if (weaponRenderer == null || sheathRenderer == null)
            return;

        switch (autoAlignType)
        {
            case AutoAlignType.WeaponPriority:
                sheathRenderer.transform.localEulerAngles =  weaponRenderer.transform.localEulerAngles;
                break;
            case AutoAlignType.SheathPriority:
                weaponRenderer.transform.localEulerAngles = sheathRenderer.transform.localEulerAngles;
                break;
        }
    }

    static AutoAlignType ConfigureWeapon(UnitViewHandSlotData hsd)
    {
        var none = AutoAlignType.None;

        if (hsd.VisualModel == null)
            return none;

        var weaponRenderer = hsd.VisualModel.GetComponentInChildren<MeshRenderer>();

        if (weaponRenderer == null)
            return none;

        var weaponConfig = GetWeaponConfig(hsd.VisibleItemVisualParameters);
        MicroLogger.Debug(() => $"Weapon config: {weaponConfig}");

        var beltConfig = GetBeltConfig(hsd.VisibleItemVisualParameters) ?? weaponConfig;

        if (weaponConfig is null && beltConfig is null)
            return none;

        if (weaponConfig is not null &&
            weaponConfig.UseHandRotation && hsd.VisualModel.transform.parent == hsd.HandTransform)
        {
            if (weaponConfig.EnableMainHandRotation && hsd.HandTransform == hsd.MainHandTransform)
            {
                MicroLogger.Debug(() => $"Setting main hand rotation {weaponConfig.MainHandRotation}");
                weaponRenderer.transform.localEulerAngles = weaponConfig.MainHandRotation;
            }
            else if (weaponConfig.EnableOffHandRotation && hsd.HandTransform == hsd.OffHandTransform)
            {
                MicroLogger.Debug(() => $"Setting off hand rotation {weaponConfig.OffHandRotation}");
                weaponRenderer.transform.localEulerAngles = weaponConfig.OffHandRotation;
            }

            if (!hsd.Owner.Descriptor.IsLeftHanded &&
                weaponConfig.MirrorOffHand &&
                hsd.HandTransform == hsd.OffHandTransform)
            {
                var s1 = hsd.VisualModel.transform.localScale;
                var s2 = new Vector3(-s1.x, s1.y, s1.z);

                MicroLogger.Debug(() => $"Setting off hand mirror: {s1} -> {s2}");

                hsd.VisualModel.transform.localScale = s2;
            }

            return none;
        }

        MicroLogger.Debug(() => $"Belt config: {beltConfig}");

        if (beltConfig is null)
            return none;

        if (beltConfig.BeltModelRotations.TryGetValue(hsd.VisualSlot, out var beltRotation))
        {
            MicroLogger.Debug(() => $"Setting weapon rotation: {beltRotation}");

            weaponRenderer.transform.localEulerAngles = beltRotation;
            
            return none;
        }

        return beltConfig.WeaponSheathAutoAlignment;
    }

    static AutoAlignType ConfigureSheath(UnitViewHandSlotData hsd)
    {
        var none = AutoAlignType.None;

        if (hsd.SheathVisualModel == null)
            return none;

        var sheathRenderer = hsd.SheathVisualModel.GetComponentInChildren<MeshRenderer>();

        var sheathConfig = GetSheathConfig(hsd.VisibleItemVisualParameters) ?? GetWeaponConfig(hsd.VisibleItemVisualParameters);
        MicroLogger.Debug(() => $"Sheath config: {sheathConfig}");

        if (sheathConfig is null)
            return none;

        if (sheathConfig.RemoveSheath)
        {
            UnityEngine.Object.Destroy(hsd.SheathVisualModel);
            sheathRenderer = null;
        }

        if (sheathRenderer == null)
            return none;

        if (sheathConfig.SheathModelRotations.TryGetValue(hsd.VisualSlot, out var sheathRotation))
        {
            sheathRenderer.transform.localEulerAngles = sheathRotation;

            return none;
        }
        
        return sheathConfig.WeaponSheathAutoAlignment;
    }

    [HarmonyPatch(typeof(UnitViewHandSlotData), nameof(UnitViewHandSlotData.AttachModel), [])]
    [HarmonyPostfix]
    static void AttachModel_Postfix(UnitViewHandSlotData __instance)
    {
        if (!Enabled)
            return;

        if (__instance.VisibleItemVisualParameters is null)
            return;

        //var weaponConfig = GetWeaponConfig(__instance.VisibleItemVisualParameters);
        //var sheathConfig = GetSheathConfig(__instance.VisibleItemVisualParameters) ?? weaponConfig;
        //var beltConfig = GetBeltConfig(__instance.VisibleItemVisualParameters) ?? weaponConfig;

        //if (weaponConfig is null && sheathConfig is null && beltConfig is null)
        //    return;

        //var visualModel = __instance.VisualModel;
        //var weaponRenderer = visualModel != null ? visualModel.GetComponentInChildren<MeshRenderer>() : null;

        //var sheathVisualModel = __instance.SheathVisualModel;
        //var sheathRenderer = sheathVisualModel != null ? sheathVisualModel.GetComponentInChildren<MeshRenderer>() : null;

        MicroLogger.Debug(() => $"Visual model is {__instance.VisualModel}. Sheath model is {__instance.SheathVisualModel}. Slot is {__instance.VisualSlot}.");

        var autoAlignWeapon = ConfigureWeapon(__instance);
        var autoAlignSheath = ConfigureSheath(__instance);

        // Sheath config has priority
        var autoAlign = autoAlignSheath != AutoAlignType.None ? autoAlignSheath : autoAlignWeapon;

        if (autoAlign != AutoAlignType.None)
            AutoAlignWeaponSheath(__instance, autoAlign);

        //MicroLogger.Debug(() => $"Weapon config: {weaponConfig}");
        //MicroLogger.Debug(() => $"Sheath config: {sheathConfig}");
        //MicroLogger.Debug(() => $"Belt config: {beltConfig}");

        //if (sheathConfig is not null && sheathRenderer != null &&
        //    sheathConfig.SheathModelRotations.TryGetValue(__instance.VisualSlot, out var sheathRotation))
        //{
        //    if (sheathConfig.RemoveSheath)
        //    {
        //        UnityEngine.Object.Destroy(sheathVisualModel);
        //        sheathVisualModel = null;
        //        sheathRenderer = null;
        //    }
        //    else
        //    {
        //        MicroLogger.Debug(() => $"Setting sheath config {sheathRotation}");

        //        sheathRenderer.transform.localEulerAngles = sheathRotation;
        //    }
        //}

        //if (weaponRenderer == null)
        //    return;

        //if (visualModel != null)
        //{
        //    if (weaponConfig is not null && visualModel.transform.parent == __instance.HandTransform)
        //    {
        //        if (weaponConfig.UseHandRotation)
        //        {
        //            if (weaponConfig.EnableMainHandRotation && __instance.HandTransform == __instance.MainHandTransform)
        //            {
        //                MicroLogger.Debug(() => $"Setting main hand rotation {weaponConfig.MainHandRotation}");
        //                weaponRenderer.transform.localEulerAngles = weaponConfig.MainHandRotation;
        //            }
        //            else if (weaponConfig.EnableOffHandRotation && __instance.HandTransform == __instance.OffHandTransform)
        //            {
        //                MicroLogger.Debug(() => $"Setting off hand rotation {weaponConfig.OffHandRotation}");
        //                weaponRenderer.transform.localEulerAngles = weaponConfig.OffHandRotation;
        //            }
        //        }
        //    }
        //    else if (beltConfig is not null && beltConfig.BeltModelRotations.TryGetValue(__instance.VisualSlot, out var beltRotation))
        //    {
        //        weaponRenderer.transform.localEulerAngles = beltRotation;
        //    }
        //    else if (sheathRenderer != null)
        //    {
        //        switch (beltConfig?.WeaponSheathAutoAlignment ?? weaponConfig?.WeaponSheathAutoAlignment ?? AutoAlignType.None)
        //        {
        //            case AutoAlignType.SheathPriority:
        //                weaponRenderer.transform.localEulerAngles = sheathRenderer.transform.localEulerAngles;
        //                break;
        //            case AutoAlignType.WeaponPriority:
        //                sheathRenderer.transform.localEulerAngles = weaponRenderer.transform.localEulerAngles;
        //                break;
        //        }
        //    }
        //}

        //MeshRenderer ren = __instance.SheathVisualModel?.GetComponentInChildren<MeshRenderer>();
        //bool flag = ren?.gameObject.name == "OHW_FalcataTaldor_Scabbard";
        //if (!flag)
        //    return;

        //if (backSlots.Contains(__instance.VisualSlot))
        //    ren.gameObject.transform.localEulerAngles = vec1;
        //else if (__instance.VisualSlot is UnitEquipmentVisualSlotType.RightFront01)
        //    ren.gameObject.transform.localEulerAngles = vec2;
        //MeshRenderer r2 = __instance.VisualModel?.GetComponentInChildren<MeshRenderer>();
        //if (r2 == null)
        //    return;

        //if (__instance.VisualModel.transform.parent == __instance.HandTransform)
        //    r2.transform.localEulerAngles = vec4;
        //else
        //    r2.transform.localEulerAngles = ren.transform.localEulerAngles;
    }
}
