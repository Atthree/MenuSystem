using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Atthree.Menus
{
    public enum ScreenTransition
    {
        None,
        Fade,
        /// <summary>Fade and grow from 90 %.</summary>
        Scale,
        SlideLeft,
        SlideRight,
        SlideUp,
        SlideDown,
    }

    /// <summary>
    /// One menu page (main, settings, credits, pause…). Put it on a panel under a Canvas that has a
    /// <see cref="MenuManager"/>. The manager shows one screen at a time and remembers the way back.
    /// </summary>
    [AddComponentMenu("Atthree/Menus/Menu Screen")]
    [RequireComponent(typeof(CanvasGroup))]
    [DisallowMultipleComponent]
    public class MenuScreen : MonoBehaviour
    {
        [Tooltip("How the screen appears and disappears.")]
        public ScreenTransition transition = ScreenTransition.Fade;
        [Tooltip("Seconds (real time, works while paused).")]
        [Min(0)] public float duration = 0.2f;

        [Tooltip("Selected when the screen opens, so keyboard and gamepad work right away. Empty = the first button.")]
        public Selectable firstSelected;

        [Tooltip("Shown on top of the previous screen instead of replacing it (dialogs, popups).")]
        public bool overlay;

        [Tooltip("Back / Escape closes this screen. Turn off for screens the player must answer.")]
        public bool closeOnBack = true;

        public UnityEvent onShow = new UnityEvent();
        public UnityEvent onHide = new UnityEvent();

        /// <summary>Shown or showing.</summary>
        public bool IsVisible { get; private set; }

        /// <summary>The manager this screen belongs to.</summary>
        public MenuManager Manager => manager != null ? manager : manager = GetComponentInParent<MenuManager>(true);

        public event Action<MenuScreen> Shown, Hidden;

        CanvasGroup group;
        RectTransform rect;
        Vector2 homePosition;
        bool homeKnown;
        Coroutine running;
        MenuManager manager;
        GameObject lastSelected;

        public CanvasGroup Group => group != null ? group : group = GetComponent<CanvasGroup>();

        void Awake()
        {
            rect = transform as RectTransform;
            RememberHome();
        }

        void RememberHome()
        {
            if (homeKnown || rect == null) return;
            homePosition = rect.anchoredPosition;
            homeKnown = true;
        }

        // ---------------------------------------------------------------- show / hide

        /// <summary>Show this screen. Normally you call <see cref="MenuManager.Open"/> instead.</summary>
        public void Show(bool instant = false)
        {
            rect ??= transform as RectTransform;
            RememberHome();
            gameObject.SetActive(true);
            IsVisible = true;
            Group.interactable = true;
            Group.blocksRaycasts = true;
            Play(1f, instant, () => SelectDefault());
            onShow.Invoke();
            Shown?.Invoke(this);
        }

        /// <summary>Hide this screen. Normally you call <see cref="MenuManager.Back"/> instead.</summary>
        public void Hide(bool instant = false)
        {
            RememberSelection();
            bool wasVisible = IsVisible;
            IsVisible = false;
            Group.interactable = false;
            Group.blocksRaycasts = false;
            if (!gameObject.activeInHierarchy) { Apply(0f); gameObject.SetActive(false); }
            else Play(0f, instant, () => gameObject.SetActive(false));
            if (wasVisible)
            {
                onHide.Invoke();
                Hidden?.Invoke(this);
            }
        }

        /// <summary>Hide right away without events (used when the manager starts).</summary>
        internal void HideImmediateSilent()
        {
            IsVisible = false;
            Group.interactable = false;
            Group.blocksRaycasts = false;
            Apply(0f);
            gameObject.SetActive(false);
        }

        void Play(float target, bool instant, Action done)
        {
            if (running != null) StopCoroutine(running);
            running = null;
            if (instant || duration <= 0f || transition == ScreenTransition.None || !isActiveAndEnabled)
            {
                Apply(target);
                done?.Invoke();
                return;
            }
            running = StartCoroutine(Animate(target, done));
        }

        float current = -1f;

        IEnumerator Animate(float target, Action done)
        {
            float start = current < 0f ? 1f - target : current;
            float t = 0f;
            float time = duration * Mathf.Abs(target - start);
            while (t < time)
            {
                Apply(Mathf.Lerp(start, target, t / time));
                yield return null;
                t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            }
            Apply(target);
            running = null;
            done?.Invoke();
        }

        /// <summary>0 = hidden, 1 = fully shown.</summary>
        void Apply(float amount)
        {
            current = amount;
            float e = 1f - (1f - amount) * (1f - amount); // ease out
            Group.alpha = transition == ScreenTransition.None ? (amount > 0f ? 1f : 0f) : e;
            if (rect == null) return;
            rect.localScale = transition == ScreenTransition.Scale ? Vector3.one * Mathf.Lerp(0.9f, 1f, e) : Vector3.one;
            var offset = SlideOffset() * (1f - e);
            rect.anchoredPosition = homePosition + offset;
        }

        Vector2 SlideOffset()
        {
            var size = rect.rect.size;
            switch (transition)
            {
                case ScreenTransition.SlideLeft: return new Vector2(size.x * 0.25f, 0);
                case ScreenTransition.SlideRight: return new Vector2(-size.x * 0.25f, 0);
                case ScreenTransition.SlideUp: return new Vector2(0, -size.y * 0.25f);
                case ScreenTransition.SlideDown: return new Vector2(0, size.y * 0.25f);
                default: return Vector2.zero;
            }
        }

        // ---------------------------------------------------------------- selection

        void RememberSelection()
        {
            var es = EventSystem.current;
            if (es != null && es.currentSelectedGameObject != null && es.currentSelectedGameObject.transform.IsChildOf(transform))
                lastSelected = es.currentSelectedGameObject;
        }

        /// <summary>Selects the last selected element, or First Selected, or the first button.</summary>
        public void SelectDefault()
        {
            var es = EventSystem.current;
            if (es == null || !IsVisible) return;
            GameObject target = null;
            if (lastSelected != null && lastSelected.activeInHierarchy) target = lastSelected;
            else if (firstSelected != null && firstSelected.IsInteractable() && firstSelected.gameObject.activeInHierarchy) target = firstSelected.gameObject;
            else
            {
                foreach (var s in GetComponentsInChildren<Selectable>())
                {
                    if (s.IsInteractable()) { target = s.gameObject; break; }
                }
            }
            if (target != null) es.SetSelectedGameObject(target);
        }

        /// <summary>Forget the remembered selection (next time First Selected is used).</summary>
        public void ClearRememberedSelection() => lastSelected = null;
    }
}
