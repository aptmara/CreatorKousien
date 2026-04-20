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

namespace CreatorKousien.Enemy
{
    /// <summary>
    /// 敵個体の思考ルーチンを実行するクラス
    /// </summary>
    public class EnemyAI
    {
        private EnemyRuntimeData _myData;               // この敵自体の現在状態
        private EnemyData _masterData;                  // 設計図 (行動パターン一覧)
        private int _localTurnCount = 0;

        // --- 内部ステート ---
        private Dictionary<EnemyActionPattern, int> _cooldownTimers = new Dictionary<EnemyActionPattern, int>();

        /// <summary>
        /// コンストラクタ: 敵がスポーンした時にEnemySystemから呼ばれる
        /// </summary>
        /// <param name="myData"></param>
        /// <param name="masterData"></param>
        /// <param name="telegraphSystem"></param>
        public EnemyAI(EnemyRuntimeData myData, EnemyData masterData)
        {
            _myData = myData;
            _masterData = masterData;

            // クールダウン辞書の初期化
            foreach (var pattern in _masterData.ActionPatterns)
            {
                _cooldownTimers[pattern] = 0;
            }
        }

        /// <summary>
        /// AIの思考ルーチン
        /// </summary>
        /// <param name="situation"></param>
        /// <returns></returns>
        public EnemyIntent Think(BattleSituation situation, Vector2Int virtualPos)
        {
            _localTurnCount++;
            TickCooldowns();    // 毎ターン、全スキルのクールダウンを1減らす

            // 抽選候補リスト
            List<(float weight, System.Func<EnemyIntent> actionFunc)> candidates = new List<(float, System.Func<EnemyIntent>)>();

            // 1. 発動可能な攻撃パターンを候補に追加
            foreach (var pattern in _masterData.ActionPatterns)
            {
                // 発動条件を満たしているか
                if (!CheckCondition(pattern, situation.PlayerPos)) continue;

                // クールダウン中ではないか
                if (_cooldownTimers[pattern] > 0) continue;

                // 基準点の絶対座標を算出
                Vector2Int originPos = GetOriginPosition(pattern.OriginRule, situation, virtualPos);

                // 基準点を元に実際の攻撃範囲を計算
                List<Vector2Int> rawCells = CalculateTargetCells(pattern, originPos, situation);

                // 自己基準なら、相対座標も計算しておく
                List<Vector2Int> relativeCells = new List<Vector2Int>();
                if (pattern.OriginRule == TargetOrigin.SelfPosition)
                {
                    relativeCells = CalculateTargetCells(pattern, Vector2Int.zero, situation);
                }

                // 有効なターゲットますが1つも無ければスキップして次の行動を考える
                if (rawCells.Count > 0)
                {
                    candidates.Add((pattern.Weight, (System.Func<EnemyIntent>)(() =>
                    {
                        _cooldownTimers[pattern] = pattern.CooldownTurns;
                        return EnemyIntent.CreateAttack(
                            _myData.ActorId,
                            pattern.Type,
                            pattern.Property,
                            rawCells,
                            pattern.ChargeTurns,
                            pattern.IsInterruptible,
                            pattern,
                            relativeCells
                        );
                    })));
                }
            }

            // 2. 移動を候補に追加
            Vector2Int randomMoveDir = GetRandomMoveDirection();
            candidates.Add((_masterData.MoveWeight, (System.Func<EnemyIntent>)(() => EnemyIntent.CreateMove(_myData.ActorId, randomMoveDir))));

            // 3. 待機を候補に追加
            candidates.Add((_masterData.WaitWeight, (System.Func<EnemyIntent>)(() => EnemyIntent.CreateWait(_myData.ActorId))));

            // 重み付きランダム抽選
            return ExecuteWeightedSelection(candidates);
        }

        /// <summary>
        /// 上下左右からランダムな方向を取得する
        /// </summary>
        /// <returns></returns>
        private Vector2Int GetRandomMoveDirection()
        {
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            return dirs[Random.Range(0, dirs.Length)];
        }

        private EnemyIntent ExecuteWeightedSelection(List<(float weight, System.Func<EnemyIntent> actionFunc)> candidates)
        {
            float totalWeight = 0;
            foreach (var c in candidates) totalWeight += c.weight;

            float randomValue = Random.Range(0, totalWeight);
            float currentWeight = 0;

            foreach (var c in candidates)
            {
                currentWeight += c.weight;
                if (randomValue <= currentWeight)
                {
                    return c.actionFunc.Invoke();
                }
            }
            return EnemyIntent.CreateWait(_myData.ActorId);
        }

        // 内部ロジック: 座標計算 & クリッピング
        // ============================================================

