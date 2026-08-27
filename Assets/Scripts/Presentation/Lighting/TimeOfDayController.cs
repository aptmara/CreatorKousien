// ------------------------------------------------------------
// File		: TimeOfDayController.cs
// Summary	: シーン全体の昼夜ブレンド値をシェーダーのグローバル変数へ流し込むためのコントローラー
//
// Author	: [浅野 勇生]
// Created	: 2026-08-25
//
// Notes	:
// - ベース作成
// ------------------------------------------------------------
using System.Collections;
using UnityEngine;

namespace Game.Presentation.Lighting
{
    /// <summary>
    /// シーン全体の昼夜ブレンド値をシェーダーのグローバル変数へ流し込むController
    /// 0 = 昼、1 = 夜。マテリアル側の Night Response で追従度を個別に調整する
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class TimeOfDayController : MonoBehaviour
    {
        private static readonly int GlobalNightId = Shader.PropertyToID("_GlobalNight");

        [Header("--- 昼夜設定 ---")]

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("0 = 昼、1 = 夜。マテリアル側の Night Response で追従度を個別に調整する")]
        private float _night = 0f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("TransitionToで遷移するときの既定の秒数")]
        private float _defaultTransitionDuration = 2f;

        private Coroutine _transitionRoutine;

        /// <summary>
        /// 現在の昼夜ブレンド値を取得
        /// </summary>
        private float Night => _night;

        /// <summary>
        /// アプリ起動時にグローバル変数を昼へ初期化する
        /// シェーダーグローバルはシーンをまたいで残るため、前回の値の持ち越しを防ぐほうがいいらしいぜよ
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetGlobalOnStartup()
        {
            Shader.SetGlobalFloat(GlobalNightId, 0f);
        }


        private void OnEnable()
        {
            Apply();
        }


        private void OnValidate()
        {
            Apply();
        }

        private void OnDisable()
        {
            if (_transitionRoutine != null)
            {
                StopCoroutine(_transitionRoutine);
                _transitionRoutine = null;
            }
        }

        /// <summary>
        /// 昼夜ブレンド値をシェーダーのグローバル変数へ反映する
        /// </summary>
        /// <param name="value">ブレンド値</param>
        public void SetNight(float value)
        {
            _night = Mathf.Clamp01(value);
            Apply();
        }


        /// <summary>
        /// 昼夜ブレンド値へ滑らかに遷移する。duration が負なら既定値を使う。
        /// </summary>
        public void TransitionTo(float target, float duration = -1f)
        {
            if (!isActiveAndEnabled)
            {
                SetNight(target);
                return;
            }

            if (_transitionRoutine != null)
            {
                StopCoroutine(_transitionRoutine);
            }

            float seconds = duration < 0f ? _defaultTransitionDuration : duration;
            _transitionRoutine = StartCoroutine(TransitionRoutine(Mathf.Clamp01(target), seconds));
        }


        /// <summary>
        /// 昼夜ブレンド値をシェーダーのグローバル変数へ反映する
        /// </summary>
        /// <param name="target">変更先のブレンド値</param>
        /// <param name="duration">遷移にかかる時間</param>
        /// <returns></returns>
        private IEnumerator TransitionRoutine(float target, float duration)
        {
            float start = _night;

            if (duration <= 0f)
            {
                SetNight(target);
                _transitionRoutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetNight(Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            SetNight(target);
            _transitionRoutine = null;
        }


        private void Apply()
        {
            Shader.SetGlobalFloat(GlobalNightId, _night);
        }
    }
}
