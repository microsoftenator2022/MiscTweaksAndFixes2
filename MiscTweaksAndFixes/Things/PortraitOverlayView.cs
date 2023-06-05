using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.MVVM._ConsoleView.Party;
using Kingmaker.UI.MVVM._PCView.Party;
using Kingmaker.UI.MVVM._VM.Party;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;

using MicroWrath;
using MicroWrath.Util.Linq;

using Newtonsoft.Json;

using Owlcat.Runtime.UI.MVVM;

using UniRx;

using UnityEngine;

namespace MiscTweaksAndFixes.Things
{
    internal partial class PortraitOverlay : ViewBase<PartyCharacterVM>, IDisposable, IFactCollectionUpdatedHandler
    {
        [HarmonyPatch]
        private class Patches
        {
            [HarmonyPatch(typeof(PartyCharacterPCView), nameof(PartyCharacterPCView.Initialize))]
            [HarmonyPostfix]
            public static void PartyCharacterPCView_Initialize_Postfix(PartyCharacterPCView __instance)
            {
                MicroLogger.Debug(() => $"{nameof(PartyCharacterPCView_Initialize_Postfix)}");

                OnInitialize(__instance);
            }

            [HarmonyPatch(typeof(PartyCharacterConsoleView), nameof(PartyCharacterConsoleView.Initialize))]
            [HarmonyPostfix]
            public static void PartyCharacterConsoleView_Initialize_Postfix(PartyCharacterConsoleView __instance)
            {
                MicroLogger.Debug(() => $"{nameof(PartyCharacterConsoleView_Initialize_Postfix)}");

                OnInitialize(__instance);
            }

            private static void OnInitialize(ViewBase<PartyCharacterVM> __instance)
            {
                //if (CreateNew(__instance)
                if (CreateNew(__instance,
                    foreground: AddedContent.RipAndTear.RipAndTear.GetSprite("STFST01"),
                    background: AddedContent.RipAndTear.RipAndTear.GetSprite("STFB1"))
                    is not var (_, po)) return;

                __instance.AddDisposable(po);

            }

            [HarmonyPatch(typeof(PartyCharacterPCView), nameof(PartyCharacterPCView.BindViewImplementation))]
            [HarmonyPostfix]
            public static void PartyCharacterPCView_BindViewImplementation_Postfix(PartyCharacterPCView __instance)
            {
                MicroLogger.Debug(() => $"{nameof(PartyCharacterPCView_BindViewImplementation_Postfix)}");

                OnBindViewImplementation(__instance);
            }

            [HarmonyPatch(typeof(PartyCharacterConsoleView), nameof(PartyCharacterConsoleView.BindViewImplementation))]
            [HarmonyPostfix]
            public static void PartyCharacterConsoleView_BindViewImplementation_Postfix(PartyCharacterConsoleView __instance)
            {
                MicroLogger.Debug(() => $"{nameof(PartyCharacterConsoleView_BindViewImplementation_Postfix)}");

                OnBindViewImplementation(__instance);
            }

            private static void OnBindViewImplementation<TBuffView>(PartyCharacterView<TBuffView> __instance)
                where TBuffView : ViewBase<UnitBuffPartVM>
            {
                if (__instance.ViewModel == default) return;

                var charaName = __instance.ViewModel.CharacterName.Value ?? "<null>";

                MicroLogger.Debug(() => $"Looking for overlay for {charaName}");

                var po = __instance.GetComponentInChildren<PortraitOverlay>(true);

                if (po == null)
                {
                    MicroLogger.Error("Could not find portrait overlay");

                    return;
                }

                MicroLogger.Debug(() => $"Binding to {charaName} ViewModel");
                po.Bind(__instance.ViewModel);
            }
        }

        public PortraitOverlay() : base() { }

        private PortraitOverlayComponentData? OverlayComponentData;

        private IDisposable? BGSpriteChanged;
        private IDisposable? FGSpriteChanged;

