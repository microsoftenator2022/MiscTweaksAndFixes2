using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UniRx;

using Kingmaker.Blueprints;

using MicroWrath;
using MicroWrath.Constructors;
using MicroWrath.Util;
using MiscTweaksAndFixes;
using Kingmaker.Utility;

namespace MicroWrath
{

    internal partial class BlueprintInitializationContext
    {
        private readonly Dictionary<BlueprintGuid, IInitContextBlueprint> Blueprints = new();
        private readonly List<Action> Initializers = new();

        private readonly IObservable<Unit> Trigger;

        private IDisposable? done;
        private void Complete() => done?.Dispose();
        
        private void Register(IBlueprintInit bpContext, IEnumerable<IInitContextBlueprint> blueprints)
        {
            foreach (var bp in blueprints)
                Blueprints[bp.BlueprintGuid] = bp;

            Initializers.Add(bpContext.Execute);
            
            Complete();

            done = Trigger.Subscribe(Observer.Create<Unit>(
                onNext: _ =>
                {
                    foreach (var (guid, bp) in Blueprints.Select(kvp => (kvp.Key, kvp.Value.CreateNew())))
                    {
                        MicroLogger.Debug(() => $"Adding blueprint {guid} {bp.name}");

                        if (ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints.ContainsKey(guid))
                            MicroLogger.Warning($"BlueprintsCache already contains guid '{guid}'");

                        ResourcesLibrary.BlueprintsCache.AddCachedBlueprint(guid, bp);
                    }

                    foreach (var initAction in Initializers) initAction();

                    Complete();
                    Blueprints.Clear();
                    Initializers.Clear();
                },
                onError: _ => { },
                onCompleted: Complete));
        }

        /// <summary>
        /// Create a new context for blueprint initialization
        /// </summary>
        /// <param name="trigger">The event used to trigger evaluation of this context</param>
        internal BlueprintInitializationContext(IObservable<Unit> trigger) { Trigger = trigger; }

        /// <summary>
        /// Adds a new initializer to the context that adds a new blueprint to the library
        /// </summary>
        /// <typeparam name="TBlueprint">Blueprint type</typeparam>
        /// <param name="assetId">GUID for new blueprint</param>
        /// <param name="name">name for new blueprint</param>
        /// <returns>Blueprint initialization context for additional initialization steps</returns>
        public ContextInitializer<TBlueprint> NewBlueprint<TBlueprint>(string assetId, string name)
            where TBlueprint : SimpleBlueprint, new()
        {
            var microBlueprint = new InitContextBlueprint<TBlueprint>(assetId, name);

            return new BlueprintInit<TBlueprint>(this, new IInitContextBlueprint[] { microBlueprint }, () => microBlueprint.ToReference());
        }

        /// <summary>
        /// Adds a new initializer to the context for an existing blueprint
        /// </summary>
        /// <typeparam name="TBlueprint">Blueprint type</typeparam>
        /// <param name="blueprint">IMicroBlueprint blueprint reference</param>
        /// <returns>Blueprint initialization context for additional initialization steps</returns>
        public ContextInitializer<TBlueprint> AddBlueprint<TBlueprint>(IMicroBlueprint<TBlueprint> blueprint)
            where TBlueprint : SimpleBlueprint =>
            new BlueprintInit<TBlueprint>(this, Enumerable.Empty<IInitContextBlueprint>(), () => blueprint.ToReference());

        /// <summary>
        /// Adds an empty initializer to the context
        /// </summary>
        public ContextInitializer Empty => new BlueprintInit<object>(this, Enumerable.Empty<IInitContextBlueprint>(), () => new object());

        public ContextInitializer<TBlueprint> NewBlueprint<TBlueprint>(Func<TBlueprint> initFunc)
            where TBlueprint : SimpleBlueprint, new() =>
            new BlueprintInit<TBlueprint>(this, Enumerable.Empty<IInitContextBlueprint>(), () =>
            {
                var bp = initFunc();

                ResourcesLibrary.BlueprintsCache.AddCachedBlueprint(bp.AssetGuid, bp);

                return bp;
            });

        public ContextInitializer<IEnumerable<TBlueprint>> NewBlueprints<TBlueprint>(IEnumerable<(string assetId, string name)> ids)
            where TBlueprint : SimpleBlueprint, new() =>
            ids.Select(bp => NewBlueprint<TBlueprint>(bp.assetId, bp.name)).Combine();

        public ContextInitializer<IEnumerable<TBlueprint>> NewBlueprints<TBlueprint>(IEnumerable<Func<TBlueprint>> initFuncs)
            where TBlueprint : SimpleBlueprint, new() =>
            initFuncs.Select(f => NewBlueprint(f)).Combine();
    }
}
