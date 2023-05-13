﻿using System;
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
                        background = new("PortraitOverlayBackground", typeof(RectTransform), typeof(Image));

                    return background;
                }
            }

            private static GameObject? foreground;
            internal static GameObject Foreground
            {
                get
                {
                    if (foreground == null)
                        foreground = new("PortraitOverlayForeground", typeof(RectTransform), typeof(Image));

                    return foreground;
                }
            }
        }

        public PortraitOverlay() : base() { }

        private static GameObject? prototype;
        private static GameObject Prototype
        {
            get
            {
                if (prototype == null)
                    prototype = new("PortraitOverlay", typeof(PortraitOverlay), typeof(RectTransform));

                return prototype;
            }
        }

        public static (GameObject, PortraitOverlay)? CreateNew(ViewBase<PartyCharacterVM> view,
            Sprite? foreground = null, Sprite? background = null)
        {
            var gameObject = Instantiate(Prototype);

            try
            {
                UnitPortraitPartView portraitView;

                if (view is PartyCharacterPCView pcView)
                {
                    portraitView = pcView.m_PortraitView;
                }
                else if (view is PartyCardCharacterConsoleView consoleView)
                {
                    portraitView = consoleView.m_PortraitView;
                }
                else
                {
                    throw new ArgumentException(
                        $"{nameof(ViewBase<PartyCharacterVM>)} parameter is neither " +
                        $"{nameof(PartyCharacterPCView)} nor {nameof(PartyCharacterConsoleView)}",
                        nameof(view));
                }
            
                gameObject.transform.SetParent(portraitView.transform, false);

                var overlay = gameObject.GetComponent<PortraitOverlay>();

                if (gameObject.transform.parent.Find("LifePortrait") is not RectTransform lifePortraitTransform)
                {
                    throw new Exception("Could not find LifePortrait");
                }

                var transform = (RectTransform)gameObject.transform;

                transform.anchorMin = lifePortraitTransform.anchorMin;
                transform.anchorMax = lifePortraitTransform.anchorMax;

                transform.offsetMin = lifePortraitTransform.offsetMin;
                transform.offsetMax = lifePortraitTransform.offsetMax;

                transform.localScale = lifePortraitTransform.localScale;

                transform.localRotation = Quaternion.identity;
                transform.sizeDelta = Vector2.zero;
                transform.pivot = new Vector2(0.5f, 0);

                transform.SetSiblingIndex(lifePortraitTransform.GetSiblingIndex() + 1);

                if (overlay.CreateBackgroundOverlay(gameObject) is not GameObject bg)
                {
                    throw new Exception("Failed to create overlay background");
                }

                overlay.Background = bg;
                //overlay.Background.transform.SetSiblingIndex(lifePortraitTransform.GetSiblingIndex() + 1);

                if (overlay.CreateForegroundOverlay(gameObject) is not GameObject fg)
                {
                    throw new Exception("Failed to create overlay foreground");
                }
                
                overlay.Foreground = fg;
                //overlay.Foreground.transform.SetSiblingIndex(lifePortraitTransform.GetSiblingIndex() + 1);

                overlay.Background.transform.SetAsFirstSibling();
                overlay.Foreground.transform.SetAsLastSibling();

                //view.AddDisposable(overlay);

                overlay.SetBGSprite(background);
                overlay.SetFGSprite(foreground);

                gameObject.SetActive(false);

                return (gameObject, overlay);
            }
            catch (Exception e)
            {
                MicroLogger.Error("Failed to initialize portrait overlay", e);
            }

            Destroy(gameObject);
            return null;
        }

        void OnDestroy()
        {
            MicroLogger.Debug(() => $"Destroying portrait overlay");

            if (Background != null) Destroy(Background);
            if (Foreground != null) Destroy(Background);
        }

        void OnEnable()
        {
            MicroLogger.Debug(() => $"Enabling portrait overlay");

            if (Background != null &&
                Background.GetComponent<Image>().sprite != null)
                Background.SetActive(true);

            if (Foreground != null &&
                Foreground.GetComponent<Image>().sprite != null)
                Foreground.SetActive(true);
        }

        void OnDisable()
        {
            MicroLogger.Debug(() => $"Disabling portrait overlay");
            
            if (Background != null) Background.SetActive(false);
            if (Foreground != null) Foreground.SetActive(false);
        }

        private static void InitializeOverlayLayer(GameObject obj, GameObject parent)
        {
            if (obj.transform is not RectTransform rt) return;

            rt.SetParent(parent.transform, false);

            if (rt.parent is RectTransform parentTransform)
            {
                rt.anchorMin = parentTransform.anchorMin;
                rt.anchorMax = parentTransform.anchorMax;

                rt.offsetMin = parentTransform.offsetMin;
                rt.offsetMax = parentTransform.offsetMax;

                rt.localScale = parentTransform.localScale;
            }

            rt.localRotation = Quaternion.identity;
            rt.sizeDelta = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0);
        }

        public GameObject? Background { get; private set; }
        public void SetBGSprite(Sprite? sprite)
        {
            if (Background == null) return;

            if (sprite == null) Background.SetActive(false);

            var image = Background.GetComponent<Image>();
            var oldSprite = image.sprite;
            image.sprite = sprite;

            if (oldSprite != null) Destroy(oldSprite);
        }
        private GameObject? CreateBackgroundOverlay(GameObject parent)
        {
            var bgOverlay = Instantiate(Prototypes.Background);

            //var image = bgOverlay.GetComponent<Image>();

            //if (bgSprite != null) image.sprite = bgSprite;

            if (bgOverlay.transform is not RectTransform bgTransform)
            {
                Destroy(bgOverlay);
                return null;
            }

            //transform.SetParent(portraitView.transform);

            //if (transform.parent.Find("LifePortrait") is RectTransform lifePortraitTransform)
            //{
            //    transform.anchorMin = lifePortraitTransform.anchorMin;
            //    transform.anchorMax = lifePortraitTransform.anchorMax;

            //    transform.offsetMin = lifePortraitTransform.offsetMin;
            //    transform.offsetMax = lifePortraitTransform.offsetMax;

            //    transform.localScale = lifePortraitTransform.localScale;

            //    transform.SetSiblingIndex(lifePortraitTransform.GetSiblingIndex() + 1);
            //}

            //transform.localRotation = Quaternion.identity;
            //transform.sizeDelta = Vector2.zero;

            //Background = bgOverlay;

            InitializeOverlayLayer(bgOverlay, parent);

            return bgOverlay;
        }

        public GameObject? Foreground { get; private set; }
        public void SetFGSprite(Sprite? sprite)
        {
            if (Foreground == null) return;

            if (sprite == null) Foreground.SetActive(false);

            var image = Foreground.GetComponent<Image>();
            var oldSprite = image.sprite;
            image.sprite = sprite;

            if (oldSprite != null) Destroy(oldSprite);
        }
        private GameObject? CreateForegroundOverlay(GameObject parent)
        {
            var fgOverlay = Instantiate(Prototypes.Foreground);

            //var image = fgOverlay.GetComponent<Image>();

            //if (fgSprite != null) image.sprite = fgSprite;

            //image.preserveAspect = false;

            if (fgOverlay.transform is not RectTransform fgTransform)
            {
                Destroy(fgOverlay);
                return null;
            }

            //fgTransform.SetParent(parent.transform, false);

            //if (fgTransform.parent is RectTransform parentTransform)
            //{
            //    fgTransform.anchorMin = parentTransform.anchorMin;
            //    fgTransform.anchorMax = parentTransform.anchorMax;

            //    fgTransform.offsetMin = parentTransform.offsetMin;
            //    fgTransform.offsetMax = parentTransform.offsetMax;

            //    fgTransform.localScale = parentTransform.localScale;
            //}

            //fgTransform.localRotation = Quaternion.identity;
            //fgTransform.sizeDelta = Vector2.zero;
            //fgTransform.pivot = new Vector2(0.5f, 0);

            //// Aspect ratio correction
            //var yScale = transform.localScale.y;
            //transform.localScale = new Vector3((float)(yScale / 1.2), yScale);

            InitializeOverlayLayer(fgOverlay, parent);

            return fgOverlay;
        }

        //private bool SetupPortraitOverlay(ViewBase<PartyCharacterVM>? view = null, Sprite? background = null, Sprite? foreground = null)
        //{
        //    if (view == null) view = this.GetComponentInParent<ViewBase<PartyCharacterVM>>();

        //    if (view.GetComponentInChildren<PortraitOverlay>(true) != null)
        //    {
        //        SetBGSprite(background);
        //        SetFGSprite(foreground);
        //        return true;
        //    }

        //    try
        //    {
        //        UnitPortraitPartView portrait;
                
        //        if (view is PartyCharacterPCView pcView)
        //        {
        //            portrait = pcView.m_PortraitView;
        //        }
        //        else if (view is PartyCardCharacterConsoleView consoleView)
        //        {
        //            portrait = consoleView.m_PortraitView;
        //        }
        //        else
        //        {
        //            throw new ArgumentException(
        //                $"{nameof(ViewBase<PartyCharacterVM>)} parameter is neither " +
        //                $"{nameof(PartyCharacterPCView)} nor {nameof(PartyCharacterConsoleView)}",
        //                nameof(view));
        //        }

        //        view.AddDisposable(this);
        //        this.transform.parent = view.transform;

        //        var sprite = foreground;
        //        var bgSprite = background;

        //        if (CreateBackgroundOverlay(portrait, bgSprite) is GameObject bg)
        //        {
        //            if (CreateForegroundOverlay(bg, sprite) is GameObject fg)
        //            {
        //                Background = bg;
        //                Foreground = fg;

        //                return true;
        //            }
                    
        //            Destroy(bg);
        //        }
                
        //        return false;
        //    }
        //    catch (ArgumentException ae)
        //    {
        //        MicroLogger.Error("Invalid view", ae);
        //        return false;
        //    }
        //}
    }
}
