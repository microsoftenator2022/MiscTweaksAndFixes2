using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Kingmaker.Blueprints;

using MicroWrath.Util.Linq;

namespace MicroWrath.BlueprintInitializationContext
{
    internal static class BlueprintInitializationContextExtension
    {
        internal static BlueprintInitializationContext.ContextInitializer<TOther> Combine<TOther>(
            this BlueprintInitializationContext.ContextInitializer obj,
            BlueprintInitializationContext.ContextInitializer<TOther> other) =>
            obj.Select(() => new object()).Combine(other).Select(x => x.Item2);

        internal static BlueprintInitializationContext.ContextInitializer<TBlueprint> GetBlueprint<TBlueprint>(
            this BlueprintInitializationContext.ContextInitializer obj,
            IMicroBlueprint<TBlueprint> blueprint)
            where TBlueprint : SimpleBlueprint =>
            obj.Select(() => new object()).GetBlueprint(blueprint).Select(x => x.Item2);

        internal static BlueprintInitializationContext.ContextInitializer<IEnumerable<TBlueprint>> Combine<TBlueprint>(
            this IEnumerable<BlueprintInitializationContext.ContextInitializer<TBlueprint>> bpcs)
            where TBlueprint : SimpleBlueprint
        {
            var head = bpcs.First();
            var tail = bpcs.Skip(1);

            return tail.Aggregate(
                head.Select(EnumerableExtensions.Singleton),
                (acc, next) => acc.Combine(next).Select(x => x.Item1.Append(x.Item2)));
        }
    }

    internal partial class BlueprintInitializationContext
    { 
        internal abstract class ContextInitializer
        {
            protected abstract BlueprintInitializationContext InitContext { get; }
            public abstract ContextInitializer Select(Action action);
            public abstract ContextInitializer<TResult> Select<TResult>(Func<TResult> selector);
            public abstract void Register();
        }

        internal abstract class ContextInitializer<T> : ContextInitializer
        {
            public abstract ContextInitializer Select(Action<T> action);
            public abstract ContextInitializer<TResult> Select<TResult>(Func<T, TResult> selector);
            public abstract ContextInitializer<(T, TOther)> Combine<TOther>(ContextInitializer<TOther> other);

            public virtual ContextInitializer<(T, TBlueprint)> GetBlueprint<TBlueprint>(IMicroBlueprint<TBlueprint> blueprint)
                where TBlueprint : SimpleBlueprint =>
                this.Combine(InitContext.GetBlueprint(blueprint));
        }

        private interface IBlueprintInit
        {
            void Execute();
        }

        private interface IBlueprintInit<T> : IBlueprintInit
        {
            Func<T> InitFunc { get; }
        }

        private class BlueprintInit<T> : ContextInitializer<T>, IBlueprintInit<T>
        {
            private readonly BlueprintInitializationContext initContext;
            protected override BlueprintInitializationContext InitContext => initContext;
            internal readonly IInitContextBlueprint[] Blueprints;

            private readonly Func<T> InitFunc;

            Func<T> IBlueprintInit<T>.InitFunc => InitFunc;

            internal bool HasValue { get; private set; } = false;

            private T GetValue()
            {
                value = InitFunc();
                HasValue = true;

                return value;
            }

            private T? value;
            internal T Value
            {
                get
                {
                    if (!HasValue) return GetValue();

                    return value!;
                }
            }

            void IBlueprintInit.Execute() => GetValue();

            internal BlueprintInit(BlueprintInitializationContext initContext, IInitContextBlueprint[] blueprints, Func<T> initFunc)
            {
                this.initContext = initContext;
                this.InitFunc = initFunc;
                Blueprints = new IInitContextBlueprint[blueprints.Length];
                blueprints.CopyTo((Span<IInitContextBlueprint>)Blueprints);
            }

            internal BlueprintInit(BlueprintInitializationContext initContext, IEnumerable<IInitContextBlueprint> blueprints, Func<T> getValue)
                : this(initContext, blueprints.ToArray(), getValue) { }

            private BlueprintInit<TResult> With<TResult>(Func<TResult> getValue) => new(initContext, Blueprints, getValue);

            public override ContextInitializer<TResult> Select<TResult>(Func<T, TResult> selector) =>
                With(() => selector(Value));

            public override ContextInitializer<TResult> Select<TResult>(Func<TResult> selector) =>
                With(() =>
                {
                    GetValue();
                    return selector();
                });

            public override ContextInitializer Select(Action<T> action) =>
                With<object>(() =>
                {
                    action(Value);
                    return new();
                });

            public override ContextInitializer Select(Action action) =>
                With<object>(() =>
                {
                    GetValue();
                    action();
                    return new();
                });

            /// <summary>
            /// Registers this initializer for execution
            /// </summary>
            public override void Register() => initContext.Register(this, Blueprints);

            public override ContextInitializer<(T, TOther)> Combine<TOther>(ContextInitializer<TOther> other)
            {
                IEnumerable<IInitContextBlueprint> blueprints = this.Blueprints;

                if (other is BlueprintInit<TOther> otherBpInit)
                    blueprints = blueprints.Concat(otherBpInit.Blueprints);

                return new BlueprintInit<(T, TOther)>(initContext, blueprints, () => (this.InitFunc(), ((IBlueprintInit<TOther> )other).InitFunc()));
            }
        }
    }
}
