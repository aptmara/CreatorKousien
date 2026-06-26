// ================================================================================
// File         : StaticCameraConfig.cs
// Author       : Iwai Shogo
//
// Description  : 固定カメラの投影モード毎の配置パラメータを保持するアセットデータ。
// Created      : 2026-06-02
// ================================================================================

using UnityEngine;

namespace Game.Gameplay.Cameras
{
    [CreateAssetMenu(fileName = "StaticCameraConfig", menuName = "Game/Camera/Static Camera Config")]
    public sealed class StaticCameraConfig : ScriptableObject
    {
        [System.Serializable]
        public struct ProjectionSettings
        {
            [Tooltip("カメラのワールド座標")]
            public Vector3 Position;

            [Tooltip("カメラの回転角")]
            public Vector3 Rotation;

            [Tooltip("Perspective用の視野角")]
            [Range(1f, 179f)] public float FieldOfView;

            [Tooltip("Orthographic時のカメラサイズ")]
            public float OrthographicSize;
        }

        [Header("Orthographic設定")]
        [SerializeField]
        private ProjectionSettings _orthoSettings = new ProjectionSettings
        {
            Position = new Vector3(0f, 15f, -15f),
            Rotation = new Vector3(45f, 0f, 0f),
            OrthographicSize = 5f
        };

        [Header("Perspective設定")]
        [SerializeField]
        private ProjectionSettings _perspSettings = new ProjectionSettings
        {
            Position = new Vector3(0f, 10f, -12f),
            Rotation = new Vector3(35f, 0f, 0f),
            FieldOfView = 60f
        };

        [Header("TileFloorPers設定")]
        [SerializeField]
        private ProjectionSettings _tiltFloorPers = new ProjectionSettings
        {
            Position = new Vector3(0f, 15f, -25f),
            Rotation = new Vector3(30f, 0f, 0f),
            FieldOfView = 60f
        };

        public ProjectionSettings OrthoSettings => _orthoSettings;
        public ProjectionSettings PerspSettings => _perspSettings;
        public ProjectionSettings TiltFloorPers => _tiltFloorPers;

        public ProjectionSettings GetSettings(CameraRigController.ProjectionMode mode)
        {
            switch (mode)
            {
                case CameraRigController.ProjectionMode.Orthographic:
                    return _orthoSettings;
                case CameraRigController.ProjectionMode.Perspective:
                    return _perspSettings;
                case CameraRigController.ProjectionMode.tiltFloorPers:
                    return _tiltFloorPers;
            }
            return _perspSettings;
        }
    }
}
