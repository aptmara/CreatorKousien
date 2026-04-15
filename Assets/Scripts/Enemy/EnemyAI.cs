// ================================================================================
// File         : EnemyAI.cs
// Author       : Iwai Shogo
//
// Description  : 個別の敵の思考ロジックを担当するPure C#クラス。
//                毎ターン呼ばれ、EnemyDataのActionPatternsを評価して行動を決定します。
// Created      : 2026-04-13
//
// Note         : 盤面の情報は FieldService に問い合わせて計算します。
//                EnemySystemからインスタンス化されます。
// ================================================================================

using System.Collections.Generic;
using UnityEngine;
using CreatorKousien.Data;
using UnityEngine.Rendering;
// TODO: 他メンバーのマージ次第
// using CreatorKousien.Field;
// using CreatorKousien.Battle;

namespace CreatorKousien.Enemy
{
    /// <summary>
    /// 敵個体の思考ルーチンを実行するクラス
    /// </summary>
    public class EnemyAI
    {
        private EnemyRuntimeData _myData;               // この敵自体の現在状態
        private EnemyData _masterData;                  // 設計図 (行動パターン一覧)
        private AttackTelegraphSystem _telegraphSystem; // 予告を登録するカレンダー

        // --- 内部ステート ---
        private int _localTurnCount = 0;
        private Dictionary<EnemyActionPattern, int> _cooldownTimers = new Dictionary<EnemyActionPattern, int>();

        /// <summary>
        /// コンストラクタ: 敵がスポーンした時にEnemySystemから呼ばれる
        /// </summary>
        /// <param name="myData"></param>
        /// <param name="masterData"></param>
        /// <param name="telegraphSystem"></param>
        public EnemyAI(EnemyRuntimeData myData, EnemyData masterData, AttackTelegraphSystem telegraphSystem)
        {
            _myData = myData;
            _masterData = masterData;
            _telegraphSystem = telegraphSystem;

            // クールダウン辞書の初期化
            foreach (var pattern in _masterData.ActionPatterns)
            {
                _cooldownTimers[pattern] = 0;
            }
        }

        // TODO: 他ドメイン結合待ち
        // FieldService, PlayerRuntimeData, AttackCommand
        // ============================================================

        //public AttackCommand ? Think(FieldService fieldService, PlayerRuntimeData playerData)
        //{
        //    _localTurnCount++;
        //    TickCooldowns();    // 毎ターン、全スキルのクールダウンを1減らす

        //    // リストの上から優先して評価
        //    foreach (var pattern in _masterData.ActionPatterns)
        //    {
        //        // 1. 発動条件を満たしているか
        //        if (!CheckCondition(pattern, playerData)) continue;

        //        // 2. クールダウン中ではないか
        //        if (_cooldownTimers[pattern] > 0) continue;

        //        // 3. 基準点の絶対座標を算出
        //        Vector2Int originPos = GetOriginPosition(pattern.OriginRule, fieldService, playerData);

        //        // 4. 基準点を元に実際の攻撃範囲を計算
        //        List<Vector2Int> targetCells = CalculateTargetCells(pattern, originPos, fieldService);

        //        // 5. 有効なターゲットますが1つも無ければスキップして次の行動を考える
        //        if (targetCells.Count == 0) continue;

        //        // 6. 行動を決定し、カレンダーに登録
        //        return ExecuteAction(pattern, targetCells);
        //    }

        //    // どの条件も満たさなかった場合、何もしない
        //    return null;
        //}

        //// 内部ロジック: 座標計算 & クリッピング
        //// ============================================================

        ///// <summary>
        ///// 攻撃範囲の基準となるマスを算出する
        ///// </summary>
        ///// <param name="originRule"></param>
        ///// <param name="field"></param>
        ///// <param name="player"></param>
        ///// <returns></returns>
        //private Vector2Int GetOriginPosition(TargetOrigin originRule, FieldService field, PlayerRuntimeData player)
        //{
        //    // 番名の最大X, Yインデックスの取得
        //    int maxX = field.GridSizeX - 1;
        //    int maxY = field.GridSizeY - 1;

        //    switch (originRule)
        //    {
        //        case TargetOrigin.PlayerPosition:
        //            return player.Position;

        //        case TargetOrigin.FieldCenter:
        //            return new Vector2Int(maxX / 2, maxY / 2);

        //        case TargetOrigin.FrontRowCenter:
        //            return new Vector2Int(maxX / 2, 0);

        //        case TargetOrigin.BackRowCenter:
        //            return new Vector2Int(maxX / 2, maxY);

        //        case TargetOrigin.LeftEdgeCenter:
        //            return new Vector2Int(0, maxY / 2);

        //        case TargetOrigin.RightEdgeCenter:
        //            return new Vector2Int(maxX, maxY / 2);

        //        default:
        //            return player.Position;
        //    }
        //}

        //private List<Vector2Int> CalculateTargetCells(EnemyActionPattern pattern, Vector2Int origin, FieldService field)
        //{
        //    List<Vector2Int> result = new List<Vector2Int>();

