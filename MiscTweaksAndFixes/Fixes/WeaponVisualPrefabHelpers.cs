using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Utility;
using Kingmaker.View.Equipment;

using MicroWrath;

namespace MiscTweaksAndFixes.Fixes;
internal static partial class WeaponOffsetFixes
{
    static void InitializeEquipmentOffsets(EquipmentOffsets eos)
    {
        eos.m_MainHand ??= new();
        eos.m_OffHand ??= new();
        eos.m_DollRoomMainHand ??= new();
        eos.m_DollRoomOffHand ??= new();
        eos.m_DollRoomIKLeft ??= new();
        eos.m_DollRoomIKRight ??= new();
    }

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
        static bool check(WeaponVisualParameters? wvp) => !(wvp?.m_WeaponModel?.AssetId.IsNullOrEmpty() ?? true);

        var vsw = slotData.GetVisualSourceWeaponVisualParams();
        MicroLogger.Debug(() => $"VisualSourceWeapon: {vsw?.m_WeaponModel?.AssetId}");

        var vswt = slotData.GetVisualSourceWeaponTypeVisualParams();
        MicroLogger.Debug(() => $"VisualSourceWeaponType: {vswt?.m_WeaponModel?.AssetId}");

        var w = slotData.GetWeaponVisualParams();
        MicroLogger.Debug(() => $"Weapon: {w?.m_WeaponModel?.AssetId}");

        var wt = slotData.GetWeaponTypeVisualParams();
        MicroLogger.Debug(() => $"WeaponType: {wt?.m_WeaponModel?.AssetId}");

        if (check(vsw)) return vsw;
        if (check(vswt)) return vswt;
        if (check(w)) return w;
        if (check(wt)) return wt;

        return null;
    }

    static string? NullIfEmpty(this string? s) => string.IsNullOrEmpty(s) ? null : s;
}