        /// <summary>
        /// 攻撃範囲の基準となるマスを算出する
        /// </summary>
        /// <param name="originRule"></param>
        /// <param name="situation"></param>
        /// <returns></returns>
        private Vector2Int GetOriginPosition(TargetOrigin originRule, BattleSituation situation, Vector2Int virtualPos)
        {
            switch (originRule)
            {
                case TargetOrigin.PlayerPosition:
                    return situation.PlayerPos;

                case TargetOrigin.FieldCenter:
                    return new Vector2Int(situation.MaxX / 2, situation.MaxY / 2);

                case TargetOrigin.FrontRowCenter:
                    return new Vector2Int(situation.MaxX / 2, 0);

                case TargetOrigin.BackRowCenter:
                    return new Vector2Int(situation.MaxX / 2, situation.MaxY);

                case TargetOrigin.LeftEdgeCenter:
                    return new Vector2Int(0, situation.MaxY / 2);

                case TargetOrigin.RightEdgeCenter:
                    return new Vector2Int(situation.MaxX, situation.MaxY / 2);

                case TargetOrigin.RandomPassableCell:
                    // プレイヤー陣地 (X座標が 0 〜 BorderX - 1) の中からランダムな座標を生成
                    return new Vector2Int(
                        Random.Range(0, situation.BorderX),
                        Random.Range(0, situation.MaxY + 1)
                    );

                default:
                    return virtualPos;
            }
        }

        /// <summary>
        /// EnemyActionPatternに応じて攻撃範囲のマスを決定する
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="origin"></param>
        /// <param name="situation"></param>
        /// <returns></returns>
        private List<Vector2Int> CalculateTargetCells(EnemyActionPattern pattern, Vector2Int origin, BattleSituation situation)
        {
            List<Vector2Int> result = new List<Vector2Int>();

            switch (pattern.TargetRule)
            {
                case TargetSelection.SingleCell:
                    result.Add(origin);
                    break;

                case TargetSelection.Cross:
                    result.Add(origin);
                    result.Add(new Vector2Int(origin.x + 1, origin.y));
                    result.Add(new Vector2Int(origin.x - 1, origin.y));
                    result.Add(new Vector2Int(origin.x, origin.y + 1));
                    result.Add(new Vector2Int(origin.x, origin.y - 1));
                    break;

                case TargetSelection.LocalGridShape:
                    // TargetOriginに合わせて基準点を取得
                    int centerIndex = GetLocalGridCenterIndex(pattern.OriginRule);
                    int cx = centerIndex % 5;
                    int cy = centerIndex / 5;

                    for (int i = 0; i < 25; i++)
                    {
                        if (pattern.LocalTargetGrid[i])
                        {
                            int x = i % 5;
                            int y = i / 5;

                            // 相対的なずれを計算
                            int dx = x - cx;
                            int dy = y - cy;

                            result.Add(new Vector2Int(origin.x + dx, origin.y + dy));
                        }
                    }
                    break;
            }

            return result;
        }

        /// <summary>
        /// TargetOrigin に応じて、5x5のグリッド内の基準点のインデックスを返す
        /// </summary>
        private int GetLocalGridCenterIndex(TargetOrigin originRule)
        {
            switch (originRule)
            {
                case TargetOrigin.FrontRowCenter: return 2;     // 一番上の真ん中
                case TargetOrigin.BackRowCenter: return 22;     // 一番下の真ん中
                case TargetOrigin.LeftEdgeCenter: return 10;    // 左端の真ん中
                case TargetOrigin.RightEdgeCenter: return 14;   // 右端の真ん中
                default: return 12;                             // それ以外はど真ん中
            }
        }

        // 内部ロジック: 行動の確定と条件判定
        // ============================================================

        /// <summary>
        /// スキルの発動条件を満たしているかチェックする
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        private bool CheckCondition(EnemyActionPattern pattern, Vector2Int playerPos)
        {
            switch (pattern.Condition)
            {
                case ConditionType.Always:
                    return true;

                case ConditionType.HpUnderPercent:
                    // 現在のHP / 最大HP * 100 でパーセンテージを計算
                    float currentHpPercent = ((float)_myData.CurrentHp / _masterData.MaxHp) * 100f;
                    return currentHpPercent <= pattern.ConditionValue;

                case ConditionType.TurnMultiple:
                    // 指定ターン周期
                    if (pattern.ConditionValue <= 0) return false;
                    return _localTurnCount % pattern.ConditionValue == 0;

                case ConditionType.PlayerInDistance:
                    // マンハッタン距離で計算
                    int distance = Mathf.Abs(_myData.Position.x - playerPos.x)
                                 + Mathf.Abs(_myData.Position.y - playerPos.y);
                    return distance <= pattern.ConditionValue;

                default:
                    return false;
            }
        }

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
