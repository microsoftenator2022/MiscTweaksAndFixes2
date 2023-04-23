using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;

using MicroWrath;
using MicroWrath.Constructors;

namespace MicroWrath.BlueprintInitializationContext
{
    internal partial class BlueprintInitializationContext
    {
        private interface IInitContextBlueprint
        {
            string Name { get; }
            BlueprintGuid BlueprintGuid { get; }
            SimpleBlueprint CreateNew();
        }

        private readonly struct InitContextBlueprint<TBlueprint> : IMicroBlueprint<TBlueprint>, IInitContextBlueprint
            where TBlueprint : SimpleBlueprint, new()
        {
            public readonly string AssetId;
            public readonly string Name;

            public BlueprintGuid BlueprintGuid { get; }

            string IInitContextBlueprint.Name => Name;
            public TBlueprint CreateNew() => Construct.New.Blueprint<TBlueprint>(AssetId, Name);
            SimpleBlueprint IInitContextBlueprint.CreateNew() => this.CreateNew();

            internal InitContextBlueprint(string assetId, string name)
            {
                AssetId = assetId;
                Name = name;
                BlueprintGuid = BlueprintGuid.Parse(assetId);
            }

            BlueprintGuid IMicroBlueprint<TBlueprint>.BlueprintGuid => BlueprintGuid;

            TBlueprint? IMicroBlueprint<TBlueprint>.GetBlueprint() => this.TryGetBlueprint().Value;
        }
    }
}
