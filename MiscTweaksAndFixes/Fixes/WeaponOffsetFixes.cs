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

using UniRx;

using UnityEngine;

namespace MiscTweaksAndFixes.Fixes;

internal class WeaponOffsetsCorrectionConfig
{
    public record class OffsetsData(
        EquipmentOffsets.Offsets MainHand,
        EquipmentOffsets.Offsets DollRoomMainHand,
        EquipmentOffsets.Offsets OffHand,
        EquipmentOffsets.Offsets DollRoomOffHand,
        IDictionary<UnitEquipmentVisualSlotType, EquipmentOffsets.Offsets> SlotOffsets,
        bool MirrorOffHand = false)
    {
        public HashSet<UnitEquipmentVisualSlotType> HideSheath { get; init; } = [];

        public OffsetsData(EquipmentOffsets offsets) : this(
            offsets.m_MainHand,
            offsets.m_DollRoomMainHand,
            offsets.m_OffHand,
            offsets.m_DollRoomOffHand,
            offsets.m_SlotOffsets
                .EmptyIfNull()
                .SelectMany(Functional.Identity)
                .Indexed()
                .Where(pair => pair.item.Rotation != default || pair.item.Position != default)
                .Select(pair => ((UnitEquipmentVisualSlotType)pair.index, pair.item))
                .ToDictionary())
        { }

        public OffsetsData() : this(new(), new(), new(), new(), new Dictionary<UnitEquipmentVisualSlotType, EquipmentOffsets.Offsets>()) { }

        public void Apply(EquipmentOffsets offsets)
        {
            offsets.m_MainHand = this.MainHand;
            offsets.m_DollRoomMainHand = this.DollRoomMainHand;
            offsets.m_OffHand = this.OffHand;
            offsets.m_DollRoomOffHand = this.DollRoomOffHand;

            var slotOffsets = !this.SlotOffsets.Keys.Empty() ? new EquipmentOffsets.Offsets[this.SlotOffsets.Keys.Select(k => (int)k).Max() + 1] : [];

            for (var i = 0; i < slotOffsets.Length; i++)
            {
                var slot = (UnitEquipmentVisualSlotType)i;
                if (this.SlotOffsets.ContainsKey(slot))
                    slotOffsets[i] = this.SlotOffsets[slot];
                else
                    slotOffsets[i] ??= new();
            }

            offsets.m_SlotOffsets = slotOffsets;
        }
    }

    public string? WeaponModelAssetId;
    public string? SheathModelOverrideAssetId;
    //public string? BeltModelOverrideAssetId;

    public string? WeaponOffsets;
    public string? SheathOffsets;

    [JsonIgnore]
    OffsetsData? weaponOffsetsData;

    [JsonIgnore]
    public OffsetsData? WeaponOffsetsData
    {
        get
        {
            if (this.WeaponOffsets is null)
                return null;

            try
            {
                MicroLogger.Debug(() => $"Loading {this.WeaponOffsets}");

                return weaponOffsetsData ??=
                    JsonConvert.DeserializeObject<OffsetsData>(
                        File.ReadAllText(Path.Combine(WeaponOffsetFixes.ConfigsDir, this.WeaponOffsets)));
            }
            catch (Exception ex)
            {
                MicroLogger.Error($"Error loading from {this.WeaponOffsets}", ex);

                return null;
            }
        }
    }

    [JsonIgnore]
    OffsetsData? sheathOffsetsData;

    [JsonIgnore]
    public OffsetsData? SheathOffsetsData
    {
        get
        {
            if (this.SheathOffsets is null)
                return null;

            try
            {
                MicroLogger.Debug(() => $"Loading {this.SheathOffsets}");

                return sheathOffsetsData ??=
                    JsonConvert.DeserializeObject<OffsetsData>(
                        File.ReadAllText(Path.Combine(WeaponOffsetFixes.ConfigsDir, this.SheathOffsets)));
            }
            catch (Exception ex)
            {
                MicroLogger.Error($"Error loading from {this.SheathOffsets}", ex);

                return null;
            }
        }
    }
}

internal class EquipmentOffsetsCorrection : MonoBehaviour
{
    public WeaponOffsetsCorrectionConfig.OffsetsData? Original;
    public WeaponOffsetsCorrectionConfig.OffsetsData? Offsets;
}

