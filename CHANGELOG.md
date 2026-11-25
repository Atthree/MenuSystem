# Changelog

## [0.1.0] - 2026-11-25
- Initial version: `MenuManager` (screen stack, Open/Back/CloseAll/OpenAsRoot, Back via Escape / gamepad B,
  only the last opened menu handles Back, `onBackAtRoot`) and `MenuScreen` (fade/scale/slide transitions in
  real time, overlays, first selected + remembered selection for keyboard/gamepad).
- `PauseMenu`: pause/resume time scale, audio and cursor; Escape goes back through screens, then resumes;
  gamepad Start; `CanPause`; un-pauses when the Scene Loader loads a scene.
- `GameSettings`: volumes, fullscreen, resolution, quality, V-Sync, frame rate limit, mouse sensitivity,
  invert Y; PlayerPrefs, applied at startup, `Changed` event, reset. Volume to the Atthree Audio Manager,
  an Audio Mixer or AudioListener.
- `SettingControl` for Slider / Toggle / Dropdown / TMP_Dropdown, `ConfirmDialog`, `MenuButton`
  (Open Screen, Back, Close All, Resume, Load Scene, Restart, Quit, Reset Settings, optional confirm),
  `MenuInput` (both input systems, replaceable), `UIText` (TMP or legacy).
- GameObject > Atthree > Menus: Main Menu, Pause Menu, Settings Screen, Empty Screen, Confirm Dialog
  templates (TextMeshPro, imports TMP Essentials when missing). Menu Manager inspector with Show Only This.
- 25 tests (21 PlayMode, 4 EditMode).
