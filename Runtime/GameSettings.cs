using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
#if ATTHREE_AUDIO
using Atthree.Audio;
#endif

namespace Atthree.Menus
{
    /// <summary>The settings the settings screen can show.</summary>
    public enum Setting
    {
        MasterVolume,
        MusicVolume,
        SfxVolume,
        Fullscreen,
        Resolution,
        Quality,
        VSync,
        FrameRateLimit,
        MouseSensitivity,
        InvertY,
    }

    /// <summary>
    /// Player settings that save themselves (PlayerPrefs) and are applied when the game starts.
    /// <code>
    /// GameSettings.Set(Setting.MusicVolume, 0.5f);
    /// float sens = GameSettings.MouseSensitivity;
    /// GameSettings.Changed += s => { if (s == Setting.InvertY) ... };
    /// </code>
    /// Volume goes to the Atthree Audio Manager if it's installed, otherwise to the Audio Mixer set on a
    /// MenuManager (exposed parameters MasterVolume / MusicVolume / SfxVolume), otherwise Master goes to
    /// AudioListener.volume.
    /// </summary>
    public static class GameSettings
    {
        public const string PrefsPrefix = "Atthree.Settings.";

        /// <summary>A setting changed (also when loaded or reset).</summary>
        public static event Action<Setting> Changed;

        /// <summary>Optional mixer for the volume settings. Set by MenuManager.audioMixer or by code.</summary>
        public static AudioMixer AudioMixer { get; set; }

        /// <summary>Exposed mixer parameter names.</summary>
        public static string MasterParameter = "MasterVolume", MusicParameter = "MusicVolume", SfxParameter = "SfxVolume";

        /// <summary>Choices of the Frame Rate Limit setting (0 = no limit).</summary>
        public static readonly int[] FrameRateOptions = { 30, 60, 120, 144, 0 };

        // ---------------------------------------------------------------- typed shortcuts

        public static float MasterVolume => GetFloat(Setting.MasterVolume);
        public static float MusicVolume => GetFloat(Setting.MusicVolume);
        public static float SfxVolume => GetFloat(Setting.SfxVolume);
        public static float MouseSensitivity => GetFloat(Setting.MouseSensitivity);
        public static bool InvertY => GetBool(Setting.InvertY);
        public static bool Fullscreen => GetBool(Setting.Fullscreen);
        public static bool VSync => GetBool(Setting.VSync);

        // ---------------------------------------------------------------- kinds and ranges

        public enum Kind { Slider, Toggle, Choice }

        public static Kind KindOf(Setting s) => s switch
        {
            Setting.Fullscreen or Setting.VSync or Setting.InvertY => Kind.Toggle,
            Setting.Resolution or Setting.Quality or Setting.FrameRateLimit => Kind.Choice,
            _ => Kind.Slider,
        };

        /// <summary>Slider range of a setting.</summary>
        public static Vector2 Range(Setting s) => s == Setting.MouseSensitivity ? new Vector2(0.1f, 3f) : new Vector2(0f, 1f);

        /// <summary>Value when nothing is saved.</summary>
        public static float Default(Setting s) => s switch
        {
            Setting.MasterVolume => 1f,
            Setting.MusicVolume => 0.8f,
            Setting.SfxVolume => 1f,
            Setting.MouseSensitivity => 1f,
            Setting.Fullscreen => Screen.fullScreenMode == FullScreenMode.Windowed ? 0f : 1f,
            Setting.VSync => QualitySettings.vSyncCount > 0 ? 1f : 0f,
            Setting.Quality => QualitySettings.GetQualityLevel(),
            Setting.Resolution => CurrentResolutionIndex(),
            Setting.FrameRateLimit => FrameRateOptions.Length - 1,
            _ => 0f,
        };

        // ---------------------------------------------------------------- get / set

        static readonly Dictionary<Setting, float> cache = new Dictionary<Setting, float>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            cache.Clear();
            Changed = null;
            AudioMixer = null;
        }

        static string Key(Setting s) => PrefsPrefix + s;

        /// <summary>Saved (or default) value. Toggles are 0/1, choices are an index.</summary>
        public static float GetFloat(Setting s)
        {
#if ATTHREE_AUDIO
            // the Audio Manager keeps (and saves) the volumes itself
            if (IsVolume(s) && Application.isPlaying) return AudioManager.GetVolume(Channel(s));
#endif
            if (cache.TryGetValue(s, out var v)) return v;
            v = PlayerPrefs.GetFloat(Key(s), Default(s));
            cache[s] = v;
            return v;
        }

        public static bool GetBool(Setting s) => GetFloat(s) > 0.5f;
        public static int GetIndex(Setting s) => Mathf.RoundToInt(GetFloat(s));

        /// <summary>True if the player changed this setting at some point.</summary>
        public static bool IsSaved(Setting s) => PlayerPrefs.HasKey(Key(s));

        public static void Set(Setting s, float value, bool save = true)
        {
            var r = KindOf(s) == Kind.Slider ? Range(s) : new Vector2(0, float.MaxValue);
            value = Mathf.Clamp(value, r.x, r.y);
            if (KindOf(s) != Kind.Slider) value = Mathf.Round(value);
#if ATTHREE_AUDIO
            if (IsVolume(s) && Application.isPlaying)
            {
                AudioManager.SetVolume(Channel(s), value, save);
                Changed?.Invoke(s);
                return;
            }
#endif
            cache[s] = value;
            if (save)
            {
                PlayerPrefs.SetFloat(Key(s), value);
                PlayerPrefs.Save();
            }
            Apply(s);
            Changed?.Invoke(s);
        }

