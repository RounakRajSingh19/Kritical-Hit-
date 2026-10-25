// MYSTIC KOMBAT - Title menu for Unity.
// ZERO SETUP: this script creates the whole menu by itself when you press Play,
// in any scene. No GameObjects to add, nothing to drag in the Inspector.
//
// Works with both the new Input System and the old Input Manager.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MysticKombat
{
    // ---------------------------------------------------------------- input
    static class MenuInput
    {
        public static bool Up()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
#else
            return false;
#endif
        }

        public static bool Down()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
#else
            return false;
#endif
        }

        public static bool Confirm()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
#else
            return false;
#endif
        }

        public static bool Back()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && k.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }

        public static Vector2 MousePos()
        {
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            return m != null ? m.position.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#else
            return Vector2.zero;
#endif
        }

        public static bool Click()
        {
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            return m != null && m.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }
    }

    // ----------------------------------------------------------------- menu
    public class TitleMenu : MonoBehaviour
    {
        static TitleMenu instance;

        // Edit these to rename / reorder options.
        static readonly string[] Labels = { "1 PLAYER", "2 PLAYERS", "CONTROLS", "QUIT" };

        class Item
        {
            public RectTransform rt;
            public Image bg;
            public Outline border;
            public Text label, arrowL, arrowR;
            public CanvasGroup cg;
            public float t, punch, flash, baseY;
        }

        // colours
        static readonly Color Gold = new Color(1f, 0.82f, 0.30f, 1f);
        static readonly Color GoldLight = new Color(1f, 0.93f, 0.55f, 1f);
        static readonly Color GoldText = new Color(1f, 0.89f, 0.48f, 1f);
        static readonly Color BorderIdle = new Color(0.91f, 0.78f, 0.42f, 1f);
        static readonly Color Cyan = new Color(0.13f, 0.88f, 1f, 1f);
        static readonly Color Dark = new Color(0.05f, 0.02f, 0.10f, 1f);

        Font font;
        RectTransform root, titleRt, bgRt, controlsPanel;
        RawImage bgImage;
        Outline titleGlow;
        Image fade;
        Text hint, toast;
        AudioSource sfx;
        AudioClip sndMove, sndConfirm;

        readonly Item[] items = new Item[Labels.Length];
        int selected;
        float clock, toastT, glitchT, lastScreenAspect;
        bool busy, controlsOpen;
        Vector2 lastMouse;

        // ------------------------------------------------------------ boot
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
            lastMouse = MenuInput.MousePos();
        }

        // -------------------------------------------------------- helpers
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

        // ------------------------------------------------------------- UI
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

            // title
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

            // menu buttons
            for (int i = 0; i < Labels.Length; i++)
            {
                var it = new Item();
                it.baseY = 20f - i * 100f;
                it.rt = NewRect("Item_" + Labels[i], root);
                it.rt.sizeDelta = new Vector2(620, 80);
                it.rt.anchoredPosition = new Vector2(0, it.baseY);
                it.bg = it.rt.gameObject.AddComponent<Image>();
                it.bg.color = new Color(0, 0, 0, 0.65f);
                it.bg.raycastTarget = false;
                it.border = it.rt.gameObject.AddComponent<Outline>();
                it.border.effectColor = BorderIdle;
                it.border.effectDistance = new Vector2(3, -3);
                it.border.useGraphicAlpha = false;
                it.cg = it.rt.gameObject.AddComponent<CanvasGroup>();
                it.cg.alpha = 0f;
                it.cg.blocksRaycasts = false;

                it.label = NewText("Label", it.rt, Labels[i], 46, GoldText, TextAnchor.MiddleCenter);
                it.label.fontStyle = FontStyle.Bold;
                Stretch(it.label.rectTransform);

                it.arrowL = NewText("ArrowL", it.rt, ">", 46, Dark, TextAnchor.MiddleCenter);
                it.arrowL.supportRichText = false;
                it.arrowL.rectTransform.sizeDelta = new Vector2(60, 80);
                it.arrowL.rectTransform.anchoredPosition = new Vector2(-265, 0);

                it.arrowR = NewText("ArrowR", it.rt, "<", 46, Dark, TextAnchor.MiddleCenter);
                it.arrowR.supportRichText = false;
                it.arrowR.rectTransform.sizeDelta = new Vector2(60, 80);
                it.arrowR.rectTransform.anchoredPosition = new Vector2(265, 0);

                items[i] = it;
            }

            // bottom hint
            hint = NewText("Hint", root, "W / S  or  UP / DOWN  to move     ENTER  to select     (mouse works too)",
                28, new Color(0.8f, 0.9f, 1f, 0.85f), TextAnchor.MiddleCenter);
            hint.supportRichText = false;
            hint.rectTransform.sizeDelta = new Vector2(1600, 50);
            hint.rectTransform.anchoredPosition = new Vector2(0, -470);

            // toast message
            toast = NewText("Toast", root, "", 40, Gold, TextAnchor.MiddleCenter);
            toast.rectTransform.sizeDelta = new Vector2(1600, 60);
            toast.rectTransform.anchoredPosition = new Vector2(0, -400);
            var toastShadow = toast.gameObject.AddComponent<Shadow>();
            toastShadow.effectColor = Color.black;
            toastShadow.effectDistance = new Vector2(3, -3);

            // controls panel
            controlsPanel = NewRect("ControlsPanel", root);
            Stretch(controlsPanel);
            var panelImg = controlsPanel.gameObject.AddComponent<Image>();
            panelImg.color = new Color(0.02f, 0f, 0.06f, 0.88f);
            var ctl = NewText("ControlsText", controlsPanel,
                "<size=90><color=#ff3fd0>CONTROLS</color></size>\n\n" +
                "<color=#ffe27a>PLAYER 1</color>\n" +
                "A / D  move      W  jump\n" +
                "F  punch      G  kick\n" +
                "R  flying kick      H  block\n\n" +
                "<color=#ffe27a>PLAYER 2</color>\n" +
                "LEFT / RIGHT  move      UP  jump\n" +
                ",  punch      .  kick\n" +
                "RIGHT SHIFT  flying kick      /  block\n\n" +
                "<size=30><color=#9fe8ff>Press ENTER or ESC to go back</color></size>",
                44, Color.white, TextAnchor.MiddleCenter);
            ctl.rectTransform.sizeDelta = new Vector2(1600, 900);
            controlsPanel.gameObject.SetActive(false);

            // fade-from-black (last = on top)
            var fadeRt = NewRect("Fade", root);
            Stretch(fadeRt);
            fade = fadeRt.gameObject.AddComponent<Image>();
            fade.color = Color.black;
            fade.raycastTarget = false;
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

        // --------------------------------------------------------- update
        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            clock += dt;

            if (!Mathf.Approximately(lastScreenAspect, (float)Screen.width / Mathf.Max(1, Screen.height)))
                UpdateBackgroundCover();

            AnimateBackground();
            AnimateTitle();
            AnimateItems(dt);
            HandleInput();

            // fade in, toast fade, hint blink
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

            // neon glow pulse
            float glow = 0.55f + 0.45f * Mathf.Sin(clock * 2.5f);
            titleGlow.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, glow);

            // occasional glitch jitter
            if (glitchT <= 0f && Random.value < 0.004f) glitchT = 0.12f;
            float gx = 0f;
            if (glitchT > 0f)
            {
                glitchT -= Time.unscaledDeltaTime;
                gx = Random.Range(-10f, 10f);
            }
            titleRt.anchoredPosition = new Vector2(gx, 285f);
        }

        void AnimateItems(float dt)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(clock * 6f);

            for (int i = 0; i < items.Length; i++)
            {
                var it = items[i];

                // slide-in intro
                float s = Mathf.Clamp01((clock - (0.7f + 0.12f * i)) / 0.45f);
                float e0 = EaseOutCubic(s);
                it.cg.alpha = e0;
                float xOff = (1f - e0) * 700f;

                // selection blend
                float target = (i == selected && !controlsOpen) ? 1f : 0f;
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

                float sc = 1f + 0.10f * e + 0.06f * it.punch;
                it.rt.localScale = new Vector3(sc, sc, 1f);
                it.rt.anchoredPosition = new Vector2(xOff + 22f * e, it.baseY);

                // blinking arrows
                float wob = Mathf.Sin(clock * 10f);
                float a = e * (0.6f + 0.4f * wob);
                var ac = Dark; ac.a = a;
                it.arrowL.color = ac;
                it.arrowR.color = ac;
                it.arrowL.rectTransform.anchoredPosition = new Vector2(-265f + 6f * wob, 0f);
                it.arrowR.rectTransform.anchoredPosition = new Vector2(265f - 6f * wob, 0f);
            }
        }

        // ---------------------------------------------------------- input
        void HandleInput()
        {
            if (busy) return;

            if (controlsOpen)
            {
                if (MenuInput.Confirm() || MenuInput.Back() || MenuInput.Click()) CloseControls();
                return;
            }

            if (MenuInput.Up()) Select((selected - 1 + items.Length) % items.Length);
            if (MenuInput.Down()) Select((selected + 1) % items.Length);

            if (MenuInput.Confirm()) Activate(selected);

            // mouse hover (only when the mouse actually moved, so keyboard isn't overridden)
            Vector2 mp = MenuInput.MousePos();
            if ((mp - lastMouse).sqrMagnitude > 0.5f)
            {
                lastMouse = mp;
                for (int i = 0; i < items.Length; i++)
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(items[i].rt, mp, null))
                    {
                        if (selected != i) Select(i);
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
                        if (selected != i) Select(i);
                        Activate(i);
                        break;
                    }
                }
            }
        }

        void Select(int i)
        {
            selected = i;
            items[i].punch = 1f;
            Play(sndMove);
        }

        void Activate(int i)
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
                    ShowToast("1 PLAYER  -  FIGHT!");
                    // Later: UnityEngine.SceneManagement.SceneManager.LoadScene("Fight");
                    break;
                case 1:
                    ShowToast("2 PLAYERS  -  FIGHT!");
                    // Later: UnityEngine.SceneManagement.SceneManager.LoadScene("Fight");
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
