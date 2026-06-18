// ------------------------------------------------------------
// File		: PlayerStatsService.cs
// Summary	: プレイヤーのステータスを書き換えるサービス
//
// Author	: [浅野 勇生]
// Created	: 2026-06-19
//
// Notes	:
// - ローグライク担当(あっきーらとたきせふかな？)はApplyUpgrade()/ApplyModifier()を呼んで強化を適用する感じでお願いします！
//   Owner: MaxHp / MoveSpeed / AttachmentScaleMultiplierって感じです！
// ------------------------------------------------------------
using System.Collections.Generic;
using Game.Core.Events;
using Game.Data.Player;
using UnityEngine;

namespace Game.Gameplay.Player.Progression
{
    /// <summary>
    /// 強化によるステータス変化をPlayerRuntimeDataへ適用するサービス。
    /// 値を書き換えるのはこのクラスのみで今は考えてます！！
    /// </summary>
    public sealed class PlayerStatsService
    {
        private readonly PlayerRuntimeData _data;                                           ///< プレイヤーのRuntimeData
        private readonly PlayerStatsData _initialStats;                                     ///< プレイヤーの初期ステータス

        private readonly List<UpgradeData> _appliedUpgrades = new List<UpgradeData>();      ///< 適用された強化のリスト

        /// <summary>
        /// 適用された強化の読み取り専用リスト
        /// </summary>
        public IReadOnlyList<UpgradeData> AppliedUpgrades => _appliedUpgrades;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="data">ランタイムデータ</param>
        /// <param name="initialStats">初期ステータス</param>
        public PlayerStatsService(PlayerRuntimeData data, PlayerStatsData initialStats)
        {
            _data = data;
            _initialStats = initialStats;
        }


        /// <summary>
        /// UpgradeDataに基づいてステータス変化を適用する。複数のステータス変化がある場合はすべて適用される。
        /// </summary>
        /// <param name="upgrade">強化内容</param>
        public void ApplyUpgrade(UpgradeData upgrade)
        {
            if (upgrade == null)
            {
                Debug.LogWarning("[PlayerStatsService] null の UpgradeData が渡されました。");
                return;
            }

            if (upgrade.Modifiers != null)
            {
                foreach (StatModifier modifier in upgrade.Modifiers)
                {
                    ApplyModifierInternal(modifier);
                }
            }

            _appliedUpgrades.Add(upgrade);

            // 適用完了とステータス変化を通知
            EventBus.Publish(new UpgradeAppliedEvent(upgrade.Id));
            EventBus.Publish(new PlayerStatsChangedEvent());
        }


        /// <summary>
        /// 単発のステータス変化を適用する。
        /// SOには今ないステータス変化を適用したいときに使う想定です！なんかここはいい感じに調整してほしいです！
        /// </summary>
        /// <param name="modifier"></param>
        public void ApplyModifier(StatModifier modifier)
        {
            ApplyModifierInternal(modifier);
            EventBus.Publish(new PlayerStatsChangedEvent());
        }

        /// <summary>
        /// ステータスとレベルを初期状態へ戻し、強化履歴もクリアする。
        /// </summary>
        public void ResetStats()
        {
            _data.ResetToInitial(_initialStats);
            _appliedUpgrades.Clear();
            EventBus.Publish(new PlayerStatsChangedEvent());
        }


        /// <summary>
        /// ステータス変化をPlayerRuntimeDataへ適用する内部処理。ApplyUpgrade()とApplyModifier()の両方から呼ばれる。
        /// </summary>
        /// <param name="modifier"></param>
        private void ApplyModifierInternal(StatModifier modifier)
        {
            switch (modifier.TargetStat)
            {
                case PlayerStatType.MaxHp:
                    _data.MaxHp = Calculate(_data.MaxHp, modifier);
                    break;
                case PlayerStatType.MoveSpeed:
                    _data.MoveSpeedMultiplier = Calculate(_data.MoveSpeedMultiplier, modifier);
                    break;
                    break;
                case PlayerStatType.AttachmentScale:
                    _data.AttachmentScaleMultiplier = Calculate(_data.AttachmentScaleMultiplier, modifier);
                    break;
                default:
                    Debug.LogWarning($"[PlayerStatsService] 未対応の ステータス種別 です！: {modifier.TargetStat}");
                    break;
            }
        }


        /// <summary>
        /// 現在のステータス値とStatModifierを受け取り、ModifierOperationに基づいて新しいステータス値を計算して返す。
        /// </summary>
        /// <param name="current">現在のステータス値</param>
        /// <param name="modifier">適用するStatModifier</param>
        /// <returns>計算後の新しいステータス値</returns>
        private static float Calculate(float current, StatModifier modifier)
        {
            float result;
            switch (modifier.Operation)
            {
                case ModifierOperation.Add:
                    result = current + modifier.Value;
                    break;
                case ModifierOperation.Multiply:
                    result = current * modifier.Value;
                    break;
                default:
                    result = current;
                    break;
            }

            // 強化でマイナスにならないようガード
            return Mathf.Max(0f, result);
        }
    }
}
