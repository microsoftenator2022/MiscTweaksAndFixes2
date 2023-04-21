using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.UnitLogic.Buffs.Blueprints;

using MicroWrath;
using MicroWrath.BlueprintsDb;
using MicroWrath.Constructors;

namespace MiscTweaksAndFixes.Fixes
{
    internal static class StrengthBlessingMajorHeavyArmor
    {
        internal static bool Enabled = true;

        private static readonly BlueprintInitializationContext PatchContext = new(Triggers.BlueprintsCache_Init);

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
            if (!Enabled) return;

            PatchContext.AddBlueprint(BlueprintsDb.Owlcat.BlueprintBuff.StrengthBlessingMajorBuff)
                .Select(bp =>
                {
                    MicroLogger.Debug(() => $"{nameof(StrengthBlessingMajorHeavyArmor)}");

                    //var aspr = Construct.New.Component<ArmorSpeedPenaltyRemoval>();
                    //var haspr = Construct.New.Component<HeavyArmorSpeedPenaltyRemoval>();
                    
                    bp.RemoveComponents(c => c is ArmorSpeedPenaltyRemoval);
                    bp.AddNewComponent<HeavyArmorSpeedPenaltyRemoval>();
                })
                .Register();

        }
    }
}
