using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Mechanics.Actions;

using MicroWrath;
//using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.Internal.InitContext;

namespace MiscTweaksAndFixes.Tweaks
{
    public static class ZippyMagicBlasts
    {
        internal static bool Enabled { get; set; }
        #if DEBUG
            = true;
        #endif

        public class ZippyMagicBlastsComponent :
            UnitFactComponentDelegate,
            IInitiatorRulebookHandler<RuleCastSpell>
        {
            private DublicateSpellComponent? Dsc => Fact.Components.OfType<DublicateSpellComponent>().FirstOrDefault();

            public void OnEventAboutToTrigger(RuleCastSpell _) { }
            public void OnEventDidTrigger(RuleCastSpell evt)
            {
                if (Dsc is not { } dsc) return;

                MicroLogger.Debug(() => $"{nameof(ZippyMagicBlasts)}.{nameof(OnEventAboutToTrigger)}");

                // From DublicateSpellComponent.OnEventDidTrigger
                if (evt.IsDuplicateSpellApplied ||
                    !evt.Success ||
                    !dsc.CheckAOE(evt.Spell) ||
                    (evt.Spell.Range == AbilityRange.Touch &&
                        evt.Spell.Blueprint.GetComponent<AbilityEffectStickyTouch>() != null))
                    return;

                var ability = evt.Spell;

                if (!ability.Blueprint.SpellResistance ||
                    ability.Blueprint.Range == AbilityRange.Weapon ||
                    ability.Blueprint.Components.OfType<AbilityDeliverProjectile>().FirstOrDefault() is not { } adp ||
                    adp.Weapon.Category != WeaponCategory.KineticBlast)
                    return;

                // From DublicateSpellComponent.OnEventDidTrigger
                if (dsc.GetNewTarget(ability, evt.SpellTarget.Unit) is not { } newTarget) return;
                Rulebook.Trigger(new RuleCastSpell(ability, newTarget) { IsDuplicateSpellApplied = true });
            }
        }

        [Init]
        public static void Init()
        {
            //var initContext = new BlueprintInitializationContext(Triggers.BlueprintsCache_Init);

            InitContext.GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeature.ZippyMagicFeature)
                .Map(feature =>
                {
                    if (!Enabled) return feature;

                    feature.AddComponent<ZippyMagicBlastsComponent>();

                    return feature;
                })
                .RegisterBlueprint(BlueprintsDb.Owlcat.BlueprintFeature.ZippyMagicFeature.BlueprintGuid);
        }
    }
}
