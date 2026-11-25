using System;
using UnityEngine;
using UnityEngine.Events;
#if ATTHREE_SCENE_LOADER
using Atthree.SceneLoading;
#endif

namespace Atthree.Menus
{
    /// <summary>
    /// Escape / gamepad Start pauses the game and opens the pause screen: time stops, sound pauses, the
    /// cursor is shown. Escape again goes back through the menu screens, and on the pause screen resumes.
    /// </summary>
    [AddComponentMenu("Atthree/Menus/Pause Menu")]
    [RequireComponent(typeof(MenuManager))]
    public class PauseMenu : MonoBehaviour
    {
        [Tooltip("The screen shown when the game is paused.")]
        public MenuScreen pauseScreen;
        [Tooltip("Escape / gamepad Start toggles the pause. Turn off to pause only from code (e.g. not during cutscenes).")]
        public bool pauseWithButton = true;
        [Tooltip("Set Time.timeScale to 0 while paused.")]
        public bool stopTime = true;
        [Tooltip("Pause all sounds (AudioListener.pause) while paused.")]
        public bool pauseAudio = true;
        [Tooltip("Show and unlock the mouse cursor while paused, restore it on resume.")]
        public bool showCursor = true;

        public UnityEvent onPause = new UnityEvent();
        public UnityEvent onResume = new UnityEvent();

        /// <summary>Some PauseMenu has the game paused.</summary>
        public static bool IsPaused { get; private set; }

        public static event Action Paused, Resumed;

        /// <summary>Can be set to false from code, e.g. during a cutscene or the game-over screen.</summary>
        public bool CanPause { get; set; } = true;

        MenuManager manager;
        float timeScaleBefore = 1f;
        bool cursorVisibleBefore;
        CursorLockMode cursorLockBefore;
        bool pausedHere;
        int pausedFrame = -1, resumedFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            IsPaused = false;
            Paused = null;
            Resumed = null;
        }

        void Awake()
        {
            manager = GetComponent<MenuManager>();
            manager.onBackAtRoot.AddListener(OnBackAtRoot);
#if ATTHREE_SCENE_LOADER
            SceneLoader.LoadStarted += OnSceneLoadStarted;
#endif
        }

        void OnDestroy()
        {
#if ATTHREE_SCENE_LOADER
            SceneLoader.LoadStarted -= OnSceneLoadStarted;
#endif
            if (pausedHere) Resume(restoreOnly: true);
        }

#if ATTHREE_SCENE_LOADER
        void OnSceneLoadStarted(SceneLoadOperation op)
        {
            // loading from the pause menu: un-pause, the new scene starts normally
            if (pausedHere) Resume(restoreOnly: true);
        }
#endif

        void Update()
        {
            if (!pauseWithButton) return;
            int frame = Time.frameCount;
            if (pausedHere)
            {
                // gamepad Start resumes; Escape is "back" and is handled by the MenuManager
                if (frame != pausedFrame && MenuInput.PausePressed() && !MenuInput.BackPressed()) Resume();
                return;
            }
            if (!CanPause || frame == pausedFrame || frame == resumedFrame) return;
            if (MenuInput.PausePressed()) Pause();
        }

        void OnBackAtRoot()
        {
            if (pausedHere && Time.frameCount != pausedFrame) Resume();
        }

        public void Pause()
        {
            if (pausedHere || !CanPause) return;
            pausedHere = true;
            IsPaused = true;
            pausedFrame = Time.frameCount;

            if (stopTime)
            {
                timeScaleBefore = Time.timeScale;
                Time.timeScale = 0f;
            }
            if (pauseAudio) AudioListener.pause = true;
            if (showCursor)
            {
                cursorVisibleBefore = Cursor.visible;
                cursorLockBefore = Cursor.lockState;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            if (pauseScreen != null) manager.OpenAsRoot(pauseScreen);
            onPause.Invoke();
            Paused?.Invoke();
        }

        public void Resume() => Resume(false);

        void Resume(bool restoreOnly)
        {
            if (!pausedHere) return;
            pausedHere = false;
            IsPaused = false;
            resumedFrame = Time.frameCount;

            if (stopTime) Time.timeScale = timeScaleBefore;
            if (pauseAudio) AudioListener.pause = false;
            if (showCursor)
            {
                Cursor.visible = cursorVisibleBefore;
                Cursor.lockState = cursorLockBefore;
            }
            if (restoreOnly) return;
            manager.CloseAll();
            onResume.Invoke();
            Resumed?.Invoke();
        }

        public void Toggle()
        {
            if (pausedHere) Resume();
            else Pause();
        }
    }
}