        public static void Set(Setting s, bool value, bool save = true) => Set(s, value ? 1f : 0f, save);
        public static void Set(Setting s, int index, bool save = true) => Set(s, (float)index, save);

        /// <summary>Forget everything the player changed and go back to the defaults.</summary>
        public static void ResetAll()
        {
            foreach (Setting s in Enum.GetValues(typeof(Setting)))
            {
                PlayerPrefs.DeleteKey(Key(s));
                cache.Remove(s);
            }
            PlayerPrefs.Save();
            foreach (Setting s in Enum.GetValues(typeof(Setting)))
            {
#if ATTHREE_AUDIO
                if (IsVolume(s) && Application.isPlaying) { AudioManager.SetVolume(Channel(s), Default(s), true); Changed?.Invoke(s); continue; }
#endif
                Apply(s);
                Changed?.Invoke(s);
            }
        }

        // ---------------------------------------------------------------- applying

        /// <summary>Applies the saved settings when the game starts (only the ones the player changed).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void ApplySavedOnStart()
        {
            foreach (Setting s in Enum.GetValues(typeof(Setting)))
            {
#if ATTHREE_AUDIO
                if (IsVolume(s)) continue; // the Audio Manager loads its own volumes
#endif
                if (IsSaved(s) || IsVolume(s)) Apply(s);
            }
        }

#if ATTHREE_AUDIO
        static AudioChannel Channel(Setting s) =>
            s == Setting.MasterVolume ? AudioChannel.Master : s == Setting.MusicVolume ? AudioChannel.Music : AudioChannel.Sfx;
#endif

        static bool IsVolume(Setting s) => s == Setting.MasterVolume || s == Setting.MusicVolume || s == Setting.SfxVolume;

        /// <summary>Pushes one setting into Unity (screen, quality, audio).</summary>
        public static void Apply(Setting s)
        {
            switch (s)
            {
                case Setting.MasterVolume:
                case Setting.MusicVolume:
                case Setting.SfxVolume:
                    ApplyVolume(s, GetFloat(s));
                    break;
                case Setting.Fullscreen:
                    if (!Application.isEditor)
                        Screen.fullScreenMode = GetBool(s) ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                    break;
                case Setting.Resolution:
                    if (!Application.isEditor)
                    {
                        var list = Resolutions();
                        if (list.Count > 0)
                        {
                            var r = list[Mathf.Clamp(GetIndex(s), 0, list.Count - 1)];
                            Screen.SetResolution(r.x, r.y, Screen.fullScreenMode);
                        }
                    }
                    break;
                case Setting.Quality:
                    int q = Mathf.Clamp(GetIndex(s), 0, QualitySettings.names.Length - 1);
                    if (QualitySettings.GetQualityLevel() != q) QualitySettings.SetQualityLevel(q, true);
                    break;
                case Setting.VSync:
                    QualitySettings.vSyncCount = GetBool(s) ? 1 : 0;
                    break;
                case Setting.FrameRateLimit:
                    int fps = FrameRateOptions[Mathf.Clamp(GetIndex(s), 0, FrameRateOptions.Length - 1)];
                    Application.targetFrameRate = fps > 0 ? fps : -1;
                    break;
            }
        }

        static void ApplyVolume(Setting s, float value)
        {
#if ATTHREE_AUDIO
            if (Application.isPlaying) AudioManager.SetVolume(Channel(s), value, save: false);
#else
            if (AudioMixer != null)
            {
                var param = s == Setting.MasterVolume ? MasterParameter : s == Setting.MusicVolume ? MusicParameter : SfxParameter;
                AudioMixer.SetFloat(param, value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f);
                return;
            }
            if (s == Setting.MasterVolume) AudioListener.volume = value;
#endif
        }

        // ---------------------------------------------------------------- choices

        /// <summary>Texts for a choice setting's dropdown.</summary>
        public static List<string> Options(Setting s)
        {
            switch (s)
            {
                case Setting.Resolution:
                    return Resolutions().Select(r => $"{r.x} × {r.y}").ToList();
                case Setting.Quality:
                    return QualitySettings.names.ToList();
                case Setting.FrameRateLimit:
                    return FrameRateOptions.Select(f => f > 0 ? $"{f} FPS" : "Unlimited").ToList();
                default:
                    return new List<string>();
            }
        }

        /// <summary>Screen sizes the monitor supports, smallest first, without duplicates.</summary>
        public static List<Vector2Int> Resolutions()
        {
            var list = Screen.resolutions
                .Select(r => new Vector2Int(r.width, r.height))
                .Distinct()
                .OrderBy(r => r.x * r.y)
                .ToList();
            if (list.Count == 0) list.Add(new Vector2Int(Screen.width, Screen.height));
            return list;
        }

        static int CurrentResolutionIndex()
        {
            var list = Resolutions();
            int best = list.Count - 1;
            for (int i = 0; i < list.Count; i++)
                if (list[i].x == Screen.width && list[i].y == Screen.height) best = i;
            return Mathf.Max(0, best);
        }

        /// <summary>Text shown next to a slider: "80%" for volume, "1.5" for sensitivity.</summary>
        public static string Format(Setting s, float value) =>
            s == Setting.MouseSensitivity ? value.ToString("0.0") : $"{Mathf.RoundToInt(value * 100f)}%";
    }
}
