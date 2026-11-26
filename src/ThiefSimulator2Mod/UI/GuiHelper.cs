using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ThiefSimulator2Mod
{
    /// <summary>
    /// Flat, square-cornered IMGUI skin for Thief2Enhanced: dark heist-tech
    /// surfaces, 1px borders, neon cyan accents and bundled Chakra Petch /
    /// IBM Plex Sans fonts. All glow is limited to the title bar, active
    /// toggles and slider handles.
    /// </summary>
    internal static class GuiHelper
    {
        // ------------------------------------------------------------ palette

        internal static readonly Color ColBg = Rgb(0x070B14);
        internal static readonly Color ColCard = Rgb(0x0C1322);
        internal static readonly Color ColCardBorder = Rgb(0x1B2A44);
        internal static readonly Color ColCardActive = Rgb(0x0C2030);
        internal static readonly Color ColCardActiveBorder = Rgb(0x2A8F9C);
        internal static readonly Color ColOuterBorder = Rgb(0x25506B);
        internal static readonly Color ColText = Rgb(0xE8F1FF);
        internal static readonly Color ColDim = Rgb(0x93A8C8);
        internal static readonly Color ColAccent = Rgb(0x3EE6EC);
        internal static readonly Color ColAccentSoft = Rgb(0x7FF2F5);
        internal static readonly Color ColTabBg = Rgb(0x0D2434);
        internal static readonly Color ColTabText = Rgb(0x7FF2F5);
        internal static readonly Color ColHeading = Rgb(0xB7A4FF);
        internal static readonly Color ColGradA = Rgb(0x8B6CFF);
        internal static readonly Color ColGradB = Rgb(0x3EE6EC);
        internal static readonly Color ColTrack = Rgb(0x1D2A42);
        internal static readonly Color ColToggleOn = Rgb(0x0F4A55);
        internal static readonly Color ColKnobOff = Rgb(0x7D90B0);
        internal static readonly Color ColKnobOffBorder = Rgb(0x53688C);

        // ------------------------------------------------------------ styles

        private static Font _fontDisplay;
        private static Font _fontBody;

        private static GUIStyle _window;
        private static GUIStyle _title;
        private static GUIStyle _titleAccent;
        private static GUIStyle _subtitle;
        private static GUIStyle _tabActive;
        private static GUIStyle _tabInactive;
        private static GUIStyle _cardTitle;
        private static GUIStyle _mini;
        private static GUIStyle _reset;
        private static GUIStyle _label;
        private static GUIStyle _muted;
        private static GUIStyle _hint;
        private static GUIStyle _value;
        private static GUIStyle _valueChanged;
        private static GUIStyle _field;
        private static GUIStyle _button;
        private static GUIStyle _buttonSmall;
        private static GUIStyle _footer;
        private static GUIStyle _footerRight;
        private static GUIStyle _footerAccent;
        private static GUIStyle _close;
        private static GUIStyle _hintChip;
        private static GUIStyle _card;
        private static GUIStyle _cardActive;
        private static GUIStyle _scrollbar;
        private static GUIStyle _scrollbarThumb;

        private static Texture2D _windowTex;
        private static Texture2D _cardTex;
        private static Texture2D _cardActiveTex;
        private static Texture2D _btnTex;
        private static Texture2D _btnHoverTex;
        private static Texture2D _btnPressedTex;
        private static Texture2D _btnFocusTex;
        private static Texture2D _fieldTex;
        private static Texture2D _fieldFocusTex;
        private static Texture2D _glowTex;
        private static Texture2D _gradientTex;
        private static Texture2D _scrollbarTex;
        private static Texture2D _scrollbarThumbTex;
        private static Texture2D _scrollbarThumbHoverTex;
        private static Texture2D _scrimTex;
        private static Texture2D _hintChipTex;

        private static bool _ready;

        // ------------------------------------------------------------ helpers

        private static Color Rgb(int value)
        {
            return new Color(
                ((value >> 16) & 0xFF) / 255f,
                ((value >> 8) & 0xFF) / 255f,
                (value & 0xFF) / 255f,
                1f);
        }

        private static Color Alpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static Texture2D Solid(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        /// <summary>8x8 nine-slice with a crisp 1px border and a flat fill.</summary>
        private static Texture2D Framed(Color fill, Color border)
        {
            const int size = 8;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool edge = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                    pixels[y * size + x] = edge ? border : fill;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeGradient(Color from, Color to)
        {
            const int width = 128;
            var tex = new Texture2D(width, 1, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int x = 0; x < width; x++)
                tex.SetPixel(x, 0, Color.Lerp(from, to, x / (float)(width - 1)));
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeGlow(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) / half;
                    float dy = (y + 0.5f - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a * a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        // ------------------------------------------------------------ fonts

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int AddFontResourceEx(string file, uint flags, IntPtr reserved);

        private static void RegisterBundledFonts()
        {
            try
            {
                string dir = Path.Combine(Path.GetTempPath(), "Thief2EnhancedFonts");
                Directory.CreateDirectory(dir);
                ExtractFont("Thief2Enhanced.Fonts.ChakraPetch-Regular.ttf", dir, "ChakraPetch-Regular.ttf");
                ExtractFont("Thief2Enhanced.Fonts.ChakraPetch-SemiBold.ttf", dir, "ChakraPetch-SemiBold.ttf");
                ExtractFont("Thief2Enhanced.Fonts.ChakraPetch-Bold.ttf", dir, "ChakraPetch-Bold.ttf");
                ExtractFont("Thief2Enhanced.Fonts.IBMPlexSans-Regular.ttf", dir, "IBMPlexSans-Regular.ttf");
                ExtractFont("Thief2Enhanced.Fonts.IBMPlexSans-Medium.ttf", dir, "IBMPlexSans-Medium.ttf");
                ExtractFont("Thief2Enhanced.Fonts.IBMPlexSans-SemiBold.ttf", dir, "IBMPlexSans-SemiBold.ttf");
            }
            catch (Exception ex)
            {
                ModLogger.Warning("Font registration failed: " + ex.Message);
            }
        }

        private static void ExtractFont(string resource, string dir, string fileName)
        {
            string path = Path.Combine(dir, fileName);
            if (!File.Exists(path) || new FileInfo(path).Length < 1024)
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
                {
                    if (stream == null) return;
                    using (FileStream file = File.Create(path))
                        stream.CopyTo(file);
                }
            }
            AddFontResourceEx(path, 0x10 /* FR_PRIVATE */, IntPtr.Zero);
        }

        private static Font LoadFont(string[] families)
        {
            Font font = Font.CreateDynamicFontFromOSFont(families, 16);
            return font != null ? font : GUI.skin.font;
        }

        // ------------------------------------------------------------ setup

        internal static void Prepare()
        {
            if (_ready) return;
            _ready = true;

            RegisterBundledFonts();
            _fontDisplay = LoadFont(new[] { "Chakra Petch", "Bahnschrift", "Segoe UI" });
            _fontBody = LoadFont(new[] { "IBM Plex Sans", "Segoe UI", "Tahoma" });
            ModLogger.Info("UI fonts: " + _fontDisplay.name + " / " + _fontBody.name);

            _windowTex = Framed(ColBg, ColOuterBorder);
            _cardTex = Framed(ColCard, ColCardBorder);
            _cardActiveTex = Framed(ColCardActive, ColCardActiveBorder);
            _btnTex = Framed(Rgb(0x0A1220), ColCardBorder);
            _btnHoverTex = Framed(Rgb(0x0F1E30), ColCardActiveBorder);
            _btnPressedTex = Framed(ColCardActive, ColAccent);
            _btnFocusTex = Framed(Rgb(0x0F1E30), ColAccent);
            _fieldTex = Framed(Rgb(0x070F1C), ColCardBorder);
            _fieldFocusTex = Framed(Rgb(0x0A1626), ColAccent);
            _scrollbarTex = Solid(Alpha(Rgb(0x0A1020), 0.55f));
            _scrollbarThumbTex = Solid(ColTrack);
            _scrollbarThumbHoverTex = Solid(ColCardActiveBorder);
            _scrimTex = Solid(new Color(0.008f, 0.014f, 0.028f, 0.62f));
            _hintChipTex = Framed(ColCard, ColCardBorder);
            _gradientTex = MakeGradient(ColGradA, ColGradB);
            _glowTex = MakeGlow(64);

            _title = new GUIStyle(GUI.skin.label)
            {
                font = _fontDisplay,
                fontSize = 21,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Overflow,
                padding = new RectOffset(0, 0, 0, 0),
                normal = { textColor = ColText }
            };
            _titleAccent = new GUIStyle(_title) { normal = { textColor = ColAccent } };
            _subtitle = new GUIStyle(GUI.skin.label)
            {
                font = _fontBody,
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Overflow,
                padding = new RectOffset(0, 0, 0, 0),
                normal = { textColor = ColDim }
            };
            _tabActive = TabStyle(ColTabText);
            _tabInactive = TabStyle(Rgb(0xAFC4E2));
            _cardTitle = new GUIStyle(GUI.skin.label)
            {
                font = _fontDisplay,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Overflow,
                padding = new RectOffset(0, 0, 0, 0),
                contentOffset = new Vector2(0f, -4f),
                normal = { textColor = ColHeading }
            };
            _mini = new GUIStyle(GUI.skin.label)
            {
                font = _fontBody,
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Overflow,
                padding = new RectOffset(0, 0, 0, 0),
                contentOffset = new Vector2(0f, -4f),
                normal = { textColor = ColDim },
                hover = { textColor = ColAccentSoft },
                active = { textColor = ColAccent },
                focused = { textColor = ColAccent }
            };
            _label = new GUIStyle(GUI.skin.label)
            {
                font = _fontBody,
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Overflow,
                padding = new RectOffset(0, 0, 0, 0),
                contentOffset = new Vector2(0f, -5f),
                normal = { textColor = ColText }
            };
            _muted = new GUIStyle(_label)
            {
                fontSize = 12,
                normal = { textColor = ColDim }
            };
            _hint = new GUIStyle(_label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = ColDim }
            };
            _value = new GUIStyle(GUI.skin.label)
            {
                font = _fontDisplay,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
                clipping = TextClipping.Overflow,
                padding = new RectOffset(0, 0, 0, 0),
                contentOffset = new Vector2(0f, 1f),
                normal = { textColor = ColText },
                hover = { textColor = ColAccentSoft },
                active = { textColor = ColAccent },
                focused = { textColor = ColAccent }
            };
            _valueChanged = new GUIStyle(_value) { normal = { textColor = ColAccentSoft } };
            _field = new GUIStyle(GUI.skin.textField)
            {
                font = _fontBody,
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Overflow,
                padding = new RectOffset(8, 8, 4, 4),
                border = new RectOffset(1, 1, 1, 1),
                margin = new RectOffset(0, 0, 0, 0),
                normal = { background = _fieldTex, textColor = ColText },
                hover = { background = _fieldTex, textColor = ColText },
                active = { background = _fieldFocusTex, textColor = ColText },
                focused = { background = _fieldFocusTex, textColor = ColText },
                onNormal = { background = _fieldTex, textColor = ColText },
                onHover = { background = _fieldTex, textColor = ColText },
                onActive = { background = _fieldFocusTex, textColor = ColText },
                onFocused = { background = _fieldFocusTex, textColor = ColText }
            };
            _button = ButtonStyle(_btnTex, _btnHoverTex, _btnPressedTex, _btnFocusTex, 12, 30);
            _buttonSmall = ButtonStyle(_btnTex, _btnHoverTex, _btnPressedTex, _btnFocusTex, 11, 26);
            _reset = new GUIStyle(GUI.skin.button)
            {
                font = _fontBody,
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Overflow,
                padding = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(1, 1, 1, 1),
                margin = new RectOffset(0, 0, 0, 0),
                fixedHeight = 18f,
                contentOffset = new Vector2(0f, -1f),
                normal = { background = _btnTex, textColor = ColDim },
                hover = { background = _btnHoverTex, textColor = ColAccentSoft },
                active = { background = _btnPressedTex, textColor = ColAccentSoft },
                focused = { background = _btnFocusTex, textColor = ColAccentSoft },
                onNormal = { background = _btnTex, textColor = ColDim },
                onHover = { background = _btnHoverTex, textColor = ColAccentSoft },
                onActive = { background = _btnPressedTex, textColor = ColAccentSoft },
                onFocused = { background = _btnFocusTex, textColor = ColAccentSoft }
            };
            _footer = new GUIStyle(GUI.skin.label)
            {
                font = _fontBody,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Overflow,
                padding = new RectOffset(0, 0, 0, 0),
                contentOffset = new Vector2(0f, -4f),
                normal = { textColor = ColDim }
            };
            _footerAccent = new GUIStyle(_footer) { normal = { textColor = ColAccentSoft } };
            _footerRight = new GUIStyle(_footer) { alignment = TextAnchor.MiddleRight };
            _close = new GUIStyle(GUI.skin.button)
            {
                font = _fontDisplay,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                contentOffset = new Vector2(0f, -1f),
                padding = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                normal = { background = null, textColor = ColDim },
                hover = { background = null, textColor = ColAccentSoft },
                active = { background = null, textColor = ColAccent },
                focused = { background = null, textColor = ColAccent }
            };
            _hintChip = new GUIStyle(GUI.skin.label)
            {
                font = _fontBody,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Overflow,
                padding = new RectOffset(14, 12, 5, 5),
                border = new RectOffset(1, 1, 1, 1),
                normal = { background = _hintChipTex, textColor = ColDim }
            };
            _card = CardStyle(_cardTex);
            _cardActive = CardStyle(_cardActiveTex);

            _scrollbar = new GUIStyle(GUI.skin.verticalScrollbar)
            {
                fixedWidth = 6,
                border = new RectOffset(0, 0, 0, 0),
                normal = { background = _scrollbarTex },
                hover = { background = _scrollbarTex },
                active = { background = _scrollbarTex },
                focused = { background = _scrollbarTex }
            };
            _scrollbarThumb = new GUIStyle(GUI.skin.verticalScrollbarThumb)
            {
                fixedWidth = 4,
                border = new RectOffset(0, 0, 0, 0),
                normal = { background = _scrollbarThumbTex },
                hover = { background = _scrollbarThumbHoverTex },
                active = { background = _scrollbarThumbHoverTex },
                focused = { background = _scrollbarThumbTex }
            };

            _window = new GUIStyle(GUI.skin.window)
            {
                border = new RectOffset(1, 1, 1, 1),
                padding = new RectOffset(1, 1, 1, 1),
                margin = new RectOffset(0, 0, 0, 0),
                normal = { background = _windowTex },
                hover = { background = _windowTex },
                active = { background = _windowTex },
                focused = { background = _windowTex },
                onNormal = { background = _windowTex },
                onHover = { background = _windowTex },
                onActive = { background = _windowTex },
                onFocused = { background = _windowTex }
            };
        }

        private static GUIStyle TabStyle(Color text)
        {
            return new GUIStyle(GUI.skin.button)
            {
                font = _fontDisplay,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Overflow,
                padding = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                normal = { background = null, textColor = text },
                hover = { background = null, textColor = ColText },
                active = { background = null, textColor = ColAccentSoft },
                focused = { background = null, textColor = ColAccentSoft },
                onNormal = { background = null, textColor = text },
                onHover = { background = null, textColor = ColText },
                onActive = { background = null, textColor = ColAccentSoft },
                onFocused = { background = null, textColor = ColAccentSoft }
            };
        }

        private static GUIStyle ButtonStyle(Texture2D normal, Texture2D hover, Texture2D pressed,
            Texture2D focus, int fontSize, float height)
        {
            return new GUIStyle(GUI.skin.button)
            {
                font = _fontDisplay,
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Overflow,
                padding = new RectOffset(8, 8, 4, 4),
                border = new RectOffset(1, 1, 1, 1),
                margin = new RectOffset(0, 0, 0, 0),
                fixedHeight = height,
                normal = { background = normal, textColor = ColText },
                hover = { background = hover, textColor = ColAccentSoft },
                active = { background = pressed, textColor = ColAccentSoft },
                focused = { background = focus, textColor = ColAccentSoft },
                onNormal = { background = normal, textColor = ColText },
                onHover = { background = hover, textColor = ColAccentSoft },
                onActive = { background = pressed, textColor = ColAccentSoft },
                onFocused = { background = focus, textColor = ColAccentSoft }
            };
        }

        private static GUIStyle CardStyle(Texture2D background)
        {
            return new GUIStyle(GUI.skin.box)
            {
                border = new RectOffset(1, 1, 1, 1),
                padding = new RectOffset(14, 14, 12, 12),
                margin = new RectOffset(0, 0, 0, 0),
                normal = { background = background }
            };
        }

        // ------------------------------------------------------------ public accessors

        public static GUIStyle WindowStyle { get { Prepare(); return _window; } }
        public static GUIStyle ScrollbarStyle { get { Prepare(); return _scrollbar; } }
        public static GUIStyle ScrollbarThumbStyle { get { Prepare(); return _scrollbarThumb; } }

        // ------------------------------------------------------------ primitives

        public static void Fill(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        public static void DrawBar(float x, float y, float width, float height, Color color)
        {
            Fill(new Rect(x, y, width, height), color);
        }

        public static void Outline(Rect rect, Color color)
        {
            DrawBar(rect.x, rect.y, rect.width, 1f, color);
            DrawBar(rect.x, rect.yMax - 1f, rect.width, 1f, color);
            DrawBar(rect.x, rect.y + 1f, 1f, rect.height - 2f, color);
            DrawBar(rect.xMax - 1f, rect.y + 1f, 1f, rect.height - 2f, color);
        }

        private static void Glow(Rect rect, float alpha)
        {
            Color old = GUI.color;
            GUI.color = Alpha(ColAccent, alpha);
            GUI.DrawTexture(rect, _glowTex);
            GUI.color = old;
        }

        public static void DrawScrim()
        {
            Prepare();
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _scrimTex);
        }

        public static void Divider()
        {
            Prepare();
            Rect rect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true), GUILayout.Height(1f));
            Fill(rect, ColCardBorder);
        }

        // ------------------------------------------------------------ chrome

        /// <summary>Draws the header and returns true when the close button was pressed.</summary>
        public static bool Header(string brand, string accent, string subtitle)
        {
            Prepare();
            Rect rect = GUILayoutUtility.GetRect(10f, 64f, GUILayout.ExpandWidth(true), GUILayout.Height(64f));
            Fill(rect, ColBg);
            DrawBar(rect.x, rect.yMax - 1f, rect.width, 1f, ColCardBorder);

            Glow(new Rect(rect.x + 20f - 12f, rect.y + 18f - 10f, 28f, 38f), 0.55f);
            Fill(new Rect(rect.x + 20f, rect.y + 18f, 3f, 28f), ColAccent);

            float brandWidth = _title.CalcSize(new GUIContent(brand)).x;
            GUI.Label(new Rect(rect.x + 33f, rect.y + 10f, brandWidth + 4f, 30f), brand, _title);
            GUI.Label(new Rect(rect.x + 33f + brandWidth + 6f, rect.y + 10f, rect.width - 120f, 30f),
                accent, _titleAccent);
            GUI.Label(new Rect(rect.x + 34f, rect.y + 38f, rect.width - 120f, 16f), subtitle, _subtitle);

            Rect close = new Rect(rect.xMax - 46f, rect.y + 19f, 26f, 26f);
            bool hover = close.Contains(Event.current.mousePosition);
            if (Event.current.type == EventType.Repaint)
                Outline(close, hover ? ColAccent : ColCardBorder);
            return GUI.Button(close, "X", _close);
        }

        public static bool TabButton(string text, bool active)
        {
            Prepare();
            Rect rect = GUILayoutUtility.GetRect(10f, 44f,
                GUILayout.ExpandWidth(true), GUILayout.Height(44f));
            bool hover = rect.Contains(Event.current.mousePosition);

            if (Event.current.type == EventType.Repaint)
            {
                if (active)
                {
                    Fill(rect, ColTabBg);
                    DrawBar(rect.x, rect.yMax - 3f, rect.width, 3f, ColAccent);
                }
                else if (hover)
                {
                    Fill(rect, Alpha(ColTabBg, 0.55f));
                    DrawBar(rect.x, rect.yMax - 1f, rect.width, 1f, Alpha(ColAccent, 0.35f));
                }
            }

            return GUI.Button(rect, text, active ? _tabActive : _tabInactive);
        }

        public static void Footer(string left, string right, bool highlight)
        {
            Prepare();
            Rect rect = GUILayoutUtility.GetRect(10f, 30f,
                GUILayout.ExpandWidth(true), GUILayout.Height(30f));
            DrawBar(rect.x, rect.y, rect.width, 1f, ColCardBorder);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 6f, rect.width * 0.6f, 18f), left,
                highlight ? _footerAccent : _footer);
            // Leave room for the resize grip at the very corner.
            GUI.Label(new Rect(rect.xMax - 334f, rect.y + 6f, 300f, 18f), right, _footerRight);
        }

        public static void DrawResizeGrip(Rect rect, bool active)
        {
            Prepare();
            Color color = active ? ColAccent : Alpha(ColDim, 0.4f);
            for (int i = 0; i < 3; i++)
            {
                float offset = i * 4f;
                DrawBar(rect.xMax - 2f - offset, rect.yMax - 2f - offset, 2f, 2f, color);
            }
        }

        public static void HintChip(string text)
        {
            Prepare();
            Rect rect = new Rect(12f, 12f, 300f, 26f);
            GUI.Label(rect, text, _hintChip);
            Fill(new Rect(rect.x + 1f, rect.y + 1f, 2f, rect.height - 2f), ColAccent);
        }

        // ------------------------------------------------------------ cards

        public static void BeginCard(bool changed)
        {
            Prepare();
            GUILayout.BeginVertical(changed ? _cardActive : _card);
        }

        public static void EndCard()
        {
            GUILayout.EndVertical();
            GUILayout.Space(14f);
        }

        /// <summary>Card heading + divider + optional reset button. Returns true on reset.</summary>
        public static bool CardHeader(string title, bool changed, bool resettable)
        {
            Prepare();
            bool reset = false;
            Rect row = GUILayoutUtility.GetRect(10f, 20f,
                GUILayout.ExpandWidth(true), GUILayout.Height(20f));

            float reserved = 0f;
            if (resettable)
            {
                reserved = 54f;
                Rect button = new Rect(row.xMax - reserved, row.y + 1f, reserved, 18f);
                if (GUI.Button(button, "RESET", _reset)) reset = true;
            }

            SpacedLabel(new Rect(row.x, row.y, row.width - reserved - 6f, 20f),
                title.ToUpperInvariant(), _cardTitle, 1.4f);
            DrawBar(row.x, row.yMax + 4f, row.width, 1f,
                changed ? Alpha(ColCardActiveBorder, 0.85f) : ColCardBorder);
            GUILayout.Space(9f);
            return reset;
        }

        private static void SpacedLabel(Rect rect, string text, GUIStyle style, float spacing)
        {
            float x = rect.x;
            for (int i = 0; i < text.Length; i++)
            {
                string glyph = text[i].ToString();
                float width = style.CalcSize(new GUIContent(glyph)).x;
                GUI.Label(new Rect(x, rect.y, width + 1f, rect.height), glyph, style);
                x += width + spacing;
            }
        }

        // ------------------------------------------------------------ controls

        public static bool Toggle(string name, bool value)
        {
            Prepare();
            GUILayout.BeginHorizontal(GUILayout.Height(30f));
            GUILayout.Label(name, _label, GUILayout.Height(30f), GUILayout.ExpandWidth(true));

            Rect cell = GUILayoutUtility.GetRect(48f, 30f,
                GUILayout.Width(48f), GUILayout.Height(30f));
            Rect box = new Rect(cell.x, cell.y + 4f, 48f, 22f);

            Event e = Event.current;
            int id = GUIUtility.GetControlID(FocusType.Passive);
            bool hover = box.Contains(e.mousePosition);
            bool clicked = false;

            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (e.button == 0 && box.Contains(e.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        GUIUtility.keyboardControl = id;
                        clicked = true;
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;
                case EventType.KeyDown:
                    if (GUIUtility.keyboardControl == id &&
                        (e.keyCode == KeyCode.Space || e.keyCode == KeyCode.Return ||
                         e.keyCode == KeyCode.KeypadEnter))
                    {
                        clicked = true;
                        e.Use();
                    }
                    break;
            }

            if (e.type == EventType.Repaint)
            {
                if (value) Glow(new Rect(box.x - 9f, box.y - 9f, box.width + 18f, box.height + 18f),
                    hover ? 0.5f : 0.34f);
                Fill(box, value ? ColToggleOn : ColBg);

                Color border = value
                    ? (hover ? ColAccentSoft : ColAccent)
                    : (hover ? ColAccent : ColKnobOffBorder);
                Outline(box, border);

                const float knob = 16f;
                Rect knobRect = new Rect(
                    value ? box.xMax - 3f - knob : box.x + 3f,
                    box.y + 3f, knob, knob);
                Fill(knobRect, value ? ColAccent : (hover ? ColAccentSoft : ColKnobOff));

                if (GUIUtility.keyboardControl == id)
                    Outline(new Rect(box.x - 2f, box.y - 2f, box.width + 4f, box.height + 4f),
                        ColAccentSoft);
            }

            GUILayout.EndHorizontal();
            return clicked ? !value : value;
        }

        public static bool Button(string text, params GUILayoutOption[] options)
        {
            Prepare();
            return GUILayout.Button(text, _button, options);
        }

        public static bool SmallButton(string text, params GUILayoutOption[] options)
        {
            Prepare();
            return GUILayout.Button(text, _buttonSmall, options);
        }

        public static int IntField(string name, int value, int min = -1000000, int max = 1000000)
        {
            Prepare();
            GUILayout.BeginHorizontal(GUILayout.Height(28f));
            GUILayout.Label(name, _label, GUILayout.Height(28f), GUILayout.ExpandWidth(true));
            string raw = GUILayout.TextField(value.ToString(), _field,
                GUILayout.Width(112f), GUILayout.Height(28f));
            int parsed;
            if (int.TryParse(raw, out parsed))
                value = Mathf.Clamp(parsed, min, max);
            GUILayout.EndHorizontal();
            GUILayout.Space(4f);
            return value;
        }

        public static float FloatField(string name, float value, float min = -1000f, float max = 1000f)
        {
            Prepare();
            GUILayout.BeginHorizontal(GUILayout.Height(28f));
            GUILayout.Label(name, _label, GUILayout.Height(28f), GUILayout.ExpandWidth(true));
            string raw = GUILayout.TextField(value.ToString("0.##"), _field,
                GUILayout.Width(112f), GUILayout.Height(28f));
            float parsed;
            if (float.TryParse(raw, out parsed))
                value = Mathf.Clamp(parsed, min, max);
            GUILayout.EndHorizontal();
            GUILayout.Space(4f);
            return value;
        }

        public static string TextField(string name, string value)
        {
            Prepare();
            GUILayout.BeginHorizontal(GUILayout.Height(28f));
            GUILayout.Label(name, _label, GUILayout.Height(28f), GUILayout.ExpandWidth(true));
            value = GUILayout.TextField(value, _field,
                GUILayout.Width(172f), GUILayout.Height(28f));
            GUILayout.EndHorizontal();
            GUILayout.Space(4f);
            return value;
        }

        public static void Hint(string text)
        {
            Prepare();
            GUILayout.Label(text, _hint);
            GUILayout.Space(3f);
        }

        public static void Muted(string text, params GUILayoutOption[] options)
        {
            Prepare();
            GUILayout.Label(text, _muted, options);
        }

        public static float Slider(string name, float value, float min, float max,
            float defaultValue = float.NaN, string format = "0.##")
        {
            Prepare();
            bool changed = !float.IsNaN(defaultValue) &&
                           Mathf.Abs(value - defaultValue) > 0.0005f;

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal(GUILayout.Height(20f));
            GUILayout.Label(name, _label, GUILayout.Height(20f), GUILayout.ExpandWidth(true));

            Rect badge = GUILayoutUtility.GetRect(84f, 20f,
                GUILayout.Width(84f), GUILayout.Height(20f));
            if (GUI.Button(badge, value.ToString(format), changed ? _valueChanged : _value) &&
                !float.IsNaN(defaultValue))
                value = defaultValue;

            GUILayout.EndHorizontal();

            Rect area = GUILayoutUtility.GetRect(10f, 20f,
                GUILayout.ExpandWidth(true), GUILayout.Height(20f));
            Rect track = new Rect(area.x, area.y + 7f, area.width, 6f);

            Event e = Event.current;
            int id = GUIUtility.GetControlID(FocusType.Passive);
            bool hover = area.Contains(e.mousePosition);
            float step = Mathf.Max((max - min) / 100f, 0.01f);

            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (e.button == 0 && area.Contains(e.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        GUIUtility.keyboardControl = id;
                        value = SliderValue(e.mousePosition.x, track, min, max);
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        value = SliderValue(e.mousePosition.x, track, min, max);
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;
                case EventType.KeyDown:
                    if (GUIUtility.keyboardControl == id)
                    {
                        if (e.keyCode == KeyCode.LeftArrow)
                        {
                            value = Mathf.Clamp(value - step, min, max);
                            e.Use();
                        }
                        else if (e.keyCode == KeyCode.RightArrow)
                        {
                            value = Mathf.Clamp(value + step, min, max);
                            e.Use();
                        }
                    }
                    break;
            }

            if (e.type == EventType.Repaint)
            {
                Fill(track, ColTrack);

                float t = Mathf.InverseLerp(min, max, value);
                if (t > 0.002f)
                {
                    Rect fill = new Rect(track.x, track.y,
                        Mathf.Max(2f, track.width * t), track.height);
                    GUI.DrawTexture(fill, _gradientTex, ScaleMode.StretchToFill, true);
                }

                if (!float.IsNaN(defaultValue))
                {
                    float dt = Mathf.InverseLerp(min, max, defaultValue);
                    Fill(new Rect(track.x + track.width * dt, track.y - 3f, 2f, track.height + 6f),
                        Alpha(ColDim, 0.75f));
                }

                float handleX = track.x + track.width * t;
                bool hot = hover || GUIUtility.hotControl == id ||
                           GUIUtility.keyboardControl == id;
                Glow(new Rect(handleX - 14f, track.center.y - 14f, 28f, 28f),
                    hot ? 0.65f : 0.38f);

                Matrix4x4 matrix = GUI.matrix;
                GUIUtility.RotateAroundPivot(45f, new Vector2(handleX, track.center.y));
                Fill(new Rect(handleX - 4.5f, track.center.y - 4.5f, 9f, 9f), Color.white);
                GUI.matrix = matrix;

                if (GUIUtility.keyboardControl == id)
                {
                    DrawBar(area.x, area.y + 5f, area.width, 1f, Alpha(ColAccent, 0.8f));
                    DrawBar(area.x, area.y + 15f, area.width, 1f, Alpha(ColAccent, 0.8f));
                }
            }

            GUILayout.EndVertical();
            return Mathf.Clamp(value, min, max);
        }

        private static float SliderValue(float mouseX, Rect track, float min, float max)
        {
            float t = Mathf.Clamp01((mouseX - track.x) / Mathf.Max(1f, track.width));
            return Mathf.Lerp(min, max, t);
        }
    }
}
