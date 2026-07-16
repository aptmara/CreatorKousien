// ------------------------------------------------------------
// File     : BossThornGroupController.cs
// Summary  : ボスに付いている複数の棘の抽選・復活・全破壊判定を管理する
//
// Author   : [浅野 勇生]
// Created  : 2026-07-16
//
// Notes:
// - 棘1本ごとのHPや破壊処理はBossThornへ任せる。
// - このクラスはフェーズ単位で有効にする棘を抽選する。
// - アングリバイト失敗時は、同じ棘の復活または再抽選を行う。
// - イバラタックル中のバリア攻撃可能時間もまとめて制御する。
// - ボス戦全体の状態遷移は、BossBattleControllerへ任せる。
// ------------------------------------------------------------
using System;
using System.Collections.Generic;
using Game.Data.Enemy.Boss;
using UnityEngine;


namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// ボスに付いている複数の棘をまとめて管理するコンポーネント。
    ///
    /// このクラスが担当するもの:
    /// ・子階層にあるBossThornの取得
    /// ・フェーズ開始時に有効な棘を抽選
    /// ・アングリバイト失敗後に棘を復活
    /// ・設定に応じた棘の再抽選
    /// ・有効な棘がすべて破壊されたかの判定
    /// ・棘のバリア攻撃可能時間の開始／終了
    /// ・BossThornHitReceiverへのボス個体ID設定
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossThornGroupController : MonoBehaviour
    {
        [Header("--- 棘の参照 ---")]

        [SerializeField]
        [Tooltip("棘のオブジェクトを格納するリスト")]
        private BossThorn[] _thorns = Array.Empty<BossThorn>();

        [SerializeField]
        [Tooltip("各棘についているヒット判定を受け取るコンポーネント")]
        private BossThornHitReceiver[] _thornHitReceivers = Array.Empty<BossThornHitReceiver>();

        [SerializeField]
        [Tooltip("各棘についている防衛バリアへの攻撃判定")]
        private BossThornBarrierAttacker[] _thornBarrierAttackers = Array.Empty<BossThornBarrierAttacker>();


        // ランタイム状態
        // ------------------------------------------------------------

        /// <summary>
        /// 現在のフェーズで選択されている棘のリスト
        /// </summary>
        private readonly HashSet<BossThorn> _selectedThorns = new HashSet<BossThorn>();

        /// <summary>
        /// 現在設定されているフェーズのデータ
        /// </summary>
        private BossPhaseData _currentPhaseData;

        /// <summary>
        /// 全棘破壊イベントを通知済みかどうかのフラグ
        /// </summary>
        private bool _hasNotifiedAllThornsBroken;


        // 公開プロパティ
        // ------------------------------------------------------------

        /// <summary>
        /// 現在のフェーズで選択されている棘の本数を取得する。
        /// </summary>
        public int ActiveThornCount => _selectedThorns.Count;

        /// <summary>
        /// 現在のフェーズで、まだ破壊されていない棘の本数を取得する
        /// </summary>
        public int RemainingActiveThornCount
        {
            get
            {
                int remainingCount = 0;

                foreach (BossThorn thorn in _selectedThorns)
                {
                    if (thorn != null || !thorn.IsBroken)
                    {
                        remainingCount++;
                    }
                }

                return remainingCount;
            }
        }


        /// <summary>
        /// 現在のフェーズで選択されている棘がすべて破壊されているかどうかを取得する
        /// </summary>
        public bool AreAllActiveThornsBroken
        {
            get
            {
                if (_selectedThorns.Count <= 0)
                {
                    return false;
                }

                foreach (BossThorn thorn in _selectedThorns)
                {
                    if (thorn == null || !thorn.IsBroken)
                    {
                        return false;
                    }
                }

                return true;
            }
        }


        // イベント
        // ------------------------------------------------------------

        /// <summary>
        /// 現在のフェーズで有効な棘が、すべて破壊されたときに発火するイベント
        /// </summary>
        public event Action AllActiveThornsBroken;


        // Unityイベント
        // ------------------------------------------------------------

        /// <summary>
        /// コンポーネント追加時に、子階層から棘の参照を取得する
        /// </summary>
        private void Reset()
        {
            FindReferences(true);
        }


        /// <summary>
        /// Inspector上で値が変更されたときに、棘の参照を取得し、設定の妥当性を確認する
        /// </summary>
        private void OnValidate()
        {
            FindReferences(false);
            ValidateReferences();
        }


        /// <summary>
        /// このコンポーネントがAwakeされるときに、棘の参照を取得し、設定の妥当性を確認する
        /// </summary>
        private void Awake()
        {
            FindReferences(false);
            ValidateReferences();

            if (_thorns == null || _thorns.Length <= 0)
            {
                Debug.LogWarning($"[{nameof(BossThornGroupController)}] 棘の参照が設定されていません。", this);

                enabled = false;
            }
        }


        /// <summary>
        /// このコンポーネントが有効化されるときに、棘の破壊イベントに購読する
        /// </summary>
        private void OnEnable()
        {
            SubscribeToThornEvents();
        }


        /// <summary>
        /// このコンポーネントが無効化されるときに、棘の破壊イベントの購読を解除する
        /// </summary>
        private void OnDisable()
        {
            UnsubscribeFromThornEvents();
            EndAttackStep();
        }


        // 参照の取得
        // ------------------------------------------------------------

        /// <summary>
        /// Inspector上で参照が設定されていない場合に、子階層から自動で参照を取得する
        /// </summary>
        /// <param name="forceRefresh">trueの場合、強制的に再取得する</param>
        private void FindReferences(bool forceRefresh)
        {
            // --- BossThornを取得する ---
            if (forceRefresh || _thorns == null || _thorns.Length <= 0)
            {
                _thorns = GetComponentsInChildren<BossThorn>(true);
            }

            // --- BossThornHitReceiverを取得する ---
            if (forceRefresh || _thornHitReceivers == null || _thornHitReceivers.Length <= 0)
            {
                _thornHitReceivers = GetComponentsInChildren<BossThornHitReceiver>(true);
            }

            // --- BossThornBarrierAttackerを取得する ---
            if (forceRefresh || _thornBarrierAttackers == null || _thornBarrierAttackers.Length <= 0)
            {
                _thornBarrierAttackers = GetComponentsInChildren<BossThornBarrierAttacker>(true);
            }

            // Inspector上でも確認しやすいように、棘IDの順番に並び変える
            if (_thorns != null && _thorns.Length > 1)
            {
                Array.Sort(_thorns, CompareThorns);
            }
        }


        /// <summary>
        /// BossThornをThornIdの小さい順にソートする
        /// </summary>
        /// <param name="left">比較する左の値</param>
        /// <param name="right">比較する右の値</param>
        /// <returns>並び順の比較結果</returns>
        private static int CompareThorns(BossThorn left, BossThorn right)
        {
            if (left == null && right == null)
            {
                return 0;
            }
            if (left == null)
            {
                return 1;
            }
            if (right == null)
            {
                return -1;
            }

            // ThornIdの昇順でソートする
            return left.ThornId.CompareTo(right.ThornId);
        }


        /// <summary>
        /// 棘関連コンポーネントの設定に不足や重複がないかを確認する
        /// </summary>
        private void ValidateReferences()
        {
            if (_thorns == null)
            {
                return;
            }

            // --- BossThornが設定されているか確認する ---
            for (int i = 0; i < _thorns.Length; i++)
            {
                BossThorn thorn = _thorns[i];

                if (thorn == null)
                {
                    Debug.LogWarning($"[{nameof(BossThornGroupController)}] 棘の参照が設定されていません。_thorns[{i}]がnullです。", this);

                    continue;
                }

                // --- ThornIdが重複していないか確認する ---
                for (int j = i + 1; j < _thorns.Length; j++)
                {
                    BossThorn otherThorn = _thorns[j];

                    if (otherThorn == null)
                    {
                        continue;
                    }

                    if (thorn.ThornId == otherThorn.ThornId)
                    {
                        Debug.LogWarning($"[{nameof(BossThornGroupController)}] 棘のIDが重複しています。_thorns[{i}]と_thorns[{j}]のThornIdが同じです。", this);
                    }
                }
            }

            // 棘の本数とヒット判定の本数が一致しているか確認する
            if (_thornHitReceivers != null && _thornHitReceivers.Length != _thorns.Length)
            {
                Debug.LogWarning($"[{nameof(BossThornGroupController)}] 棘の本数とヒット判定の本数が一致していません。_thorns.Length={_thorns.Length}, _thornHitReceivers.Length={_thornHitReceivers.Length}", this);
            }

            // 棘の本数とバリア攻撃判定の本数が一致しているか確認する
            if (_thornBarrierAttackers != null && _thornBarrierAttackers.Length != _thorns.Length)
            {
                Debug.LogWarning($"[{nameof(BossThornGroupController)}] 棘の本数とバリア攻撃判定の本数が一致していません。_thorns.Length={_thorns.Length}, _thornBarrierAttackers.Length={_thornBarrierAttackers.Length}", this);
            }
        }


        // イベント購読
        // ------------------------------------------------------------

        /// <summary>
        /// 全ての棘の破壊イベントに購読する
        /// </summary>
        private void SubscribeToThornEvents()
        {
            if (_thorns == null)
            {
                return;
            }

            foreach (BossThorn thorn in _thorns)
            {
                if (thorn == null)
                {
                    continue;
                }

                // 多重購読を防ぐため、一度購読を解除してから再度購読する
                thorn.Broken -= HandleThornBroke;
                thorn.Broken += HandleThornBroke;
            }
        }


        /// <summary>
        /// 全ての棘の破壊イベントから購読を解除する
        /// </summary>
        private void UnsubscribeFromThornEvents()
        {
            if (_thorns == null)
            {
                return;
            }

            foreach (BossThorn thorn in _thorns)
            {
                if (thorn != null)
                {
                    thorn.Broken -= HandleThornBroke;
                }
            }
        }


        /// <summary>
        /// いずれかの棘が破壊された際に、現在有効な棘がすべて破壊されたかどうかを確認する
        /// </summary>
        /// <param name="brokenThorn">破壊された棘</param>
        private void HandleThornBroke(BossThorn brokenThorn)
        {
            // 現在のフェーズで選ばれていない棘は判定に含めない
            if (brokenThorn == null || !_selectedThorns.Contains(brokenThorn))
            {
                return;
            }

            // 同じ挑戦中に全破壊イベントを複数回発火させないようにする
            if (_hasNotifiedAllThornsBroken)
            {
                return;
            }

            if (!AreAllActiveThornsBroken)
            {
                return;
            }

            _hasNotifiedAllThornsBroken = true;

            AllActiveThornsBroken?.Invoke();
        }


        // 初期化
        // ------------------------------------------------------------

        /// <summary>
        /// 各棘のヒット判定へボス個体IDを設定する
        /// </summary>
        /// <param name="bossInstanceId">Wave内でボスを一意に識別するための名前</param>
        public void Initialize(string bossInstanceId)
        {
            FindReferences(false);

            if (_thornHitReceivers == null)
            {
                return;
            }

            foreach (BossThornHitReceiver receiver in _thornHitReceivers)
            {
                if (receiver != null)
                {
                    receiver.Initialize(bossInstanceId);
                }
            }
        }


        // フェーズ開始
        // ------------------------------------------------------------

        /// <summary>
        /// 新しいフェーズを開始し、フェーズ設定に応じた本数の棘を抽選する
        /// </summary>
        /// <param name="phaseData">開始するフェーズの設定</param>
        /// <returns>棘を正常に開始できた場合はtrue</returns>
        public bool BeginPhase(BossPhaseData phaseData)
        {
            bool isConfigured = ConfigureThorns(phaseData, true);

            if (!isConfigured)
            {
                return false;
            }

            _currentPhaseData = phaseData;

            return true;
        }


        /// <summary>
        /// 指定されたフェーズデータを使用して棘の状態を復元する
        /// </summary>
        /// <param name="phaseData">再挑戦するフェーズの設定</param>
        /// <returns>棘を正常に復活できた場合はtrue</returns>
        public bool RestoreForRetry(BossPhaseData phaseData)
        {
            if (phaseData == null)
            {
                Debug.LogWarning($"[{nameof(BossThornGroupController)}] フェーズデータがnullです。", this);
                return false;
            }

            // 異なるフェーズデータが渡された場合は、棘の再抽選を行う
            bool isDifferentPhase = !object.ReferenceEquals(_currentPhaseData, phaseData);

            bool shouldReroll = isDifferentPhase || phaseData.RerollThornsOnRetry;

            bool isConfigured = ConfigureThorns(phaseData, shouldReroll);

            if (!isConfigured)
            {
                return false;
            }

            _currentPhaseData = phaseData;

            return true;
        }


        /// <summary>
        /// フェーズ設定を使用して各棘のHPと有効状態を設定する
        /// </summary>
        /// <param name="phaseData">使用するフェーズ設定</param>
        /// <param name="rerollThorns">有効な棘を新しく抽選するかどうか</param>
        /// <returns></returns>
        private bool ConfigureThorns(BossPhaseData phaseData, bool rerollThorns)
        {
            if (phaseData == null)
            {
                Debug.LogWarning($"[{nameof(BossThornGroupController)}] フェーズデータがnullです。", this);
                return false;
            }

            FindReferences(false);

            if (_thorns == null || _thorns.Length <= 0)
            {
                Debug.LogWarning($"[{nameof(BossThornGroupController)}] 棘の参照が設定されていません。", this);
                return false;
            }

            int activeThornCount = Mathf.Clamp(phaseData.ActiveThornCount, 1, _thorns.Length);

            // 保存中の抽選結果が使用できない場合も再抽選する
            if (rerollThorns || !IsCurrentSelectionValid(activeThornCount))
            {
                bool isSelected = SelectActiveThorns(activeThornCount);

                if (!isSelected)
                {
                    return false;
                }
            }

            // 新しい挑戦が始まるため、全破壊通知をリセットする
            _hasNotifiedAllThornsBroken = false;

            // 前回のイバラタックルの攻撃判定を終了する
            EndAttackStep();

            // --- 各棘のHPと有効状態を設定する ---
            foreach (BossThorn thorn in _thorns)
            {
                if (thorn == null)
                {
                    continue;
                }

                bool isCombatActive = _selectedThorns.Contains(thorn);

                thorn.Configure(phaseData.ThornMaxHp, isCombatActive);
            }

            return true;
        }


        // 棘の抽選
        // ------------------------------------------------------------

        /// <summary>
        /// 指定された本数の棘を重複なしで抽選する
        /// </summary>
        /// <param name="activeThornCount">有効にする棘の本数</param>
        /// <returns>抽選できた場合はtrue</returns>
        private bool SelectActiveThorns(int activeThornCount)
        {
            List<BossThorn> candidates = new List<BossThorn>(_thorns.Length);

            // null ではない棘を候補としてリストに追加する
            foreach (BossThorn thorn in _thorns)
            {
                if (thorn != null)
                {
                    candidates.Add(thorn);
                }
            }

            if (candidates.Count <= 0)
            {
                Debug.LogWarning($"[{nameof(BossThornGroupController)}] 有効な棘が存在しません。", this);

                return false;
            }

            int safeActiveThornCount = Mathf.Clamp(activeThornCount, 1, candidates.Count);

            if (safeActiveThornCount != activeThornCount)
            {
                Debug.LogWarning($"[{nameof(BossThornGroupController)}] 指定された有効棘数({activeThornCount})が候補棘数({candidates.Count})を超えていたため、{safeActiveThornCount}に調整しました。", this);
            }

            _selectedThorns.Clear();

            // ランダムに棘を選択する
            for (int i = 0; i < safeActiveThornCount; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, candidates.Count);

                BossThorn temporary = candidates[i];
                candidates[i] = candidates[randomIndex];
                candidates[randomIndex] = temporary;

                _selectedThorns.Add(candidates[i]);
            }

            return true;
        }


        /// <summary>
        /// 現在保存されている抽選結果を次も使用できるか確認する
        /// </summary>
        /// <param name="requiredCount"></param>
        /// <returns></returns>
        private bool IsCurrentSelectionValid(int requiredCount)
        {
            if (_selectedThorns.Count != requiredCount)
            {
                return false;
            }

            foreach (BossThorn thorn in _selectedThorns)
            {
                if (thorn == null)
                {
                    return false;
                }

                if (!ContainsThornReference(thorn))
                {
                    return false;
                }
            }

            return true;
        }


        /// <summary>
        /// 指定された棘が、管理対象の棘リストに含まれているかどうかを確認する
        /// </summary>
        /// <param name="targetThorn">確認する棘</param>
        /// <returns>管理対象に含まれている場合はtrue</returns>
        private bool ContainsThornReference(BossThorn targetThorn)
        {
            foreach (BossThorn thorn in _thorns)
            {
                if (thorn == targetThorn)
                {
                    return true;
                }
            }

            return false;
        }


        // イバラタックル
        // ------------------------------------------------------------

        /// <summary>
        /// 現在のイバラタックル段階を開始し、全ての棘のバリア攻撃判定を有効にする
        /// </summary>
        /// <param name="barrierDamagePerThorn">棘1本が防衛バリアへ与えるダメージ</param>
        /// <param name="canDamageBarrier">この攻撃段階でバリアへダメージを与えられるかどうか</param>
        public void BeginAttackStep(float barrierDamagePerThorn, bool canDamageBarrier)
        {
            if (_thornBarrierAttackers == null)
            {
                return;
            }

            foreach (BossThornBarrierAttacker attacker in _thornBarrierAttackers)
            {
                if (attacker != null)
                {
                    attacker.BeginAttackStep(barrierDamagePerThorn, canDamageBarrier);
                }
            }
        }


        /// <summary>
        /// 現在のイバラタックル段階を終了し、全ての棘のバリア攻撃判定を閉じる
        /// </summary>
        public void EndAttackStep()
        {
            if (_thornBarrierAttackers == null)
            {
                return;
            }

            foreach (BossThornBarrierAttacker attacker in _thornBarrierAttackers)
            {
                if (attacker != null)
                {
                    attacker.EndAttackStep();
                }
            }
        }


        // 棘全体の無効化
        // ------------------------------------------------------------

        /// <summary>
        /// すべての棘を一時的に戦闘対象外へ変更する
        /// </summary>
        public void DisableAllThorns()
        {
            EndAttackStep();

            if (_thorns == null)
            {
                return;
            }

            foreach (BossThorn thorn in _thorns)
            {
                if (thorn != null)
                {
                    thorn.SetCombatActive(false);
                }
            }
        }


        /// <summary>
        /// 現在のフェーズの棘の状態をリセットする。
        /// </summary>
        public void ClearBattleState()
        {
            DisableAllThorns();

            _selectedThorns.Clear();
            _currentPhaseData = null;
            _hasNotifiedAllThornsBroken = false;
        }
    }
}