        //    switch (pattern.TargetRule)
        //    {
        //        case TargetSelection.SingleCell:
        //            result.Add(origin);
        //            break;

        //        case TargetSelection.Cross:
        //            result.Add(origin);
        //            result.Add(new Vector2Int(origin.x + 1, origin.y));
        //            result.Add(new Vector2Int(origin.x - 1, origin.y));
        //            result.Add(new Vector2Int(origin.x, origin.y + 1));
        //            result.Add(new Vector2Int(origin.x, origin.y - 1));
        //            break;

        //        case TargetSelection.LocalGridShape:
        //            // TargetOriginに合わせて基準点を取得
        //            int centerIndex = GetLocalGridCenterIndex(pattern.OriginRule);
        //            int cx = centerIndex % 5;
        //            int cy = centerIndex / 5;

        //            for (int i = 0; i < 25; i++)
        //            {
        //                if (pattern.LocalTargetGrid[i])
        //                {
        //                    int x = i % 5;
        //                    int y = i / 5;

        //                    // 相対的なずれを計算
        //                    int dx = x - cx;
        //                    int dy = y - cy;

        //                    result.Add(new Vector2Int(origin.x + dx, origin.y + dy));
        //                }
        //            }
        //            break;
        //    }

        //    // 場外 & 障害物クリッピング
        //    result.RemoveAll(pos => !field.IsInBounds(pos) || !field.IsPassable(pos));

        //    return result;
        //}

        ///// <summary>
        ///// TargetOrigin に応じて、5x5のグリッド内の基準点のインデックスを返す
        ///// </summary>
        //private int GetLocalGridCenterIndex(TargetOrigin originRule)
        //{
        //    switch (originRule)
        //    {
        //        case TargetOrigin.FrontRowCenter: return 2;     // 一番上の真ん中
        //        case TargetOrigin.BackRowCenter: return 22;     // 一番下の真ん中
        //        case TargetOrigin.LeftEdgeCenter: return 10;    // 左端の真ん中
        //        case TargetOrigin.RightEdgeCenter: return 14;   // 右端の真ん中
        //        default: return 12;                             // それ以外はど真ん中
        //    }
        //}

        //// 内部ロジック: 行動の確定と条件判定
        //// ============================================================

        ///// <summary>
        ///// 決定した行動を実行し、クールダウンを適用する
        ///// </summary>
        ///// <param name="pattern"></param>
        ///// <param name="targets"></param>
        ///// <returns></returns>
        //private AttackCommand? ExecuteAction(EnemyActionPattern pattern, List<Vector2Int> targets)
        //{
        //    // クールダウン開始
        //    _cooldownTimers[pattern] = pattern.CooldownTurns;

        //    if (pattern.ChargeTurns > 0)
        //    {
        //        // 予告攻撃
        //        var telegraph = new TelegraphRuntimeData
        //        {
        //            // 重複しない固有IDを発行
        //            TelegraphId = _myData.ActorId * 1000 + _localTurnCount,
        //            SourceActorId = _myData.ActorId,
        //            TargetCells = targets,
        //            RemainingTurn = pattern.ChargeTurns,
        //            PatternType = pattern.AttackType,
        //            IsInterruptible = pattern.IsInterruptible
        //        };

        //        _telegraphSystem.RegisterTelegraph(telegraph);

        //        return null;
        //    }
        //    else
        //    {
        //        // 即時攻撃
        //        return new AttackCommand
        //        {
        //            SourceActorId = _myData.ActorId,
        //            PatternType = pattern.AttackType,
        //            TargetCells = targets,
        //        };
        //    }
        //}

        ///// <summary>
        ///// スキルの発動条件を満たしているかチェックする
        ///// </summary>
        ///// <param name="pattern"></param>
        ///// <param name="player"></param>
        ///// <returns></returns>
        //private bool CheckCondition(EnemyActionPattern pattern, PlayerRuntimeData player)
        //{
        //    switch (pattern.Condition)
        //    {
        //        case ConditionType.Always:
        //            return true;

        //        case ConditionType.HpUnderPercent:
        //            // 現在のHP / 最大HP * 100 でパーセンテージを計算
        //            float currentHpPercent = ((float)_myData.CurrentHp / _masterData.MaxHp) * 100f;
        //            return currentHpPercent <= pattern.ConditionValue;

        //        case ConditionType.TurnMultiple:
        //            // 指定ターン周期
        //            if (pattern.ConditionValue <= 0) return false;
        //            return _localTurnCount % pattern.ConditionValue == 0;

        //        case ConditionType.PlayerInDistance:
        //            // マンハッタン距離で計算
        //            int distance = Mathf.Abs(_myData.Position.x - player.Position.x)
        //                         + Mathf.Abs(_myData.Position.y - player.Position.y);
        //            return distance <= pattern.ConditionValue;

        //        default:
        //            return false;
        //    }
        //}

        private void TickCooldowns()
        {
            List<EnemyActionPattern> keys = new List<EnemyActionPattern>(_cooldownTimers.Keys);
            foreach (var key in keys)
            {
                if (_cooldownTimers[key] > 0)
                {
                    _cooldownTimers[key]--;
                }
            }
        }
    }
}
