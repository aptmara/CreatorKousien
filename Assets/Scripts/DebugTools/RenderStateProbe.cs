// ------------------------------------------------------------
// File		: RenderStateProbe.cs
// Summary	: ビルドでしか再現しない描画不具合を調査するための描画状態を確認するデバッグツール
//
// Author	: [浅野 勇生]
// Created	: 2026-08-26
//
// Notes	:
// - ベース作成
// ------------------------------------------------------------
using UnityEngine;
using UnityEngine.Rendering.Universal;


namespace Game.DebugTools
{
    public sealed class RenderStateProbe : MonoBehaviour
    {
        [SerializeField] private float _interval = 1.0f; // 状態を確認する間隔（秒）

        private float _timer;

        private void Update()
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer < _interval)
            {
                return;
            }

            _timer = 0;


            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[RenderStateProbe] Main Cameraが見つかりません");
                return;
            }

            var data = cam.GetUniversalAdditionalCameraData();
            int stackCount = data.cameraStack != null ? data.cameraStack.Count : -1;
            int aliveCount = 0;
            if (data.cameraStack != null)
            {
                foreach (var c in data.cameraStack)
                {
                    if (c != null)
                    {
                        aliveCount++;
                    }
                }
            }

            Debug.Log(
                $"[Probe] Screen={Screen.width}x{Screen.height} mode={Screen.fullScreenMode} " +
                $"pixelRect={cam.pixelRect} enabled={cam.isActiveAndEnabled} " +
                $"renderType={data.renderType} stack={aliveCount}/{stackCount} " +
                $"pos={cam.transform.position} rot={cam.transform.eulerAngles.x:F1},{cam.transform.eulerAngles.y:F1} " +
                $"fov={cam.fieldOfView:F1} ortho={cam.orthographic} far={cam.farClipPlane}");
        }
    }
}

