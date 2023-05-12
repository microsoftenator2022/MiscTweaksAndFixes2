using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.UI.MVVM._ConsoleView.Party;
using Kingmaker.UI.MVVM._PCView.Party;
using Kingmaker.UI.MVVM._VM.Party;

using MicroWrath;

using Owlcat.Runtime.UI.MVVM;

using UnityEngine;
using UnityEngine.UI;

namespace MiscTweaksAndFixes.Things
{
    internal partial class PortraitOverlay
    {
        private static class Prototypes
        {
            private static GameObject? background;
            internal static GameObject Background
            {
                get
                {
                    if (background == null)
                        background = new(
                            "PortraitOverlayBackground",
                            new Type[] { typeof(RectTransform), typeof(Image) });

                    return background;
                }
            }

            private static GameObject? foreground;
            internal static GameObject Foreground
            {
                get
                {
                    if (foreground == null)
                        foreground = new(
                            "PortraitOverlayForeground",
                            new Type[] { typeof(RectTransform), typeof(Image) });

                    return foreground;
                }
            }
        }

        internal PortraitOverlay() : base()
        {
            
        }

        void OnDestroy()
        {
            MicroLogger.Debug(() => $"Destroying portrait overlay");

            if (Background != null) Destroy(Background);
        }

        void OnEnable()
        {
            if (Background == null) return;

            MicroLogger.Debug(() => $"Enabling portrait overlay");

            Background.SetActive(true);
        }

        void OnDisable()
        {
            if (Background == null) return;

            MicroLogger.Debug(() => $"Disabling portrait overlay");

            Background.SetActive(false);
        }

        public GameObject? Background { get; private set; }
        public void SetBGSprite(Sprite sprite)
        {
            if (Background == null) return;

            var image = Background.GetComponent<Image>();

            var oldSprite = image.sprite;
            image.sprite = sprite;
            Destroy(oldSprite);
        }

        private GameObject? CreateBackgroundOverlay(UnitPortraitPartView portraitView, Sprite? bgSprite)
        {
            var bgOverlay = Instantiate(Prototypes.Background);

            var image = bgOverlay.GetComponent<Image>();
            
            if (bgSprite != null) image.sprite = bgSprite;

            if (bgOverlay.transform is not RectTransform transform)
            {
                Destroy(bgOverlay);
                return null;
            }

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

            //Background = bgOverlay;

            return bgOverlay;
        }

        public GameObject? Foreground { get; private set; }
        public void SetFGSprite(Sprite sprite)
        {
            if (Foreground == null) return;

            var image = Foreground.GetComponent<Image>();

            var oldSprite = image.sprite;
            image.sprite = sprite;
            Destroy(oldSprite);
        }
        private GameObject? CreateForegroundOverlay(GameObject parent, Sprite? fgSprite)
        {
            var fgOverlay = Instantiate(Prototypes.Foreground);

            var image = fgOverlay.GetComponent<Image>();
            
            if (fgSprite != null) image.sprite = fgSprite;

            image.preserveAspect = false;

            if (fgOverlay.transform is not RectTransform transform)
            {
                Destroy(fgOverlay);
                return null;
            }

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

            //// Aspect ratio correction
            //var yScale = transform.localScale.y;
            //transform.localScale = new Vector3((float)(yScale / 1.2), yScale);

            return fgOverlay;
        }

        public bool SetupPortraitOverlay(ViewBase<PartyCharacterVM> view, Sprite? background = null, Sprite? foreground = null)
        {
            try
            {
                UnitPortraitPartView portrait;
                
                if (view is PartyCharacterPCView pcView)
                {
                    portrait = pcView.m_PortraitView;
                }
                else if (view is PartyCardCharacterConsoleView consoleView)
                {
                    portrait = consoleView.m_PortraitView;
                }
                else
                {
                    throw new ArgumentException(
                        $"{nameof(ViewBase<PartyCharacterVM>)} parameter is neither " +
                        $"{nameof(PartyCharacterPCView)} nor {nameof(PartyCharacterConsoleView)}",
                        nameof(view));
                }

                view.AddDisposable(this);
                this.transform.parent = view.transform;

                var sprite = foreground;
                var bgSprite = background;

                if (CreateBackgroundOverlay(portrait, bgSprite) is GameObject bg)
                {
                    if (CreateForegroundOverlay(bg, sprite) is GameObject fg)
                    {
                        Background = bg;
                        Foreground = fg;

                        return true;
                    }
                    
                    Destroy(bg);
                }
                
                return false;
            }
            catch (ArgumentException ae)
            {
                MicroLogger.Error("Invalid view", ae);
                return false;
            }
        }
    }
}
