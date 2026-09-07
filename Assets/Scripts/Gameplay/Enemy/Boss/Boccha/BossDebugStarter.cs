// ------------------------------------------------------------
// File		: BossDebugStarter.cs
// Summary	: 動作確認用にボス戦を単体で開始させるコンポーネント
//
// Author	: [浅野 勇生]
// Created	: 2026-09-07
//
// Notes	:
// - 本番フローではEnemySpawnerがStartBattleを呼ぶため、
//   本番Prefabではこのコンポーネントを外すか、_startOnPlayをOFFにすること!!
// ------------------------------------------------------------
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// 動作確認用にボス戦を単体で開始させるコンポーネント。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossDebugStarter : MonoBehaviour
    {
        [Header("==== 参照 ====")]

        [SerializeField]
        [Tooltip("開始対象。未設定なら同じGameObjectから取得する")]
        private BossBattleFlowController _flowController;


        [Header("==== 開始設定 ====")]

        [SerializeField]
        [Tooltip("再生開始時に自動でボス戦を始めるかどうか")]
        private bool _startOnPlay = true;

        [SerializeField]
        [Min(0f)]
        [Tooltip("再生開始からボス戦を始めるまでの待ち時間")]
        private float _startDelay = 0.5f;

        [SerializeField]
        [Tooltip("デバッグ用のボス個体ID")]
        private string _debugBossInstanceId = "DEBUG_BOSS";


        private void Awake()
        {
            if (_flowController == null)
            {
                _flowController = GetComponent<BossBattleFlowController>();
            }
        }

        private void Start()
        {
            if (!_startOnPlay) return;

            Invoke(nameof(StartBattleNow), _startDelay);
        }

        /// <summary>
        /// ボス戦を開始する。
        /// </summary>
        [ContextMenu("ぱちチート/ボス戦を開始")]
        public void StartBattleNow()
        {
            if (_flowController == null)
            {
                Debug.LogError("[BossDebugStarter] BossBattleFlowControllerが設定されていません。", this);

                return;
            }

            if (!_flowController.StartBattle(_debugBossInstanceId))
            {
                Debug.LogWarning("[BossDebugStarter] ボス戦の開始に失敗しました。", this);

                return;
            }

            Debug.Log($"[BossDebugStarter] ボス戦を開始しました。(id: {_debugBossInstanceId})");
        }
    }
}
