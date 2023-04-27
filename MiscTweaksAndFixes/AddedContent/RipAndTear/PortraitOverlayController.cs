using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.MVVM._ConsoleView.Party;
using Kingmaker.UI.MVVM._PCView.Party;
using Kingmaker.UI.MVVM._VM.Party;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Visual.Sound;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;

using Owlcat.Runtime.UI.MVVM;
using Owlcat.Runtime.UniRx;

using UniRx;

using UnityEngine;

namespace MiscTweaksAndFixes.AddedContent.RipAndTear
{
    internal static partial class RipAndTear
    {
        private static bool enabled = true;
        internal static bool Enabled
        {
            get => enabled; set
            {
                enabled = value;
            }
        }

        [HarmonyPatch]
        private class Patches
        {
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
                MicroLogger.Debug(() => $"Adding portrait overlays");

                (GameObject, Action<Sprite>)? overlay = null;
                if (__instance is PartyCharacterPCView pcView)
                    overlay = PortraitOverlay.CreatePortraitOverlay(pcView);
                else if (__instance is PartyCharacterConsoleView consoleView)
                    overlay = PortraitOverlay.CreatePortraitOverlay(consoleView);
                
                if (overlay is null)
                {
                    MicroLogger.Error("Failed to add overlay");
                    return;
                }

                var controller = new PortraitOverlayController<TBuffView>(__instance, overlay.Value);

                controller.transform.SetParent(overlay.Value.Item1.transform);
            }
        }

        #region DGFace
        internal enum DGFace
        {
            STFDEAD00,
            STFEVL0,
            STFEVL1,
            STFEVL2,
            STFEVL3,
            STFEVL4,
            STFGOD0,
            STFKILL0,
            STFKILL1,
            STFKILL2,
            STFKILL3,
            STFKILL4,
            STFOUCH0,
            STFOUCH1,
            STFOUCH2,
            STFOUCH3,
            STFOUCH4,
            STFST00,
            STFST01,
            STFST02,
            STFST10,
            STFST11,
            STFST12,
            STFST20,
            STFST21,
            STFST22,
            STFST30,
            STFST31,
            STFST32,
            STFST40,
            STFST41,
            STFST42,
            STFTL00,
            STFTL10,
            STFTL20,
            STFTL30,
            STFTL40,
            STFTR00,
            STFTR10,
            STFTR20,
            STFTR30,
            STFTR40
        }

        private readonly record struct FaceSet(DGFace EVL, DGFace KILL, DGFace OUCH, DGFace ST0, DGFace ST1, DGFace ST2,
            DGFace FTL, DGFace FTR)
        {
            public FaceSet() : this(DGFace.STFEVL0, DGFace.STFKILL0, DGFace.STFOUCH0, DGFace.STFST00, DGFace.STFST01,
                DGFace.STFST02, DGFace.STFTL00, DGFace.STFTR00) { }
        }

        private static FaceSet FacesForHP(int hpPercent)
        {
            return hpPercent switch
            {
                >= 80 => new FaceSet(
                    EVL: DGFace.STFEVL0,
                    KILL: DGFace.STFKILL0,
                    OUCH: DGFace.STFOUCH0,
                    ST0: DGFace.STFST00,
                    ST1: DGFace.STFST01,
                    ST2: DGFace.STFST02,
                    FTL: DGFace.STFTL00,
                    FTR: DGFace.STFTR00),
                < 80 and >= 60 => new FaceSet(
                    EVL: DGFace.STFEVL1,
                    KILL: DGFace.STFKILL1,
                    OUCH: DGFace.STFOUCH1,
                    ST0: DGFace.STFST10,
                    ST1: DGFace.STFST11,
                    ST2: DGFace.STFST12,
                    FTL: DGFace.STFTL10,
                    FTR: DGFace.STFTR10),
                < 60 and >= 40 => new FaceSet(
                    EVL: DGFace.STFEVL2,
                    KILL: DGFace.STFKILL2,
                    OUCH: DGFace.STFOUCH2,
                    ST0: DGFace.STFST20,
                    ST1: DGFace.STFST21,
                    ST2: DGFace.STFST22,
                    FTL: DGFace.STFTL20,
                    FTR: DGFace.STFTR20),
                < 40 and >= 20 => new FaceSet(
                    EVL: DGFace.STFEVL3,
                    KILL: DGFace.STFKILL3,
                    OUCH: DGFace.STFOUCH3,
                    ST0: DGFace.STFST30,
                    ST1: DGFace.STFST31,
                    ST2: DGFace.STFST32,
                    FTL: DGFace.STFTL30,
                    FTR: DGFace.STFTR30),
                < 20 => new FaceSet(
                    EVL: DGFace.STFEVL4,
                    KILL: DGFace.STFKILL4,
                    OUCH: DGFace.STFOUCH4,
                    ST0: DGFace.STFST40,
                    ST1: DGFace.STFST41,
                    ST2: DGFace.STFST42,
                    FTL: DGFace.STFTL40,
                    FTR: DGFace.STFTR40),
            };
        }
        #endregion

