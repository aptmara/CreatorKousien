// 制作者: 山内陽
using System;

namespace Game.Core.Enemy
{
    /// <summary>
    /// 敵の状態種別。
    /// 新しい状態（Enraged等）を追加する際はここに追記し、EnemyControllerのswitch文に対応ケースを足す。
    /// </summary>
    public enum EnemyState
    {
        /// <summary>通常：ゲージが自然増加し、ゲージダメージを受け付ける</summary>
        Normal,
        /// <summary>ダウン：本体HPへのダメージを受け付ける。ゲージは停止。</summary>
        Down,
        /// <summary>撃破済み：一切の状態遷移・ダメージを受け付けない</summary>
        Defeated,
    }

    /// <summary>
    /// 敵の状態遷移を管理する純粋クラス（MonoBehaviour非依存）。
    /// 遷移の正当性チェック（Defeatedからの遷移防止等）をここに集約し、
    /// EnemyControllerが複数のコンポーネントを誤った順序で呼ぶことを防ぐ。
    /// </summary>
    public class EnemyStateManager
    {
        /// <summary>現在の状態</summary>
        public EnemyState CurrentState { get; private set; } = EnemyState.Normal;

        /// <summary>状態変化時に発火するコールバック。引数は新しい状態。</summary>
        public event Action<EnemyState> OnStateChanged;

        /// <summary>ダウン中かどうか</summary>
        public bool IsDown => CurrentState == EnemyState.Down;

        /// <summary>撃破済みかどうか</summary>
        public bool IsDefeated => CurrentState == EnemyState.Defeated;

        /// <summary>ゲージダメージを受け付けられるか（Normal状態のみ）</summary>
        public bool CanReceiveGaugeDamage => CurrentState == EnemyState.Normal;

        /// <summary>本体ダメージを受け付けられるか（Down状態のみ）</summary>
        public bool CanReceiveBodyDamage => CurrentState == EnemyState.Down;

        /// <summary>
        /// 状態を遷移させる。
        /// Defeated後の遷移、および同一状態への遷移は無視する。
        /// </summary>
        /// <param name="newState">遷移先の状態</param>
        public void TransitionTo(EnemyState newState)
        {
            // 撃破済みからは一切遷移しない（最終状態）
            if (CurrentState == EnemyState.Defeated) return;
            // 同じ状態への遷移は無視
            if (CurrentState == newState) return;

            var prev = CurrentState;
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);

            UnityEngine.Debug.Log($"[EnemyStateManager] {prev} → {newState}");
        }
    }
}