        public void HandleFactCollectionUpdated(EntityFactsProcessor collection)
        {
            if (Unit is null)
            {
                Dispose();
                return;
            }

            if (collection.Manager.Owner != Unit) return;

            MicroLogger.Debug(() => $"{nameof(PortraitOverlay)}.{nameof(HandleFactCollectionUpdated)}");
            //MicroLogger.Debug(() => $"collection type: {collection.GetType()}");

            if (collection is not Kingmaker.UnitLogic.Buffs.BuffCollection buffs)
                return;

            //MicroLogger.Debug(() =>
            //{
            //    var sb = new StringBuilder();
            //    sb.AppendLine("Buff components:");

            //    foreach (var (i, b) in buffs.Enumerable.Indexed())
            //    {
            //        sb.AppendLine($"Buff {i}: {b}");

            //        foreach (var c in b.Components)
            //        {
            //            sb.AppendLine($"  {c.SourceBlueprintComponent.GetType()}");
            //            sb.AppendLine($"  Is {nameof(IPortraitOverlayComponent)}? {c.SourceBlueprintComponent is IPortraitOverlayComponent}");
            //        }
            //    }

            //    return sb.ToString();
            //});

            if (buffs.Enumerable
                .SelectMany(b => b.Components)
                .Where(c => c.SourceBlueprintComponent is IPortraitOverlayComponent)
                .Select(c => c.GetData<PortraitOverlayComponentData>())
                .FirstOrDefault()
                is not { } overlayComponentData)
            {
                if (!gameObject.activeSelf) return;

                Dispose();
                return;
            }

            if (OverlayComponentData != null && overlayComponentData == OverlayComponentData) return;

            Dispose();

            EnableOverlay(overlayComponentData);
        }

        public void EnableOverlay(PortraitOverlayComponentData overlayComponentData)
        {
            //if (overlayComponentData is null)
            //{
            //    MicroLogger.Error("Tried to enable null overlay");

            //    Dispose();
            //    return;
            //}

            OverlayComponentData = overlayComponentData;

            SetBGSprite(overlayComponentData.BackgroundSprite.Value);
            BGSpriteChanged = overlayComponentData.BackgroundSprite.Subscribe(SetBGSprite);
            AddDisposable(BGSpriteChanged);

            void setFG((Sprite, float)? value)
            {
                if (value is not var (fgSprite, aspectRatio))
                    SetFGSprite(null);
                else SetFGSprite(fgSprite, aspectRatio);
            }

            setFG(overlayComponentData.ForegroundSprite.Value);

            FGSpriteChanged = overlayComponentData.ForegroundSprite.Subscribe(setFG);
            AddDisposable(FGSpriteChanged);

            gameObject.SetActive(true);
        }

        public void Dispose()
        {
            gameObject.SetActive(false);

            RemoveDisposable(BGSpriteChanged);
            BGSpriteChanged?.Dispose();
            SetBGSprite(null);

            RemoveDisposable(FGSpriteChanged);
            FGSpriteChanged?.Dispose();
            SetFGSprite(null);
            
            OverlayComponentData?.Dispose();
            OverlayComponentData = null;
        }

        internal UnitEntityData? Unit => IsBinded ? ViewModel!.UnitEntityData : null;

        public override void BindViewImplementation()
        {
            //gameObject.SetActive(true);

            if (Unit is null) return;

            AddDisposable(EventBus.Subscribe(this));
        }

        public override void DestroyViewImplementation()
        {
            Dispose();
        }
    }

    internal interface IPortraitOverlayComponent { }

    public class PortraitOverlayComponentData : IDisposable
    {
        [JsonIgnore]
        private ReactiveProperty<Sprite?>? backgroundSprite;

        [JsonIgnore]
        public ReactiveProperty<Sprite?> BackgroundSprite
        {
            get => backgroundSprite ??= new(null);
            set => backgroundSprite = value;
        }
     
        [JsonIgnore]
        private ReactiveProperty<(Sprite, float)?>? foregroundSprite;

        [JsonIgnore]
        public ReactiveProperty<(Sprite, float)?> ForegroundSprite
        {
            get => foregroundSprite ??= new(null);
            set => foregroundSprite = value;
        }

        public virtual void Dispose() { }
    }

    public abstract class PortraitOverlayComponent<TData> : UnitFactComponentDelegate<TData>, IPortraitOverlayComponent
        where TData : PortraitOverlayComponentData, new() { }
}
