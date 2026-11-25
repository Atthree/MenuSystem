using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Atthree.Menus
{
    /// <summary>
    /// GameObject > Atthree > Menus: builds ready-to-use menus out of normal uGUI objects (Canvas, panels,
    /// TextMeshPro texts, Buttons, Sliders…). Restyle them however you like afterwards.
    /// </summary>
    public static class MenuBuilder
    {
        // ---------------------------------------------------------------- style

        public static Color Background = new Color(0.06f, 0.07f, 0.09f, 0.94f);
        public static Color Panel = new Color(0.11f, 0.12f, 0.16f, 0.98f);
        public static Color ButtonColor = new Color(0.19f, 0.21f, 0.27f, 1f);
        public static Color Accent = new Color(0.98f, 0.73f, 0.27f, 1f);
        public static Color TextColor = new Color(0.95f, 0.95f, 0.97f, 1f);
        public static Color MutedText = new Color(0.7f, 0.72f, 0.78f, 1f);

        const float ContentWidth = 720f;
        const float RowHeight = 48f;
        const float ButtonHeight = 64f;

        // ---------------------------------------------------------------- menu items

        [MenuItem("GameObject/Atthree/Menus/Main Menu", false, 10)]
        static void MainMenuItem() => WithTextMeshPro(() => Select(BuildMainMenu()));

        [MenuItem("GameObject/Atthree/Menus/Pause Menu", false, 11)]
        static void PauseMenuItem() => WithTextMeshPro(() => Select(BuildPauseMenu()));

        [MenuItem("GameObject/Atthree/Menus/Settings Screen", false, 30)]
        static void SettingsItem() => WithTextMeshPro(SettingsItemNow);

        static void SettingsItemNow()
        {
            var manager = SelectedManager() ?? CreateCanvas("Settings Menu", 0).GetComponent<MenuManager>();
            var settings = BuildSettingsScreen(manager.transform);
            if (manager.startScreen == null) manager.startScreen = settings;
            Select(settings.gameObject);
        }

        [MenuItem("GameObject/Atthree/Menus/Empty Screen", false, 31)]
        static void EmptyScreenItem() => WithTextMeshPro(EmptyScreenItemNow);

        static void EmptyScreenItemNow()
        {
            var manager = SelectedManager() ?? CreateCanvas("Menu", 0).GetComponent<MenuManager>();
            var screen = CreateScreen(manager.transform, "New Screen", "New Screen", out var content);
            CreateButton(content, "Back", MenuAction.Back);
            if (manager.startScreen == null) manager.startScreen = screen;
            Select(screen.gameObject);
        }

        [MenuItem("GameObject/Atthree/Menus/Confirm Dialog", false, 32)]
        static void ConfirmItem() => WithTextMeshPro(ConfirmItemNow);

        static void ConfirmItemNow()
        {
            var manager = SelectedManager() ?? CreateCanvas("Menu", 0).GetComponent<MenuManager>();
            Select(BuildConfirmDialog(manager.transform).gameObject);
        }

        static MenuManager SelectedManager() =>
            Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<MenuManager>(true) : null;

        static void Select(GameObject go)
        {
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }

        // ---------------------------------------------------------------- whole menus

        /// <summary>Canvas with Main, Settings and a Confirm dialog. Play loads the second build scene.</summary>
        public static GameObject BuildMainMenu()
        {
            EnsureTextMeshPro();
            var canvas = CreateCanvas("Main Menu", 0);
            var manager = canvas.GetComponent<MenuManager>();

            var settings = BuildSettingsScreen(canvas.transform);
            var main = CreateScreen(canvas.transform, "Main", string.IsNullOrEmpty(PlayerSettings.productName) ? "My Game" : PlayerSettings.productName, out var content, bigTitle: true);
            main.transform.SetSiblingIndex(0);

            var play = CreateButton(content, "Play", MenuAction.LoadScene);
            play.scene = GuessGameScene();
            main.firstSelected = play.GetComponent<Button>();
            CreateButton(content, "Settings", MenuAction.OpenScreen).targetScreen = settings;
            var quit = CreateButton(content, "Quit", MenuAction.Quit);
            quit.confirm = true;
            quit.confirmMessage = "Quit the game?";

            BuildConfirmDialog(canvas.transform);
            manager.startScreen = main;
            return canvas;
        }

        /// <summary>Canvas with a PauseMenu: Pause (Resume, Settings, Restart, Main Menu, Quit), Settings, Confirm.</summary>
        public static GameObject BuildPauseMenu()
        {
            EnsureTextMeshPro();
            var canvas = CreateCanvas("Pause Menu", 100);
            var manager = canvas.GetComponent<MenuManager>();
            var pause = canvas.AddComponent<PauseMenu>();

            var settings = BuildSettingsScreen(canvas.transform);
            var screen = CreateScreen(canvas.transform, "Pause", "Paused", out var content);
            screen.transform.SetSiblingIndex(0);
            screen.transition = ScreenTransition.Scale;
            screen.GetComponent<Image>().color = new Color(Background.r, Background.g, Background.b, 0.75f); // the paused game shows through

            var resume = CreateButton(content, "Resume", MenuAction.Resume);
            screen.firstSelected = resume.GetComponent<Button>();
            CreateButton(content, "Settings", MenuAction.OpenScreen).targetScreen = settings;
            var restart = CreateButton(content, "Restart", MenuAction.RestartScene);
            restart.confirm = true;
            restart.confirmMessage = "Restart? Unsaved progress is lost.";
            var menu = CreateButton(content, "Main Menu", MenuAction.LoadScene);
            menu.scene = EditorBuildSettings.scenes.Length > 0 ? Path.GetFileNameWithoutExtension(EditorBuildSettings.scenes[0].path) : "";
            menu.confirm = true;
            menu.confirmMessage = "Go to the main menu?";
            var quit = CreateButton(content, "Quit", MenuAction.Quit);
            quit.confirm = true;
            quit.confirmMessage = "Quit the game?";

            BuildConfirmDialog(canvas.transform);
            pause.pauseScreen = screen;
            manager.startScreen = null;
            // hidden while editing too, so it doesn't cover the game view
            foreach (var s in manager.Screens) s.gameObject.SetActive(false);
            return canvas;
        }

        /// <summary>Audio, display and control settings with a Reset and a Back button.</summary>
        public static MenuScreen BuildSettingsScreen(Transform parent)
        {
            EnsureTextMeshPro();
            var screen = CreateScreen(parent, "Settings", "Settings", out var content, spacing: 6f);
            screen.transition = ScreenTransition.SlideLeft;

            CreateHeader(content, "Audio");
            var first = CreateSliderRow(content, "Master Volume", Setting.MasterVolume);
            CreateSliderRow(content, "Music", Setting.MusicVolume);
            CreateSliderRow(content, "Sound Effects", Setting.SfxVolume);

            CreateHeader(content, "Display");
            CreateToggleRow(content, "Fullscreen", Setting.Fullscreen);
            CreateDropdownRow(content, "Resolution", Setting.Resolution);
            CreateDropdownRow(content, "Quality", Setting.Quality);
            CreateToggleRow(content, "V-Sync", Setting.VSync);
            CreateDropdownRow(content, "Frame Rate", Setting.FrameRateLimit);

            CreateHeader(content, "Controls");
            CreateSliderRow(content, "Mouse Sensitivity", Setting.MouseSensitivity);
            CreateToggleRow(content, "Invert Y", Setting.InvertY);

            var buttons = CreateRow(content, "Buttons", ButtonHeight + 8);
            var hl = buttons.gameObject.GetComponent<HorizontalLayoutGroup>();
            hl.spacing = 16;
            hl.childForceExpandWidth = true;
            var reset = CreateButton(buttons, "Reset", MenuAction.ResetSettings);
            reset.confirm = true;
            reset.confirmMessage = "Reset all settings?";
            CreateButton(buttons, "Back", MenuAction.Back);

            screen.firstSelected = first;
            return screen;
        }

        /// <summary>A dimmed overlay with a question and Yes / No.</summary>
        public static ConfirmDialog BuildConfirmDialog(Transform parent)
        {
            EnsureTextMeshPro();
            var go = new GameObject("Confirm Dialog", typeof(RectTransform), typeof(CanvasGroup));
            Undo.RegisterCreatedObjectUndo(go, "Create Confirm Dialog");
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);
            var screen = go.AddComponent<MenuScreen>();
            screen.overlay = true;
            screen.transition = ScreenTransition.Scale;
            screen.duration = 0.15f;
            go.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f); // dims and blocks what's behind

            var box = new GameObject("Box", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            box.SetParent(go.transform, false);
            box.sizeDelta = new Vector2(640, 0);
            box.GetComponent<Image>().color = Panel;
            var v = box.GetComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(40, 40, 36, 36);
            v.spacing = 28;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandHeight = false;
            box.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var msg = CreateText(box, "Message", "Are you sure?", 34, TextAlignmentOptions.Center, TextColor);
            msg.GetComponent<LayoutElement>().preferredHeight = 90;

            var row = CreateRow(box, "Buttons", ButtonHeight);
            var hl = row.GetComponent<HorizontalLayoutGroup>();
            hl.spacing = 20;
            var yes = CreatePlainButton(row, "Yes");
            var no = CreatePlainButton(row, "No");

            var dialog = go.AddComponent<ConfirmDialog>();
            dialog.message = msg;
            dialog.yesButton = yes;
            dialog.noButton = no;
            screen.firstSelected = no;
            go.SetActive(false);
            return dialog;
        }

        // ---------------------------------------------------------------- building blocks

        public static GameObject CreateCanvas(string name, int sortingOrder)
        {
            EnsureEventSystem();
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.layer = LayerMask.NameToLayer("UI");
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<MenuManager>();
            return go;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
#if ATTHREE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        /// <summary>A full-screen panel with a centered column; <paramref name="content"/> is the column.</summary>
        public static MenuScreen CreateScreen(Transform parent, string name, string title, out RectTransform content, bool bigTitle = false, float spacing = 14f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);
            go.GetComponent<Image>().color = Background;
            var screen = go.AddComponent<MenuScreen>();

            content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            content.SetParent(go.transform, false);
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 0.5f);
            content.pivot = new Vector2(0.5f, 0.5f);
            content.sizeDelta = new Vector2(ContentWidth, 0);
            var v = content.GetComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (!string.IsNullOrEmpty(title))
            {
                var t = CreateText(content, "Title", title, bigTitle ? 88 : 56, TextAlignmentOptions.Center, bigTitle ? Accent : TextColor);
                t.fontStyle = FontStyles.Bold;
                t.GetComponent<LayoutElement>().preferredHeight = bigTitle ? 170 : 96;
            }
            return screen;
        }

        public static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        static void CreateHeader(Transform parent, string text)
        {
            var t = CreateText(parent, text + " Header", text.ToUpperInvariant(), 24, TextAlignmentOptions.BottomLeft, Accent);
            t.fontStyle = FontStyles.Bold;
            t.characterSpacing = 6;
            t.GetComponent<LayoutElement>().preferredHeight = 44;
        }

        /// <summary>A button with a MenuButton component.</summary>
        public static MenuButton CreateButton(Transform parent, string label, MenuAction action)
        {
            var b = CreatePlainButton(parent, label);
            var mb = b.gameObject.AddComponent<MenuButton>();
            mb.action = action;
            return mb;
        }

        public static Button CreatePlainButton(Transform parent, string label)
        {
            var go = TMP_DefaultControls.CreateButton(TmpResources());
            go.name = label + " Button";
            go.transform.SetParent(parent, false);
            var layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = ButtonHeight;
            layout.flexibleWidth = 1; // buttons side by side share the row
            var img = go.GetComponent<Image>();
            img.color = Color.white;
            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = ButtonColor;
            colors.highlightedColor = Color.Lerp(ButtonColor, Accent, 0.35f);
            colors.selectedColor = Color.Lerp(ButtonColor, Accent, 0.35f);
            colors.pressedColor = Accent;
            colors.disabledColor = new Color(ButtonColor.r, ButtonColor.g, ButtonColor.b, 0.4f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            var text = go.GetComponentInChildren<TMP_Text>();
            text.text = label;
            text.fontSize = 32;
            text.color = TextColor;
            return button;
        }

        static RectTransform CreateRow(Transform parent, string name, float height)
        {
            var row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement)).GetComponent<RectTransform>();
            row.SetParent(parent, false);
            row.GetComponent<LayoutElement>().preferredHeight = height;
            var h = row.GetComponent<HorizontalLayoutGroup>();
            h.spacing = 16;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            return row;
        }

        static RectTransform SettingRow(Transform parent, string label)
        {
            var row = CreateRow(parent, label + " Row", RowHeight);
            var l = CreateText(row, "Label", label, 28, TextAlignmentOptions.MidlineLeft, MutedText);
            var le = l.GetComponent<LayoutElement>();
            le.preferredWidth = 280;
            le.flexibleWidth = 0;
            return row;
        }

        static Selectable CreateSliderRow(Transform parent, string label, Setting setting)
        {
            var row = SettingRow(parent, label);
            var go = DefaultControls.CreateSlider(UiResources());
            go.name = "Slider";
            go.transform.SetParent(row, false);
            go.AddComponent<LayoutElement>().flexibleWidth = 1;
            StyleSlider(go.GetComponent<Slider>());

            var value = CreateText(row, "Value", "100%", 26, TextAlignmentOptions.MidlineRight, TextColor);
            var le = value.GetComponent<LayoutElement>();
            le.preferredWidth = 96;
            le.flexibleWidth = 0;

            var sc = go.AddComponent<SettingControl>();
            sc.setting = setting;
            sc.valueLabel = value;
            sc.Refresh(); // show real values while editing too
            return go.GetComponent<Slider>();
        }

        static void CreateToggleRow(Transform parent, string label, Setting setting)
        {
            var row = SettingRow(parent, label);
            var go = DefaultControls.CreateToggle(UiResources());
            go.name = "Toggle";
            go.transform.SetParent(row, false);
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            Object.DestroyImmediate(go.GetComponentInChildren<Text>().gameObject); // the row has its own label
            var bg = go.transform.Find("Background") as RectTransform;
            bg.anchorMin = bg.anchorMax = new Vector2(0, 0.5f);
            bg.pivot = new Vector2(0, 0.5f);
            bg.anchoredPosition = Vector2.zero;
            bg.sizeDelta = new Vector2(36, 36);
            bg.GetComponent<Image>().color = ButtonColor;
            var check = bg.Find("Checkmark") as RectTransform;
            check.anchorMin = Vector2.zero;
            check.anchorMax = Vector2.one;
            check.offsetMin = new Vector2(4, 4);
            check.offsetMax = new Vector2(-4, -4);
            var checkImage = check.GetComponent<Image>();
            checkImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            checkImage.type = Image.Type.Sliced;
            checkImage.color = Accent; // a filled box reads better than the small default tick
            var sc = go.AddComponent<SettingControl>();
            sc.setting = setting;
            sc.Refresh();
        }

        static void CreateDropdownRow(Transform parent, string label, Setting setting)
        {
            var row = SettingRow(parent, label);
            var go = TMP_DefaultControls.CreateDropdown(TmpResources());
            go.name = "Dropdown";
            go.transform.SetParent(row, false);
            go.AddComponent<LayoutElement>().flexibleWidth = 1;
            go.GetComponent<Image>().color = ButtonColor;
            var dd = go.GetComponent<TMP_Dropdown>();
            dd.captionText.fontSize = 26;
            dd.captionText.color = TextColor;
            dd.itemText.fontSize = 26;
            var template = dd.template;
            template.GetComponent<Image>().color = Panel;
            template.sizeDelta = new Vector2(template.sizeDelta.x, 300);
            var item = template.GetComponentInChildren<Toggle>(true);
            item.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 40);
            item.targetGraphic.color = Panel;
            var colors = item.colors;
            colors.highlightedColor = Color.Lerp(Panel, Accent, 0.4f);
            colors.selectedColor = Color.Lerp(Panel, Accent, 0.4f);
            item.colors = colors;
            dd.itemText.color = TextColor;
            var content = template.Find("Viewport/Content") as RectTransform;
            if (content != null) content.sizeDelta = new Vector2(content.sizeDelta.x, 40);
            var sc = go.AddComponent<SettingControl>();
            sc.setting = setting;
            sc.Refresh();
        }

        static void StyleSlider(Slider s)
        {
            s.transform.Find("Background").GetComponent<Image>().color = ButtonColor;
            s.fillRect.GetComponent<Image>().color = Accent;
            s.handleRect.GetComponent<Image>().color = TextColor;
            s.handleRect.sizeDelta = new Vector2(28, 0);
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// <summary>The scene after the first one in the build list (usually the game), if any.</summary>
        static string GuessGameScene()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();
            if (scenes.Length > 1) return Path.GetFileNameWithoutExtension(scenes[1].path);
            return "";
        }

        // ---------------------------------------------------------------- resources

        static DefaultControls.Resources UiResources() => new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd"),
        };

        static TMP_DefaultControls.Resources TmpResources() => new TMP_DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd"),
        };

        /// <summary>TextMeshPro needs its Essential Resources (default font) once per project.</summary>
        public static bool EnsureTextMeshPro()
        {
            if (TextMeshProReady()) return true;
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog("TextMeshPro",
                    "The menus use TextMeshPro, which needs its Essential Resources (the default font) in this project. Import them now?",
                    "Import", "Skip"))
                return false;
            TMP_PackageResourceImporter.ImportResources(true, false, false);
            AssetDatabase.Refresh();
            return TextMeshProReady();
        }

        /// <summary>
        /// Runs <paramref name="build"/> now, or (first time in a project) after TextMeshPro's Essential
        /// Resources have been imported. The import finishes a moment later, so the build waits for it.
        /// </summary>
        public static void WithTextMeshPro(System.Action build)
        {
            if (TextMeshProReady()) { build(); return; }
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog("TextMeshPro",
                    "The menus use TextMeshPro, which needs its Essential Resources (the default font) in this project. Import them now?",
                    "Import", "Cancel"))
                return;

            void Completed(string _) { Cleanup(); AssetDatabase.Refresh(); EditorApplication.delayCall += () => build(); }
            void Failed(string _, string error) { Cleanup(); Debug.LogError("Importing TextMeshPro resources failed: " + error); }
            void Cancelled(string _) => Cleanup();
            void Cleanup()
            {
                AssetDatabase.importPackageCompleted -= Completed;
                AssetDatabase.importPackageFailed -= Failed;
                AssetDatabase.importPackageCancelled -= Cancelled;
            }
            AssetDatabase.importPackageCompleted += Completed;
            AssetDatabase.importPackageFailed += Failed;
            AssetDatabase.importPackageCancelled += Cancelled;
            TMP_PackageResourceImporter.ImportResources(true, false, false);
        }

        // Checks the settings asset directly: touching TMP_Settings without it opens TMP's importer window.
        public static bool TextMeshProReady() =>
            Resources.Load<TMP_Settings>("TMP Settings") != null && TMP_Settings.defaultFontAsset != null;
    }
}
