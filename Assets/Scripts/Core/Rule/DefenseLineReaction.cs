using Game.Core.Events;
using Unity.Mathematics;
using UnityEngine;


namespace Game.Core.DefenceLine
{
    // 防衛ラインオブジェクトのリアクション用基底クラス。
    // リアクション関数の引数で専用構造体を受け取ることで、
    // 呼び出し処理とリアクション関数を完全に切り分け、
    // 呼び出し処理を基底クラスの変更のみで置き換え可能にする
    public class DefenseLineReaction : MonoBehaviour
    {
        protected struct HitReactionData
        {
            // 現状データが存在しないが、何かしら必要になった場合に向けて構造体だけ作成しておく
        }

        protected struct BreakReactionData
        {
            // 現状データが存在しないが、何かしら必要になった場合に向けて構造体だけ作成しておく
        }

        // イベントの登録
        private void OnEnable()
        {
            EventBus.Subscribe<DefLineHitReactionEvent>(OnPlayHitReaction);
            EventBus.Subscribe<DefLineBreakReactionEvent>(OnPlayBreakReaction);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DefLineHitReactionEvent>(OnPlayHitReaction);
            EventBus.Unsubscribe<DefLineBreakReactionEvent>(OnPlayBreakReaction);
        }

        // 呼び出し処理、現在はイベントで実装している。
        void OnPlayHitReaction(DefLineHitReactionEvent reactionEvent)
        {
            HitReactionData data = new HitReactionData();
            PlayHitReaction(data);
        }

        void OnPlayBreakReaction(DefLineBreakReactionEvent breakEvent)
        {
            BreakReactionData data = new BreakReactionData();
            PlayBreakReaction(data);
        }

        // 攻撃が命中した際のリアクション、ここをoverrideして実装することにより
        // 呼び出し方が変わっても問題なく実装できる
        protected virtual void PlayHitReaction(HitReactionData data)
        {
            // 初期では何もしない
        }

        // バリアを破壊された際のリアクション、ここをoverrideして実装することにより
        // 呼び出し方が変わっても問題なく実装できる
        protected virtual void PlayBreakReaction(BreakReactionData data)
        {
            // 初期では自身の描画を停止
            if (TryGetComponent<Renderer>(out var rendererComponent))
            {
                rendererComponent.enabled = false;
            }
        }
    }
}
