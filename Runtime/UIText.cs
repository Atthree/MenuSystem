using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Atthree.Menus
{
    /// <summary>Sets/gets text on a TextMeshPro text or a legacy UI Text, whichever the object has.</summary>
    public static class UIText
    {
        public static void Set(Component target, string text)
        {
            if (target == null) return;
            if (target is TMP_Text tmp) { tmp.text = text; return; }
            if (target is Text legacy) { legacy.text = text; return; }
            var t = target.GetComponent<TMP_Text>();
            if (t != null) { t.text = text; return; }
            var l = target.GetComponent<Text>();
            if (l != null) l.text = text;
        }

        public static string Get(Component target)
        {
            if (target == null) return null;
            if (target is TMP_Text tmp) return tmp.text;
            if (target is Text legacy) return legacy.text;
            var t = target.GetComponent<TMP_Text>();
            if (t != null) return t.text;
            var l = target.GetComponent<Text>();
            return l != null ? l.text : null;
        }

        /// <summary>The first text component in the object or its children (TextMeshPro first).</summary>
        public static Component Find(Component root)
        {
            if (root == null) return null;
            var tmp = root.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null) return tmp;
            return root.GetComponentInChildren<Text>(true);
        }
    }
}
