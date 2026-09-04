// ------------------------------------------------------------
// File		: OpeningPhotoPopSlide.cs
// Summary	: オープニングの写真ポップスライドを管理するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-09-04
//
// Notes	:
// - ベース作成
// ------------------------------------------------------------
using System.Collections;
using UnityEngine;


namespace Game.Presentation.Opening
{
    /// <summary>
    /// 写真が下から順番にぴょんぴょん出るスライド
    /// </summary>
    public sealed class OpeningPhotoPopSlide : OpeningSlideView
    {
        [Header("--- 写真のポップ ---")]
        [Tooltip("出したい順番に並べる")]
        [SerializeField] private RectTransform[] _photos;

        [Tooltip("1枚ができるまでの時間(秒)")]
        [SerializeField, Min(0.01f)] private float _popDuration = 0.38f;

        [Tooltip("次の写真が出るまでの間隔(秒)")]
        [SerializeField, Min(0f)] private float _popInterval = 0.20f;

        [Tooltip("下から何ピクセル上がってくるか")]
        [SerializeField] private float _riseDistance = 180f;

        [Tooltip("出るときの傾きのブレ(度)")]
        [SerializeField, Min(0f)] private float _tiltAngle = 8f;

        private Vector2[] _showPositions;
        private float[] _showRotations;
        private CanvasGroup[] _photoGroups;

        public override IEnumerator PlayEnterRoutine()
        {
            CacheLayoutIfNeeded();

            // スライド全体はすぐ見える状態にして、写真を一枚ずつだしていく
            Group.alpha = 1f;
            HideAllPhotos();

            for (int i = 0; i < _photos.Length; i++)
            {
                StartCoroutine(PopPhotoRoutine(i));

                if (i < _photos.Length - 1)
                {
                    yield return new WaitForSecondsRealtime(_popInterval);
                }
            }

            // 最後の一枚が着地しきるまで待つ
            yield return new WaitForSecondsRealtime(_popDuration);
        }


        /// <summary>
        /// Inspectorで置いた最終位置を覚えておく、最初の1回だけ実行される
        /// </summary>
        private void CacheLayoutIfNeeded()
        {
            if (_showPositions != null)
            {
                return;
            }

            // レイアウトをキャッシュ
            _showPositions = new Vector2[_photos.Length];
            _showRotations = new float[_photos.Length];
            _photoGroups = new CanvasGroup[_photos.Length];

            for (int i = 0; i < _photos.Length; i++)
            {
                RectTransform photo = _photos[i];
                if (photo == null)
                {
                    continue;
                }

                _showPositions[i] = photo.anchoredPosition;

                float rotation = photo.localEulerAngles.z;
                if (rotation > 180f)
                {
                    rotation -= 360f;
                }
                _showRotations[i] = rotation;

                CanvasGroup group = photo.GetComponent<CanvasGroup>();
                if (group == null)
                {
                    group = photo.gameObject.AddComponent<CanvasGroup>();
                }
                _photoGroups[i] = group;
            }
        }


        private void HideAllPhotos()
        {
            // すべての写真を非表示にする
            for (int i = 0; i < _photos.Length; i++)
            {
                if (_photos[i] == null)
                {
                    continue;
                }

                _photoGroups[i].alpha = 0f;
                _photos[i].localScale = Vector3.zero;
            }
        }


        private IEnumerator PopPhotoRoutine(int index)
        {
            RectTransform photo = _photos[index];
            if (photo == null)
            {
                yield break;
            }

            // オープニングの写真
            CanvasGroup group = _photoGroups[index];
            Vector2 shownPosition = _showPositions[index];
            float shownRotation = _showRotations[index];

            // 開始位置と角度を計算
            Vector2 startPosition = shownPosition + Vector2.down * _riseDistance;
            float startRotation = shownRotation + Random.Range(-_tiltAngle, _tiltAngle);

            // 初期化
            photo.anchoredPosition = startPosition;
            photo.localRotation = Quaternion.Euler(0f, 0f, startRotation);
            photo.localScale = Vector3.zero;
            group.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < _popDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _popDuration);

                // OutBackは1を超えるので、必ずLerpUnclampedを使う
                float eased = OpeningEase.OutBack(t);

                photo.anchoredPosition = Vector2.LerpUnclamped(startPosition, shownPosition, eased);
                photo.localScale = Vector3.one * Mathf.LerpUnclamped(0f, 1f, eased);
                photo.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(startRotation, shownRotation, eased));

                // 序盤で素早く不透明にして、動きのほうを見せる
                group.alpha = Mathf.Clamp01(t * 2.5f);

                yield return null;
            }

            // 最終値で固定
            photo.anchoredPosition = shownPosition;
            photo.localScale = Vector3.one;
            photo.localRotation = Quaternion.Euler(0f, 0f, shownRotation);
            group.alpha = 1f;
        }
    }
}
