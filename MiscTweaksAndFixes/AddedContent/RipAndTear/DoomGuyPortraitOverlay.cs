using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;

using MicroWrath;
//using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.InitContext;
using MicroWrath.Util;
using MicroWrath.Util.Linq;

using MiscTweaksAndFixes.Things;

using Newtonsoft.Json;

using Owlcat.Runtime.UniRx;

using UnityEngine;

using Wat;

namespace MiscTweaksAndFixes.AddedContent.RipAndTear
{
    [AllowedOn(typeof(BlueprintBuff))]
    public class DoomGuyFaceOverlay : PortraitOverlayComponent<DoomGuyFaceOverlay.ComponentData>, IDamageHandler, IUnitLifeStateChanged
    //, IFactCollectionUpdatedHandler
    {
        #region DGFace
        public enum DGFace
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

        public readonly record struct FaceSet(DGFace EVL, DGFace KILL, DGFace OUCH, DGFace ST0, DGFace ST1, DGFace ST2,
            DGFace FTL, DGFace FTR)
        {
            public FaceSet() : this(DGFace.STFEVL0, DGFace.STFKILL0, DGFace.STFOUCH0, DGFace.STFST00, DGFace.STFST01,
                DGFace.STFST02, DGFace.STFTL00, DGFace.STFTR00)
            { }
        }

        public static FaceSet FacesForHP(int hpPercent)
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

        public class ComponentData : PortraitOverlayComponentData, IDisposable
        {
            //public ComponentData() { }


            [JsonIgnore]
            private DGFace currentFace;

            [JsonIgnore]
            public DGFace CurrentFace
            {
                get => currentFace;

                set
                {
                    currentFace = value;

                    var faceName = Enum.GetName(typeof(DGFace), CurrentFace);

                    MicroLogger.Debug(() => $"Setting face to {faceName}");

                    ForegroundSprite.Value = (RipAndTear.GetSprite(faceName), (float)1.2);
                }
            }

            public void Init()
            {
                MicroLogger.Debug(() => $"{nameof(DoomGuyFaceOverlay)}.{nameof(ComponentData)}.{nameof(Init)}");

                BackgroundSprite.Value = RipAndTear.GetSprite("STFB1");
            }

            [JsonIgnore]
            public FaceSet CurrentFaceSet = FacesForHP(100);

            public void RefreshFace()
            {
                MicroLogger.Debug(() => $"{nameof(DoomGuyFaceOverlay)}.{nameof(RefreshFace)}");

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

            [JsonIgnore]
            private IDisposable? UpdateTimer;

            // Doom's ticrate (fps) is 35/s
            internal void ResetTimer(int tics)
            {
                UpdateTimer?.Dispose();

                var ts = TimeSpan.FromMilliseconds((double)tics * 1000 / 35);

                UpdateTimer = DelayedInvoker.InvokeInTime(() =>
                {
                    UpdateTimer?.Dispose();
                    UpdateTimer = null;

                    RefreshFace();
                    ResetTimer();
                },
                (float)ts.TotalSeconds, true);
            }

            internal void ResetTimer()
            {
                var ticsInterval = 70 + UnityEngine.Random.Range(-20, 20);
                ResetTimer(ticsInterval);
            }
            internal void ResetTimerShort()
            {
                var ticsInterval = 35 + UnityEngine.Random.Range(-10, 10);
                ResetTimer(ticsInterval);
            }

            public override void Dispose()
            {
                UpdateTimer?.Dispose();
            }

        }

        public override void OnDeactivate()
        {
            MicroLogger.Debug(() => $"{nameof(DoomGuyFaceOverlay)}.{nameof(OnDeactivate)}");

            Data.Dispose();

            base.OnDeactivate();
        }

        public override void OnFactAttached()
        {
            MicroLogger.Debug(() => $"{nameof(DoomGuyFaceOverlay)}.{nameof(OnFactAttached)}");

            Data.Init();

            Data.RefreshFace();
            Data.ResetTimer();

            base.OnFactAttached();
        }

        //[JsonIgnore]
        //private DGFace currentFace;

        internal DGFace CurrentFace
        {
            get
            {
                if (Owner is null)
                {
                    MicroLogger.Error("Null owner for overlay");

                    return Data.CurrentFace;
                }

                if (Owner.State.IsDead) return DGFace.STFDEAD00;

                //if (IsGodMode) return DGFace.STFGOD0;

                return Data.CurrentFace;
            }
        }

        internal void UpdateFaceSet()
        {
            if (Owner is null)
            {
                MicroLogger.Error("Null owner for overlay");
                return;
            }

            var hpPercent = ((Owner.HPLeft + Owner.TemporaryHP) * 100) / Owner.MaxHP;

            Data.CurrentFaceSet = FacesForHP(hpPercent);
        }

        void IDamageHandler.HandleDamageDealt(RuleDealDamage dealDamage)
        {
            if (Owner is null)
            {
                return;
            }

            if (dealDamage.Target != this.Owner) return;
            MicroLogger.Debug(() => $"{nameof(DoomGuyFaceOverlay)} {nameof(IDamageHandler.HandleDamageDealt)}");

            var amount = dealDamage.Result;
            var sourceUnit = dealDamage.Initiator;

            MicroLogger.Debug(() => $"Damage amount: {amount}. HP: {Owner.HPLeft + 1}/{Owner.MaxHP + Owner.TemporaryHP}");

            if (amount < 1) return;

            UpdateFaceSet();

            if (amount >= Owner.MaxHP / 0.2)
            {
                Data.CurrentFace = Data.CurrentFaceSet.OUCH;
            }
            else
            {
                if (sourceUnit is not null)
                {
                    var camera = Game.GetCamera();

                    var camWidth = camera.rect.width;

                    var screenPosition = camera.WorldToViewportPoint(sourceUnit.Position);

                    if (screenPosition.x < camWidth / 3)
                        Data.CurrentFace = Data.CurrentFaceSet.FTL;

                    else if (screenPosition.x > (camWidth * 2) / 3)
                        Data.CurrentFace = Data.CurrentFaceSet.FTR;

                    else Data.CurrentFace = Data.CurrentFaceSet.KILL;
                }
                else Data.CurrentFace = Data.CurrentFaceSet.KILL;
            }

            Data.ResetTimerShort();
        }

        void IUnitLifeStateChanged.HandleUnitLifeStateChanged(UnitEntityData unit, UnitLifeState _)
        {
            if (unit is null || unit != Owner) return;

            MicroLogger.Debug(() => $"{nameof(DoomGuyFaceOverlay)} {nameof(IUnitLifeStateChanged.HandleUnitLifeStateChanged)}");

            Data.RefreshFace();
        }
    }

    internal static partial class RipAndTear
    {
        private static bool enabled;
        internal static bool Enabled
        {
            get => enabled; set => enabled = value;
        }

        //        public interface IPortraitOverlayController
        //        {
        //            void RefreshFace();
        //            void Damage(int amount, UnitEntityData? sourceUnit = null);

        //        }

        //        public class PortraitOverlayController<TBuffView> : MonoBehaviour, IDisposable, IPortraitOverlayController,
        //            IFactCollectionUpdatedHandler where TBuffView : ViewBase<UnitBuffPartVM>
        //        {
        //            public PortraitOverlayController(PartyCharacterView<TBuffView> view, (GameObject gameobject, Action<Sprite> setSprite) overlay)
        //            {
        //                PartyCharacterView = view;
        //                SetSprite = overlay.setSprite;
        //                overlayObject = overlay.gameobject;

        //                var characterName = Unit?.CharacterName ?? "<null>";

        //                MicroLogger.Debug(() => $"Initializing overlay controller for {characterName}");

        //                EventBusSubscription = EventBus.Subscribe(this);

        //                TryActivate();
        //            }

        //            public readonly PartyCharacterView<TBuffView> PartyCharacterView;

        //            private PartyCharacterVM? VM => PartyCharacterView.ViewModel;

        //            internal UnitEntityData? Unit => VM?.UnitEntityData;

        //            private IDisposable EventBusSubscription;

        //            void IFactCollectionUpdatedHandler.HandleFactCollectionUpdated(EntityFactsProcessor collection)
        //            {
        //                if (Unit is null) return;

        //                if (collection.Manager.Owner != Unit || collection is not BuffCollection) return;

        //                TryActivate();
        //            }

        //            private bool isActive = false;

        //            private void TryActivate()
        //            {
        //                if (!Enabled || Unit is null)
        //                {
        //                    Deactivate();
        //                    return;
        //                }

        //                if (Unit.Buffs.RawFacts.FirstOrDefault(buff => buff.Blueprint.AssetGuid == buffBp.BlueprintGuid) is not Buff buff)
        //                {
        //                    Deactivate();
        //                    return;
        //                }

        //                if (isActive) return;

        //                buff.GetComponent<PortraitOverlayComponent>().Controller = this;

        //                //SoundState.Instance.MusicPlayer.SetCustomStoryTheme("Music_MythicGain_Play", "Music_MythicGain_Stop", null);

        //                Activate();
        //            }

        //            void OnDisable()
        //            {
        //                MicroLogger.Debug(() => "Disabled");
        //                Deactivate();
        //            }

        //            void OnEnable()
        //            {
        //                MicroLogger.Debug(() => "Enabled");
        //                TryActivate();
        //            }

        //            private void Activate()
        //            {
        //                isActive = true;

        //                UpdateFaceSet();
        //                RefreshFace();
        //                overlayObject.SetActive(true);
        //                ResetTimer();
        //            }

        //            private void Deactivate()
        //            {
        //                isActive = false;

        //                UpdateTimer?.Dispose();
        //                overlayObject.SetActive(false);
        //            }

        //            internal readonly GameObject overlayObject;
        //            public readonly Action<Sprite> SetSprite;

        //            internal void OnDestroy()
        //            {
        //                MicroLogger.Debug(() => $"Destroyed");
        //                Destroy(overlayObject);
        //                this.Dispose();
        //            }

        //            //private bool disposed = false;
        //            public void Dispose()
        //            {
        //                MicroLogger.Debug(() => $"Disposing portrait controller for {Unit?.CharacterName ?? "<null>"}");

        //                //if (disposed) return;
        //                //disposed = true;

        //                UpdateTimer?.Dispose();
        //                EventBusSubscription?.Dispose();
        //            }


        //            private FaceSet CurrentFaceSet = FacesForHP(100);

        //            private DGFace currentFace = DGFace.STFST00;
        //            internal DGFace CurrentFace
        //            {
        //                get
        //                {
        //                    if (IsDead) return DGFace.STFDEAD00;

        //                    if (IsGodMode) return DGFace.STFGOD0;

        //                    return currentFace;
        //                }

        //                set
        //                {
        //                    currentFace = value;
        //                    var faceName = Enum.GetName(typeof(DGFace), CurrentFace);

        //                    MicroLogger.Debug(() => $"Setting face to {faceName}");

        //                    SetSprite(PortraitOverlay.GetSprite(faceName));
        //                }
        //            }

        //            private readonly string[] GodModeMusicThemeEventNames = new[]
        //            {
        //                "Music_MythicGain_Play"
        //            };

        //            private bool IsGodMode
        //            {
        //                get
        //                {
        //                    if (SoundState.Instance is null || SoundState.Instance.MusicPlayer is null) return false;

        //                    return SoundState.Instance.MusicPlayer.m_Themes
        //                        .Where(theme => GodModeMusicThemeEventNames.Contains(theme.StartEvent) && theme.IsSet)
        //                        .Any();
        //                }
        //            }

        //            private bool IsDead => Unit?.State?.IsDead ?? false;

        //            internal void UpdateFaceSet()
        //            {
        //                if (Unit is null)
        //                {
        //                    Deactivate();
        //                    return;
        //                }

        //                var hpPercent = ((Unit.HPLeft + Unit.TemporaryHP) * 100) / Unit.MaxHP;

        //                CurrentFaceSet = FacesForHP(hpPercent);
        //            }

        //            private IDisposable? UpdateTimer;

        //            // Doom's ticrate (fps) is 35
        //            private void ResetTimer(int tics)
        //            {
        //                UpdateTimer?.Dispose();

        //                var ts = TimeSpan.FromMilliseconds((double)tics * 1000 / 35);

        //                UpdateTimer = DelayedInvoker.InvokeInTime(() =>
        //                {
        //                    UpdateTimer?.Dispose();
        //                    UpdateTimer = null;

        //                    RefreshFace();
        //                    ResetTimer();
        //                }
        //                , (float)ts.TotalSeconds, true);
        //            }

        //            private void ResetTimer()
        //            {
        //                var ticsInterval = 70 + UnityEngine.Random.Range(-20, 20);
        //                ResetTimer(ticsInterval);
        //            }
        //            private void ResetTimerShort()
        //            {
        //                var ticsInterval = 35 + UnityEngine.Random.Range(-10, 10);
        //                ResetTimer(ticsInterval);
        //            }

        //            public void RefreshFace()
        //            {
        //                if (CurrentFace == CurrentFaceSet.ST1)
        //                {
        //                    if (UnityEngine.Random.Range(0, 2) > 0)
        //                        CurrentFace = CurrentFaceSet.ST2;
        //                    else
        //                        CurrentFace = CurrentFaceSet.ST0;
        //                }
        //                else
        //                {
        //                    CurrentFace = CurrentFaceSet.ST1;
        //                }
        //            }

        //            public void Damage(int amount, UnitEntityData? sourceUnit = null)
        //            {
        //                if (Unit is null) return;

        //                MicroLogger.Debug(() => $"Damage amount: {amount}. HP: {Unit.HPLeft + 1}/{Unit.MaxHP + Unit.TemporaryHP}");

        //                if (amount < 1) return;

        //                UpdateFaceSet();

        //                if (amount >= Unit.MaxHP / 0.2)
        //                {
        //                    CurrentFace = CurrentFaceSet.OUCH;
        //                }
        //                else
        //                {
        //                    if (sourceUnit is not null)
        //                    {
        //                        var camera = Game.GetCamera();

        //                        var camWidth = camera.rect.width;

        //                        var screenPosition = camera.WorldToViewportPoint(sourceUnit.Position);

        //                        if (screenPosition.x < camWidth / 3)
        //                            CurrentFace = CurrentFaceSet.FTL;

        //                        else if (screenPosition.x > (camWidth * 2) / 3)
        //                            CurrentFace = CurrentFaceSet.FTR;

        //                        else CurrentFace = CurrentFaceSet.KILL;
        //                    }
        //                    else CurrentFace = CurrentFaceSet.KILL;
        //                }

        //                ResetTimerShort();
        //            }
        //        }

        //        [AllowedOn(typeof(BlueprintBuff))]
        //        internal class PortraitOverlayComponent :
        //            UnitFactComponentDelegate, IGlobalSubscriber, ISubscriber, IDamageHandler, IUnitLifeStateChanged
        //        {
        //            private IPortraitOverlayController? controller;

        //            public IPortraitOverlayController? Controller
        //            {
        //                get => controller;
        //                set => controller = value;
        //            }

        //            void IUnitLifeStateChanged.HandleUnitLifeStateChanged(UnitEntityData unit, UnitLifeState prevLifeState)
        //            {
        //                if (unit is null || unit != this.Owner) return;
        //                MicroLogger.Debug(() => $"{nameof(PortraitOverlayComponent)} {nameof(IUnitLifeStateChanged.HandleUnitLifeStateChanged)}");

        //                Controller?.RefreshFace();
        //            }

        //            void IDamageHandler.HandleDamageDealt(RuleDealDamage dealDamage)
        //            {
        //                if (dealDamage.Target != this.Owner) return;
        //                MicroLogger.Debug(() => $"{nameof(PortraitOverlayComponent)} {nameof(IDamageHandler.HandleDamageDealt)}");

        //                Controller?.Damage(dealDamage.Result, dealDamage.Initiator);
        //            }
        //        }

        //        private static readonly IMicroBlueprint<BlueprintBuff> buffBp = new MicroBlueprint<BlueprintBuff>("03ABBCCA-C01C-4057-A183-9CB20B3D4C8C");

        private static readonly Regex ResourceNameRegex = new(@"\G(?:(?:[^\.]+\.)*[^\.]+)\.(?:RipAndTear\.Resources)\.([^\.]+)\z");

        private static IEnumerable<(string, byte[])> GetResources()
        {
            MicroLogger.Debug(() => $"{nameof(RipAndTear)}.{nameof(GetResources)}");

            var assembly = Assembly.GetExecutingAssembly();
            var resourcesNames = assembly.GetManifestResourceNames().Choose(n =>
            {
                var match = ResourceNameRegex.Match(n);

                return (Option<(string, string)>)(match.Success ?
                    Option.Some<(string, string)>((match.Value, match.Groups[1].Value)) :
                    Option<(string, string)>.None);
            });

            foreach (var n in resourcesNames)
            {
                var (resourceName, fileName) = n;

                using var stream = assembly.GetManifestResourceStream(resourceName);
                using var reader = new BinaryReader(stream);

                yield return (fileName, reader.ReadBytes((int)(ulong)stream.Length));
            }
        }

        private static readonly Lazy<IDictionary<string, byte[]>> resources = new(() => GetResources().ToDictionary());
        private static IDictionary<string, byte[]> Resources => resources.Value;

        private static Palette[] Palettes
        {
            get
            {
                var arr = new Palette[14];
                for (var i = 0; i <= 13; i++)
                {
                    arr[i] = new(Resources["PLAYPAL"], i);
                }

                return arr;
            }
        }

        private static PatchImage GetPatchImage(string resourceName) => new(Resources[resourceName]);
        internal static Sprite GetSprite(string resourceName)
        {
            var pi = GetPatchImage(resourceName);
            var texture = UnityWat.CreateTexture(pi, Palettes[0]);

            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

            return sprite;
        }

        [Init]
        internal static void CreateEnchant()
        {
            //var bic = new BlueprintInitializationContext(Triggers.BlueprintsCache_Init);

            var buff = InitContext.NewBlueprint<BlueprintBuff>("03ABBCCA-C01C-4057-A183-9CB20B3D4C8C", "RipAndTearBuff")
                .Map(buff =>
                {
                    buff.AddComponent<DoomGuyFaceOverlay>();

                    buff.m_Flags = BlueprintBuff.Flags.HiddenInUi;

                    return buff;
                })
                .AddOnTrigger(BlueprintGuid.Parse("03ABBCCA-C01C-4057-A183-9CB20B3D4C8C"), Triggers.BlueprintsCache_Init);

            var feature = InitContext.NewBlueprint<BlueprintFeature>("563B8476-B1FE-4314-8D1C-C567FEA0F537", "RipAndTearFeature")
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
                })
                .AddOnTrigger(BlueprintGuid.Parse("563B8476-B1FE-4314-8D1C-C567FEA0F537"), Triggers.BlueprintsCache_Init);

            var enchant = InitContext.NewBlueprint<BlueprintEquipmentEnchantment>("89BF4CDB-9C4D-462E-8271-86FA30B20B33", "RipAndTearEnchant")
                .Combine(feature)
                .Map(ef =>
                {
                    var (enchant, feature) = ef;

                    if (enabled)
                    {
                        var component = enchant.AddAddUnitFeatureEquipment();

                        component.m_Feature = feature.ToReference<BlueprintFeatureReference>();
                    }

                    return enchant;
                })
                .AddOnTrigger(BlueprintGuid.Parse("89BF4CDB-9C4D-462E-8271-86FA30B20B33"), Triggers.BlueprintsCache_Init);

            InitContext.GetBlueprint(BlueprintsDb.Owlcat.BlueprintItemEquipmentHead.KillerHelm_easterEgg)
                .Combine(enchant)
                .Map(ie =>
                {
                    var (item, enchant) = ie;
                    
                    if (enabled) item.Enchantments.Add(enchant);

                    return item;
                })
                .AddOnTrigger(BlueprintGuid.Parse("89BF4CDB-9C4D-462E-8271-86FA30B20B33"), Triggers.BlueprintsCache_Init);
        }
    }
}
