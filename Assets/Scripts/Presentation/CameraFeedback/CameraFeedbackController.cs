// 制作者: 山内陽
using UnityEngine;
using Game.Core.Events;
using Game.Gameplay.Cameras;
using System.Collections;

namespace Game.Presentation.CameraFeedback
{
    /// <summary>
    /// フィードバック演出を制御するクラス。
    /// ※「イベントカメラ（揺れ演出）」は「あらぶり」防止のため無効化されました。
    /// フォーカス機能は CameraRigController に統合されています。
    /// </summary>
    public class CameraFeedbackController : MonoBehaviour
    {
        private void Start()
        {
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }
    }
}
