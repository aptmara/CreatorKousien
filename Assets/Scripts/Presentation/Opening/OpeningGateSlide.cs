// ------------------------------------------------------------
// File		: OpeningGateSlide.cs
// Summary	: オープニングのゲートスライドを管理するクラス
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

namespace Game.Presentation.Opening
{
    /// <summary>
    /// ゲートの後ろから左右にモンスターが飛び出すスライド
    /// </summary>
    public sealed class OpeningGateSlide : OpeningSlideView
    {
        [Header("--- ゲート ---")]
        [SerializeField] private RectTransform _gate;

        [Tooltip("ゲートができるまでの時間(秒)")]
        [SerializeField, Min(0.01f)] private float _gateEnterDuration = 0.45f;

        [Tooltip("ゲートが下から何ピクセル上がってくるか")]
        [SerializeField] private float _gateRiseDistance = 60f;


        [Header("--- モンスター ---")]
        [Tooltip("左へ飛び出す個体。出したい順に並べる")]
        [SerializeField] private RectTransform[] _leftMonsters;

        [Tooltip("右へ飛び出す個体。出したい順に並べる")]
        [SerializeField] private RectTransform[] _rightMonsters;

        [Tooltip("一体が飛びきるまでの時間(秒)")]
        [SerializeField, Min(0.01f)] private float _monsterPopDuration = 0.40f;

        [Tooltip("次の一体が飛び出すまでの間隔(秒)")]
        [SerializeField, Min(0f)] private float _monsterPopInterval = 0.16f;

        [Tooltip("飛ぶときの弧の高さ(ピクセル)")]
        [SerializeField] private float _monsterArcHeight = 90f;

        [Tooltip("湧き出す位置")]
        [SerializeField] private Vector2 _monsterStartOffset = new Vector2(0f, -40f);


        // 左右を交互に並べ替えたモノ
        private RectTransform[] _monsters;
        private Vector2[] _monsterShownPositions;
        private CanvasGroup[] _monsterGroups;

        private Vector2 _gateShownPosition;
        private CanvasGroup _gateGroup;


        public override IEnumerator PlayEnterRoutine()
        {
            CacheLayoutIfNeeded();

            Group.alpha = 1f;
            HideAll();

            // 先にゲートを出す
            yield return GateEnterRoutine();

            // ゲートの後ろから左右交互に飛び出す
            for (int i = 0; i < _monsters.Length; i++)
            {
                StartCoroutine(PopMonsterRoutine(i));

                if (i < _monsters.Length - 1)
                {
                    yield return new WaitForSecondsRealtime(_monsterPopInterval);
                }
            }

            // 最後の一体が着地しきるまで待つ
            yield return new WaitForSecondsRealtime(_monsterPopDuration);
        }


        /// <summary>
        /// Inspectorで置いた最終位置を覚えておく
        /// </summary>
        private void CacheLayoutIfNeeded()
        {
            if (_monsters != null)
            {
                return;
            }

            int leftCount = _leftMonsters != null ? _leftMonsters.Length : 0;
            int rightCount = _rightMonsters != null ? _rightMonsters.Length : 0;
            int pairCount = Mathf.Max(leftCount, rightCount);

            // 左右交互に並べ替える
            List<RectTransform> ordered = new List<RectTransform>(leftCount + rightCount);
            for (int i = 0; i < pairCount; i++)
            {
                if (i < leftCount && _leftMonsters[i] != null)
                {
                    ordered.Add(_leftMonsters[i]);
                }

                if (i < rightCount && _rightMonsters[i] != null)
                {
                    ordered.Add(_rightMonsters[i]);
                }
            }

            // モンスターのキャンバスグループを取得
            _monsters = ordered.ToArray();
            _monsterShownPositions = new Vector2[_monsters.Length];
            _monsterGroups = new CanvasGroup[_monsters.Length];

            for (int i = 0; i < _monsters.Length; i++)
            {
                _monsterShownPositions[i] = _monsters[i].anchoredPosition;
                _monsterGroups[i] = GetOrAddGroup(_monsters[i]);
            }

            if (_gate != null)
            {
                _gateShownPosition = _gate.anchoredPosition;
                _gateGroup = GetOrAddGroup(_gate);
            }
        }


        private void HideAll()
        {
            if (_gateGroup != null)
            {
                _gateGroup.alpha = 0f;
            }

            for (int i = 0; i < _monsters.Length; i++)
            {
                _monsterGroups[i].alpha = 0f;
                _monsters[i].localScale = Vector3.zero;
            }
        }


        private IEnumerator GateEnterRoutine()
        {
            if (_gate == null)
            {
                yield break;
            }

            // ゲートの登場開始位置
            Vector2 startPosition = _gateShownPosition + Vector2.down * _gateRiseDistance;
            float elapsed = 0f;

            // ゲートの登場ルーチン
            while (elapsed < _gateEnterDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float eased = OpeningEase.SmoothStep(elapsed / _gateEnterDuration);

                _gate.anchoredPosition = Vector2.Lerp(startPosition, _gateShownPosition, eased);
                _gateGroup.alpha = eased;

                yield return null;
            }

            _gate.anchoredPosition = _gateShownPosition;
            _gateGroup.alpha = 1f;
        }


        private IEnumerator PopMonsterRoutine(int index)
        {
            RectTransform monster = _monsters[index];
            CanvasGroup group = _monsterGroups[index];
            Vector2 shownPosition = _monsterShownPositions[index];

            // ゲートの中心あたりから湧き出す
            Vector2 startPosition = _gateShownPosition + _monsterStartOffset;

            monster.anchoredPosition = startPosition;
            monster.localScale = Vector3.zero;
            group.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < _monsterPopDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _monsterPopDuration);
                float eased = OpeningEase.OutBack(t);

                // 横方向はOutBackで行き過ぎて戻り、縦方向にsinの山を足して弧を描かせる
                Vector2 flatPosition = Vector2.LerpUnclamped(startPosition, shownPosition, eased);
                float arc = Mathf.Sin(t * Mathf.PI) * _monsterArcHeight;
                monster.anchoredPosition = flatPosition + Vector2.up * arc;

                monster.localScale = Vector3.one * Mathf.LerpUnclamped(0f, 1f, eased);
                group.alpha = Mathf.Clamp01(t * 2.5f);

                yield return null;
            }

            // 最終的な位置を設定
            monster.anchoredPosition = shownPosition;
            monster.localScale = Vector3.one;
            group.alpha = 1f;
        }


        /// <summary>
        /// 指定したRectTransformにCanvasGroupを取得または追加します。
        /// </summary>
        /// <param name="target">座標</param>
        /// <returns>CanvasGroup</returns>
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