[HarmonyPatch]
internal static partial class WeaponOffsetFixes
{
    static string OffsetsToJson(EquipmentOffsets? offsets)
    {
        EquipmentOffsets.Offsets mainHand;
        EquipmentOffsets.Offsets dollRoomMainHand;
        EquipmentOffsets.Offsets offHand;
        EquipmentOffsets.Offsets dollRoomOffHand;
        IDictionary<UnitEquipmentVisualSlotType, EquipmentOffsets.Offsets> slotOffsets;

        if (offsets != null)
        {
            mainHand = offsets.m_MainHand;
            dollRoomMainHand = offsets.m_DollRoomMainHand;
            offHand = offsets.m_OffHand;
            dollRoomOffHand = offsets.m_DollRoomOffHand;
            slotOffsets = offsets.m_SlotOffsets
                .Indexed()
                .Where(pair => pair.item.Rotation != default || pair.item.Position != default)
                .Select(pair => ((UnitEquipmentVisualSlotType)pair.index, pair.item))
                .ToDictionary();
        }
        else
        {
            mainHand = new();
            dollRoomMainHand = new();
            offHand = new();
            dollRoomOffHand = new();
            slotOffsets = new Dictionary<UnitEquipmentVisualSlotType, EquipmentOffsets.Offsets>();
        }
        
        var data = new WeaponOffsetsCorrectionConfig.OffsetsData(mainHand, dollRoomMainHand, offHand, dollRoomOffHand, slotOffsets);
        return JsonConvert.SerializeObject(data, Formatting.Indented);
    }

    static void DumpOffsets(GameObject model)
    {
        var equipmentOffsets = model.GetComponent<EquipmentOffsets>();

        var path = Path.Combine(ConfigsDir, $"{model.name.Replace("(Clone)", "")}.{nameof(WeaponOffsetsCorrectionConfig.OffsetsData)}.json");

        if (!File.Exists(path))
            File.WriteAllText(path, OffsetsToJson(equipmentOffsets));
    }

    internal static bool Enabled = true;
    internal static bool EditMode = true;

    static WeaponOffsetsCorrectionConfig[] Configs = [];

