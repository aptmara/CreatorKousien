using System;
using System.Collections.Generic;
using Game.Core.Management;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Presentation.UI.Pause
{
    [DisallowMultipleComponent]
    public sealed class PauseAudioSettingsPanel : MonoBehaviour
    {
        public enum AudioChannel
        {
            Master,
            BGM,
            SE
        }

        [Serializable]
        private sealed class AudioRow
        {
            public AudioChannel Channel = AudioChannel.Master;
            public Slider Slider = null;
            public Button SelectButton = null;
            public Button MuteButton = null;
            public RectTransform SpeakerIcon = null;
            public Image SpeakerBody = null;
            public TextMeshProUGUI SpeakerText = null;
            public PauseSelectionOutline SelectionOutline = null;
        }

        private const string MasterVolumeKey = "Options.Audio.MasterVolume";
        private const string BGMVolumeKey = "Options.Audio.BGMVolume";
        private const string SEVolumeKey = "Options.Audio.SEVolume";
        private const string MasterMuteKey = "Options.Audio.MasterMute";
        private const string BGMMuteKey = "Options.Audio.BGMMute";
        private const string SEMuteKey = "Options.Audio.SEMute";

        [SerializeField] private AudioRow[] _rows = new AudioRow[3];

        private readonly Dictionary<AudioChannel, AudioRow> _rowByChannel = new();
        private SoundManager _appliedSoundManager;
        private AudioRow _editingRow;
        private float _valueBeforeEditing;
        private float _masterVolume;
        private float _bgmVolume;
        private float _seVolume;
        private bool _masterMuted;
        private bool _bgmMuted;
        private bool _seMuted;
        private bool _initialized;

        public bool IsEditing => _editingRow != null;
        public Selectable FirstSelectable => _rows.Length > 0 ? _rows[0].SelectButton : null;
        public Selectable LastSelectable => _rows.Length > 0 ? _rows[^1].SelectButton : null;

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            LoadSavedSettings();
            _rowByChannel.Clear();
            for (int i = 0; i < _rows.Length; i++)
            {
                AudioRow row = _rows[i];
                _rowByChannel[row.Channel] = row;
                row.Slider.SetValueWithoutNotify(GetChannelVolume(row.Channel));
                row.Slider.interactable = false;
                row.SelectButton.transition = Selectable.Transition.None;
                row.SelectionOutline.SetVisualTargets((RectTransform)row.Slider.transform, row.Slider.transform);
                row.SelectionOutline.SetHighlighted(false);

                AudioRow capturedRow = row;
                row.Slider.onValueChanged.AddListener(value => OnSliderChanged(capturedRow.Channel, value));
                row.SelectButton.onClick.AddListener(() => BeginEditing(capturedRow));
                row.MuteButton.onClick.AddListener(() => ToggleMute(capturedRow.Channel));
                UpdateSpeakerIcon(row.Channel);
            }

            ConfigureNavigation();
            ApplyAudioSettings();
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            for (int i = 0; i < _rows.Length; i++)
            {
                AudioRow row = _rows[i];
                row.SelectionOutline.SetHighlighted(selected == row.SelectButton.gameObject || selected == row.Slider.gameObject);
            }
        }

        public void SelectDefault()
        {
            if (_rows.Length > 0)
            {
                Select(_rows[0].SelectButton);
            }
        }

        public void SetNavigationBoundaries(Selectable tab, Selectable back)
        {
            for (int i = 0; i < _rows.Length; i++)
            {
                Selectable previous = i == 0 ? tab : _rows[i - 1].SelectButton;
                Selectable next = i == _rows.Length - 1 ? back : _rows[i + 1].SelectButton;
                Selectable previousMute = i == 0 ? tab : _rows[i - 1].MuteButton;
                Selectable nextMute = i == _rows.Length - 1 ? back : _rows[i + 1].MuteButton;
                _rows[i].SelectButton.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = previous,
                    selectOnDown = next,
                    selectOnRight = _rows[i].MuteButton
                };
                _rows[i].MuteButton.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = previousMute,
                    selectOnDown = nextMute,
                    selectOnLeft = _rows[i].SelectButton
                };
            }
        }

        public void HandleEditingInput(bool submitPressed, bool cancelPressed, InputAction moveAction)
        {
            if (_editingRow == null)
            {
                return;
            }

            if (submitPressed)
            {
                CommitEditing();
                return;
            }

            if (cancelPressed || IsVerticalMovePerformed(moveAction))
            {
                CancelEditing();
            }
        }

        public void CancelEditing()
        {
            if (_editingRow == null)
            {
                return;
            }

            AudioRow row = _editingRow;
            SetChannelVolume(row.Channel, _valueBeforeEditing);
            row.Slider.SetValueWithoutNotify(_valueBeforeEditing);
            ApplyAudioSettings();
            FinishEditing(row);
        }

        public void ApplyToNewSoundManager()
        {
            if (SoundManager.instance != null && SoundManager.instance != _appliedSoundManager)
            {
                ApplyAudioSettings();
            }
        }

        private void BeginEditing(AudioRow row)
        {
            if (_editingRow != null)
            {
                return;
            }

            _editingRow = row;
            _valueBeforeEditing = GetChannelVolume(row.Channel);
            row.Slider.interactable = true;
            row.SelectButton.targetGraphic.raycastTarget = false;
            Select(row.Slider);
        }

        private void CommitEditing()
        {
            AudioRow row = _editingRow;
            SaveChannelVolume(row.Channel, GetChannelVolume(row.Channel));
            PlayerPrefs.Save();
            FinishEditing(row);
        }

        private void FinishEditing(AudioRow row)
        {
            row.Slider.interactable = false;
            row.SelectButton.targetGraphic.raycastTarget = true;
            _editingRow = null;
            Select(row.SelectButton);
        }

        private void ConfigureNavigation()
        {
            for (int i = 0; i < _rows.Length; i++)
            {
                _rows[i].Slider.navigation = new Navigation { mode = Navigation.Mode.None };
            }
        }

        private void OnSliderChanged(AudioChannel channel, float value)
        {
            SetChannelVolume(channel, value);
            UpdateSpeakerIcon(channel);
            ApplyAudioSettings();
        }

        private void ToggleMute(AudioChannel channel)
        {
            switch (channel)
            {
                case AudioChannel.Master:
                    _masterMuted = !_masterMuted;
                    PlayerPrefs.SetInt(MasterMuteKey, _masterMuted ? 1 : 0);
                    break;
                case AudioChannel.BGM:
                    _bgmMuted = !_bgmMuted;
                    PlayerPrefs.SetInt(BGMMuteKey, _bgmMuted ? 1 : 0);
                    break;
                case AudioChannel.SE:
                    _seMuted = !_seMuted;
                    PlayerPrefs.SetInt(SEMuteKey, _seMuted ? 1 : 0);
                    break;
            }

            PlayerPrefs.Save();
            UpdateSpeakerIcon(channel);
            ApplyAudioSettings();
        }

        private void UpdateSpeakerIcon(AudioChannel channel)
        {
            if (!_rowByChannel.TryGetValue(channel, out AudioRow row))
            {
                return;
            }

            float value = GetChannelVolume(channel);
            bool muted = IsChannelMuted(channel);
            row.SpeakerIcon.localScale = Vector3.one * Mathf.Lerp(0.72f, 1.16f, value);
            Color color = muted ? new Color(0.65f, 0.65f, 0.65f, 1f) : Color.white;
            row.SpeakerText.text = muted || value <= 0.001f ? "×" : "))";
            row.SpeakerText.color = color;
            row.SpeakerBody.color = color;
        }

        private void ApplyAudioSettings()
        {
            float master = _masterMuted ? 0f : _masterVolume;
            float bgm = _bgmMuted ? 0f : _bgmVolume;
            float se = _seMuted ? 0f : _seVolume;

            if (SoundManager.instance != null)
            {
                SoundManager.instance.ApplyVolumeSettings(master, bgm, se);
                _appliedSoundManager = SoundManager.instance;
            }
        }

        private void LoadSavedSettings()
        {
            _masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
            _bgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(BGMVolumeKey, 1f));
            _seVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SEVolumeKey, 1f));
            _masterMuted = PlayerPrefs.GetInt(MasterMuteKey, 0) != 0;
            _bgmMuted = PlayerPrefs.GetInt(BGMMuteKey, 0) != 0;
            _seMuted = PlayerPrefs.GetInt(SEMuteKey, 0) != 0;
        }

        private float GetChannelVolume(AudioChannel channel)
        {
            return channel switch
            {
                AudioChannel.Master => _masterVolume,
                AudioChannel.BGM => _bgmVolume,
                AudioChannel.SE => _seVolume,
                _ => 1f
            };
        }

        private void SetChannelVolume(AudioChannel channel, float value)
        {
            value = Mathf.Clamp01(value);
            switch (channel)
            {
                case AudioChannel.Master:
                    _masterVolume = value;
                    break;
                case AudioChannel.BGM:
                    _bgmVolume = value;
                    break;
                case AudioChannel.SE:
                    _seVolume = value;
                    break;
            }
        }

        private bool IsChannelMuted(AudioChannel channel)
        {
            return channel switch
            {
                AudioChannel.Master => _masterMuted,
                AudioChannel.BGM => _bgmMuted,
                AudioChannel.SE => _seMuted,
                _ => false
            };
        }

        private static void SaveChannelVolume(AudioChannel channel, float value)
        {
            string key = channel switch
            {
                AudioChannel.Master => MasterVolumeKey,
                AudioChannel.BGM => BGMVolumeKey,
                AudioChannel.SE => SEVolumeKey,
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(key))
            {
                PlayerPrefs.SetFloat(key, value);
            }
        }

        private static bool IsVerticalMovePerformed(InputAction moveAction)
        {
            if (moveAction == null || !moveAction.WasPerformedThisFrame())
            {
                return false;
            }

            Vector2 move = moveAction.ReadValue<Vector2>();
            return Mathf.Abs(move.y) > 0.5f && Mathf.Abs(move.y) > Mathf.Abs(move.x);
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
