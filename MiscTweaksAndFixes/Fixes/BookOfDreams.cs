using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Persistence.Versioning;
using Kingmaker.PubSubSystem;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;

namespace MiscTweaksAndFixes.Fixes
{
    internal static class BookOfDreams
    {
        public const string BookOfDreamsItemConvert_v2 = "8ea9114c683e4f218af674575aefcd57";

        internal static bool Enabled = true;

        private static readonly BlueprintInitializationContext PatchContext = new(Triggers.BlueprintsCache_Init);

        private class EtudesUpdateEventHandler : IEtudesUpdateHandler
        {
            public readonly Action Action;

            public EtudesUpdateEventHandler(Action action) => this.Action = action;

            public void OnEtudesUpdate() => Action();
        }

        [Init]
        internal static void Init()
        {
            if (!Enabled) return;

            PatchContext.GetBlueprint(new MicroBlueprint<BlueprintPlayerUpgrader>(BookOfDreamsItemConvert_v2))
                .Select(bp =>
                {
                    MicroLogger.Debug(() => $"{nameof(BookOfDreams)}");

                    EventBus.Subscribe(new EtudesUpdateEventHandler(() => {
                        MicroLogger.Debug(() => "Running Book of Dreams updater");
                        bp.m_Actions.Run();
                    }));
                })
                .Register();
        }
    }
}
