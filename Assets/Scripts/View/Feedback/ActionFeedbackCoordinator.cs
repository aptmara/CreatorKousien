// ------------------------------------------------------------
// File		: ActionFeedbackCoordinator.cs
// Summary	: アクションに対するフィードバックを管理するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-20
//
// Notes	:
// - アクションに対するフィードバックを管理するクラス
// ------------------------------------------------------------
using UnityEngine;
using CreatorKousien.Core;

namespace CreatorKousien.View.Feedback
{
    /// <summary>
    /// バトル中の演出を管理するクラス。
    /// </summary>
    [RequireComponent(typeof(TimeFeedback), typeof(CameraFeedback))]
    public class ActionFeedbackCoordinator : MonoBehaviour
    {
        private TimeFeedback _timeFeedback;
        private CameraFeedback _cameraFeedback;

        /// <summary>
        /// 初期化処理。
        /// </summary>
        /// <param name="eventBus">登録するイベントバス</param>
        public void Initialize(GameEventBus eventBus)
        {
            _timeFeedback = GetComponent<TimeFeedback>();
            _cameraFeedback = GetComponent<CameraFeedback>();
            _cameraFeedback.Initialize();

            eventBus.OnDamageTaken += OnDamageTaken;
        }

        /// <summary>
        /// ダメージを受けたときのフィードバックを再生するイベントハンドラー。
        /// </summary>
        /// <param name="targetId">ターゲット</param>
        /// <param name="damage">ダメージ量</param>
        private void OnDamageTaken(int targetId, int damage)
        {
            // TODO: ダメージの量に応じてフィードバックの強さを変えるなど、よりリッチな演出にすることも検討

            // ダメージを受けたときのフィードバックを再生
            _timeFeedback.PlayHitStop();
            _cameraFeedback.PlayShake();
        }
    }
}