        public interface IPortraitOverlayController
        {
            void RefreshFace();
            void Damage(int amount, UnitEntityData? sourceUnit = null);

        }

        public class PortraitOverlayController<TBuffView> : MonoBehaviour, IDisposable, IPortraitOverlayController,
            IFactCollectionUpdatedHandler where TBuffView : ViewBase<UnitBuffPartVM>
        {
            public PortraitOverlayController(PartyCharacterView<TBuffView> view, (GameObject gameobject, Action<Sprite> setSprite) overlay)
            {
                PartyCharacterView = view;
                SetSprite = overlay.setSprite;
                overlayObject = overlay.gameobject;

                MicroLogger.Debug(() => $"Initializing overlay controller for {Unit.CharacterName}");

                EventBusSubscription = EventBus.Subscribe(this);

                TryActivate();
            }

            private IDisposable EventBusSubscription;

            void IFactCollectionUpdatedHandler.HandleFactCollectionUpdated(EntityFactsProcessor collection)
            {
                if (collection.Manager.Owner != Unit || collection is not BuffCollection) return;

                TryActivate();
            }

            private bool isActive = false;

            private void TryActivate()
            {
                if (!Enabled)
                {
                    Deactivate();
                    return;
                }

                if (Unit.Buffs.RawFacts.FirstOrDefault(buff => buff.Blueprint.AssetGuid == buffBp.BlueprintGuid) is not Buff buff)
                {
                    Deactivate();
                    return;
                }

                if (isActive) return;

                buff.GetComponent<PortraitOverlayComponent>().Controller = this;

                //SoundState.Instance.MusicPlayer.SetCustomStoryTheme("Music_MythicGain_Play", "Music_MythicGain_Stop", null);

                Activate();
            }
            
            private void Activate()
            {
                isActive = true;

                UpdateFaceSet();
                RefreshFace();
                overlayObject.SetActive(true);
                ResetTimer();
            }

            private void Deactivate()
            {
                isActive = false;

                UpdateTimer?.Dispose();
                overlayObject.SetActive(false);
            }

            private IDisposable? BuffAddedObserver;
            private IDisposable? BuffRemovedObserver;

            private readonly GameObject overlayObject;
            public readonly Action<Sprite> SetSprite;

            void OnDestroy()
            {
                this.Dispose();
            }

            private bool disposed = true;
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;

                UpdateTimer?.Dispose();
                BuffAddedObserver?.Dispose();
                BuffRemovedObserver?.Dispose();
                EventBusSubscription?.Dispose();
            }

            public readonly PartyCharacterView<TBuffView> PartyCharacterView;

            internal UnitEntityData Unit => ((PartyCharacterVM)PartyCharacterView.GetViewModel()).UnitEntityData;

            private FaceSet CurrentFaceSet = FacesForHP(100);

            private DGFace currentFace = DGFace.STFST00;
            internal DGFace CurrentFace
            {
                get
                {
                    if (IsDead) return DGFace.STFDEAD00;
                    
                    if (IsGodMode) return DGFace.STFGOD0;

                    return currentFace;
                }

                set
                {
                    currentFace = value;
                    var faceName = Enum.GetName(typeof(DGFace), CurrentFace);

                    MicroLogger.Debug(() => $"Setting face to {faceName}");

                    SetSprite(PortraitOverlay.GetSprite(faceName));
                }
            }

            private readonly string[] GodModeMusicThemeEventNames = new[]
            {
                "Music_MythicGain_Play"
            };

            private bool IsGodMode
            {
                get
                {
                    if (SoundState.Instance is null || SoundState.Instance.MusicPlayer is null) return false;
                    
                    return SoundState.Instance.MusicPlayer.m_Themes
                        .Where(theme => GodModeMusicThemeEventNames.Contains(theme.StartEvent) && theme.IsSet)
                        .Any();
                }
            }

            private bool IsDead => Unit.State.IsDead;

            internal void UpdateFaceSet()
            {
                var hpPercent = ((Unit.HPLeft + Unit.TemporaryHP) * 100) / Unit.MaxHP;

                CurrentFaceSet = FacesForHP(hpPercent);
            }

            private IDisposable? UpdateTimer;

            // Doom's ticrate (fps) is 35
            private void ResetTimer(int tics)
            {
                UpdateTimer?.Dispose();

                var ts = TimeSpan.FromMilliseconds((double)tics * 1000 / 35);

                UpdateTimer = DelayedInvoker.InvokeInTime(() =>
                {
                    UpdateTimer?.Dispose();
                    UpdateTimer = null;

                    RefreshFace();
                    ResetTimer();
                }
                , (float)ts.TotalSeconds, true);
            }

