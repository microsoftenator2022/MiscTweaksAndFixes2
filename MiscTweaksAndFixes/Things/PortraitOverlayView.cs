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
using Kingmaker.UnitLogic.Buffs.Blueprints;

using MicroWrath;

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

        private PortraitOverlayComponent? OverlayComponent;

        private IDisposable? BGSpriteChanged;
        private IDisposable? FGSpriteChanged;

        public void HandleFactCollectionUpdated(EntityFactsProcessor collection)
        {
            if (Unit is null) Dispose();

            if (collection.Manager.Owner != Unit ||
                collection is not Kingmaker.UnitLogic.Buffs.BuffCollection buffs)
                return;

            if (buffs.Enumerable
                .SelectMany(b => b.Components)
                .OfType<PortraitOverlayComponent>()
                .FirstOrDefault()
                is not { } overlayComponent)
            {
                if (!gameObject.activeSelf) return;

                Dispose();
                return;
            }

            if (OverlayComponent is not null && overlayComponent == OverlayComponent) return;

            Dispose();
            EnableOverlay(overlayComponent);
        }

        public void EnableOverlay(PortraitOverlayComponent? overlayComponent = null)
        {
            overlayComponent ??= OverlayComponent;

            if (overlayComponent is null)
            {
                MicroLogger.Error("Tried to enable null overlay");

                Dispose();
                return;
            }

            BGSpriteChanged = overlayComponent.BackgroundSprite.Subscribe(SetBGSprite);
            AddDisposable(BGSpriteChanged);

            FGSpriteChanged = overlayComponent.ForegroundSprite.Subscribe(SetFGSprite);
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

            OverlayComponent = null;
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

    [AllowedOn(typeof(BlueprintFeature))]
    [AllowedOn(typeof(BlueprintBuff))]
    public class PortraitOverlayComponent : BlueprintComponent
    {
        public ReactiveProperty<Sprite?> ForegroundSprite = new(null);
        public ReactiveProperty<Sprite?> BackgroundSprite = new(null);
    }
}
