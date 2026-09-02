// ------------------------------------------------------------
// File		: WaveTester.cs
// Summary	: ウェーブシステムのテスト用クラス
//
// Author	: [浅野勇生]
// Created	: 2026-07-15
//
// Notes	:
// - WaveTesterクラスは、ウェーブシステムのテストやデバッグに使用されるクラスです。
// ------------------------------------------------------------
using UnityEngine;
using System.Collections;
using Game.Core.Enemy;
using Game.Core.Management;
using Game.Presentation.GameClearCinematic;

namespace Game.WaveSystem
{
    public sealed class WaveTester : MonoBehaviour
    {
        [Header("--- テスト対象 ---")]
        [SerializeField]
        [Tooltip("スポーンして試したいWaveDataSO")]
        private WaveDataSO testWave;

        [Header("--- 参照 ---")]
        [SerializeField] private WaveRunner waveRunner;
        [SerializeField] private EnemySpawner enemySpawner;

        [Header("--- リザルトフローのテスト ---")]
        [SerializeField]
        [Tooltip("Wave完了後にクリア演出とリザルト遷移まで再生するかどうか")]
        private bool _playResultFlowOnClear;

        private Coroutine runningRoutine;

        public WaveDataSO TestWave => testWave;
        public bool IsWaveRunning => waveRunner != null && waveRunner.IsRunning;


        private void Awake()
        {
            if (waveRunner == null)
                waveRunner = Object.FindAnyObjectByType<WaveRunner>();

            if (enemySpawner == null)
                enemySpawner = Object.FindAnyObjectByType<EnemySpawner>();
        }


        /// <summary>
        /// テストウェーブをスポーンする
        /// </summary>
        public void SpawnTestWave()
        {
            if (!EnsureReferences()) return;

            if (testWave == null)
            {
                Debug.LogError("[WaveTester] Test Wave が設定されていません", this);
                return;
            }

            if (waveRunner.IsRunning)
            {
                Debug.LogWarning("[WaveTester] WaveRunnerが既に実行中です。新しいウェーブを開始する前に、現在のウェーブを停止してください", this);
                return;
            }

            runningRoutine = StartCoroutine(RunWaveRoutine());
        }


        /// <summary>
        /// テストウェーブを開始する
        /// </summary>
        /// <returns></returns>
        private System.Collections.IEnumerator RunWaveRoutine()
        {
            Debug.Log($"[WaveTester] Wave「{testWave.WaveName}」を開始します", this);
            yield return StartCoroutine(waveRunner.PlayWave(testWave, enemySpawner));

            // Waveが正常完了した場合はリザルトフローを再生する
            if (_playResultFlowOnClear && waveRunner.LastRunSucceeded)
            {
                yield return StartCoroutine(PlayResultFlowRoutine());
            }

            runningRoutine = null;
        }


        /// <summary>
        /// 本番の最終Waveクリアと同じ流れ（クリア演出→リザルト遷移）を再生する
        /// </summary>
        /// <returns></returns>
        private System.Collections.IEnumerator PlayResultFlowRoutine()
        {
            SoundManager.instance?.StopBGM();

            GameClearCinematicController cinematic = Object.FindFirstObjectByType<GameClearCinematicController>();

            if (cinematic != null)
            {
                yield return StartCoroutine(cinematic.PlayRoutine());
            }

            if (GameProgressionManagerBase.Instance != null)
            {
                GameProgressionManagerBase.Instance.GoToResult(true);
            }
            else
            {
                Debug.LogWarning("[WaveTester] GameProgressionManagerが見つからないため、リザルトへ遷移できません", this);
            }
        }


        /// <summary>
        /// 全ての敵を消去する
        /// </summary>
        public void ClearAllEnemies()
        {
            // 外側コルーチンを止める
            if (runningRoutine != null)
            {
                StopCoroutine(runningRoutine);
                runningRoutine = null;
            }

            // WaveRunner内部の状態を中断・リセットする
            if (waveRunner != null)
            {
                waveRunner.AbortAndReset();
            }

            // 生存敵を全部消す
            EnemyController[] enemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }
            }

            Debug.Log("[WaveTester] 全ての敵を消去しました", this);
        }



        /// <summary>
        /// 参照が正しく設定されているか確認する
        /// </summary>
        /// <returns></returns>
        private bool EnsureReferences()
        {
            if (waveRunner == null)
                waveRunner = Object.FindAnyObjectByType<WaveRunner>();
            if (enemySpawner == null)
                enemySpawner = Object.FindAnyObjectByType<EnemySpawner>();

            if (waveRunner == null)
            {
                Debug.LogError("[WaveTester] WaveRunnerが見つかりません", this);
                return false;
            }
            if (enemySpawner == null)
            {
                Debug.LogError("[WaveTester] EnemySpawnerが見つかりません", this);
                return false;
            }

            return true;
        }
    }
}

