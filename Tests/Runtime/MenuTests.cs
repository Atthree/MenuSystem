using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Atthree.Menus.Tests
{
    public class MenuTests
    {
        readonly List<Object> created = new List<Object>();
        bool fakeBack, fakePause;

        [SetUp]
        public void SetUp()
        {
            fakeBack = fakePause = false;
            MenuInput.BackPressed = () => fakeBack;
            MenuInput.PausePressed = () => fakePause;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            created.Add(es);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) if (o != null) Object.Destroy(o);
            created.Clear();
            MenuInput.BackPressed = MenuInput.DefaultBack;
            MenuInput.PausePressed = MenuInput.DefaultPause;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        // ---------------------------------------------------------------- helpers

        MenuManager Canvas(string name = "Canvas", bool active = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            go.SetActive(active);
            created.Add(go);
            return go.AddComponent<MenuManager>();
        }

        static MenuScreen Screen(MenuManager m, string name, float duration = 0f, bool withButton = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(m.transform, false);
            var s = go.AddComponent<MenuScreen>();
            s.duration = duration;
            if (withButton)
            {
                var b = new GameObject(name + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
                b.transform.SetParent(go.transform, false);
            }
            return s;
        }

        static Button ButtonOf(MenuScreen s) => s.GetComponentInChildren<Button>(true);

        IEnumerator PressBack()
        {
            fakeBack = true;
            yield return null;
            fakeBack = false;
            yield return null;
        }

        IEnumerator PressPause()
        {
            fakePause = true;
            fakeBack = true; // Escape is both
            yield return null;
            fakePause = fakeBack = false;
            yield return null;
        }

        // ---------------------------------------------------------------- screens

        [UnityTest]
        public IEnumerator StartScreen_IsShown_OthersHidden()
        {
            var m = Canvas();
            var main = Screen(m, "Main");
            var settings = Screen(m, "Settings");
            m.startScreen = main;
            m.gameObject.SetActive(true);
            yield return null;

            Assert.IsTrue(main.gameObject.activeSelf && main.IsVisible);
            Assert.IsFalse(settings.gameObject.activeSelf);
            Assert.AreEqual(main, m.Current);
            Assert.AreEqual(ButtonOf(main).gameObject, EventSystem.current.currentSelectedGameObject, "first button selected for gamepad/keyboard");
        }

        [UnityTest]
        public IEnumerator OpenAndBack_WorkLikePages()
        {
            var m = Canvas();
            var main = Screen(m, "Main");
            var settings = Screen(m, "Settings");
            var audio = Screen(m, "Audio");
            m.startScreen = main;
            m.gameObject.SetActive(true);
            yield return null;

            m.Open(settings);
            m.Open(audio);
            CollectionAssert.AreEqual(new[] { main, settings, audio }, m.Stack);
            Assert.IsFalse(main.gameObject.activeSelf);
            Assert.IsFalse(settings.gameObject.activeSelf);

            m.Back();
            Assert.AreEqual(settings, m.Current);
            Assert.IsTrue(settings.gameObject.activeSelf);
            Assert.IsFalse(audio.gameObject.activeSelf);

            m.Open(main); // already in the stack: goes back down to it
            Assert.AreEqual(1, m.Stack.Count);
            Assert.AreEqual(main, m.Current);
        }

        [UnityTest]
        public IEnumerator BackButton_GoesBack_AndRootCallsEvent()
        {
            var m = Canvas();
            var main = Screen(m, "Main");
            var settings = Screen(m, "Settings");
            m.startScreen = main;
            int rootBacks = 0;
            m.onBackAtRoot.AddListener(() => rootBacks++);
            m.gameObject.SetActive(true);
            yield return null;

            m.Open(settings);
            yield return null;
            yield return PressBack();
            Assert.AreEqual(main, m.Current);

            yield return PressBack();
            Assert.AreEqual(main, m.Current, "the first screen stays");
            Assert.AreEqual(1, rootBacks);
        }

        [UnityTest]
        public IEnumerator Selection_IsRestored_WhenComingBack()
        {
            var m = Canvas();
            var main = Screen(m, "Main");
            var second = new GameObject("Second", typeof(RectTransform), typeof(Image), typeof(Button));
            second.transform.SetParent(main.transform, false);
            var settings = Screen(m, "Settings");
            m.startScreen = main;
            m.gameObject.SetActive(true);
            yield return null;

            EventSystem.current.SetSelectedGameObject(second);
            m.Open(settings);
            Assert.AreEqual(ButtonOf(settings).gameObject, EventSystem.current.currentSelectedGameObject);
            m.Back();
            Assert.AreEqual(second, EventSystem.current.currentSelectedGameObject, "back on the button you left from");
        }

        [UnityTest]
        public IEnumerator Overlay_KeepsScreenBelowVisible()
        {
            var m = Canvas();
            var main = Screen(m, "Main");
            var popup = Screen(m, "Popup");
            popup.overlay = true;
            m.startScreen = main;
            m.gameObject.SetActive(true);
            yield return null;

            m.Open(popup);
            Assert.IsTrue(main.gameObject.activeSelf, "still visible below");
            Assert.IsFalse(main.Group.interactable, "but not clickable");
            m.Back();
            Assert.IsTrue(main.Group.interactable);
        }

        [UnityTest]
        public IEnumerator Transition_UsesRealTime_AndEnds()
        {
            var m = Canvas();
            var main = Screen(m, "Main", duration: 0.2f);
            var other = Screen(m, "Other", duration: 0.2f);
            m.startScreen = main;
            m.gameObject.SetActive(true);
            yield return null;
            Time.timeScale = 0f; // paused game

            m.Open(other);
            yield return null;
            Assert.Less(other.Group.alpha, 1f, "fading in");
            float t = 0;
            while (main.gameObject.activeSelf && t < 2f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsFalse(main.gameObject.activeSelf, "old screen turned off after fading out");
            Assert.AreEqual(1f, other.Group.alpha, 0.001f);
        }

        [UnityTest]
        public IEnumerator OpenByName()
        {
            var m = Canvas();
            Screen(m, "Main");
            var credits = Screen(m, "Credits");
            m.gameObject.SetActive(true);
            yield return null;
            m.Open("Credits");
            Assert.AreEqual(credits, m.Current);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no screen called"));
            m.Open("Nope");
        }

        [UnityTest]
        public IEnumerator OnlyLastOpenedMenu_HandlesBack()
        {
            var a = Canvas("A");
            var aMain = Screen(a, "A Main");
            var aSub = Screen(a, "A Sub");
            a.startScreen = aMain;
            var b = Canvas("B");
            var bMain = Screen(b, "B Main");
            var bSub = Screen(b, "B Sub");
            a.gameObject.SetActive(true);
            b.gameObject.SetActive(true);
            yield return null;
            a.Open(aSub);
            yield return null;
            b.Open(bMain);
            b.Open(bSub);
            yield return null;

            yield return PressBack();
            Assert.AreEqual(bMain, b.Current, "B was opened last");
            Assert.AreEqual(aSub, a.Current, "A didn't react");
        }

        // ---------------------------------------------------------------- pause

        (MenuManager, PauseMenu, MenuScreen) PauseSetup()
        {
            var m = Canvas("Pause");
            var pauseScreen = Screen(m, "Paused");
            var settings = Screen(m, "Settings");
            var p = m.gameObject.AddComponent<PauseMenu>();
            p.pauseScreen = pauseScreen;
            m.gameObject.SetActive(true);
            return (m, p, settings);
        }

        [UnityTest]
        public IEnumerator Pause_StopsTimeAndAudio_ResumeRestores()
        {
            var (m, p, _) = PauseSetup();
            Time.timeScale = 0.75f;
            Cursor.lockState = CursorLockMode.Locked; // (batch mode can't lock it; then it stays None)
            Cursor.visible = false;
            var lockBefore = Cursor.lockState;
            yield return null;
            Assert.IsFalse(m.IsOpen, "nothing shown before pausing");

            yield return PressPause();
            Assert.IsTrue(PauseMenu.IsPaused);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(AudioListener.pause);
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
            Assert.IsTrue(Cursor.visible, "cursor shown in the menu");
            Assert.AreEqual(p.pauseScreen, m.Current);

            yield return PressPause(); // Escape again: back at the first screen = resume
            Assert.IsFalse(PauseMenu.IsPaused);
            Assert.AreEqual(0.75f, Time.timeScale);
            Assert.IsFalse(AudioListener.pause);
            Assert.AreEqual(lockBefore, Cursor.lockState, "cursor lock restored");
            Assert.IsFalse(Cursor.visible, "cursor hidden again");
            Assert.IsFalse(m.IsOpen);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        [UnityTest]
        public IEnumerator Pause_EscapeFromSettings_GoesBackFirst()
        {
            var (m, p, settings) = PauseSetup();
            yield return null;
            p.Pause();
            yield return null;
            m.Open(settings);
            yield return null;

            yield return PressPause();
            Assert.IsTrue(PauseMenu.IsPaused, "still paused");
            Assert.AreEqual(p.pauseScreen, m.Current, "back on the pause screen");
        }

        [UnityTest]
        public IEnumerator Pause_GamepadStart_Resumes()
        {
            var (_, p, _) = PauseSetup();
            yield return null;
            p.Pause();
            yield return null;
            fakePause = true; // Start only, not Back
            yield return null;
            fakePause = false;
            Assert.IsFalse(PauseMenu.IsPaused);
        }

        [UnityTest]
        public IEnumerator Pause_CanBeBlocked()
        {
            var (_, p, _) = PauseSetup();
            p.CanPause = false;
            yield return null;
            yield return PressPause();
            Assert.IsFalse(PauseMenu.IsPaused);
        }

        [UnityTest]
        public IEnumerator Pause_DestroyedWhilePaused_RestoresTime()
        {
            var (m, p, _) = PauseSetup();
            yield return null;
            p.Pause();
            Object.Destroy(m.gameObject);
            yield return null;
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(PauseMenu.IsPaused);
        }

        // ---------------------------------------------------------------- buttons and dialog

        [UnityTest]
        public IEnumerator MenuButton_OpensAndGoesBack()
        {
            var m = Canvas();
            var main = Screen(m, "Main");
            var settings = Screen(m, "Settings");
            m.startScreen = main;
            var open = ButtonOf(main).gameObject.AddComponent<MenuButton>();
            open.action = MenuAction.OpenScreen;
            open.targetScreen = settings;
            var back = ButtonOf(settings).gameObject.AddComponent<MenuButton>();
            back.action = MenuAction.Back;
            m.gameObject.SetActive(true);
            yield return null;

            ButtonOf(main).onClick.Invoke();
            Assert.AreEqual(settings, m.Current);
            ButtonOf(settings).onClick.Invoke();
            Assert.AreEqual(main, m.Current);
        }

        ConfirmDialog Dialog(MenuManager m)
        {
            var s = Screen(m, "Confirm", withButton: false);
            var msg = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
            msg.transform.SetParent(s.transform, false);
            Button Make(string n)
            {
                var b = new GameObject(n, typeof(RectTransform), typeof(Image), typeof(Button));
                b.transform.SetParent(s.transform, false);
                return b.GetComponent<Button>();
            }
            var d = s.gameObject.AddComponent<ConfirmDialog>();
            d.message = msg.GetComponent<TextMeshProUGUI>();
            d.yesButton = Make("Yes");
            d.noButton = Make("No");
            return d;
        }

        [UnityTest]
        public IEnumerator ConfirmDialog_YesNoAndEscape()
        {
            var m = Canvas();
            var main = Screen(m, "Main");
            m.startScreen = main;
            var d = Dialog(m);
            m.gameObject.SetActive(true);
            yield return null;

            string result = null;
            ConfirmDialog.Ask("Quit?", () => result = "yes", () => result = "no");
            Assert.AreEqual(d.Screen, m.Current);
            Assert.IsTrue(main.gameObject.activeSelf, "dialog is an overlay");
            Assert.AreEqual("Quit?", d.message.GetComponent<TMP_Text>().text);
            Assert.AreEqual(d.noButton.gameObject, EventSystem.current.currentSelectedGameObject, "No is selected: the safe answer");
            d.yesButton.onClick.Invoke();
            Assert.AreEqual("yes", result);
            Assert.AreEqual(main, m.Current);

            ConfirmDialog.Ask("Again?", () => result = "yes2", () => result = "no2");
            d.noButton.onClick.Invoke();
            Assert.AreEqual("no2", result);

            ConfirmDialog.Ask("Escape?", () => result = "yes3", () => result = "no3");
            yield return null;
            yield return PressBack();
            Assert.AreEqual("no3", result, "Escape = No");
            Assert.AreEqual(main, m.Current);
        }

        [UnityTest]
        public IEnumerator MenuButton_Confirm_AsksFirst()
        {
            var m = Canvas();
            var main = Screen(m, "Main");
            var settings = Screen(m, "Settings");
            m.startScreen = main;
            var d = Dialog(m);
            var mb = ButtonOf(main).gameObject.AddComponent<MenuButton>();
            mb.action = MenuAction.OpenScreen;
            mb.targetScreen = settings;
            mb.confirm = true;
            m.gameObject.SetActive(true);
            yield return null;

            ButtonOf(main).onClick.Invoke();
            Assert.AreEqual(d.Screen, m.Current, "asks first");
            d.yesButton.onClick.Invoke();
            Assert.AreEqual(settings, m.Current, "then does it");
        }

        // ---------------------------------------------------------------- settings

        [UnityTest]
        public IEnumerator SettingControl_SliderToggleDropdown()
        {
            GameSettings.ResetAll();
            var m = Canvas();
            var s = Screen(m, "Settings", withButton: false);
            m.startScreen = s;

            var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(s.transform, false);
            var label = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(s.transform, false);
            var sc = sliderGo.AddComponent<SettingControl>();
            sc.setting = Setting.MusicVolume;
            sc.valueLabel = label.GetComponent<TextMeshProUGUI>();

            var toggleGo = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
            toggleGo.transform.SetParent(s.transform, false);
            toggleGo.AddComponent<SettingControl>().setting = Setting.InvertY;

            var ddGo = new GameObject("Dropdown", typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
            ddGo.transform.SetParent(s.transform, false);
            ddGo.AddComponent<SettingControl>().setting = Setting.FrameRateLimit;

            m.gameObject.SetActive(true);
            yield return null;

            var slider = sliderGo.GetComponent<Slider>();
            Assert.AreEqual(GameSettings.Default(Setting.MusicVolume), slider.value, 0.001f, "shows the saved/default value");
            Assert.AreEqual("80%", label.GetComponent<TMP_Text>().text);
            slider.value = 0.25f;
            Assert.AreEqual(0.25f, GameSettings.MusicVolume, 0.001f, "saved right away");
            Assert.AreEqual("25%", label.GetComponent<TMP_Text>().text);

            toggleGo.GetComponent<Toggle>().isOn = true;
            Assert.IsTrue(GameSettings.InvertY);

            var dd = ddGo.GetComponent<TMP_Dropdown>();
            Assert.AreEqual(GameSettings.FrameRateOptions.Length, dd.options.Count, "options filled in");
            dd.value = 1; // 60 FPS
            Assert.AreEqual(60, Application.targetFrameRate);

            GameSettings.Set(Setting.MusicVolume, 0.6f); // changed from code: the slider follows
            Assert.AreEqual(0.6f, slider.value, 0.001f);

            GameSettings.ResetAll();
            Assert.AreEqual(GameSettings.Default(Setting.MusicVolume), slider.value, 0.001f);
            Assert.IsFalse(toggleGo.GetComponent<Toggle>().isOn);
        }

        [Test]
        public void GameSettings_SaveClampAndReset()
        {
            GameSettings.ResetAll();
            Assert.IsFalse(GameSettings.IsSaved(Setting.MouseSensitivity));
            GameSettings.Set(Setting.MouseSensitivity, 99f);
            Assert.AreEqual(3f, GameSettings.MouseSensitivity, "clamped to the range");
            Assert.IsTrue(GameSettings.IsSaved(Setting.MouseSensitivity));
            Assert.AreEqual(3f, PlayerPrefs.GetFloat(GameSettings.PrefsPrefix + "MouseSensitivity"), "in PlayerPrefs");

            var changed = new List<Setting>();
            GameSettings.Changed += changed.Add;
            GameSettings.Set(Setting.VSync, true);
            GameSettings.Changed -= changed.Add;
            CollectionAssert.AreEqual(new[] { Setting.VSync }, changed);
            Assert.AreEqual(1, QualitySettings.vSyncCount);

            GameSettings.ResetAll();
            Assert.AreEqual(1f, GameSettings.MouseSensitivity);
            Assert.IsFalse(GameSettings.IsSaved(Setting.MouseSensitivity));
        }

        [Test]
        public void MasterVolume_GoesToAudioListener()
        {
            GameSettings.ResetAll();
            float before = AudioListener.volume;
            try
            {
                GameSettings.Set(Setting.MasterVolume, 0.3f);
#if ATTHREE_AUDIO
                Assert.AreEqual(0.3f, Atthree.Audio.AudioManager.GetVolume(Atthree.Audio.AudioChannel.Master), 0.001f, "goes to the Audio Manager");
                Assert.AreEqual(0.3f, GameSettings.MasterVolume, 0.001f, "and is read back from it");
#else
                Assert.AreEqual(0.3f, AudioListener.volume, 0.001f);
#endif
            }
            finally
            {
                GameSettings.ResetAll();
                AudioListener.volume = before;
            }
        }

        [Test]
        public void ChoiceOptions_AreFilled()
        {
            Assert.AreEqual(QualitySettings.names.Length, GameSettings.Options(Setting.Quality).Count);
            Assert.Greater(GameSettings.Options(Setting.Resolution).Count, 0);
            Assert.AreEqual("Unlimited", GameSettings.Options(Setting.FrameRateLimit)[GameSettings.FrameRateOptions.Length - 1]);
            Assert.AreEqual("80%", GameSettings.Format(Setting.MusicVolume, 0.8f));
            Assert.AreEqual(1.5f.ToString("0.0"), GameSettings.Format(Setting.MouseSensitivity, 1.5f), "uses the player's number format");
        }

        [Test]
        public void UIText_WorksWithTmpAndLegacy()
        {
            var a = new GameObject("tmp", typeof(RectTransform), typeof(TextMeshProUGUI));
            var b = new GameObject("legacy", typeof(RectTransform), typeof(Text));
            created.Add(a);
            created.Add(b);
            UIText.Set(a.transform, "hello");
            UIText.Set(b.GetComponent<Text>(), "world");
            Assert.AreEqual("hello", UIText.Get(a.GetComponent<TMP_Text>()));
            Assert.AreEqual("world", UIText.Get(b.transform));
        }
    }
}
