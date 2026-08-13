using System.Collections.Generic;
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
        private static readonly int[] FrameRateOptions = { 30, 60, 120 };

        [SerializeField] private Button _resolutionPreviousButton;
        [SerializeField] private Button _resolutionNextButton;
        [SerializeField] private TextMeshProUGUI _resolutionValueText;
        [SerializeField] private Button _vSyncButton;
        [SerializeField] private TextMeshProUGUI _vSyncValueText;
        [SerializeField] private Button _frameRatePreviousButton;
        [SerializeField] private Button _frameRateNextButton;
        [SerializeField] private TextMeshProUGUI _frameRateValueText;

        private readonly List<Resolution> _resolutions = new();
        private int _currentResolutionIndex;
        private int _currentFrameRateIndex;
        private bool _vSyncEnabled;
        private bool _initialized;

        public Selectable FirstSelectable => _resolutionNextButton;
        public Selectable LastSelectable => _frameRateNextButton;

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            LoadSavedSettings();
            BuildResolutionList();
            _resolutionPreviousButton.onClick.AddListener(() => ChangeResolution(-1));
            _resolutionNextButton.onClick.AddListener(() => ChangeResolution(1));
            _vSyncButton.onClick.AddListener(ToggleVSync);
            _frameRatePreviousButton.onClick.AddListener(() => ChangeFrameRate(-1));
            _frameRateNextButton.onClick.AddListener(() => ChangeFrameRate(1));
            ConfigureNavigation();
            ApplyViewSettings(true);
            RefreshLabels();
        }

        public void SelectDefault()
        {
            Select(_resolutionNextButton);
        }

        public void SetNavigationBoundaries(Selectable tab, Selectable back)
        {
            _resolutionPreviousButton.navigation = CreateNavigation(tab, _vSyncButton, null, _resolutionNextButton);
            _resolutionNextButton.navigation = CreateNavigation(tab, _vSyncButton, _resolutionPreviousButton, null);
            _vSyncButton.navigation = CreateNavigation(_resolutionNextButton, _frameRateNextButton, null, null);
            _frameRatePreviousButton.navigation = CreateNavigation(_vSyncButton, back, null, _frameRateNextButton);
            _frameRateNextButton.navigation = CreateNavigation(_vSyncButton, back, _frameRatePreviousButton, null);
        }

        private void LoadSavedSettings()
        {
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
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode);
            RefreshLabels();
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
                Resolution resolution = _resolutions[_currentResolutionIndex];
                Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode);
            }
        }

        private void RefreshLabels()
        {
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
            _resolutionPreviousButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = _resolutionNextButton };
            _resolutionNextButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = _resolutionPreviousButton };
            _vSyncButton.navigation = new Navigation { mode = Navigation.Mode.Explicit };
            _frameRatePreviousButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = _frameRateNextButton };
            _frameRateNextButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = _frameRatePreviousButton };
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
