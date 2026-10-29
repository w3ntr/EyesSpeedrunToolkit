using UnityEngine;

namespace EyesSpeedrunToolkit
{
    /// <summary>
    /// Современная тема оформления IMGUI (IMGUI Toolkit).
    /// Все стили собираются один раз и пересобираются при смене пресета.
    /// </summary>
    public static class UITheme
    {
        // ---------- Пресеты ----------
        public struct ThemePreset
        {
            public string Name;
            public Color Bg;      // фон окна/секций
            public Color Accent;  // акцент (активные табы, подсветки)
            public Color Text;    // основной текст
            public Color Dim;     // второстепенный текст
        }

        public static readonly ThemePreset[] Presets = new ThemePreset[]
        {
            new ThemePreset { Name = "Catppuccin", Bg = Hex("1E1E2E"), Accent = Hex("CBA6F7"), Text = Hex("CDD6F4"), Dim = Hex("A6ADC8") },
            new ThemePreset { Name = "Midnight",   Bg = Hex("0D0F18"), Accent = Hex("00E5FF"), Text = Hex("D0D6E8"), Dim = Hex("8A93B0") },
            new ThemePreset { Name = "Gruvbox",    Bg = Hex("1D2021"), Accent = Hex("FABD2F"), Text = Hex("D5C4A1"), Dim = Hex("A89968") },
            new ThemePreset { Name = "Mono",       Bg = Hex("050508"), Accent = Hex("FFFFFF"), Text = Hex("E8E8E8"), Dim = Hex("9A9A9A") },
        };

        public static int ActivePresetIndex = 0;

        // Текущие цвета (публичные — HUDManager читает их во время отрисовки)
        public static Color BgColor;
        public static Color PanelColor;   // чуть светлее фона (карточки/табы)
        public static Color HoverColor;
        public static Color AccentColor;
        public static Color TextColor;
        public static Color DimColor;

        // ---------- Кэшированные GUI-ресурсы ----------
        private static Texture2D _whiteTex; // для DrawTexture (полупрозрачные подложки)
        private static readonly System.Collections.Generic.Dictionary<Color, Texture2D> _texCache =
            new System.Collections.Generic.Dictionary<Color, Texture2D>();

        // ---------- Стили ----------
        public static GUIStyle WindowStyle;
        public static GUIStyle TabStyle;
        public static GUIStyle TabActiveStyle;
        public static GUIStyle ButtonStyle;
        public static GUIStyle ToggleStyle;
        public static GUIStyle LabelStyle;
        public static GUIStyle SectionStyle;   // заголовки секций
        public static GUIStyle HintStyle;      // мелкие подсказки серым
        public static GUIStyle WarningStyle;   // красные предупреждения
        public static GUIStyle BoxStyle;       // карточка-контейнер
        public static GUIStyle HSliderStyle;
        public static GUIStyle TextFieldStyle;

        private static bool _initialized = false;

        [RuntimeInitializeOnLoadMethod]
        private static void OnReset() => _initialized = false;

        public static void EnsureInitialized()
        {
            if (_initialized && WindowStyle != null) return;
            BuildTextures();
            ApplyPreset(ActivePresetIndex);
            _initialized = true;
        }

        public static void SetPreset(int index)
        {
            ActivePresetIndex = Mathf.Clamp(index, 0, Presets.Length - 1);
            ApplyPreset(ActivePresetIndex);
        }

        private static void ApplyPreset(int index)
        {
            var p = Presets[index];
            BgColor = p.Bg;
            AccentColor = p.Accent;
            TextColor = p.Text;
            DimColor = p.Dim;
            PanelColor = Lift(p.Bg, 0.09f);
            HoverColor = Lift(p.Bg, 0.16f);
            RebuildStyles();
        }

        // Осветление цвета в HSV (для панелей/ховера)
        public static Color Lift(Color c, float amount)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            v = Mathf.Clamp01(v + amount);
            s = Mathf.Clamp01(s * 0.85f);
            Color r = Color.HSVToRGB(h, s, v);
            r.a = c.a;
            return r;
        }

        private static void BuildTextures()
        {
            if (_whiteTex == null) _whiteTex = Texture2D.whiteTexture;
        }

