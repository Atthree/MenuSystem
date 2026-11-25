using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ATTHREE_SCENE_LOADER
using Atthree.SceneLoading;
#endif

namespace Atthree.Menus
{
    public enum MenuAction
    {
        /// <summary>Open the Target Screen (Settings, Credits…).</summary>
        OpenScreen,
        /// <summary>Go back one screen.</summary>
        Back,
        /// <summary>Close every screen of this menu.</summary>
        CloseAll,
        /// <summary>Resume from the pause menu.</summary>
        Resume,
        /// <summary>Load the scene named in Scene.</summary>
        LoadScene,
        /// <summary>Load the active scene again.</summary>
        RestartScene,
        /// <summary>Quit the game (leave Play mode in the editor).</summary>
        Quit,
        /// <summary>Put every setting back to its default.</summary>
        ResetSettings,
    }

    /// <summary>
    /// Put it on a UI Button: Open Screen, Back, Resume, Load Scene, Restart, Quit… without code.
    /// With <see cref="confirm"/> it asks "Are you sure?" first (needs a ConfirmDialog in the scene).
    /// If the Atthree Scene Loader is installed, scenes load with its fade.
    /// </summary>
    [AddComponentMenu("Atthree/Menus/Menu Button")]
    public class MenuButton : MonoBehaviour
    {
        public MenuAction action = MenuAction.OpenScreen;
        [Tooltip("Open Screen: the screen to show.")]
        public MenuScreen targetScreen;
        [Tooltip("Load Scene: a scene from the build list.")]
        [SceneName] public string scene = "";

        [Tooltip("Ask \"Are you sure?\" first.")]
        public bool confirm;
        public string confirmMessage = "Are you sure?";

        Button button;

        void Awake()
        {
            button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(Click);
        }

        void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(Click);
        }

        /// <summary>Do the action (also callable from any UnityEvent).</summary>
        public void Click()
        {
            if (confirm) ConfirmDialog.Ask(confirmMessage, Run);
            else Run();
        }

        public void Run()
        {
            var manager = GetComponentInParent<MenuManager>(true);
            switch (action)
            {
                case MenuAction.OpenScreen:
                    if (targetScreen == null) { Debug.LogWarning($"MenuButton {name}: no Target Screen.", this); return; }
                    var m = targetScreen.Manager != null ? targetScreen.Manager : manager;
                    if (m != null) m.Open(targetScreen); else targetScreen.Show();
                    break;
                case MenuAction.Back:
                    manager?.Back();
                    break;
                case MenuAction.CloseAll:
                    manager?.CloseAll();
                    break;
                case MenuAction.Resume:
                    var pause = GetComponentInParent<PauseMenu>(true);
                    if (pause == null) pause = FindAnyObjectByType<PauseMenu>();
                    if (pause != null) pause.Resume(); else manager?.CloseAll();
                    break;
                case MenuAction.LoadScene:
                    LoadScene(scene);
                    break;
                case MenuAction.RestartScene:
                    LoadScene(SceneManager.GetActiveScene().path);
                    break;
                case MenuAction.Quit:
                    QuitGame();
                    break;
                case MenuAction.ResetSettings:
                    GameSettings.ResetAll();
                    break;
            }
        }

        /// <summary>Loads a scene, with the Scene Loader's fade if it's installed.</summary>
        public static void LoadScene(string sceneNameOrPath)
        {
            if (string.IsNullOrEmpty(sceneNameOrPath)) { Debug.LogWarning("MenuButton: no scene set."); return; }
#if ATTHREE_SCENE_LOADER
            SceneLoader.Load(sceneNameOrPath);
#else
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneNameOrPath);
#endif
        }

        public static void QuitGame()
        {
#if ATTHREE_SCENE_LOADER
            SceneLoader.Quit();
#elif UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    /// <summary>Shows a dropdown of the build scenes for a string field in the Inspector.</summary>
    public class SceneNameAttribute : PropertyAttribute { }
}
