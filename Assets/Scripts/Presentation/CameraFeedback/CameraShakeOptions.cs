using UnityEngine;

namespace Game.Presentation.CameraFeedback
{
    public static class CameraShakeOptions
    {
        private const string PreferenceKey = "Options.View.CameraShake";

        public static int Level
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(PreferenceKey, 2), 0, 2);
            set
            {
                PlayerPrefs.SetInt(PreferenceKey, Mathf.Clamp(value, 0, 2));
                PlayerPrefs.Save();
            }
        }

        public static float StrengthMultiplier => Level * 0.5f;
    }
}
