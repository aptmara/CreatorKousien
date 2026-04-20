// ------------------------------------------------------------
// File		: TurnDisplayView.cs
// Summary	: ターン表示のUIを管理するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-20
//
// Notes	:
// - このクラスは、現在のターン数やフェーズをUIに表示する役割を担います。
// ------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CreatorKousien.Battle;
using CreatorKousien.Core;

namespace CreatorKousien.View.UI
{
    public class TurnDisplayView : MonoBehaviour
    {
        [Header("タイムラインの枠(P1, E1, P2, E2, P3, E3の順で登録)")]
        [SerializeField] private List<Image> timelineSlots;

        [Header("ターン数表示用のテキスト")]
        [SerializeField] private Sprite moveIcon;               /// ターン数表示用のアイコン(例: 歩アイコン)
        [SerializeField] private Sprite attackIcon;             /// ターン数表示用のアイコン(例: 攻撃アイコン)
        [SerializeField] private Sprite waitIcon;               /// ターン数表示用のアイコン(例: 待機アイコン)
        [SerializeField] private Sprite unknownIcon;            /// ターン数表示用のアイコン(例: 不明アイコン)

        [Header("アクションがない場合のアイコン")]
        [SerializeField] private Sprite emptyIcon;              /// アクションがない場合のアイコン
        [Header("実行中ハイライト設定")]
        [SerializeField] private Color timelineSlotColor = Color.white;
        [SerializeField] private Color activeTimelineSlotColor = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private Vector3 activeTimelineSlotScale = new Vector3(1.15f, 1.15f, 1f);

        private int _activeTimelineIndex = -1;

        /// <summary>
        /// 初期化
        /// </summary>
        public void Initialize(GameEventBus eventBus)
        {
            eventBus.OnTimelineUpdated += UpdateDisplay;
            eventBus.OnTimelineActionExecutionChanged += UpdateExecutionHighlight;
            ClearDisplay();
        }


        /// <summary>
        /// 描画を消すための関数。タイムラインの全てのスロットを初期状態に戻します。
        /// </summary>
        private void ClearDisplay()
        {
            if (timelineSlots == null) return;
            foreach (var slot in timelineSlots)
            {
                if (slot != null)
                {
                    slot.gameObject.SetActive(true);            // 常に表示しておく！
                    slot.sprite = emptyIcon;                    // デフォルトは「空の枠」
                    slot.color = new Color(timelineSlotColor.r, timelineSlotColor.g, timelineSlotColor.b, 0.3f);   // 未入力感を持たせるため半透明(Alpha 0.3)にする
                    slot.rectTransform.localScale = Vector3.one;
                }
            }
        }


        /// <summary>
        /// タイムラインの更新イベントを受け取って、UIを更新する関数
        /// </summary>
        /// <param name="enemyActions">敵のアクションリスト</param>
        /// <param name="playerActions">プレイヤーのアクションリスト</param>
        private void UpdateDisplay(List<ActionType> enemyActions, List<ActionType> playerActions)
        {
            ClearDisplay();

            // タイムラインのスロットはP1, E1, P2, E2, P3, E3の順で登録されていると仮定
            for (int i = 0; i < 3; i++)
            {
                // 先攻: プレイヤーのアイコンを表示
                int pSlotIndex = i * 2;
                if (pSlotIndex < timelineSlots.Count && timelineSlots[pSlotIndex] != null)
                {
                    if (i < playerActions.Count)
                    {
                        timelineSlots[pSlotIndex].sprite = GetIcon(playerActions[i]);
                        timelineSlots[pSlotIndex].color = timelineSlotColor;
                    }
                }

                // 後攻: 敵のアイコンを表示
                int eSlotIndex = i * 2 + 1;
                if (eSlotIndex < timelineSlots.Count && timelineSlots[eSlotIndex] != null)
                {
                    if (i < enemyActions.Count)
                    {
                        timelineSlots[eSlotIndex].sprite = GetIcon(enemyActions[i]);
                        timelineSlots[eSlotIndex].color = timelineSlotColor;
                    }
                }
            }

            ApplyExecutionHighlight();
        }


        /// <summary>
        /// アクションタイプに応じたアイコンを返す関数
        /// </summary>
        /// <param name="type">アクションタイプ</param>
        /// <returns>アイコンのスプライト</returns>
        private Sprite GetIcon(ActionType type)
        {
            switch (type)
            {
                case ActionType.Move: return moveIcon;
                case ActionType.FastAttack:
                case ActionType.WideAttack: return attackIcon;
                case ActionType.Wait:
                case ActionType.Guard: return waitIcon;
                default: return unknownIcon;
            }
        }

        /// <summary>
        /// 実行中のタイムライン枠を更新します。
        /// </summary>
        /// <param name="activeTimelineIndex">強調するタイムライン枠。強調解除時は-1</param>
        private void UpdateExecutionHighlight(int activeTimelineIndex)
        {
            _activeTimelineIndex = activeTimelineIndex;
            ApplyExecutionHighlight();
        }

        /// <summary>
        /// 現在の実行インデックスに応じてタイムラインの見た目を調整します。
        /// </summary>
        private void ApplyExecutionHighlight()
        {
            if (timelineSlots == null)
            {
                return;
            }

            for (int i = 0; i < timelineSlots.Count; i++)
            {
                Image slot = timelineSlots[i];
                if (slot == null)
                {
                    continue;
                }

                bool hasAction = slot.sprite != null && slot.sprite != emptyIcon;
                bool isActive = hasAction && i == _activeTimelineIndex;

                slot.color = hasAction
                    ? (isActive ? activeTimelineSlotColor : timelineSlotColor)
                    : new Color(timelineSlotColor.r, timelineSlotColor.g, timelineSlotColor.b, 0.3f);
                slot.rectTransform.localScale = isActive ? activeTimelineSlotScale : Vector3.one;
            }
        }
    }
}
