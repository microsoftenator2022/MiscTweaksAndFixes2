#if DEBUG
using System;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Mechanics;

namespace MiscTweaksAndFixes.AddedContent
{
    [AllowedOn(typeof(BlueprintUnitFact))]
    internal class ExtraSummonCount : UnitFactComponentDelegate, ICalculateSummonUnitsCount
    {
        public ContextValue ExtraCount = 1;

        public bool VariableOnly;

        //public void HandleCalculateSummonUnitsCount(UnitEntityData unit, DiceType diceType, int diceCount, int diceBonus, ref int count)
        //{
        //    if (unit != this.Owner)
        //        return;

        //    if (this.VariableOnly && (diceType is DiceType.Zero or DiceType.One || diceCount < 1))
        //        return;

        //    if ((diceType is not DiceType.Zero && diceCount > 0) || diceBonus > 1)
        //        count += this.ExtraCount.Calculate(base.Context);
        //}

        public void HandleCalculateSummonUnitsCount(MechanicsContext mechanicsContext, ContextDiceValue contextDiceValue, ref int count)
        {
            if (mechanicsContext.MaybeCaster != this.Owner)
                return;

            if (this.VariableOnly && (!contextDiceValue.IsVariable || contextDiceValue.DiceCountValue.IsZero))
                return;

            if ((contextDiceValue.DiceType is not DiceType.Zero && !contextDiceValue.DiceCountValue.IsZero) || contextDiceValue.BonusValue.Value > 1)
                count += this.ExtraCount.Calculate(base.Context);
        }
    }
}
#endif