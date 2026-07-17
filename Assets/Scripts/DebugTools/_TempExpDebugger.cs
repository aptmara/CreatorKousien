// ------------------------------------------------------------
// File		: _TempExpDebugger.cs
// Summary	: 動作確認用。経験値の発火とイベント受信をログする。
//
// Author	: [浅野 勇生]
// Created	: 2026-06-19
//
// Notes	:
// - 簡単なデバッグツール
// ------------------------------------------------------------
using Game.Core.Events;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.DebugTools
{
    /// <summary>
    /// シーン上の適当な空オブジェクトにアタッチして使う。
    /// </summary>
    public class _TempExpDebugger : MonoBehaviour
    {
        [Tooltip("Eキー1回で発火する経験値量")]
        [SerializeField] private float _expPerPress = 8f;

        [Tooltip("確認用：レベルアップ時に適用する強化SO")]
        [SerializeField] private Game.Data.Player.UpgradeData _testUpgrade;

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerExpChangedEvent>(OnExpChanged);
            EventBus.Subscribe<PlayerLeveledUpEvent>(OnLeveledUp);
            EventBus.Subscribe<UpgradeSelectionRequestedEvent>(OnUpgradeRequested);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerExpChangedEvent>(OnExpChanged);
            EventBus.Unsubscribe<PlayerLeveledUpEvent>(OnLeveledUp);
            EventBus.Unsubscribe<UpgradeSelectionRequestedEvent>(OnUpgradeRequested);
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                EventBus.Publish(new ExpGainedEvent(_expPerPress, "debug"));
                Debug.Log($"[TempExpDebugger] Rキー → ExpGainedEvent({_expPerPress}) を発火");
            }
#endif
        }

        private void OnExpChanged(PlayerExpChangedEvent e)
        {
            Debug.Log($"[TempExpDebugger] 経験値変化: Current={e.CurrentExp}, ToNext={e.ExpToNextLevel}, Ratio={e.Ratio:F2}");
        }

        private void OnLeveledUp(PlayerLeveledUpEvent e)
        {
            Debug.Log($"[TempExpDebugger] ●レベルアップ！ → Lv.{e.NewLevel}");
        }

        private void OnUpgradeRequested(UpgradeSelectionRequestedEvent e)
        {
            Debug.Log($"[TempExpDebugger] ▶強化選択リクエスト発行（Lv.{e.Level}）※あっきーらとたきせふのとこに飛ばす予定！");

            // 確認用: 本来はローグライク担当がやる「選択→適用」を仮で適用して動作確認してみます！
            var player = FindFirstObjectByType<Game.Gameplay.Player.PlayerFacade>();
            if (player != null && _testUpgrade != null)
            {
                player.ApplyUpgrade(_testUpgrade);
                Debug.Log($"[TempExpDebugger] 強化適用: MoveSpeedMultiplier={player.RuntimeData.MoveSpeedMultiplier}, AttachScale={player.RuntimeData.AttachmentScaleMultiplier}");
            }
        }
    }
}