        private static void RebuildStyles()
        {
            // Базовый стиль — чистый шрифт без системного мусора
            GUIStyle baseStyle = new GUIStyle(GUI.skin.label);

            WindowStyle = new GUIStyle(GUI.skin.window);
            WindowStyle.normal.background = MakeTex(BgColor, 0.96f);
            WindowStyle.onNormal.background = WindowStyle.normal.background;
            WindowStyle.normal.textColor = TextColor;
            WindowStyle.onNormal.textColor = TextColor;
            WindowStyle.fontSize = 14;
            WindowStyle.fontStyle = FontStyle.Bold;
            WindowStyle.padding = new RectOffset(12, 12, 28, 12);

            TabStyle = new GUIStyle(baseStyle);
            TabStyle.alignment = TextAnchor.MiddleCenter;
            TabStyle.normal.background = MakeTex(PanelColor, 1f);
            TabStyle.hover.background = MakeTex(HoverColor, 1f);
            TabStyle.active.background = MakeTex(HoverColor, 1f);
            TabStyle.normal.textColor = DimColor;
            TabStyle.hover.textColor = TextColor;
            TabStyle.active.textColor = TextColor;
            TabStyle.onNormal.background = TabStyle.normal.background;
            TabStyle.onNormal.textColor = TextColor;
            TabStyle.fontSize = 13;
            TabStyle.fontStyle = FontStyle.Bold;
            TabStyle.padding = new RectOffset(6, 6, 4, 4);

            TabActiveStyle = new GUIStyle(TabStyle);
            TabActiveStyle.normal.background = MakeTex(AccentColor, 1f);
            TabActiveStyle.hover.background = TabActiveStyle.normal.background;
            TabActiveStyle.active.background = TabActiveStyle.normal.background;
            TabActiveStyle.normal.textColor = GetContrastText(AccentColor);
            TabActiveStyle.hover.textColor = TabActiveStyle.normal.textColor;
            TabActiveStyle.active.textColor = TabActiveStyle.normal.textColor;
            TabActiveStyle.onNormal.background = TabActiveStyle.normal.background;
            TabActiveStyle.onNormal.textColor = TabActiveStyle.normal.textColor;

            ButtonStyle = new GUIStyle(TabStyle);
            ButtonStyle.padding = new RectOffset(8, 8, 5, 5);

            ToggleStyle = new GUIStyle(GUI.skin.toggle);
            ApplyTextColor(ToggleStyle);
            ToggleStyle.fontSize = 13;

            LabelStyle = new GUIStyle(baseStyle);
            LabelStyle.normal.textColor = TextColor;
            LabelStyle.fontSize = 13;

            SectionStyle = new GUIStyle(baseStyle);
            SectionStyle.normal.textColor = AccentColor;
            SectionStyle.fontSize = 14;
            SectionStyle.fontStyle = FontStyle.Bold;
            SectionStyle.alignment = TextAnchor.MiddleLeft;
            SectionStyle.padding = new RectOffset(0, 0, 2, 2); // немного воздуха снизу — жирный шрифт не обрезается

            HintStyle = new GUIStyle(baseStyle);
            HintStyle.normal.textColor = DimColor;
            HintStyle.fontSize = 11;

            WarningStyle = new GUIStyle(baseStyle);
            WarningStyle.normal.textColor = new Color(1f, 0.4f, 0.4f, 1f);
            WarningStyle.fontSize = 13;
            WarningStyle.fontStyle = FontStyle.Bold;

            BoxStyle = new GUIStyle(GUI.skin.box);
            BoxStyle.normal.background = MakeTex(PanelColor, 1f);

            HSliderStyle = new GUIStyle(GUI.skin.horizontalSlider);
            var thumb = new GUIStyle(GUI.skin.horizontalSliderThumb);
            thumb.normal.background = MakeTex(AccentColor, 1f);
            thumb.onNormal.background = thumb.normal.background;
            thumb.hover.background = thumb.normal.background;
            thumb.active.background = thumb.normal.background;
            HSliderStyle.normal.background = MakeTex(Lift(BgColor, 0.2f), 1f);
            GUI.skin.horizontalSliderThumb = thumb; // бросаем вызов стандартному ползунку тоже

            TextFieldStyle = new GUIStyle(GUI.skin.textField);
            ApplyTextColor(TextFieldStyle);
            TextFieldStyle.fontSize = 13;
        }

