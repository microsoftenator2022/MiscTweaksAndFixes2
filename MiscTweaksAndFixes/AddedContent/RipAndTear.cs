using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using HarmonyLib;

using Kingmaker;
using Kingmaker.UI.MVVM;
using Kingmaker.UI.MVVM._ConsoleView.InGame;
using Kingmaker.UI.MVVM._ConsoleView.Party;
using Kingmaker.UI.MVVM._PCView.InGame;
using Kingmaker.UI.MVVM._PCView.Party;
using Kingmaker.UI.MVVM._VM.Party;

using MicroWrath.BlueprintsDb;
using MicroWrath.Util;
using MicroWrath.Util.Linq;

using Wat;
using MicroWrath;

namespace MiscTweaksAndFixes.AddedContent.RipAndTear
{
    internal static class RipAndTear
    {
       
        internal static bool Enabled = true;

        internal static class PortraitOverlay
        {
            [HarmonyPatch(typeof(RootUIContext), nameof(RootUIContext.InitializeUiScene))]
            internal class RootUIContext_InitializeUiScene_Patch
            {
                public static void Postfix(string loadedUIScene)
                {
                    MicroLogger.Debug(() => $"{nameof(RootUIContext_InitializeUiScene_Patch)}.{nameof(Postfix)}");

                    MicroLogger.Debug(() => $"Loaded scene '{loadedUIScene}'");

                    foreach (var r in Resources.Where(r => r.Key.StartsWith("S")))
                    {
                        MicroLogger.Debug(() => $"{r.Key}: {GetPatchImage(r.Key).Width}x{GetPatchImage(r.Key).Height}");
                    }

                    if (!Enabled) return;

                    CreatePortraitOverlays();
                }
            }

            private static readonly Regex ResourceNameRegex = new(@"\G(?:(?:[^\.]+\.)*[^\.]+)\.(?:RipAndTear\.Resources)\.([^\.]+)\z");

