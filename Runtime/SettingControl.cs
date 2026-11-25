using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Atthree.Menus
{
    /// <summary>
    /// Connects a Slider, Toggle or Dropdown (uGUI or TextMeshPro) on the same object to a
    /// <see cref="GameSettings"/> setting. It shows the saved value, saves changes right away, and fills
    /// dropdowns (resolutions, quality levels, frame rates) by itself.
    /// </summary>
    [AddComponentMenu("Atthree/Menus/Setting Control")]
    [DisallowMultipleComponent]
    public class SettingControl : MonoBehaviour
    {
        public Setting setting = Setting.MasterVolume;
        [Tooltip("Optional text that shows the value, e.g. \"80%\". TextMeshPro or legacy Text.")]
        public Component valueLabel;

        Slider slider;
        Toggle toggle;
        Dropdown dropdown;
        TMP_Dropdown tmpDropdown;
        bool updating, found;

        void Find()
        {
            if (found) return;
            found = true;
            slider = GetComponent<Slider>();
            toggle = GetComponent<Toggle>();
            dropdown = GetComponent<Dropdown>();
            tmpDropdown = GetComponent<TMP_Dropdown>();
        }

        void Awake()
        {
            Find();
            if (slider != null) slider.onValueChanged.AddListener(OnSlider);
            if (toggle != null) toggle.onValueChanged.AddListener(OnToggle);
            if (dropdown != null) dropdown.onValueChanged.AddListener(OnChoice);
            if (tmpDropdown != null) tmpDropdown.onValueChanged.AddListener(OnChoice);
            if (slider == null && toggle == null && dropdown == null && tmpDropdown == null)
                Debug.LogWarning($"SettingControl on {name} needs a Slider, Toggle or Dropdown on the same object.", this);
        }

        void OnEnable()
        {
            GameSettings.Changed += OnSettingChanged;
            Refresh();
        }

        void OnDisable() => GameSettings.Changed -= OnSettingChanged;

        void OnSettingChanged(Setting s)
        {
            if (s == setting && !updating) Refresh();
        }

        /// <summary>Shows the current value of the setting.</summary>
        public void Refresh()
        {
            Find();
            updating = true;
            float value = GameSettings.GetFloat(setting);
            if (slider != null)
            {
                var r = GameSettings.Range(setting);
                slider.minValue = r.x;
                slider.maxValue = r.y;
                slider.wholeNumbers = false;
                slider.SetValueWithoutNotify(value);
            }
            if (toggle != null) toggle.SetIsOnWithoutNotify(value > 0.5f);
            if (dropdown != null || tmpDropdown != null) FillOptions(Mathf.RoundToInt(value));
            UpdateLabel(value);
            updating = false;
        }

        void FillOptions(int index)
        {
            List<string> options = GameSettings.Options(setting);
            if (dropdown != null)
            {
                dropdown.ClearOptions();
                dropdown.AddOptions(options);
                dropdown.SetValueWithoutNotify(Mathf.Clamp(index, 0, Mathf.Max(0, options.Count - 1)));
                dropdown.RefreshShownValue();
            }
            if (tmpDropdown != null)
            {
                tmpDropdown.ClearOptions();
                tmpDropdown.AddOptions(options);
                tmpDropdown.SetValueWithoutNotify(Mathf.Clamp(index, 0, Mathf.Max(0, options.Count - 1)));
                tmpDropdown.RefreshShownValue();
            }
        }

        void UpdateLabel(float value)
        {
            if (valueLabel == null) return;
            string text = GameSettings.KindOf(setting) switch
            {
                GameSettings.Kind.Slider => GameSettings.Format(setting, value),
                GameSettings.Kind.Toggle => value > 0.5f ? "On" : "Off",
                _ => "",
            };
            UIText.Set(valueLabel, text);
        }

        void OnSlider(float v) => Write(v);
        void OnToggle(bool v) => Write(v ? 1f : 0f);
        void OnChoice(int i) => Write(i);

        void Write(float v)
        {
            updating = true;
            GameSettings.Set(setting, v);
            updating = false;
            UpdateLabel(GameSettings.GetFloat(setting));
        }
    }
}
