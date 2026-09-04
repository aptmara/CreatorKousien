// ------------------------------------------------------------
// File		: OpeningDropRainSlide.cs
// Summary	: オープニングの落し物スライドを管理するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-09-04
//
// Notes	:
// - ベース作成
// ------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.Opening
{
    /// <summary>
    /// プレイヤーの左右に落し物が降り続けるスライド
    /// </summary>
    public sealed class OpeningDropRainSlide : OpeningSlideView
    {
        [Header("--- プレイヤー ---")]
        [SerializeField] private RectTransform _player;

        [Tooltip("プレイヤーが出きるまでの時間(秒)")]
        [SerializeField, Min(0.01f)] private float _playerPopDuration = 0.45f;

        [Tooltip("下から何ピクセル上がってくるか")]
        [SerializeField] private float _playerRiseDistance = 120f;

        [Header("--- 落し物 ---")]
        [Tooltip("落し物の絵。1個ごとにランダムで選ばれる")]
        [SerializeField] private Sprite[] _dropSprites;

        [Tooltip("落し物を生成する親。SpawnAreaもこの子に置くこと")]
        [SerializeField] private RectTransform _dropParent;

        [Tooltip("左側の落下開始範囲。この矩形の幅の中でランダムなX位置になる")]
        [SerializeField] private RectTransform _leftSpawnArea;

        [Tooltip("右側の落下開始範囲")]
        [SerializeField] private RectTransform _rightSpawnArea;

        [Tooltip("1回の「ぽろっ！」で片側から落ちる数")]
        [SerializeField, Min(1)] private int _dropsPerSideBurst = 2;

        [Tooltip("「ぽろっ！」と「ぽろっ！」の間隔(秒)")]
        [SerializeField, Min(0.1f)] private float _burstInterval = 1.2f;

        [Tooltip("同じ「ぽろっ！」の中で1個ずつずらす時間(秒)")]
        [SerializeField, Min(0f)] private float _dropStagger = 0.07f;

        [Tooltip("落ちきるまでの時間(秒)")]
        [SerializeField, Min(0.1f)] private float _fallDuration = 1.5f;

        [Tooltip("落下距離(ピクセル)")]
        [SerializeField] private float _fallDistance = 900f;

        [Tooltip("落し物の大きさ(ピクセル)")]
        [SerializeField] private Vector2 _dropSize = new Vector2(110f, 110f);

        [Tooltip("回転速度の最大(度/秒)")]
        [SerializeField] private float _spinSpeedRange = 120f;

        // 使い回すためのプール
        private readonly Queue<Image> _pool = new Queue<Image>();
        private readonly List<Image> _activeDrops = new List<Image>();

        private Vector2 _playerShownPosition;
        private CanvasGroup _playerGroup;


        public override IEnumerator PlayEnterRoutine()
        {
            Group.alpha = 1f;

            yield return PlayerPopRoutine();

            // ループを起動して即座に返す
            StartCoroutine(RainLoopRoutine());
        }


        public override IEnumerator PlayExitRoutine()
        {
            StopRain();

            yield return base.PlayExitRoutine();

            ReturnAllDrops();
        }


        private void StopRain()
        {
            // 降らせるループも落下中のコルーチンもまとめて止める
            StopAllCoroutines();
        }


        private IEnumerator PlayerPopRoutine()
        {
            if (_player == null)
            {
                yield break;
            }

            if (_playerGroup == null)
            {
                _playerShownPosition = _player.anchoredPosition;
                _playerGroup = GetOrAddGroup(_player);
            }

            Vector2 startPosition = _playerShownPosition + Vector2.down * _playerRiseDistance;

            _player.anchoredPosition = startPosition;
            _player.localScale = Vector3.zero;
            _playerGroup.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < _playerPopDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _playerPopDuration);
                float eased = OpeningEase.OutBack(t);

                _player.anchoredPosition = Vector2.LerpUnclamped(startPosition, _playerShownPosition, eased);
                _player.localScale = Vector3.one * Mathf.LerpUnclamped(0f, 1f, eased);
                _playerGroup.alpha = Mathf.Clamp01(t * 2.5f);

                yield return null;
            }

            _player.anchoredPosition = _playerShownPosition;
            _player.localScale = Vector3.one;
            _playerGroup.alpha = 1f;
        }


        /// <summary>
        /// 止められるまで、一定間隔でぽろっ！を繰り返す
        /// </summary>
        /// <returns></returns>
        private IEnumerator RainLoopRoutine()
        {
            if (_dropSprites == null || _dropSprites.Length == 0)
            {
                Debug.LogWarning("[Opening] 落し物のSpriteが未設定です！", this);
                yield break;
            }

            // 飴の雨を振らせるにダ！
            while (true)
            {
                yield return SpawnBurstRoutine();
                yield return new WaitForSecondsRealtime(_burstInterval);
            }
        }


        private IEnumerator SpawnBurstRoutine()
        {
            for (int i = 0; i < _dropsPerSideBurst; i++)
            {
                SpawnDrop(_leftSpawnArea);
                SpawnDrop(_rightSpawnArea);

                if (_dropStagger > 0f)
                {
                    yield return new WaitForSecondsRealtime(_dropStagger);
                }
            }
        }


        private void SpawnDrop(RectTransform spawnArea)
        {
            if (spawnArea == null)
            {
                return;
            }

            Image drop = RentDrop();
            RectTransform rect = drop.rectTransform;

            drop.sprite = _dropSprites[Random.Range(0, _dropSprites.Length)];
            drop.color = Color.white;

            // 範囲の幅の中でランダムなX位置から落とす
            float halfWidth = spawnArea.rect.width * 0.5f;
            Vector2 areaPosition = spawnArea.anchoredPosition;
            Vector2 startPosition = new Vector2(areaPosition.x  + Random.Range(-halfWidth, halfWidth), areaPosition.y);

            rect.anchoredPosition = startPosition;
            rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            rect.localScale = Vector3.one;
            drop.gameObject.SetActive(true);

            StartCoroutine(FallRoutine(drop, startPosition, Random.Range(-_spinSpeedRange, _spinSpeedRange)));
        }


        private IEnumerator FallRoutine(Image drop, Vector2 startPosition, float spinSpeed)
        {
            RectTransform rect = drop.rectTransform;
            float startRotation = rect.localEulerAngles.z;
            float elapsed = 0f;

            while (elapsed < _fallDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _fallDuration);

                // 重力っぽく加速させる
                float fallRate = t * t;

                rect.anchoredPosition = startPosition + Vector2.down * (_fallDistance * fallRate);
                rect.localRotation = Quaternion.Euler(0f, 0f, startRotation + spinSpeed * elapsed);

                // 終盤で消えていく
                drop.color = new Color(1f, 1f, 1f, Mathf.Clamp01((1f - t) * 3f));

                yield return null;
            }

            ReturnDrop(drop);
        }


        private Image RentDrop()
        {
            Image drop = _pool.Count > 0 ? _pool.Dequeue() : CreateDrop();
            _activeDrops.Add(drop);
            return drop;
        }


        private Image CreateDrop()
        {
            // プールにあれば使い回す
            GameObject dropObject = new GameObject("Drop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

            // 親を設定して大きさを整える
            RectTransform rect = (RectTransform)dropObject.transform;
            rect.SetParent(_dropParent, false);

            // コードで作ったRectTransformは左下基準になるので、SpawnAreaと同じように中央基準にする
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            rect.sizeDelta = _dropSize;

            // Imageを設定
            Image image = dropObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;

            return image;
        }


        private void ReturnDrop(Image drop)
        {
            drop.gameObject.SetActive(false);
            _activeDrops.Remove(drop);
            _pool.Enqueue(drop);
        }


        private void ReturnAllDrops()
        {
            for (int i = _activeDrops.Count - 1; i >= 0; i--)
            {
                _activeDrops[i].gameObject.SetActive(false);
                _pool.Enqueue(_activeDrops[i]);
            }

            _activeDrops.Clear();
        }


        private static CanvasGroup GetOrAddGroup(RectTransform target)
        {
            CanvasGroup group = target.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = target.gameObject.AddComponent<CanvasGroup>();
            }

            return group;
        }
    }
}
