using System.Collections.Generic;
using Game.Presentation.CameraFeedback;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Presentation.UI.Pause
{
    [DisallowMultipleComponent]
    public sealed class PauseViewSettingsPanel : MonoBehaviour
    {
        private const string ResolutionWidthKey = "Options.View.ResolutionWidth";
        private const string ResolutionHeightKey = "Options.View.ResolutionHeight";
        private const string VSyncKey = "Options.View.VSync";
        private const string FrameRateKey = "Options.View.FrameRate";
        private const string DisplayModeKey = "Options.View.DisplayMode";
        private static readonly int[] FrameRateOptions = { 30, 60, 120 };
        private static readonly FullScreenMode[] DisplayModes =
        {
            FullScreenMode.Windowed,
            FullScreenMode.ExclusiveFullScreen,
            FullScreenMode.FullScreenWindow
        };
        private static readonly string[] DisplayModeLabels = { "ウィンドウ", "フルスクリーン", "ボーダーレス" };
        private static readonly string[] CameraShakeLabels = { "OFF", "弱", "標準" };

        [SerializeField] private Button _displayModePreviousButton;
        [SerializeField] private Button _displayModeNextButton;
        [SerializeField] private TextMeshProUGUI _displayModeValueText;
        [SerializeField] private Button _resolutionPreviousButton;
        [SerializeField] private Button _resolutionNextButton;
        [SerializeField] private TextMeshProUGUI _resolutionValueText;
        [SerializeField] private Button _vSyncButton;
        [SerializeField] private TextMeshProUGUI _vSyncValueText;
        [SerializeField] private Button _frameRatePreviousButton;
        [SerializeField] private Button _frameRateNextButton;
        [SerializeField] private TextMeshProUGUI _frameRateValueText;
        [SerializeField] private Button _cameraShakePreviousButton;
        [SerializeField] private Button _cameraShakeNextButton;
        [SerializeField] private TextMeshProUGUI _cameraShakeValueText;

        private readonly List<Resolution> _resolutions = new();
        private int _currentResolutionIndex;
        private int _currentFrameRateIndex;
        private int _currentDisplayModeIndex;
        private bool _vSyncEnabled;
        private bool _initialized;

        public Selectable FirstSelectable => _displayModeNextButton;
        public Selectable LastSelectable => _cameraShakeNextButton;

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            LoadSavedSettings();
            BuildResolutionList();
            _displayModePreviousButton.onClick.AddListener(() => ChangeDisplayMode(-1));
            _displayModeNextButton.onClick.AddListener(() => ChangeDisplayMode(1));
            _resolutionPreviousButton.onClick.AddListener(() => ChangeResolution(-1));
            _resolutionNextButton.onClick.AddListener(() => ChangeResolution(1));
            _vSyncButton.onClick.AddListener(ToggleVSync);
            _frameRatePreviousButton.onClick.AddListener(() => ChangeFrameRate(-1));
            _frameRateNextButton.onClick.AddListener(() => ChangeFrameRate(1));
            _cameraShakePreviousButton.onClick.AddListener(() => ChangeCameraShake(-1));
            _cameraShakeNextButton.onClick.AddListener(() => ChangeCameraShake(1));
            ConfigureNavigation();
            ApplyViewSettings(true);
            RefreshLabels();
        }

        public void SelectDefault()
        {
            Select(_displayModeNextButton);
        }

        public void SetNavigationBoundaries(Selectable tab, Selectable back)
        {
            _displayModePreviousButton.navigation = CreateNavigation(tab, _resolutionPreviousButton, null, _displayModeNextButton);
            _displayModeNextButton.navigation = CreateNavigation(tab, _resolutionNextButton, _displayModePreviousButton, null);
            _resolutionPreviousButton.navigation = CreateNavigation(_displayModePreviousButton, _vSyncButton, null, _resolutionNextButton);
            _resolutionNextButton.navigation = CreateNavigation(_displayModeNextButton, _vSyncButton, _resolutionPreviousButton, null);
            _vSyncButton.navigation = CreateNavigation(_resolutionNextButton, _frameRateNextButton, null, null);
            _frameRatePreviousButton.navigation = CreateNavigation(_vSyncButton, _cameraShakePreviousButton, null, _frameRateNextButton);
            _frameRateNextButton.navigation = CreateNavigation(_vSyncButton, _cameraShakeNextButton, _frameRatePreviousButton, null);
            _cameraShakePreviousButton.navigation = CreateNavigation(_frameRatePreviousButton, back, null, _cameraShakeNextButton);
            _cameraShakeNextButton.navigation = CreateNavigation(_frameRateNextButton, back, _cameraShakePreviousButton, null);
        }

        private void LoadSavedSettings()
        {
            int savedMode = PlayerPrefs.GetInt(DisplayModeKey, (int)Screen.fullScreenMode);
            _currentDisplayModeIndex = 0;
            for (int i = 0; i < DisplayModes.Length; i++)
            {
                if ((int)DisplayModes[i] == savedMode)
                {
                    _currentDisplayModeIndex = i;
                    break;
                }
            }

            _vSyncEnabled = PlayerPrefs.GetInt(VSyncKey, QualitySettings.vSyncCount > 0 ? 1 : 0) != 0;
            int savedFrameRate = PlayerPrefs.GetInt(FrameRateKey, 60);
            _currentFrameRateIndex = 1;
            for (int i = 0; i < FrameRateOptions.Length; i++)
            {
                if (FrameRateOptions[i] == savedFrameRate)
                {
                    _currentFrameRateIndex = i;
                    break;
                }
            }
        }

        private void BuildResolutionList()
        {
            _resolutions.Clear();
            Resolution[] available = Screen.resolutions;
            for (int i = 0; i < available.Length; i++)
            {
                Resolution resolution = available[i];
                bool duplicate = false;
                for (int j = 0; j < _resolutions.Count; j++)
                {
                    if (_resolutions[j].width == resolution.width && _resolutions[j].height == resolution.height)
                    {
                        duplicate = true;
                        break;
                    }
                }

                if (!duplicate)
                {
                    _resolutions.Add(resolution);
                }
            }

            if (_resolutions.Count == 0)
            {
                _resolutions.Add(Screen.currentResolution);
            }

            int savedWidth = PlayerPrefs.GetInt(ResolutionWidthKey, Screen.width);
            int savedHeight = PlayerPrefs.GetInt(ResolutionHeightKey, Screen.height);
            _currentResolutionIndex = FindResolutionIndex(savedWidth, savedHeight);
            if (_currentResolutionIndex < 0)
            {
                _currentResolutionIndex = FindResolutionIndex(Screen.width, Screen.height);
            }

            _currentResolutionIndex = Mathf.Clamp(_currentResolutionIndex, 0, _resolutions.Count - 1);
        }

        private int FindResolutionIndex(int width, int height)
        {
            for (int i = 0; i < _resolutions.Count; i++)
            {
                if (_resolutions[i].width == width && _resolutions[i].height == height)
                {
                    return i;
                }
            }

            return -1;
        }

        private void ChangeResolution(int direction)
        {
            if (_resolutions.Count == 0)
            {
                return;
            }

            _currentResolutionIndex = WrapIndex(_currentResolutionIndex + direction, _resolutions.Count);
            Resolution resolution = _resolutions[_currentResolutionIndex];
            PlayerPrefs.SetInt(ResolutionWidthKey, resolution.width);
            PlayerPrefs.SetInt(ResolutionHeightKey, resolution.height);
            PlayerPrefs.Save();
            ApplyDisplaySettings();
            RefreshLabels();
        }

        private void ChangeDisplayMode(int direction)
        {
            _currentDisplayModeIndex = WrapIndex(_currentDisplayModeIndex + direction, DisplayModes.Length);
            PlayerPrefs.SetInt(DisplayModeKey, (int)DisplayModes[_currentDisplayModeIndex]);
            PlayerPrefs.Save();
            ApplyDisplaySettings();
            RefreshLabels();
        }

        private void ChangeCameraShake(int direction)
        {
            CameraShakeOptions.Level = WrapIndex(CameraShakeOptions.Level + direction, CameraShakeLabels.Length);
            RefreshLabels();
        }

        private void ApplyDisplaySettings()
        {
            if (_resolutions.Count == 0)
            {
                return;
            }

            Resolution resolution = _resolutions[_currentResolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, DisplayModes[_currentDisplayModeIndex]);
        }

        private void ToggleVSync()
        {
            _vSyncEnabled = !_vSyncEnabled;
            PlayerPrefs.SetInt(VSyncKey, _vSyncEnabled ? 1 : 0);
            PlayerPrefs.Save();
            ApplyViewSettings(false);
            RefreshLabels();
        }

        private void ChangeFrameRate(int direction)
        {
            _currentFrameRateIndex = WrapIndex(_currentFrameRateIndex + direction, FrameRateOptions.Length);
            PlayerPrefs.SetInt(FrameRateKey, FrameRateOptions[_currentFrameRateIndex]);
            PlayerPrefs.Save();
            ApplyViewSettings(false);
            RefreshLabels();
        }

        private void ApplyViewSettings(bool applyResolution)
        {
            QualitySettings.vSyncCount = _vSyncEnabled ? 1 : 0;
            Application.targetFrameRate = FrameRateOptions[_currentFrameRateIndex];
            if (applyResolution && _resolutions.Count > 0)
            {
                ApplyDisplaySettings();
            }
        }

        private void RefreshLabels()
        {
            _displayModeValueText.text = DisplayModeLabels[_currentDisplayModeIndex];
            _cameraShakeValueText.text = CameraShakeLabels[CameraShakeOptions.Level];
            if (_resolutions.Count > 0)
            {
                Resolution resolution = _resolutions[_currentResolutionIndex];
                _resolutionValueText.text = $"{resolution.width} × {resolution.height}";
            }

            _vSyncValueText.text = _vSyncEnabled ? "ON" : "OFF";
            _frameRateValueText.text = $"{FrameRateOptions[_currentFrameRateIndex]} FPS";
        }

        private void ConfigureNavigation()
        {
            _displayModePreviousButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = _displayModeNextButton };
            _displayModeNextButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = _displayModePreviousButton };
            _resolutionPreviousButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = _resolutionNextButton };
            _resolutionNextButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = _resolutionPreviousButton };
            _vSyncButton.navigation = new Navigation { mode = Navigation.Mode.Explicit };
            _frameRatePreviousButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = _frameRateNextButton };
            _frameRateNextButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = _frameRatePreviousButton };
            _cameraShakePreviousButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = _cameraShakeNextButton };
            _cameraShakeNextButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = _cameraShakePreviousButton };
        }

        private static Navigation CreateNavigation(Selectable up, Selectable down, Selectable left, Selectable right)
        {
            return new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = up,
                selectOnDown = down,
                selectOnLeft = left,
                selectOnRight = right
            };
        }

        private static int WrapIndex(int index, int count)
        {
            return count <= 0 ? 0 : (index % count + count) % count;
        }

        private static void Select(Selectable selectable)
        {
            if (EventSystem.current == null || selectable == null)
            {
                return;
            }

            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }
    }
}
