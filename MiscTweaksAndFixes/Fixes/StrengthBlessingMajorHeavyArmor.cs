using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.Buffs.Blueprints;

using MicroWrath;
using MicroWrath.BlueprintsDb;
//using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Extensions;
using MicroWrath.InitContext;

namespace MiscTweaksAndFixes.Fixes
{
    internal static class StrengthBlessingMajorHeavyArmor
    {
        internal static bool Enabled = true;

        //private static readonly BlueprintInitializationContext PatchContext = new(Triggers.BlueprintsCache_Init);

        [AllowMultipleComponents]
        [AllowedOn(typeof(BlueprintBuff), false)]
        public class HeavyArmorSpeedPenaltyRemoval : ArmorSpeedPenaltyRemoval
        {
            public override void OnTurnOn()
            {
                base.Owner.State.Features.ImmuneToArmorSpeedPenalty.Retain();

                base.OnTurnOn();
            }

            public override void OnTurnOff()
            {
                base.Owner.State.Features.ImmuneToArmorSpeedPenalty.Release();

                base.OnTurnOff();
            }
        }

        [Init]
        public static void Init()
        {
            //var context = 
            InitContext.GetBlueprint(BlueprintsDb.Owlcat.BlueprintBuff.StrengthBlessingMajorBuff)
                .Map(buff =>
                {
                    if (Enabled)
                    {
                        MicroLogger.Debug(() => $"{nameof(StrengthBlessingMajorHeavyArmor)}");

                        buff.RemoveComponents(c => c is ArmorSpeedPenaltyRemoval);
                        buff.AddComponent<HeavyArmorSpeedPenaltyRemoval>();
                    }

                    return buff;
                })
                .RegisterBlueprint(BlueprintsDb.Owlcat.BlueprintBuff.StrengthBlessingMajorBuff.BlueprintGuid, Triggers.BlueprintsCache_Init);

            //PatchContext.GetBlueprint(BlueprintsDb.Owlcat.BlueprintBuff.StrengthBlessingMajorBuff)
            //    .Map((BlueprintBuff bp) =>
            //    {
            //        if (!Enabled) return;

            //        MicroLogger.Debug(() => $"{nameof(StrengthBlessingMajorHeavyArmor)}");

            //        bp.RemoveComponents(c => c is ArmorSpeedPenaltyRemoval);
            //        bp.AddComponent<HeavyArmorSpeedPenaltyRemoval>();
            //    })
            //    .Register();
        }
    }
}
