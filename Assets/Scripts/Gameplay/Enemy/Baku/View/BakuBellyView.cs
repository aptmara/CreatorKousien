// ------------------------------------------------------------
// File		: BakuBellyView.cs
// Summary	: 食べた量に応じてバクの身体を膨らませるコンポーネント
//
// Author	: [浅野勇生]
// Created	: 2026-08-22
//
// Notes	:
// - ベース作成
// ------------------------------------------------------------
using UnityEngine;

namespace Game.Gameplay.Enemy.Baku
{
    /// <summary>
    /// 膨張表示。EnemyBodyControllerのスカッシュ演出が触る Transform とは別のノード（Belly）にアタッチする!!
    /// </summary>
    public class BakuBellyView : MonoBehaviour
    {
        [Tooltip("膨らませる対象.未設定ならこのGameObject自身を使う")]
        [SerializeField] private Transform _bellyRoot;

        private Vector3 _baseScale = Vector3.one;
        private Vector3 _maxScale = Vector3.one;
        private AnimationCurve _curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        private float _lerpTime = 0.25f;

        private float _targetRatio;
        private Vector3 _currentScale = Vector3.one;

        private void Awake()
        {
            if (_bellyRoot == null)
            {
                _bellyRoot = transform;
            }
            _baseScale = _bellyRoot.localScale;
            _currentScale = _baseScale;
        }


        /// <summary>
        /// BakuDataから初期化
        /// </summary>
        /// <param name="maxScaleMultiplier">スケール</param>
        /// <param name="curve">アニメーションカーブ</param>
        /// <param name="lerpTime">補間時間</param>
        public void Initialize(Vector3 maxScaleMultiplier, AnimationCurve curve, float lerpTime)
        {
            if (_bellyRoot == null)
            {
                _bellyRoot = transform;
            }

            _maxScale = Vector3.Scale(_baseScale, maxScaleMultiplier);
            if (curve != null && curve.length > 0)
            {
                _curve = curve;
            }
            _lerpTime = Mathf.Max(0.01f, lerpTime);

            _targetRatio = 0f;
            _currentScale = _baseScale;
            _bellyRoot.localScale = _currentScale;
        }

        /// <summary>
        /// 食べた量の割合を設定する
        /// </summary>
        /// <param name="ratio01">割合</param>
        public void SetFill(float ratio01)
        {
            _targetRatio = Mathf.Clamp01(ratio01);
        }


        private void Update()
        {
            if (_bellyRoot == null)
            {
                return;
            }

            float curved = Mathf.Clamp01(_curve.Evaluate(_targetRatio));
            Vector3 targetScale = Vector3.Lerp(_baseScale, _maxScale, curved);

            // フレームレートに依存しない指数補間
            float t = 1f - Mathf.Exp(-Time.deltaTime / _lerpTime);
            _currentScale = Vector3.Lerp(_currentScale, targetScale, t);
            _bellyRoot.localScale = _currentScale;
        }
    }

}
