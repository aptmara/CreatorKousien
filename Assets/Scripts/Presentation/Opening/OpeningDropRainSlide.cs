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


        [Header("--- 雨の回数・積み上げ ---")]
        [Tooltip("左右に降らせる回数")]
        [SerializeField, Min(1)] private int _burstCount = 3;

        [Tooltip("片側の山を横何列にするか")]
        [SerializeField, Min(1)] private int _pileColumns = 4;

        [Tooltip("一段目の飴の中心Y。DropRootの中央基準")]
        [SerializeField] private float _pileBaseY = -230f;

        [Tooltip("次の段を何ピクセル高くするか")]
        [SerializeField, Min(1f)] private float _pileLayerHeight = 70f;

        private int _leftPileCount;
        private int _rightPileCount;
        private int _fallingDropCount;
        private Coroutine _rainRoutine;

        // 使い回すためのプール
        private readonly Queue<Image> _pool = new Queue<Image>();
        private readonly List<Image> _activeDrops = new List<Image>();

        private Vector2 _playerShownPosition;
        private CanvasGroup _playerGroup;


        public override IEnumerator PlayEnterRoutine()
        {
            // まずは落下中のコルーチンを止めて、落下中の飴も回収する
            StopRain();
            ReturnAllDrops();

            // どの段の何列目に置くかを決めるためのカウンタをリセットする
            _leftPileCount = 0;
            _rightPileCount = 0;
            _fallingDropCount = 0;

            Group.alpha = 1f;

            // プレイヤーをぽろっと出す
            yield return PlayerPopRoutine();

            _rainRoutine = StartCoroutine(RainLoopRoutine());
        }


        public override IEnumerator PlayExitRoutine()
        {
            // 雨を止める。落下中の飴があれば着地するまで待つ
            if (_rainRoutine != null)
            {
                yield return _rainRoutine;
                _rainRoutine = null;
            }

            // 雨を止める。落下中の飴があれば着地するまで待つ
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
            for (int i = 0; i < _burstCount; i++)
            {
                yield return SpawnBurstRoutine();

                if (i < _burstCount - 1)
                {
                    yield return new WaitForSecondsRealtime(_burstInterval);
                }
            }

            // 最後の飴が着地するまで待つ
            while (_fallingDropCount > 0)
            {
                yield return null;
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

            // どの段の何列目に置くかを決める
            int pileIndex = spawnArea == _leftSpawnArea ? _leftPileCount++ : _rightPileCount++;

            int columns = Mathf.Max(1, _pileColumns);
            int column = pileIndex % columns;
            int row = pileIndex / columns;

            // 落下開始位置を決める
            Vector2 areaPosition = spawnArea.anchoredPosition;
            float columnWidth = spawnArea.rect.width / columns;

            // 各列の中心。少しずらして整列感を弱める
            float x = areaPosition.x - spawnArea.rect.width * 0.5f + columnWidth * (column + 0.5f) + Random.Range(-columnWidth * 0.15f, columnWidth * 0.15f);

            float y = _pileBaseY + row * _pileLayerHeight + Random.Range(-30f, 30f);

            // 落ち始める高さも、飴ごとに変える
            float startY = areaPosition.y + Random.Range(0f, 300f);

            // 落下開始位置と落下終了位置を決める
            Vector2 startPosition = new Vector2(x, startY);
            Vector2 endPosition = new Vector2(x, y);

            Image drop = RentDrop();
            RectTransform rect = drop.rectTransform;

            drop.sprite = _dropSprites[Random.Range(0, _dropSprites.Length)];
            drop.color = Color.white;

            // 位置・回転・スケールを初期化して、最後に表示する
            rect.anchoredPosition = startPosition;
            rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            rect.localScale = Vector3.one;
            rect.SetAsLastSibling();

            drop.gameObject.SetActive(true);

            // 落下コルーチンを開始する
            StartCoroutine(FallRoutine(drop, startPosition, endPosition, Random.Range(-_spinSpeedRange, _spinSpeedRange)));
        }


        private IEnumerator FallRoutine(Image drop, Vector2 startPosition, Vector2 endPosition, float spinSpeed)
        {
            _fallingDropCount++;

            // 落下中はRaycastを無効にする
            RectTransform rect = drop.rectTransform;
            float startRotation = rect.localEulerAngles.z;
            float duration = Mathf.Max(0.01f, _fallDuration * Random.Range(0.8f, 1.25f));
            float elapsed = 0f;

            float swayWidth = Random.Range(15f, 45f);
            float swayPhase = Random.Range(0f, Mathf.PI * 2f);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // 下に向かって加速
                Vector2 position = Vector2.Lerp(startPosition, endPosition, t * t);

                // 飴ごとに違う揺れ
                float sway = Mathf.Sin(t * Mathf.PI * 2f + swayPhase);
                float envelope = Mathf.Sin(t * Mathf.PI);

                position.x += sway * envelope * swayWidth;
                rect.anchoredPosition = position;

                rect.localRotation = Quaternion.Euler(0f, 0f, startRotation + spinSpeed * Mathf.Min(elapsed, duration));

                yield return null;
            }

            rect.anchoredPosition = endPosition;
            drop.color = Color.white;

            _fallingDropCount--;

            // 着地後は残す。スライド終了時にReturnAllDropsで回収する!
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
