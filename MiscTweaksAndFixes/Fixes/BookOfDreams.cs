using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence.Versioning;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic.FactLogic;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Constructors;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.Internal.InitContext;
using MicroWrath.Util;

using MiscTweaksAndFixes.AddedContent;

using UniRx;

namespace MiscTweaksAndFixes.Fixes
{
    internal static class BookOfDreams
    {
        public const string BookOfDreamsItemConvert_v2 = "8ea9114c683e4f218af674575aefcd57";

        internal static bool Enabled = false;

        private class EtudesUpdateEventHandler(Action action) : IEtudesUpdateHandler
        {
            public readonly Action Action = action;

            public void OnEtudesUpdate() => Action();
        }

        [Init]
        internal static void Init()
        {
            var bp = new OwlcatBlueprint<BlueprintPlayerUpgrader>(BookOfDreamsItemConvert_v2);
            EventBus.Subscribe(new EtudesUpdateEventHandler(() =>
            {
                if (Enabled)
                {
                    MicroLogger.Debug(() => "Running Book of Dreams updater");
                    
                    bp.TryGetBlueprint().Map(bp => { bp.m_Actions.Run(); return Unit.Default; });
                }
            }));
        }
    }
}
