using System;
using UnityEngine;
using UnityEngine.UI;

namespace Atthree.Menus
{
    /// <summary>
    /// "Are you sure?" popup with Yes / No. Put it on a screen with a message text and two buttons, then:
    /// <code>ConfirmDialog.Ask("Quit the game?", () => Application.Quit());</code>
    /// </summary>
    [AddComponentMenu("Atthree/Menus/Confirm Dialog")]
    [RequireComponent(typeof(MenuScreen))]
    public class ConfirmDialog : MonoBehaviour
    {
        [Tooltip("TextMeshPro text or legacy Text for the question.")]
        public Component message;
        public Button yesButton;
        public Button noButton;

        Action onYes, onNo;
        MenuScreen screen;
        bool answered;

        public MenuScreen Screen => screen != null ? screen : screen = GetComponent<MenuScreen>();

        void Awake()
        {
            Screen.overlay = true;
            if (yesButton != null) yesButton.onClick.AddListener(Yes);
            if (noButton != null) noButton.onClick.AddListener(No);
            Screen.Hidden += OnHidden;
            if (Screen.firstSelected == null) Screen.firstSelected = noButton; // the safe answer
        }

        /// <summary>Ask with the first ConfirmDialog in the scene. Returns false if there's none.</summary>
        public static bool Ask(string question, Action onYes, Action onNo = null, string yes = null, string no = null)
        {
            var dialog = FindAnyObjectByType<ConfirmDialog>(FindObjectsInactive.Include);
            if (dialog == null)
            {
                Debug.LogWarning("ConfirmDialog.Ask: there's no ConfirmDialog in the scene; answering Yes.");
                onYes?.Invoke();
                return false;
            }
            dialog.Show(question, onYes, onNo, yes, no);
            return true;
        }

        public void Show(string question, Action yes, Action no = null, string yesText = null, string noText = null)
        {
            onYes = yes;
            onNo = no;
            answered = false;
            UIText.Set(message, question);
            if (yesText != null && yesButton != null) UIText.Set(UIText.Find(yesButton), yesText);
            if (noText != null && noButton != null) UIText.Set(UIText.Find(noButton), noText);

            Screen.overlay = true; // decided before the screen is shown
            var manager = Screen.Manager;
            if (manager != null) manager.Open(Screen);
            else Screen.Show();
        }

        public void Yes() => Answer(true);
        public void No() => Answer(false);

        void Answer(bool yes)
        {
            if (answered) return;
            answered = true;
            var a = yes ? onYes : onNo;
            onYes = onNo = null;
            Close();
            a?.Invoke();
        }

        void Close()
        {
            var manager = Screen.Manager;
            if (manager != null && manager.Current == Screen) manager.Back();
            else Screen.Hide();
        }

        void OnHidden(MenuScreen _)
        {
            // closed with Escape: that's a No
            if (!answered)
            {
                answered = true;
                var a = onNo;
                onYes = onNo = null;
                a?.Invoke();
            }
        }
    }
}
