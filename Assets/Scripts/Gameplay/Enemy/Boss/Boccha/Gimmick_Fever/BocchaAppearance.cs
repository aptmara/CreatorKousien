// ------------------------------------------------------------
// File		: BocchaAppearance.cs
// Summary	: 本体とダミーの見た目差分をまとめた設定
//
// Author	: [浅野 勇生]
// Created	: 2026-09-09
//
// Notes	:
// - 差分が大きいほど本体が一目で分かるため、難易度に直結する！
// - Distinctivenessで差分量を一括スケールし、プレイテストで1本だけ動かせるようにする。
// ------------------------------------------------------------
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// 見た目の差分設定。
    /// </summary>
    [System.Serializable]
    public class BocchaAppearance
    {
        [SerializeField]
        [Min(0.1f)]
        [Tooltip("大きさの倍率")]
        private float _scaleMultiplier = 1.0f;

        [SerializeField]
        [Tooltip("色味。MaterialPropertyBlockで着色するのでマテリアルは増えない！")]
        private Color _tint = Color.white;


        /// <summary>
        /// 差分の大きさに応じたスケールを取得する
        /// </summary>
        /// <param name="distinctiveness">0で差分なし、1で設定値そのまま</param>
        /// <returns></returns>
        public float GetScale(float distinctiveness)
        {
            // Inspectorのリストへ要素を追加した直後は0埋めされる。0なら未設定として等倍を返す
            float target = _scaleMultiplier <= 0.01f ? 1.0f : _scaleMultiplier;

            return Mathf.Lerp(1.0f, target, Mathf.Clamp01(distinctiveness));
        }


        /// <summary>
        /// 差分量を反映した色味を取得する
        /// </summary>
        /// <param name="distinctiveness">0で差分なし、1で設定値そのまま</param>
        /// <returns></returns>
        public Color GetTint(float distinctiveness)
        {
            // Inspectorのリストへ要素を追加した直後は0埋めされる。アルファ0なら未設定として白を返す
            Color target = _tint.a <= 0.001f ? Color.white : _tint;

            return Color.Lerp(Color.white, target, Mathf.Clamp01(distinctiveness));
        }
    }
}
