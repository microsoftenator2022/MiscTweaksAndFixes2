using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.MVVM._ConsoleView.Party;
using Kingmaker.UI.MVVM._PCView.Party;
using Kingmaker.UI.MVVM._VM.Party;

using MicroWrath;

using Owlcat.Runtime.UI.MVVM;

using UnityEngine;

namespace MiscTweaksAndFixes.Things
{
    internal partial class PortraitOverlay : ViewBase<PartyCharacterVM>, IDisposable
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
                if (CreateNew(__instance,
                    AddedContent.RipAndTear.RipAndTear.PortraitOverlay.Face.Value,
                    AddedContent.RipAndTear.RipAndTear.PortraitOverlay.Background.Value)
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
                MicroLogger.Debug(() => $"Looking for overlay for {__instance.ViewModel?.CharacterName.Value ?? "<null>"}");

                var po = __instance.GetComponentInChildren<PortraitOverlay>(true);

                if (po == null)
                {
                    MicroLogger.Error("Could not find portrait overlay");

                    return;
                }

                po.VM = __instance.ViewModel;

                po.gameObject.SetActive(true);
            }
        }

        void IDisposable.Dispose()
        {
            OnDisable();
        }
        
        private PartyCharacterVM? vm;
        private PartyCharacterVM? VM
        {
            get => vm;
            set
            {
                if (value != null)
                {
                    MicroLogger.Debug(() => $"Binding to {value.CharacterName} ViewModel");

                    value.OnDispose += DestroyViewImplementation;
                }
                
                OnDisable();

                if (vm != null)
                {
                    vm.OnDispose -= DestroyViewImplementation;
                    return;
                }

                vm = value;
            }
        }

        internal UnitEntityData? Unit => IsBinded ? VM!.UnitEntityData : null;

        public override void BindViewImplementation() { }

        public override void DestroyViewImplementation()
        {
            if (vm == null) return;

            vm = null;
        }

    }
}