            private static IEnumerable<(string, byte[])> GetResources()
            {
                MicroLogger.Debug(() => $"{nameof(PortraitOverlay)}.{nameof(GetResources)}");

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

            internal static readonly Lazy<Sprite> Face = new(() =>
            {
                var pi = GetPatchImage("STFST01");
                var texture = UnityWat.CreateTexture(pi, Palettes[0]);

                return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            });

            internal static readonly Lazy<Sprite> Background = new(() =>
            {
                var pi = GetPatchImage("STFB1");
                var texture = UnityWat.CreateTexture(pi, Palettes[0]);

                return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            });

            internal static bool SetupBackgroundOverlay(GameObject overlay, GameObject portraitView, Sprite bgSprite)
            {
                var image = overlay.GetComponent<Image>();
                image.sprite = bgSprite;

                if (overlay.transform is not RectTransform transform) return false;

                transform.SetParent(portraitView.transform);
                transform.SetAsLastSibling();

                if (transform.parent.Find("LifePortrait") is RectTransform lifePortraitTransform)
                {
                    transform.anchorMin = lifePortraitTransform.anchorMin;
                    transform.anchorMax = lifePortraitTransform.anchorMax;

                    transform.offsetMin = lifePortraitTransform.offsetMin;
                    transform.offsetMax = lifePortraitTransform.offsetMax;

                    transform.localScale = lifePortraitTransform.localScale;
                }

                transform.localRotation = Quaternion.identity;
                transform.sizeDelta = Vector2.zero;

                return true;
            }

            internal static bool SetupFaceOverlay(GameObject faceOverlay, GameObject parent, Sprite faceSprite)
            {
                var image = faceOverlay.GetComponent<Image>();
                image.sprite = faceSprite;
                image.preserveAspect = true;

                if (faceOverlay.transform is not RectTransform transform) return false;

                transform.SetParent(parent.transform);
                transform.SetAsLastSibling();

                if (transform.parent is RectTransform parentTransform)
                {
                    transform.anchorMin = parentTransform.anchorMin;
                    transform.anchorMax = parentTransform.anchorMax;

                    transform.offsetMin = parentTransform.offsetMin;
                    transform.offsetMax = parentTransform.offsetMax;

                    transform.localScale = parentTransform.localScale;
                }

                transform.localRotation = Quaternion.identity;
                transform.sizeDelta = Vector2.zero;
                transform.pivot = new Vector2(0.5f, 0);

                return true;
            }

            private static GameObject? OverlayBackgroundPrototype;
            internal static GameObject CreateOverlayBackgroundPrototype() =>
                OverlayBackgroundPrototype ??= new("PortraitOverlayBackground", new Type[] { typeof(RectTransform), typeof(Image) });
            private static GameObject? OverlayFacePrototype;
            internal static GameObject GetOverlayFacePrototype() =>
                OverlayFacePrototype ??= new("PortraitOverlay", new Type[] { typeof(RectTransform), typeof(Image) });

            public static void CreatePortraitOverlays()
            {
                if (Game.Instance.RootUiContext.m_UIView is null) return;

                var inGamePCView = Game.Instance.RootUiContext.m_UIView.GetComponent<InGamePCView>();
            
                var inGameConsoleView = Game.Instance.RootUiContext.m_UIView.GetComponent<InGameConsoleView>();

                var portraits =
                    inGamePCView?.GetComponentsInChildren<PartyCharacterPCView>()
                        ?.Select(pc => (vm: pc.GetViewModel() as PartyCharacterVM, view: pc.m_PortraitView.gameObject)) ??
                    inGameConsoleView?.GetComponentsInChildren<PartyCharacterConsoleView>()
                        ?.Select(pc => (pc.GetViewModel() as PartyCharacterVM, pc.m_PortraitView.gameObject));

                if (portraits is null) return;

                MicroLogger.Debug(() => "portraits is not null");

                //var overlayFacePrototype = new GameObject("PortraitOverlay", new Type[] { typeof(RectTransform), typeof(Image) });

                foreach (var portrait in portraits)
                {
                    var unit = portrait.vm?.UnitEntityData;

                    if (unit is null || !unit.Body.Head.HasItem) continue;

                    MicroLogger.Debug(() => $"{unit} exists and has head item {unit.Body.Head.Item}");
                    MicroLogger.Debug(() => $"icon {unit.Body.Head.Item.Icon}");

                    var backgroundOverlay = UnityEngine.Object.Instantiate(CreateOverlayBackgroundPrototype());
                    var faceOverlay = UnityEngine.Object.Instantiate(GetOverlayFacePrototype());

                    var bgSprite = Background.Value;

                    //if (faceOverlay.transform is not RectTransform transform) continue;

                    var headItem = unit.Body.Head.Item;
                    var sprite = headItem.Icon;

                    if (headItem.Blueprint == BlueprintsDb.Owlcat.BlueprintItemEquipmentHead.KillerHelm_easterEgg.GetBlueprint())
                    {
                        sprite = Face.Value;
                    }

                    //var image = faceOverlay.GetComponent<Image>();

                    //image.sprite = sprite;
                    //image.preserveAspect = true;

                    if (!SetupBackgroundOverlay(backgroundOverlay, portrait.view, bgSprite) ||
                        !SetupFaceOverlay(faceOverlay, backgroundOverlay, sprite))
                        MicroLogger.Debug(() => "Failed to setup overlay");

                    //transform.SetParent(portrait.view.transform);
                    //transform.SetAsLastSibling();

                    //if (transform.parent.Find("LifePortrait") is RectTransform lifePortraitTransform)
                    //{
                    //    transform.anchorMin = lifePortraitTransform.anchorMin;
                    //    transform.anchorMax = lifePortraitTransform.anchorMax;

                    //    transform.offsetMin = lifePortraitTransform.offsetMin;
                    //    transform.offsetMax = lifePortraitTransform.offsetMax;

                    //    transform.localScale = lifePortraitTransform.localScale;
                    //}

                    //transform.anchorMin = Vector2.zero;
                    //transform.anchorMax = Vector2.one;
                    //transform.localRotation = Quaternion.identity;
                    //transform.sizeDelta = Vector2.zero;
                    //transform.pivot = new Vector2(0.5f, 0);

                    faceOverlay.SetActive(true);
                }
            }
        }
    }
}