    internal static string ConfigsDir
    {
        get
        {
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "WeaponPrefabCorrections");

            if (!Directory.Exists(path))
                _ = Directory.CreateDirectory(path);

            return path;
        }
    }

    static IEnumerable<WeaponOffsetsCorrectionConfig> GetConfigs(string directory)
    {
        if (Configs.Length < 1 || EditMode)
        {
            MicroLogger.Debug(() => "Loading configs");

            Configs = Directory.EnumerateFiles(directory, $"*.{nameof(EquipmentOffsetsCorrection)}.json")
                .Select(f =>
                {
                    try
                    {
                        var text = File.ReadAllText(f);

                        return JsonConvert.DeserializeObject<WeaponOffsetsCorrectionConfig>(text);
                    }
                    catch (Exception ex)
                    {
                        MicroLogger.Error($"Error reading config {f}", ex);

                        return null;
                    }
                })
                .SkipIfNull()
                .ToArray();
        }

        return Configs;
    }

    static IEnumerable<WeaponOffsetsCorrectionConfig> GetConfigs() => GetConfigs(ConfigsDir);

    static WeaponOffsetsCorrectionConfig? TryLoadOffsetsCorrection(string weaponAssetId, string? sheathAssetId)
    {
        if (weaponAssetId is null && sheathAssetId is null)
            return null;

        var config = GetConfigs().FirstOrDefault(c => c.WeaponModelAssetId == weaponAssetId && c.SheathModelOverrideAssetId == sheathAssetId);

        return config ?? GetConfigs().FirstOrDefault(c =>
            (c.WeaponModelAssetId == weaponAssetId && c.SheathModelOverrideAssetId is null) ||
            (c.SheathModelOverrideAssetId is null && c.SheathModelOverrideAssetId == sheathAssetId));
    }

    static void ApplyOffsets(WeaponOffsetsCorrectionConfig.OffsetsData? offsets, GameObject model)
    {
        var equipmentOffsets = model.GetComponent<EquipmentOffsets>();
        var correction = model.GetComponent<EquipmentOffsetsCorrection>();

        if (offsets is not null)
        {
            if (equipmentOffsets == null)
            {
                equipmentOffsets = model.AddComponent<EquipmentOffsets>();
                InitializeEquipmentOffsets(equipmentOffsets);
            }

            if (correction == null)
            {
                correction = model.AddComponent<EquipmentOffsetsCorrection>();
            }

            correction.Original ??= new(equipmentOffsets);

            correction.Offsets = offsets;
        }
        else if (EditMode)
        {
            if (correction == null || correction.Offsets is null)
            {
                DumpOffsets(model);
            }
        }

        if (correction != null && equipmentOffsets != null)
        {
            correction.Offsets?.Apply(equipmentOffsets);
        }
    }

    static void ApplyWeaponOffsets(WeaponOffsetsCorrectionConfig config, GameObject model)
    {
        if (config.WeaponModelAssetId is not { })
            return;

        ApplyOffsets(config.WeaponOffsetsData, model);
    }

    static void ApplySheathOffsets(WeaponOffsetsCorrectionConfig config, UnitEquipmentVisualSlotType slot, GameObject sheathModel, GameObject weaponModel)
    {
        if (config.SheathModelOverrideAssetId is not { })
            return;

        var mr = sheathModel.GetComponentInChildren<MeshRenderer>();

        var correction = sheathModel.GetComponent<EquipmentOffsetsCorrection>();

        if (config.SheathOffsetsData is null)
        {
            if (EditMode) DumpOffsets(sheathModel);

            return;
        }

        if (correction == null)
        {
            correction = sheathModel.AddComponent<EquipmentOffsetsCorrection>();
            correction.Offsets = config.SheathOffsetsData;
        }

        if (correction.Offsets is null)
        {
            return;
        }

        if (correction.Offsets.HideSheath.Contains(slot))
        {
            sheathModel.SetActive(false);

            return;
        }

        if (correction.Offsets.SlotOffsets.ContainsKey(slot))
        {
            mr.transform.localEulerAngles = correction.Offsets.SlotOffsets[slot].Rotation;
            mr.transform.localPosition = correction.Offsets.SlotOffsets[slot].Position;
        }
        else
        {
            mr.transform.localEulerAngles = weaponModel.GetComponentInChildren<MeshRenderer>().transform.localEulerAngles;
            mr.transform.localPosition = weaponModel.GetComponentInChildren<MeshRenderer>().transform.localPosition;
        }

        //ApplyOffsets(config.SheathOffsetsData, sheathModel);
    }

    [HarmonyPatch(typeof(UnitViewHandSlotData), nameof(UnitViewHandSlotData.AttachModel), [])]
    [HarmonyPrefix]
    static void AttachModel_Prefix(UnitViewHandSlotData __instance)
    {
        if (!Enabled)
            return;

        if (__instance.VisualModel == null || !__instance.Character.gameObject.activeSelf)
            return;

        var visualParams = __instance.VisualParametersSource();

        if (visualParams == null)
            return;

        var weaponAssetId = visualParams.m_WeaponModel?.AssetId;
        if (weaponAssetId is null)
            return;

        var sheathAssetId = visualParams.m_WeaponSheathModelOverride?.AssetId;

        var config = TryLoadOffsetsCorrection(weaponAssetId, sheathAssetId);

        if (config is null && EditMode)
        {
            MicroLogger.Debug(() => $"config for {weaponAssetId}+{sheathAssetId} not found.");

            config = new() { WeaponModelAssetId = weaponAssetId, SheathModelOverrideAssetId = sheathAssetId };

            var fileName = __instance.VisualModel.name.Replace("(Clone)", "");
            if (!sheathAssetId.IsNullOrEmpty())
                fileName += "+" + __instance.SheathVisualModel.name.Replace("(Clone)", "");

            fileName += $".{nameof(EquipmentOffsetsCorrection)}.json";
            var filePath = Path.Combine(ConfigsDir, fileName);

            if (!File.Exists(filePath))
                File.WriteAllText(filePath, JsonConvert.SerializeObject(config, Formatting.Indented));
        }
        
        if (config is not null)
        {
            ApplyWeaponOffsets(config, __instance.VisualModel);

            if (__instance.SheathVisualModel != null)
            {
                ApplySheathOffsets(config, __instance.VisualSlot, __instance.SheathVisualModel, __instance.VisualModel);
            }

            if (config.WeaponOffsetsData?.MirrorOffHand ?? false &&
                !__instance.Owner.Descriptor.IsLeftHanded &&
                __instance.VisualModel.transform.parent == __instance.OffHandTransform)
            {
                var s = __instance.VisualModel.transform.localScale;
                __instance.VisualModel.transform.localScale = new Vector3(-s.x, s.y, s.z);
            }
        }
    }
}
