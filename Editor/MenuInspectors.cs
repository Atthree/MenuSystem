using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Atthree.Menus
{
    /// <summary>A dropdown of the build scenes for [SceneName] string fields.</summary>
    [CustomPropertyDrawer(typeof(SceneNameAttribute))]
    public class SceneNameDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }
            var names = EditorBuildSettings.scenes.Select(s => Path.GetFileNameWithoutExtension(s.path)).ToList();
            var current = property.stringValue;
            var options = names.ToList();
            int index = options.IndexOf(current);
            if (index < 0)
            {
                options.Insert(0, string.IsNullOrEmpty(current) ? "(none)" : current + " (not in build!)");
                index = 0;
            }
            EditorGUI.BeginProperty(position, label, property);
            int picked = EditorGUI.Popup(position, label.text, index, options.ToArray());
            if (picked != index)
            {
                var name = options[picked];
                property.stringValue = names.Contains(name) ? name : current;
            }
            EditorGUI.EndProperty();
        }
    }

    /// <summary>Menu Manager inspector: buttons to look at each screen while editing.</summary>
    [CustomEditor(typeof(MenuManager))]
    public class MenuManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var manager = (MenuManager)target;
            var screens = manager.Screens;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Screens", EditorStyles.boldLabel);
            if (screens.Length == 0)
            {
                EditorGUILayout.HelpBox("No screens yet. Add a panel with a Menu Screen component under this Canvas, or use GameObject > Atthree > Menus.", MessageType.Info);
                return;
            }
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Open: " + string.Join(" > ", manager.Stack.Select(s => s.name)));
                foreach (var s in screens)
                    if (GUILayout.Button("Open " + s.name)) manager.Open(s);
                return;
            }

            EditorGUILayout.LabelField("Show one screen to edit it. In Play mode only the Start Screen is shown.", EditorStyles.wordWrappedMiniLabel);
            foreach (var s in screens)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool visible = s.gameObject.activeSelf;
                    GUILayout.Label((visible ? "● " : "○ ") + s.name, visible ? EditorStyles.boldLabel : EditorStyles.label);
                    if (GUILayout.Button("Show Only This", GUILayout.Width(110))) ShowOnly(screens, s);
                    if (GUILayout.Button("Select", GUILayout.Width(60))) Selection.activeGameObject = s.gameObject;
                }
            }
            if (GUILayout.Button("Show All")) ShowOnly(screens, null);
        }

        static void ShowOnly(MenuScreen[] screens, MenuScreen only)
        {
            foreach (var s in screens)
            {
                Undo.RecordObject(s.gameObject, "Show Screen");
                Undo.RecordObject(s.Group, "Show Screen");
                bool show = only == null || s == only;
                s.gameObject.SetActive(show);
                s.Group.alpha = 1f;
            }
        }
    }
}