        private static void ApplyTextColor(GUIStyle s)
        {
            s.normal.textColor = TextColor;
            s.hover.textColor = TextColor;
            s.active.textColor = TextColor;
            s.focused.textColor = TextColor;
        }

        private static Color GetContrastText(Color bg)
        {
            float lum = 0.2126f * bg.r + 0.7152f * bg.g + 0.0722f * bg.b;
            return lum > 0.55f ? new Color(0.08f, 0.08f, 0.10f, 1f) : Color.white;
        }

        private static Texture2D MakeTex(Color c, float alpha)
        {
            var col = new Color(c.r, c.g, c.b, alpha);
            if (_texCache.TryGetValue(col, out var cached) && cached != null) return cached;

            var t = new Texture2D(2, 2);
            t.filterMode = FilterMode.Point;
            t.SetPixels(new Color[4] { col, col, col, col });
            t.Apply();
            _texCache[col] = t;
            return t;
        }

        // ---------- Публичные хелперы для отрисовки ----------

        /// Парсинг "#RRGGBB" / "RRGGBB" в Color.
        public static Color HexToColor(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.white;
            hex = hex.Replace("#", "").Trim();
            if (hex.Length == 6) hex += "FF";
            if (hex.Length != 8) return Color.white;
            Color c = Color.white;
            ColorUtility.TryParseHtmlString("#" + hex, out c);
            return c;
        }

        /// Color -> "#RRGGBBAA"
        public static string ColorToHex(Color c)
        {
            return "#" + ColorUtility.ToHtmlStringRGBA(c);
        }

        private static Color Hex(string h) => HexToColor(h);

        /// <summary>
        /// Подложка через GUI.DrawTexture. НЕ трогает Event.current — безопасно рисовать поверх drag-зоны.
        /// (GUI.Box со стили-хелперами нельзя вызывать на каждом кадре поверх DragWindow: Box во время
        /// MouseDown делает Event.Use() и «съедает» начало перетаскивания.)
        /// </summary>
        public static void Solid(Rect r, Color c)
        {
            // Рисуем однотонную текстуру с GUI.color = white; сама текстура уже нужного цвета.
            // Делаем её ОДИН раз при инициализации темы (см. EnsureInitialized), а не на каждый кадр.
            Texture2D tex = GetTex(c);
            Color old = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(r, tex);
            GUI.color = old;
        }

        // Кэш текстур по цвету. ВАЖНО: словарь по struct-ключу Color может разрастись при
        // анимациях/переборах цветов, поэтому храним максимум несколько записей и чистим при переполнении.
        private const int MaxSolidCache = 16;
        private static readonly System.Collections.Generic.Dictionary<Color32, Texture2D> _solidCache =
            new System.Collections.Generic.Dictionary<Color32, Texture2D>();

        private static Texture2D GetTex(Color c)
        {
            Color32 key = c; // квантуем до 8-бит, чтобы ключей было конечно
            Texture2D t;
            if (_solidCache.TryGetValue(key, out t) && t != null) return t;

            if (_solidCache.Count >= MaxSolidCache)
            {
                foreach (var kv in _solidCache)
                    if (kv.Value != null) Object.Destroy(kv.Value);
                _solidCache.Clear();
            }

            t = new Texture2D(1, 1);
            t.filterMode = FilterMode.Point;
            t.SetPixel(0, 0, c);
            t.Apply();
            _solidCache[key] = t;
            return t;
        }

        /// Тонкая акцентная линия (разделители / нижняя черточка активного таба).
        public static void Line(Rect r, Color c) => Solid(r, c);

        /// Стиль переключателя с цветным маркером ON/OFF.
        public static GUIStyle ToggleFor(bool on)
        {
            if (!on) return ToggleStyle;
            var s = new GUIStyle(ToggleStyle);
            s.normal.textColor = AccentColor;
            s.onNormal.textColor = AccentColor;
            s.fontStyle = FontStyle.Bold;
            return s;
        }
    }
}
