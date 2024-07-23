using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Epic.OnlineServices;

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
    
    public Vector3? HandRotation = null;

    public AutoAlignType WeaponSheathAutoAlignment = AutoAlignType.SheathPriority;

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{nameof(WeaponPrefabRotationConfig)} {this.AssetId} {this.Type}");
        sb.AppendLine($"Hand rotation: {this.HandRotation}");
        
        sb.Append("Belt rotations:");
        foreach (var (key, value) in this.BeltModelRotations.Select(pair => (pair.Key, pair.Value)))
        {
            sb.AppendLine();
            sb.Append($"{key}: {value}");
        }

        sb.AppendLine();

        sb.Append("Sheath rotations:");
        foreach (var (key, value) in this.SheathModelRotations.Select(pair => (pair.Key, pair.Value)))
        {
            sb.AppendLine();
            sb.Append($"{key}: {value}");
        }

        return sb.ToString();
    }
}

[HarmonyPatch]
internal static class WeaponPrefabOrientationFixes
{
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
            HandRotation =  new(0, 90, 0)
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

    static List<WeaponPrefabRotationConfig> LoadConfigs(string path)
    {
        return JsonConvert.DeserializeObject<List<WeaponPrefabRotationConfig>>(File.ReadAllText(path));
    }

    static void SaveConfigs(string path) =>
        File.WriteAllText(path, JsonConvert.SerializeObject(Configs, Formatting.Indented));

    static List<WeaponPrefabRotationConfig> Configs
    {
        get
        {
            if (configs is not null && !EditMode)
                return configs;

            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "WeaponRotationCorrections.json");

            if (!File.Exists(path))
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(
                    new List<WeaponPrefabRotationConfig>()
                    {
                        ExampleFalcata,
                        ExampleFalcataSheath
                    }, Formatting.Indented));
            }

            return configs = LoadConfigs(path);
        }
    }

    static WeaponPrefabRotationConfig? GetWeaponConfig(WeaponVisualParameters wvp) => 
        Configs?.FirstOrDefault(config =>
            config.Type == ConfigType.Weapon &&
            config.AssetId == wvp.m_WeaponModel?.AssetId);

    static WeaponPrefabRotationConfig? GetSheathConfig(WeaponVisualParameters wvp)
    {
        if ((wvp.m_WeaponSheathModelOverride?.AssetId).IsNullOrEmpty())
            return GetWeaponConfig(wvp);

        return Configs?.FirstOrDefault(config =>
            config.Type == ConfigType.SheathOverride &&
            config.AssetId == wvp.m_WeaponSheathModelOverride?.AssetId);
    }

    static WeaponPrefabRotationConfig? GetBeltConfig(WeaponVisualParameters wvp)
    {
        if ((wvp.m_WeaponBeltModelOverride?.AssetId).IsNullOrEmpty())
            return GetWeaponConfig(wvp);

        return Configs?.FirstOrDefault(config =>
            config.Type == ConfigType.BeltOverride &&
            config.AssetId == wvp.m_WeaponBeltModelOverride?.AssetId);
    }

    [HarmonyPatch(typeof(UnitViewHandSlotData), nameof(UnitViewHandSlotData.AttachModel), [])]
    [HarmonyPostfix]
    static void AttachModel_Postfix(UnitViewHandSlotData __instance)
    {
        if (!Enabled)
            return;

        if (__instance.VisibleItemVisualParameters is null)
            return;

        var weaponConfig = GetWeaponConfig(__instance.VisibleItemVisualParameters);
        var sheathConfig = GetSheathConfig(__instance.VisibleItemVisualParameters);
        var beltConfig = GetBeltConfig(__instance.VisibleItemVisualParameters);

        if (weaponConfig is null && sheathConfig is null && beltConfig is null)
            return;

        var visualModel = __instance.VisualModel;
        var weaponRenderer = visualModel != null ? visualModel.GetComponentInChildren<MeshRenderer>() : null;

        var sheathVisualModel = __instance.SheathVisualModel;
        var sheathRenderer = sheathVisualModel != null ? sheathVisualModel.GetComponentInChildren<MeshRenderer>() : null;

        MicroLogger.Debug(() => $"Visual model is {visualModel}. Sheath model is {sheathVisualModel}. Slot is {__instance.VisualSlot}.");
        MicroLogger.Debug(() => $"Weapon config: {weaponConfig}");
        MicroLogger.Debug(() => $"Sheath config: {sheathConfig}");
        MicroLogger.Debug(() => $"Belt config: {beltConfig}");

        if (sheathConfig is not null && sheathRenderer != null &&
            sheathConfig.SheathModelRotations.TryGetValue(__instance.VisualSlot, out var sheathRotation))
        {
            MicroLogger.Debug(() => $"Setting sheath config {sheathRotation}");

            sheathRenderer.transform.localEulerAngles = sheathRotation;
        }

        if (weaponRenderer == null)
            return;
        
        if (beltConfig is not null && beltConfig.BeltModelRotations.TryGetValue(__instance.VisualSlot, out var beltRotation))
        {
            weaponRenderer.transform.localEulerAngles = beltRotation;
        }
        else if (weaponConfig is not null && visualModel != null)
        {
            if (visualModel.transform.parent == __instance.HandTransform && weaponConfig.HandRotation is { } handRotation)
            {
                MicroLogger.Debug(() => $"Setting hand rotation {handRotation}");
                weaponRenderer.transform.localEulerAngles = handRotation;
            }
            else if (weaponConfig.WeaponSheathAutoAlignment != AutoAlignType.None && sheathRenderer != null)
            {
                switch (weaponConfig.WeaponSheathAutoAlignment)
                {
                    case AutoAlignType.SheathPriority:
                        weaponRenderer.transform.localEulerAngles = sheathRenderer.transform.localEulerAngles;
                        break;
                    case AutoAlignType.WeaponPriority:
                        weaponRenderer.transform.localEulerAngles = sheathRenderer.transform.localEulerAngles;
                        break;

                }
            }
        }

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
