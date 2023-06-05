using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;

using MicroWrath;

namespace MiscTweaksAndFixes.Fixes
{
    internal static class NaturalWeapons
    {
        internal static bool IdentifyNaturalWeapons { get; set; }

        [HarmonyPatch(typeof(ItemEntity), nameof(ItemEntity.OnDidEquipped))]
        internal class ItemEntity_OnDidEquipped_Patch
        {
            // Automatically identify natural weapons on equip
            static void Prefix(UnitEntityData wielder, ItemEntity __instance)
            {
                if (!IdentifyNaturalWeapons) return;

                if (!wielder.IsPlayerFaction) return;

                if (__instance is ItemEntityWeapon weapon && weapon.Blueprint.IsNatural)
                    weapon.Identify();
            }
        }

        internal static bool EquipBestEmptyHandWeapon { get; set; }

        static void Postfix(UnitEntityData wielder, ItemEntity __instance)
        {
            if (!EquipBestEmptyHandWeapon) return;

            if (__instance is not ItemEntityWeapon weapon) return;

            if (wielder is null || wielder.Body is null) return;

            if (!wielder.IsPlayerFaction || wielder.IsPet || wielder.Body.IsPolymorphed) return;
            
            var blueprint = weapon.Blueprint;
            if (!blueprint.IsNatural || blueprint.IsUnarmed) return;

            var ehws = wielder.Facts.List.SelectMany(f => f.Blueprint.Components.OfType<EmptyHandWeaponOverride>());
            if (ehws.Where(ehw => ehw.Weapon == blueprint).Count() == 0) return;

            ehws = ehws.Where(ehw => ehw.Weapon != blueprint);
            if (!ehws.Any()) return;

            MicroLogger.Debug(() => $"{nameof(ItemEntity_OnDidEquipped_Patch)}.{nameof(Postfix)}");
            MicroLogger.Debug(() => $"{wielder}");

            // TODO: Handle weapon size scaling
            foreach (var ehw in ehws)
            {
                if (ehws.Any(c => c.Weapon.BaseDamage.IsBetterThan(ehw.Weapon.BaseDamage)))
                    continue;

                MicroLogger.Debug(() => $"{ehw} ({ehw.Weapon.BaseDamage}) > {weapon} ({weapon.Blueprint.BaseDamage})? {ehw.Weapon.BaseDamage.IsBetterThan(weapon.Blueprint.BaseDamage)}");

                if (ehw.Weapon.BaseDamage.IsBetterThan(weapon.Blueprint.BaseDamage))
                {
                    ehw.SetWeapon();
                    return;
                }
            }
        }
    }
}
