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

using MiscTweaksAndFixes.AddedContent;

namespace MiscTweaksAndFixes.Fixes
{
    internal static class BookOfDreams
    {
        public const string BookOfDreamsItemConvert_v2 = "8ea9114c683e4f218af674575aefcd57";

        internal static bool Enabled = false;

        //private static readonly BlueprintInitializationContext PatchContext = new(Triggers.BlueprintsCache_Init);

        private class EtudesUpdateEventHandler(Action action) : IEtudesUpdateHandler
        {
            public readonly Action Action = action;

            public void OnEtudesUpdate() => Action();
        }

        [Init]
        internal static void Init()
        {
            //PatchContext.GetBlueprint(new MicroBlueprint<BlueprintPlayerUpgrader>(BookOfDreamsItemConvert_v2))
            //    .Map(bp =>
            //    {
            //        if (!Enabled) return;

            //        MicroLogger.Debug(() => $"{nameof(BookOfDreams)}");

            //        EventBus.Subscribe(new EtudesUpdateEventHandler(() => {
            //            MicroLogger.Debug(() => "Running Book of Dreams updater");
            //            bp!.m_Actions.Run();
            //        }));
            //    })
            //    .Register();

            InitContext.GetBlueprint(new MicroBlueprint<BlueprintPlayerUpgrader>(BookOfDreamsItemConvert_v2))
                .Map(maybeBp => maybeBp.MaybeValue!)
                .Map(bp =>
                {
                    if (!Enabled) return bp;

                    MicroLogger.Debug(() => $"{nameof(BookOfDreams)}");

                    EventBus.Subscribe(new EtudesUpdateEventHandler(() =>
                    {
                        MicroLogger.Debug(() => "Running Book of Dreams updater");
                        bp!.m_Actions.Run();
                    }));

                    return bp;
                })
                .RegisterBlueprint(BlueprintGuid.Parse(BookOfDreamsItemConvert_v2));
            
            //PatchContext.GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeature.BooksOfDreamsIStageFeature)
            //    .Map((BlueprintFeature bp) =>
            //    {
            //        bp.Components = [];
            //        bp.AddComponent<ExtraSummonCount>();
            //    })
            //    .Register();

            InitContext.GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeature.BooksOfDreamsIStageFeature)
                .Map((BlueprintFeature bp) =>
                {
                    if (!Enabled) return bp;

                    bp.Components = [];
                    bp.AddComponent<ExtraSummonCount>();

                    return bp;
                })
                .RegisterBlueprint(BlueprintsDb.Owlcat.BlueprintFeature.BooksOfDreamsIStageFeature.BlueprintGuid);
        }
    }
}
