using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

namespace Atthree.Menus
{
    /// <summary>
    /// Shows <see cref="MenuScreen"/>s one at a time, like pages: <see cref="Open"/> goes forward,
    /// <see cref="Back"/> (or Escape / gamepad B) goes back. Put it on the Canvas; the screens are its children.
    /// </summary>
    [AddComponentMenu("Atthree/Menus/Menu Manager")]
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class MenuManager : MonoBehaviour
    {
        [Tooltip("Shown when the scene starts. Empty = nothing is shown (e.g. a pause menu).")]
        public MenuScreen startScreen;

        [Tooltip("Escape / gamepad B goes back one screen.")]
        public bool handleBackButton = true;

        [Tooltip("Optional. The settings screen sets these exposed volume parameters: MasterVolume, MusicVolume, SfxVolume.")]
        public AudioMixer audioMixer;

        [Tooltip("Called when Back is pressed on the first screen (nothing to go back to).")]
        public UnityEvent onBackAtRoot = new UnityEvent();

        /// <summary>The screen on top, or null.</summary>
        public MenuScreen Current => stack.Count > 0 ? stack[stack.Count - 1] : null;

        /// <summary>A screen is open.</summary>
        public bool IsOpen => stack.Count > 0;

        /// <summary>Open screens, bottom first.</summary>
        public IReadOnlyList<MenuScreen> Stack => stack;

        /// <summary>Every screen under this manager (also hidden ones).</summary>
        public MenuScreen[] Screens => GetComponentsInChildren<MenuScreen>(true);

        /// <summary>A screen opened or closed.</summary>
        public event Action<MenuScreen> ScreenChanged;

        readonly List<MenuScreen> stack = new List<MenuScreen>();
        int openedFrame = -1;
        float lastOpenTime;

        static readonly List<MenuManager> all = new List<MenuManager>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        void Awake()
        {
            if (audioMixer != null) GameSettings.AudioMixer = audioMixer;
            foreach (var s in Screens) s.HideImmediateSilent();
            if (startScreen != null) Open(startScreen, instant: true);
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        void Update()
        {
            if (!handleBackButton || !IsOpen || Time.frameCount == openedFrame) return;
            if (!IsTopManager()) return;
            if (MenuInput.BackPressed()) Back();
        }

        /// <summary>Only the manager whose screen was opened last reacts to Back.</summary>
        bool IsTopManager()
        {
            foreach (var m in all)
                if (m != this && m.IsOpen && m.handleBackButton && m.lastOpenTime > lastOpenTime) return false;
            return true;
        }

        // ---------------------------------------------------------------- navigation

        /// <summary>Show a screen. The current one is hidden (unless the new one is an overlay) and remembered for Back.</summary>
        public void Open(MenuScreen screen) => Open(screen, false);

        public void Open(MenuScreen screen, bool instant)
        {
            if (screen == null) return;
            if (Current == screen) return;

            // already in the stack: go back down to it
            int index = stack.IndexOf(screen);
            if (index >= 0)
            {
                while (Current != screen) Pop(instant, showPrevious: false);
                screen.Show(instant);
                Changed(screen);
                return;
            }

            if (Current != null && !screen.overlay) Current.Hide(instant);
            else if (Current != null) Current.Group.interactable = false;
            stack.Add(screen);
            screen.Show(instant);
            openedFrame = Time.frameCount;
            lastOpenTime = Time.realtimeSinceStartup + Time.frameCount * 1e-6f;
            Changed(screen);
        }

        /// <summary>Show a screen by its GameObject name (for UnityEvents and code).</summary>
        public void Open(string screenName)
        {
            foreach (var s in Screens)
            {
                if (s.name == screenName) { Open(s); return; }
            }
            Debug.LogWarning($"MenuManager: no screen called '{screenName}' under {name}.", this);
        }

        /// <summary>Close the top screen and show the one before. On the first screen: <see cref="onBackAtRoot"/>.</summary>
        public void Back()
        {
            if (Current == null) return;
            if (!Current.closeOnBack) return;
            if (stack.Count == 1)
            {
                onBackAtRoot.Invoke();
                return;
            }
            Pop(false, showPrevious: true);
        }

        /// <summary>Close every screen (e.g. resume the game from a pause menu).</summary>
        public void CloseAll(bool instant = false)
        {
            for (int i = stack.Count - 1; i >= 0; i--) stack[i].Hide(instant);
            stack.Clear();
            Changed(null);
        }

        /// <summary>Close everything, then open <paramref name="screen"/> as the first screen.</summary>
        public void OpenAsRoot(MenuScreen screen, bool instant = false)
        {
            CloseAll(instant);
            Open(screen, instant);
        }

        void Pop(bool instant, bool showPrevious)
        {
            var top = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            top.Hide(instant);
            if (!showPrevious || Current == null) return;
            if (top.overlay && Current.IsVisible)
            {
                Current.Group.interactable = true;
                Current.SelectDefault();
            }
            else Current.Show(instant);
            Changed(Current);
        }

        void Changed(MenuScreen screen) => ScreenChanged?.Invoke(screen);
    }
}
