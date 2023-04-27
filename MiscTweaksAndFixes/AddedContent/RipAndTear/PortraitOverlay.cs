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
    internal static partial class RipAndTear
    {
        internal static class PortraitOverlay
        {            
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
            internal static Sprite GetSprite(string resourceName)
            {
                var pi = GetPatchImage(resourceName);
                var texture = UnityWat.CreateTexture(pi, Palettes[0]);
                
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

                return sprite;
            }

            internal static readonly Lazy<Sprite> Face = new(() => GetSprite("STFST01"));
            
            internal static readonly Lazy<Sprite> Background = new(() => GetSprite("STFB1"));

            internal static bool SetupBackgroundOverlay(GameObject overlay, GameObject portraitView, Sprite bgSprite)
            {
                var image = overlay.GetComponent<Image>();
                image.sprite = bgSprite;

                if (overlay.transform is not RectTransform transform) return false;

                transform.SetParent(portraitView.transform);

                if (transform.parent.Find("LifePortrait") is RectTransform lifePortraitTransform)
                {
                    transform.anchorMin = lifePortraitTransform.anchorMin;
                    transform.anchorMax = lifePortraitTransform.anchorMax;

                    transform.offsetMin = lifePortraitTransform.offsetMin;
                    transform.offsetMax = lifePortraitTransform.offsetMax;

                    transform.localScale = lifePortraitTransform.localScale;

                    transform.SetSiblingIndex(lifePortraitTransform.GetSiblingIndex() + 1);
                }

                transform.localRotation = Quaternion.identity;
                transform.sizeDelta = Vector2.zero;

                return true;
            }

            internal static (bool, Action<Sprite>?) SetupFaceOverlay(GameObject faceOverlay, GameObject parent, Sprite faceSprite)
            {
                var image = faceOverlay.GetComponent<Image>();
                image.sprite = faceSprite;
                image.preserveAspect = false;

                if (faceOverlay.transform is not RectTransform transform) return (false, null);

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

                // Aspect ratio correction
                var yScale = transform.localScale.y;
                transform.localScale = new Vector3((float)(yScale / 1.2), yScale);
    
                return (true, sprite =>
                {
                    var oldSprite = image.sprite;
                    image.sprite = sprite;
                    UnityEngine.Object.Destroy(oldSprite); 
                });
            }

            private static GameObject? OverlayBackgroundPrototype;
            internal static GameObject CreateOverlayBackgroundPrototype() =>
                OverlayBackgroundPrototype ??= new("PortraitOverlayBackground", new Type[] { typeof(RectTransform), typeof(Image) });
            private static GameObject? OverlayFacePrototype;
            internal static GameObject GetOverlayFacePrototype() =>
                OverlayFacePrototype ??= new("PortraitOverlay", new Type[] { typeof(RectTransform), typeof(Image) });

            private static (GameObject, Action<Sprite>)? CreatePortraitOverlay(GameObject view)
            {
                var backgroundOverlay = UnityEngine.Object.Instantiate(CreateOverlayBackgroundPrototype());
                var faceOverlay = UnityEngine.Object.Instantiate(GetOverlayFacePrototype());

                Sprite sprite = Face.Value;
                var bgSprite = Background.Value;

                var bgSuccess = SetupBackgroundOverlay(backgroundOverlay, view, bgSprite);
                var (fgSucc, setSprite) = SetupFaceOverlay(faceOverlay, backgroundOverlay, sprite);
                if (!bgSuccess || !fgSucc)
                {
                    MicroLogger.Error("Failed to setup overlay");
                    return null;
                }

                backgroundOverlay.SetActive(false);
                return (backgroundOverlay, setSprite!);
            }

            internal static (GameObject, Action<Sprite>)? CreatePortraitOverlay(PartyCharacterPCView view) => CreatePortraitOverlay(view.m_PortraitView.gameObject);
            internal static (GameObject, Action<Sprite>)? CreatePortraitOverlay(PartyCharacterConsoleView view) => CreatePortraitOverlay(view.m_PortraitView.gameObject);
        }
    }
}