            private void ResetTimer()
            {
                var ticsInterval = 70 + UnityEngine.Random.Range(-20, 20);
                ResetTimer(ticsInterval);
            }
            private void ResetTimerShort()
            {
                var ticsInterval = 35 + UnityEngine.Random.Range(-10, 10);
                ResetTimer(ticsInterval);
            }

            public void RefreshFace()
            {
                if (CurrentFace == CurrentFaceSet.ST1)
                {
                    if (UnityEngine.Random.Range(0, 2) > 0)
                        CurrentFace = CurrentFaceSet.ST2;
                    else
                        CurrentFace = CurrentFaceSet.ST0;
                }
                else
                {
                    CurrentFace = CurrentFaceSet.ST1;
                }
            }

            public void Damage(int amount, UnitEntityData? sourceUnit = null)
            {
                if (Unit is null) return;

                MicroLogger.Debug(() => $"Damage amount: {amount}. HP: {Unit.HPLeft + 1}/{Unit.MaxHP + Unit.TemporaryHP}");

                if (amount < 1) return;

                UpdateFaceSet();

                if (amount >= Unit.MaxHP / 0.2)
                {
                    CurrentFace = CurrentFaceSet.OUCH;
                }
                else
                {
                    if (sourceUnit is not null)
                    {
                        var camera = Game.GetCamera();

                        var camWidth = camera.pixelWidth;

                        var screenPosition = camera.WorldToViewportPoint(sourceUnit.Position);

                        if (screenPosition.x < camWidth / 3)
                            CurrentFace = CurrentFaceSet.FTL;

                        else if  (screenPosition.x > (camWidth * 2) / 3)
                            CurrentFace = CurrentFaceSet.FTR;

                        else CurrentFace = CurrentFaceSet.KILL;
                    }
                    else CurrentFace = CurrentFaceSet.KILL;
                }

                ResetTimerShort();
            }
        }

        [AllowedOn(typeof(BlueprintBuff))]
        internal class PortraitOverlayComponent :
            UnitFactComponentDelegate, IGlobalSubscriber, ISubscriber, IDamageHandler, IUnitLifeStateChanged
        {
            private IPortraitOverlayController? controller;

            public IPortraitOverlayController? Controller
            {
                get => controller;
                set => controller = value;
            }

            void IUnitLifeStateChanged.HandleUnitLifeStateChanged(UnitEntityData unit, UnitLifeState prevLifeState)
            {
                if (unit is null || unit != this.Owner) return;
                MicroLogger.Debug(() => $"{nameof(PortraitOverlayComponent)} {nameof(IUnitLifeStateChanged.HandleUnitLifeStateChanged)}");

                Controller?.RefreshFace();
            }

            void IDamageHandler.HandleDamageDealt(RuleDealDamage dealDamage)
            {
                if (dealDamage.Target != this.Owner) return;
                MicroLogger.Debug(() => $"{nameof(PortraitOverlayComponent)} {nameof(IDamageHandler.HandleDamageDealt)}");

                Controller?.Damage(dealDamage.Result, dealDamage.Initiator);
            }
        }

        private static readonly IMicroBlueprint<BlueprintBuff> buffBp = new MicroBlueprint<BlueprintBuff>("03ABBCCA-C01C-4057-A183-9CB20B3D4C8C");
        
        [Init]
        internal static void CreateEnchant()
        {
            var bic = new BlueprintInitializationContext(Triggers.BlueprintsCache_Init);

            var buff = bic.NewBlueprint<BlueprintBuff>("03ABBCCA-C01C-4057-A183-9CB20B3D4C8C", "RipAndTearBuff")
                .Map(buff =>
                {
                    buff.AddComponent<PortraitOverlayComponent>();

                    buff.m_Flags = BlueprintBuff.Flags.HiddenInUi;

                    return buff;
                });

            var feature = bic.NewBlueprint<BlueprintFeature>("563B8476-B1FE-4314-8D1C-C567FEA0F537", "RipAndTearFeature")
                .Combine(buff)
                .Map(fb =>
                {
                    var (feature, buff) = fb;

                    feature.AddComponent<AddFacts>(component =>
                    {
                        component.m_Facts = new[] { buff.ToReference<BlueprintUnitFactReference>() };

                        return component;
                    });

                    return feature;
                });

            var enchant = bic.NewBlueprint<BlueprintEquipmentEnchantment>("89BF4CDB-9C4D-462E-8271-86FA30B20B33", "RipAndTearEnchant")
                .Combine(feature)
                .Map(ef =>
                {
                    var (enchant, feature) = ef;

                    var component = enchant.AddAddUnitFeatureEquipment();

                    component.m_Feature = feature.ToReference<BlueprintFeatureReference>();

                    return enchant;
                });

            bic.GetBlueprint(BlueprintsDb.Owlcat.BlueprintItemEquipmentHead.KillerHelm_easterEgg)
                .Combine(enchant)
                .Map(ie =>
                {
                    var (item, enchant) = ie;

                    item.Enchantments.Add(enchant);
                })
                .Register();
        }
    }
}
