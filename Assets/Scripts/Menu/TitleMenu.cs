// Title menu and in-battle pause menu. Builds its own UI at runtime, so nothing needs
// to be added to the scene. Works with both the new Input System and the old Input Manager.

using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MysticKombat
{
    public class TitleMenu : MonoBehaviour
    {
        static TitleMenu instance;

        // True while the in-battle pause menu is open (BattleScene freezes itself while this is set).
        public static bool IsPaused { get { return instance != null && instance.pauseOpen; } }

        static readonly string[] Labels = { "1 PLAYER", "2 PLAYERS", "CONTROLS", "QUIT" };
        static readonly string[] PauseLabels = { "OPTIONS", "EXIT" };

        // Top-middle pause button vertical offset from top edge (in line with health bars)
        const float PauseButtonY = -55f;

        class Item
        {
            public RectTransform rt;
            public Image bg;
            public Outline border;
            public Text label, arrowL, arrowR;
            public CanvasGroup cg;
            public float t, punch, flash, baseY;
        }

        static readonly Color Gold = new Color(1f, 0.82f, 0.30f, 1f);
        static readonly Color GoldLight = new Color(1f, 0.93f, 0.55f, 1f);
        static readonly Color GoldText = new Color(1f, 0.89f, 0.48f, 1f);
        static readonly Color BorderIdle = new Color(0.91f, 0.78f, 0.42f, 1f);
        static readonly Color Cyan = new Color(0.13f, 0.88f, 1f, 1f);
        static readonly Color Dark = new Color(0.05f, 0.02f, 0.10f, 1f);

        Font font;
        RectTransform root, titleRt, bgRt, controlsPanel;
        RectTransform hudRoot, pausePanel;
        RawImage bgImage;
        Outline titleGlow;
        Image fade;
        Text hint, toast;
        AudioSource sfx;
        AudioClip sndMove, sndConfirm;

        readonly Item[] items = new Item[Labels.Length];
        readonly Item[] pauseItems = new Item[PauseLabels.Length];
        Item pauseTopBtn;

        int selected, pauseSelected;
        float clock, toastT, glitchT, lastScreenAspect;
        bool busy, controlsOpen, inBattle, pauseOpen;
        Vector2 lastMouse;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (instance != null) return;

            if (Camera.allCamerasCount == 0)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                camGo.AddComponent<AudioListener>();
            }

            new GameObject("MysticKombat_TitleMenu").AddComponent<TitleMenu>();
        }

        void Awake()
        {
            instance = this;
            font = MakeFont();
            sndMove = Resources.Load<AudioClip>("sfx_move");
            sndConfirm = Resources.Load<AudioClip>("sfx_confirm");
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0f;
            BuildUI();
            BuildPauseUI();
            lastMouse = MenuInput.MousePos();
        }

        static Font MakeFont()
        {
            Font f = null;
            try { f = Font.CreateDynamicFontFromOSFont(new[] { "Impact", "Arial Black", "Arial", "Helvetica" }, 64); } catch { }
            if (f == null) { try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { } }
            if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
            return f;
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        Text NewText(string name, Transform parent, string s, int size, Color c, TextAnchor a)
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = s;
            t.fontSize = size;
            t.color = c;
            t.alignment = a;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        Item CreateStyledButton(string labelText, Transform parent, Vector2 size, float baseY, int fontSize = 46, bool showArrows = true)
        {
            var it = new Item();
            it.baseY = baseY;
            it.rt = NewRect("Btn_" + labelText, parent);
            it.rt.sizeDelta = size;
            it.rt.anchoredPosition = new Vector2(0, baseY);
            it.bg = it.rt.gameObject.AddComponent<Image>();
            it.bg.color = new Color(0, 0, 0, 0.65f);
            it.bg.raycastTarget = false;
            it.border = it.rt.gameObject.AddComponent<Outline>();
            it.border.effectColor = BorderIdle;
            it.border.effectDistance = new Vector2(3, -3);
            it.border.useGraphicAlpha = false;
            it.cg = it.rt.gameObject.AddComponent<CanvasGroup>();
            it.cg.alpha = 1f;
            it.cg.blocksRaycasts = false;

            it.label = NewText("Label", it.rt, labelText, fontSize, GoldText, TextAnchor.MiddleCenter);
            it.label.fontStyle = FontStyle.Bold;
            Stretch(it.label.rectTransform);

            if (showArrows)
            {
                float arrowOffset = (size.x * 0.5f) - 45f;
                it.arrowL = NewText("ArrowL", it.rt, ">", fontSize, Dark, TextAnchor.MiddleCenter);
                it.arrowL.supportRichText = false;
                it.arrowL.rectTransform.sizeDelta = new Vector2(60, size.y);
                it.arrowL.rectTransform.anchoredPosition = new Vector2(-arrowOffset, 0);

                it.arrowR = NewText("ArrowR", it.rt, "<", fontSize, Dark, TextAnchor.MiddleCenter);
                it.arrowR.supportRichText = false;
                it.arrowR.rectTransform.sizeDelta = new Vector2(60, size.y);
                it.arrowR.rectTransform.anchoredPosition = new Vector2(arrowOffset, 0);
            }

            return it;
        }

        static Sprite MakeVignette()
        {
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x / (n - 1f) - 0.5f) * 2f;
                    float dy = (y / (n - 1f) - 0.5f) * 2f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float s = Mathf.Clamp01((d - 0.45f) / 0.9f);
                    s = s * s * (3f - 2f * s);
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, s * 0.9f));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            x = Mathf.Clamp01(x) - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        static float EaseOutCubic(float x)
        {
            x = 1f - Mathf.Clamp01(x);
            return 1f - x * x * x;
        }

        void BuildUI()
        {
            var canvasGo = new GameObject("MenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            root = canvasGo.GetComponent<RectTransform>();

            // background picture (cover-fit, slowly drifting)
            bgRt = NewRect("Background", root);
            Stretch(bgRt);
            bgImage = bgRt.gameObject.AddComponent<RawImage>();
            bgImage.texture = Resources.Load<Texture2D>("menu_bg");
            bgImage.color = bgImage.texture != null ? new Color(0.8f, 0.8f, 0.9f, 1f) : new Color(0.08f, 0.04f, 0.15f, 1f);
            bgImage.raycastTarget = false;
            UpdateBackgroundCover();

            // darkening + vignette
            var dim = NewRect("Dim", root);
            Stretch(dim);
            var dimImg = dim.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0.04f, 0f, 0.1f, 0.40f);
            dimImg.raycastTarget = false;

            var vig = NewRect("Vignette", root);
            Stretch(vig);
            var vigImg = vig.gameObject.AddComponent<Image>();
            vigImg.sprite = MakeVignette();
            vigImg.raycastTarget = false;

            var title = NewText("Title", root,
                "<color=#ff3fd0>MYSTIC</color>\n<color=#ffe27a>KOMBAT</color>",
                140, Color.white, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;
            title.lineSpacing = 0.85f;
            titleRt = title.rectTransform;
            titleRt.sizeDelta = new Vector2(1400, 320);
            titleRt.anchoredPosition = new Vector2(0, 285);
            titleGlow = title.gameObject.AddComponent<Outline>();
            titleGlow.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.8f);
            titleGlow.effectDistance = new Vector2(4, -4);
            titleGlow.useGraphicAlpha = false;
            var titleShadow = title.gameObject.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0, 0, 0, 0.9f);
            titleShadow.effectDistance = new Vector2(10, -10);

            for (int i = 0; i < Labels.Length; i++)
            {
                items[i] = CreateStyledButton(Labels[i], root, new Vector2(620, 80), 20f - i * 100f, 46, true);
                items[i].cg.alpha = 0f;
            }

            hint = NewText("Hint", root, "W / S  or  UP / DOWN  to move     ENTER  to select     (mouse works too)",
                28, new Color(0.8f, 0.9f, 1f, 0.85f), TextAnchor.MiddleCenter);
            hint.supportRichText = false;
            hint.rectTransform.sizeDelta = new Vector2(1600, 50);
            hint.rectTransform.anchoredPosition = new Vector2(0, -470);

            toast = NewText("Toast", root, "", 40, Gold, TextAnchor.MiddleCenter);
            toast.rectTransform.sizeDelta = new Vector2(1600, 60);
            toast.rectTransform.anchoredPosition = new Vector2(0, -400);
            var toastShadow = toast.gameObject.AddComponent<Shadow>();
            toastShadow.effectColor = Color.black;
            toastShadow.effectDistance = new Vector2(3, -3);

            // fade-from-black (last = on top of main menu)
            var fadeRt = NewRect("Fade", root);
            Stretch(fadeRt);
            fade = fadeRt.gameObject.AddComponent<Image>();
            fade.color = Color.black;
            fade.raycastTarget = false;
        }

        void BuildPauseUI()
        {
            var hudGo = new GameObject("PauseCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            hudGo.transform.SetParent(transform, false);
            var canvas = hudGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // Above battle UI and main menu
            var scaler = hudGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            hudRoot = hudGo.GetComponent<RectTransform>();

            pauseTopBtn = CreateStyledButton("PAUSE", hudRoot, new Vector2(210, 58), PauseButtonY, 32, false);
            pauseTopBtn.rt.anchorMin = new Vector2(0.5f, 1f);
            pauseTopBtn.rt.anchorMax = new Vector2(0.5f, 1f);
            pauseTopBtn.rt.pivot = new Vector2(0.5f, 0.5f);
            pauseTopBtn.rt.anchoredPosition = new Vector2(0f, PauseButtonY);

            pausePanel = NewRect("PausePanel", hudRoot);
            Stretch(pausePanel);
            var bg = pausePanel.gameObject.AddComponent<Image>();
            bg.color = new Color(0.02f, 0f, 0.06f, 0.80f);
            bg.raycastTarget = false;

            var pauseTitle = NewText("PauseTitle", pausePanel, "<color=#ff3fd0>PAUSED</color>", 110, Color.white, TextAnchor.MiddleCenter);
            pauseTitle.fontStyle = FontStyle.Bold;
            pauseTitle.rectTransform.sizeDelta = new Vector2(900, 160);
            pauseTitle.rectTransform.anchoredPosition = new Vector2(0, 200);
            var ptGlow = pauseTitle.gameObject.AddComponent<Outline>();
            ptGlow.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.8f);
            ptGlow.effectDistance = new Vector2(4, -4);
            var ptShadow = pauseTitle.gameObject.AddComponent<Shadow>();
            ptShadow.effectColor = new Color(0, 0, 0, 0.9f);
            ptShadow.effectDistance = new Vector2(8, -8);

            for (int i = 0; i < PauseLabels.Length; i++)
            {
                pauseItems[i] = CreateStyledButton(PauseLabels[i], pausePanel, new Vector2(620, 80), 20f - i * 100f, 46, true);
            }

            // Shared Controls / Options panel (placed on hudRoot so it works from both Main Menu and Pause Menu)
            controlsPanel = NewRect("ControlsPanel", hudRoot);
            Stretch(controlsPanel);
            var panelImg = controlsPanel.gameObject.AddComponent<Image>();
            panelImg.color = new Color(0.02f, 0f, 0.06f, 0.92f);
            var ctl = NewText("ControlsText", controlsPanel,
                "<size=90><color=#ff3fd0>OPTIONS & CONTROLS</color></size>\n\n" +
                "<color=#ffe27a>PLAYER 1</color>\n" +
                "A / D  move      W  jump\n" +
                "F  punch      G  kick      T  special\n" +
                "R  flying kick      H  block\n\n" +
                "<color=#ffe27a>PLAYER 2</color>\n" +
                "LEFT / RIGHT  move      UP  jump\n" +
                ",  punch      .  kick      ;  special\n" +
                "RIGHT SHIFT  flying kick      /  block\n\n" +
                "<size=30><color=#9fe8ff>Press ENTER, ESC, or CLICK to go back</color></size>",
                44, Color.white, TextAnchor.MiddleCenter);
            ctl.rectTransform.sizeDelta = new Vector2(1600, 900);

            controlsPanel.gameObject.SetActive(false);
            pausePanel.gameObject.SetActive(false);
            pauseTopBtn.rt.gameObject.SetActive(false);
        }

        void UpdateBackgroundCover()
        {
            if (bgImage == null || bgImage.texture == null) return;
            float screenAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            lastScreenAspect = screenAspect;
            float texAspect = (float)bgImage.texture.width / bgImage.texture.height;
            Rect uv;
            if (screenAspect > texAspect)
            {
                float h = texAspect / screenAspect;
                uv = new Rect(0f, (1f - h) * 0.5f, 1f, h);
            }
            else
            {
                float w = screenAspect / texAspect;
                uv = new Rect((1f - w) * 0.5f, 0f, w, 1f);
            }
            bgImage.uvRect = uv;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            clock += dt;

            if (inBattle)
            {
                UpdateBattlePause(dt);
                return;
            }

            if (!Mathf.Approximately(lastScreenAspect, (float)Screen.width / Mathf.Max(1, Screen.height)))
                UpdateBackgroundCover();

            AnimateBackground();
            AnimateTitle();
            AnimateButtonList(items, selected, !controlsOpen, dt, true);
            HandleMainMenuInput();

            fade.color = new Color(0, 0, 0, 1f - Mathf.Clamp01(clock / 1.0f));

            if (toastT > 0f)
            {
                toastT -= dt;
                var c = toast.color;
                c.a = Mathf.Clamp01(toastT / 0.5f);
                toast.color = c;
            }

            var hc = hint.color;
            hc.a = Mathf.Clamp01((clock - 1.3f) * 2f) * (0.55f + 0.35f * Mathf.Sin(clock * 3f));
            hint.color = hc;
        }

        void AnimateBackground()
        {
            float zoom = 1.08f + 0.02f * Mathf.Sin(clock * 0.3f);
            bgRt.localScale = new Vector3(zoom, zoom, 1f);
            bgRt.anchoredPosition = new Vector2(Mathf.Sin(clock * 0.2f) * 35f, Mathf.Cos(clock * 0.15f) * 15f);
        }

        void AnimateTitle()
        {
            float s = Mathf.Clamp01(clock / 0.8f);
            float scale = Mathf.LerpUnclamped(1.8f, 1f, EaseOutBack(s));
            scale *= 1f + 0.012f * Mathf.Sin(clock * 2f);
            titleRt.localScale = new Vector3(scale, scale, 1f);

            float glow = 0.55f + 0.45f * Mathf.Sin(clock * 2.5f);
            titleGlow.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, glow);

            if (glitchT <= 0f && Random.value < 0.004f) glitchT = 0.12f;
            float gx = 0f;
            if (glitchT > 0f)
            {
                glitchT -= Time.unscaledDeltaTime;
                gx = Random.Range(-10f, 10f);
            }
            titleRt.anchoredPosition = new Vector2(gx, 285f);
        }

        void AnimateButtonList(Item[] list, int selIndex, bool activeState, float dt, bool slideIntro)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(clock * 6f);

            for (int i = 0; i < list.Length; i++)
            {
                var it = list[i];

                float xOff = 0f;
                if (slideIntro)
                {
                    float s = Mathf.Clamp01((clock - (0.7f + 0.12f * i)) / 0.45f);
                    float e0 = EaseOutCubic(s);
                    it.cg.alpha = e0;
                    xOff = (1f - e0) * 700f;
                }
                else
                {
                    it.cg.alpha = 1f;
                }

                float target = (i == selIndex && activeState) ? 1f : 0f;
                it.t = Mathf.MoveTowards(it.t, target, dt * 7f);
                float e = it.t * it.t * (3f - 2f * it.t);
                it.punch = Mathf.MoveTowards(it.punch, 0f, dt * 4f);

                Color bgC = Color.Lerp(new Color(0, 0, 0, 0.65f), Color.Lerp(Gold, GoldLight, pulse * 0.5f), e);
                if (it.flash > 0f)
                {
                    it.flash -= dt;
                    if (((int)(it.flash * 30f)) % 2 == 0) bgC = Color.white;
                }
                it.bg.color = bgC;
                it.label.color = Color.Lerp(GoldText, Dark, e);
                it.border.effectColor = Color.Lerp(BorderIdle, Cyan, e);
                it.border.effectDistance = Vector2.Lerp(new Vector2(3, -3), new Vector2(5, -5), e);

                // Fixed scale and aligned X position (no magnification on hover/selection)
                it.rt.localScale = Vector3.one;
                it.rt.anchoredPosition = new Vector2(xOff, it.baseY);

                if (it.arrowL != null && it.arrowR != null)
                {
                    float wob = Mathf.Sin(clock * 10f);
                    float a = e * (0.6f + 0.4f * wob);
                    var ac = Dark; ac.a = a;
                    it.arrowL.color = ac;
                    it.arrowR.color = ac;
                    it.arrowL.rectTransform.anchoredPosition = new Vector2(-265f + 6f * wob, 0f);
                    it.arrowR.rectTransform.anchoredPosition = new Vector2(265f - 6f * wob, 0f);
                }
            }
        }

        void AnimateTopPauseButton(float dt)
        {
            Vector2 mp = MenuInput.MousePos();
            bool hovered = RectTransformUtility.RectangleContainsScreenPoint(pauseTopBtn.rt, mp, null);
            float target = (hovered || pauseOpen) ? 1f : 0f;
            pauseTopBtn.t = Mathf.MoveTowards(pauseTopBtn.t, target, dt * 8f);
            float e = pauseTopBtn.t * pauseTopBtn.t * (3f - 2f * pauseTopBtn.t);

            float pulse = 0.5f + 0.5f * Mathf.Sin(clock * 6f);
            pauseTopBtn.bg.color = Color.Lerp(new Color(0, 0, 0, 0.65f), Color.Lerp(Gold, GoldLight, pulse * 0.5f), e);
            pauseTopBtn.label.color = Color.Lerp(GoldText, Dark, e);
            pauseTopBtn.border.effectColor = Color.Lerp(BorderIdle, Cyan, e);
            pauseTopBtn.border.effectDistance = Vector2.Lerp(new Vector2(3, -3), new Vector2(4, -4), e);
            pauseTopBtn.rt.localScale = Vector3.one;
        }

        void HandleMainMenuInput()
        {
            if (busy) return;

            if (controlsOpen)
            {
                if (MenuInput.Confirm() || MenuInput.Back() || MenuInput.Click()) CloseControls();
                return;
            }

            if (MenuInput.Up()) Select(items, ref selected, (selected - 1 + items.Length) % items.Length);
            if (MenuInput.Down()) Select(items, ref selected, (selected + 1) % items.Length);

            if (MenuInput.Confirm()) ActivateMainMenu(selected);

            Vector2 mp = MenuInput.MousePos();
            if ((mp - lastMouse).sqrMagnitude > 0.5f)
            {
                lastMouse = mp;
                for (int i = 0; i < items.Length; i++)
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(items[i].rt, mp, null))
                    {
                        if (selected != i) Select(items, ref selected, i);
                        break;
                    }
                }
            }

            if (MenuInput.Click())
            {
                for (int i = 0; i < items.Length; i++)
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(items[i].rt, mp, null))
                    {
                        if (selected != i) Select(items, ref selected, i);
                        ActivateMainMenu(i);
                        break;
                    }
                }
            }
        }

        void UpdateBattlePause(float dt)
        {
            AnimateTopPauseButton(dt);

            if (controlsOpen)
            {
                if (MenuInput.Confirm() || MenuInput.Back() || MenuInput.Click()) CloseControls();
                return;
            }

            Vector2 mp = MenuInput.MousePos();
            bool topPauseClicked = MenuInput.Click() &&
                                   RectTransformUtility.RectangleContainsScreenPoint(pauseTopBtn.rt, mp, null);

            if (topPauseClicked)
            {
                TogglePause(!pauseOpen);
                return;
            }

            if (!pauseOpen)
            {
                if (MenuInput.Back()) TogglePause(true);
                return;
            }

            AnimateButtonList(pauseItems, pauseSelected, true, dt, false);

            if (MenuInput.Back())
            {
                TogglePause(false);
                return;
            }

            if (MenuInput.Up()) Select(pauseItems, ref pauseSelected, (pauseSelected - 1 + pauseItems.Length) % pauseItems.Length);
            if (MenuInput.Down()) Select(pauseItems, ref pauseSelected, (pauseSelected + 1) % pauseItems.Length);

            if (MenuInput.Confirm())
            {
                ActivatePauseItem(pauseSelected);
                return;
            }

            if ((mp - lastMouse).sqrMagnitude > 0.5f)
            {
                lastMouse = mp;
                for (int i = 0; i < pauseItems.Length; i++)
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(pauseItems[i].rt, mp, null))
                    {
                        if (pauseSelected != i) Select(pauseItems, ref pauseSelected, i);
                        break;
                    }
                }
            }

            if (MenuInput.Click())
            {
                for (int i = 0; i < pauseItems.Length; i++)
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(pauseItems[i].rt, mp, null))
                    {
                        if (pauseSelected != i) Select(pauseItems, ref pauseSelected, i);
                        ActivatePauseItem(i);
                        break;
                    }
                }
            }
        }

        void TogglePause(bool open)
        {
            pauseOpen = open;
            pauseSelected = 0;
            pausePanel.gameObject.SetActive(open);
            Time.timeScale = open ? 0f : 1f;
            Play(open ? sndConfirm : sndMove);
            lastMouse = MenuInput.MousePos();
        }

        void ActivatePauseItem(int index)
        {
            Play(sndConfirm);
            pauseItems[index].flash = 0.25f;

            if (index == 0)
            {
                OpenControls();
            }
            else if (index == 1)
            {
                ExitBattleToMainMenu();
            }
        }

        void ExitBattleToMainMenu()
        {
            Time.timeScale = 1f;
            pauseOpen = false;
            pausePanel.gameObject.SetActive(false);
            pauseTopBtn.rt.gameObject.SetActive(false);

            // Clean up active BattleScene object if present
            var battleObj = FindObjectOfType<BattleScene>();
            if (battleObj != null)
            {
                // Try invoking a cleanup method if BattleScene defines one, then destroy its GameObject
                var m = typeof(BattleScene).GetMethod("Cleanup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                     ?? typeof(BattleScene).GetMethod("End", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m != null && m.GetParameters().Length == 0)
                {
                    try { m.Invoke(battleObj, null); } catch { }
                }
                if (battleObj != null && battleObj.gameObject != null)
                    Destroy(battleObj.gameObject);
            }

            ReturnToMenu();
        }

        void Select(Item[] list, ref int currentSelection, int i)
        {
            currentSelection = i;
            list[i].punch = 1f;
            Play(sndMove);
        }

        void ActivateMainMenu(int i)
        {
            if (busy) return;
            busy = true;
            Play(sndConfirm);
            items[i].flash = 0.35f;
            StartCoroutine(RunAction(i));
        }

        IEnumerator RunAction(int i)
        {
            yield return new WaitForSecondsRealtime(0.35f);
            busy = false;

            switch (i)
            {
                case 0:
                    StartBattle(false);
                    break;
                case 1:
                    StartBattle(true);
                    break;
                case 2:
                    OpenControls();
                    break;
                case 3:
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;
            }
        }

        // Hides the menu and enters the battle screen (see BattleScene.cs).
        void StartBattle(bool twoPlayers)
        {
            root.gameObject.SetActive(false);
            inBattle = true;
            pauseOpen = false;
            pausePanel.gameObject.SetActive(false);
            pauseTopBtn.rt.gameObject.SetActive(true);
            Time.timeScale = 1f;
            BattleScene.Begin(font, twoPlayers, ReturnToMenu);
        }

        // Called when exiting battle back to the main menu.
        void ReturnToMenu()
        {
            Time.timeScale = 1f;
            inBattle = false;
            pauseOpen = false;
            controlsOpen = false;
            controlsPanel.gameObject.SetActive(false);
            pausePanel.gameObject.SetActive(false);
            pauseTopBtn.rt.gameObject.SetActive(false);

            root.gameObject.SetActive(true);
            clock = 0f;                                 // replay the menu intro
            busy = false;
            lastMouse = MenuInput.MousePos();
        }

        void OpenControls()
        {
            controlsOpen = true;
            controlsPanel.gameObject.SetActive(true);
        }

        void CloseControls()
        {
            controlsOpen = false;
            controlsPanel.gameObject.SetActive(false);
            Play(sndMove);
        }

        void ShowToast(string msg)
        {
            toast.text = msg;
            toastT = 2.2f;
            var c = toast.color; c.a = 1f; toast.color = c;
        }

        void Play(AudioClip clip)
        {
            if (clip != null && sfx != null) sfx.PlayOneShot(clip, 0.8f);
        }
    }
}
