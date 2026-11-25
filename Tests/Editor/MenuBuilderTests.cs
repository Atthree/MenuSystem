using System.Linq;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Atthree.Menus.Tests
{
    public class MenuBuilderTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // first time in a project TextMeshPro's resources are imported; that takes a few frames
            bool ready = false;
            MenuBuilder.WithTextMeshPro(() => ready = true);
            for (int i = 0; i < 600 && !ready; i++) yield return null;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [UnityTest]
        public IEnumerator TextMeshPro_IsImportedWhenMissing()
        {
            bool built = false;
            MenuBuilder.WithTextMeshPro(() => built = true);
            for (int i = 0; i < 600 && !built; i++) yield return null; // the import finishes a few frames later
            Assert.IsTrue(built, "the build ran after the import");
            Assert.IsTrue(MenuBuilder.TextMeshProReady());
        }

        [Test]
        public void MainMenu_HasScreensButtonsAndEventSystem()
        {
            var canvas = MenuBuilder.BuildMainMenu();
            var manager = canvas.GetComponent<MenuManager>();
            Assert.IsNotNull(Object.FindAnyObjectByType<EventSystem>(), "EventSystem created");
            Assert.AreEqual(1, Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length);

            var names = manager.Screens.Select(s => s.name).ToArray();
            CollectionAssert.AreEquivalent(new[] { "Main", "Settings", "Confirm Dialog" }, names);
            Assert.AreEqual("Main", manager.startScreen.name);

            var buttons = manager.startScreen.GetComponentsInChildren<MenuButton>(true);
            CollectionAssert.AreEqual(new[] { MenuAction.LoadScene, MenuAction.OpenScreen, MenuAction.Quit }, buttons.Select(b => b.action));
            Assert.AreEqual("Settings", buttons[1].targetScreen.name);
            Assert.IsTrue(buttons[2].confirm, "Quit asks first");
            Assert.AreEqual("Play", buttons[0].GetComponentInChildren<TMP_Text>().text);
            Assert.IsNotNull(buttons[0].GetComponentInChildren<TMP_Text>().font, "texts have a font");

            var dialog = canvas.GetComponentInChildren<ConfirmDialog>(true);
            Assert.IsNotNull(dialog.message);
            Assert.IsNotNull(dialog.yesButton);
            Assert.IsNotNull(dialog.noButton);
            Assert.IsTrue(dialog.GetComponent<MenuScreen>().overlay);

            // a second menu reuses the EventSystem
            MenuBuilder.BuildPauseMenu();
            Assert.AreEqual(1, Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length);
        }

        [Test]
        public void SettingsScreen_HasAControlForEverySetting()
        {
            var canvas = MenuBuilder.CreateCanvas("Test", 0);
            var screen = MenuBuilder.BuildSettingsScreen(canvas.transform);
            var settings = screen.GetComponentsInChildren<SettingControl>(true).Select(c => c.setting).ToArray();
            CollectionAssert.AreEquivalent(System.Enum.GetValues(typeof(Setting)), settings);

            foreach (var c in screen.GetComponentsInChildren<SettingControl>(true))
            {
                var kind = GameSettings.KindOf(c.setting);
                if (kind == GameSettings.Kind.Slider) Assert.IsNotNull(c.GetComponent<Slider>(), c.setting.ToString());
                if (kind == GameSettings.Kind.Toggle) Assert.IsNotNull(c.GetComponent<Toggle>(), c.setting.ToString());
                if (kind == GameSettings.Kind.Choice) Assert.IsNotNull(c.GetComponent<TMP_Dropdown>(), c.setting.ToString());
            }
            Assert.IsNotNull(screen.firstSelected, "gamepad starts on the first slider");
            var buttons = screen.GetComponentsInChildren<MenuButton>(true);
            Assert.IsTrue(buttons.Any(b => b.action == MenuAction.Back));
            Assert.IsTrue(buttons.Any(b => b.action == MenuAction.ResetSettings && b.confirm));
        }

        [Test]
        public void PauseMenu_IsHiddenAndWired()
        {
            var canvas = MenuBuilder.BuildPauseMenu();
            var pause = canvas.GetComponent<PauseMenu>();
            var manager = canvas.GetComponent<MenuManager>();
            Assert.IsNotNull(pause.pauseScreen);
            Assert.IsNull(manager.startScreen, "nothing shown until paused");
            Assert.IsTrue(manager.Screens.All(s => !s.gameObject.activeSelf), "doesn't cover the game while editing");
            Assert.Greater(canvas.GetComponent<Canvas>().sortingOrder, 0, "drawn above the game's own UI");
            var actions = pause.pauseScreen.GetComponentsInChildren<MenuButton>(true).Select(b => b.action).ToArray();
            CollectionAssert.AreEqual(new[] { MenuAction.Resume, MenuAction.OpenScreen, MenuAction.RestartScene, MenuAction.LoadScene, MenuAction.Quit }, actions);
        }
    }
}
