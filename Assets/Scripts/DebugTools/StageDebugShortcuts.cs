#if UNITY_EDITOR
// ------------------------------------------------------------
// File		: StageDebugShortcuts.cs
// Summary	: Stage進行の動作確認用ショートカット
//
// Author	: [浅野 勇生]
// Created	: 2026-08-16
//
// Notes	:
// - DebugOverlayシーンに置いて使います。
// - エディタ実行時のみ動作します。
// ------------------------------------------------------------
using Game.Core.Enemy;
using Game.Core.Management;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Core.Events;

namespace Game.DebugTools
{
    /// <summary>
    /// Stage進行を手早く確認するためのデバッグ用ショートカット
    /// </summary>
    public sealed class StageDebugShortcuts : MonoBehaviour
    {
        [Header("--- 有効化 ---")]
        [Tooltip("OFFにするとキー入力を受け付けません")]
        [SerializeField] private bool _isEnabled = true;

        [Header("--- 全滅キル ---")]
        [Tooltip("F1で敵に与えるダメージ量。敵のHPを確実に上回る値にしておきます")]
        [SerializeField] private float _debugKillDamage = 999999f;


        private void Update()
        {
#if UNITY_EDITOR
            if (!_isEnabled || Keyboard.current == null)
            {
                return;
            }

            // F1: 今のWaveの敵を全滅させて、次のWaveへ進める
            if (Keyboard.current.f1Key.wasPressedThisFrame)
            {
                KillAllEnemies();
            }

            // F2: リザルト画面から次のStageへ移行する
            if (Keyboard.current.f2Key.wasPressedThisFrame)
            {
                RequestNextStage();
            }

            // F3: 演出を飛ばして次のWaveへ進む
            if (Keyboard.current.f3Key.wasPressedThisFrame)
            {
                SkipToNextWave();
            }

            // F4: 演出を飛ばして最終Wave(Boss)へ飛ぶ
            if (Keyboard.current.f4Key.wasPressedThisFrame)
            {
                JumpToFinalWave();
            }

            // F6: 防衛ラインのHPを0にする
            if (Keyboard.current.f6Key.wasPressedThisFrame)
            {

            }

            // F7: バリア破壊演出を飛ばして
            if (Keyboard.current.f7Key.wasPressedThisFrame)
            {
                JumpToGameOver();
            }
#endif
        }


        /// <summary>
        /// シーン上の生存敵に致死ダメージを与えます。
        /// 通常の被弾経路を通すので、撃破演出・ドロップ・Wave完了判定がそのまま走ります。
        /// </summary>
        private void KillAllEnemies()
        {
            EnemyController[] enemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);

            if (enemies.Length == 0)
            {
                Debug.Log("[StageDebug] F1: 生存している敵がいません。");
                return;
            }

            foreach (EnemyController enemy in enemies)
            {
                if (enemy != null)
                {
                    enemy.OnBodyHit(_debugKillDamage);
                }
            }

            Debug.Log($"[StageDebug] F1: 敵{enemies.Length}体に致死ダメージを与えました。");
        }


        /// <summary>
        /// 次のStageへ移行します。リザルト表示中のみ有効です。
        /// </summary>
        private static void RequestNextStage()
        {
            if (GameProgressionManagerBase.Instance == null)
            {
                Debug.LogWarning("[StageDebug] F2: GameProgressionManagerが見つかりません。");
                return;
            }

            Debug.Log("[StageDebug] F2: 次のStageへの移行を要求します。");
            GameProgressionManagerBase.Instance.RequestNextStage();
        }


        /// <summary>
        /// 演出を飛ばして次のWaveへ進みます。バトル中のみ有効です。
        /// </summary>
        private static void SkipToNextWave()
        {
            if (GameProgressionManagerBase.Instance == null)
            {
                Debug.LogWarning("[StageDebug] F3: GameProgressionManagerが見つかりません。");
                return;
            }

            Debug.Log("[StageDebug] F3: 次のWaveへスキップします。");
            GameProgressionManagerBase.Instance.DebugSkipToNextWave();
        }


        /// <summary>
        /// 演出を飛ばして最終Wave(Boss)へ飛びます。バトル中のみ有効です。
        /// </summary>
        private static void JumpToFinalWave()
        {
            if (GameProgressionManagerBase.Instance == null)
            {
                Debug.LogWarning("[StageDebug] F4: GameProgressionManagerが見つかりません。");
                return;
            }

            Debug.Log("[StageDebug] F4: 最終Wave(Boss)へジャンプします。");
            GameProgressionManagerBase.Instance.DebugJumpToFinalWave();
        }

        /// <summary>
        /// 防衛ラインに即死するダメージを与えます
        /// </summary>
        private static void BreakDefenceline()
        {
            if (GameProgressionManagerBase.Instance == null)
            {
                Debug.LogWarning("[StageDebug] F6: GameProgressionManagerが見つかりません。");
                return;
            }

            Debug.Log("[StageDebug] F6: 防衛ラインを破壊します。");

            EventBus.Publish(new RuleBarrierAttackEvent(999999f, new Vector3()));
        }

        /// <summary>
        /// 防衛ラインに即死するダメージを与えます
        /// </summary>
        private static void JumpToGameOver()
        {
            //if (GameProgressionManagerBase.Instance == null)
            //{
            //    Debug.LogWarning("[StageDebug] F7: GameProgressionManagerが見つかりません。");
            //    return;
            //}

            //Debug.Log("[StageDebug] F7: ゲームオーバー演出へ遷移します");
            //GameProgressionManagerBase.Instance.DebugJumpToGameOver();
            EventBus.Publish(new DefLineBreakReactionEvent(90000, new Vector3()));

        }
    }
}
#endif
