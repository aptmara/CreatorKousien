using System;
using System.Collections.Generic;
using UnityEngine;



namespace Game.Gameplay.Enemy.Boss
{
    public enum BossDirectionType
    {
        Left,
        Right,
        Top,
        Bottom,
        Custom,
    }

    [Serializable]
    public struct BossTransformPose
    {
        public BossDirectionType directionType;
        public Vector3 localPosition;
        public Vector3 eularRotation;
    }

    [Serializable]
    public class BossSpawnStepData
    {
        [Header("==== 識別情報 =====")]
        [SerializeField] private string _stepName = "Spawn Step";

        [Header("==== トランスフォーム設定(方向別) =====")]
        [SerializeField]
        private List<BossTransformPose> _spawnPose = new List<BossTransformPose>
        {
            new BossTransformPose {directionType = BossDirectionType.Left,localPosition = new Vector3(-10.0f,-5.0f,0.0f)},
            new BossTransformPose {directionType = BossDirectionType.Right,localPosition = new Vector3(-10.0f,-5.0f,0.0f),eularRotation = new Vector3(0.0f,90.0f,0.0f)},
        };

        [Header("==== アニメーション設定 ====")]
        [SerializeField, Min(0.01f)]
        private float _animationSpeed = 1.0f;

        [Header("===== 攻撃・属性フラグ ====")]
        [SerializeField]
        private bool _isThreatStep = true;

        [SerializeField]
        private string _customTag = "";

        public string StepName => _stepName;
        public float AnimationSpeed => _animationSpeed;
        public bool IsThreatStep => _isThreatStep;

        public BossTransformPose GetPose(BossDirectionType directionType)
        {
            var pose = _spawnPose.Find(p => p.directionType == directionType);
            return pose.directionType == directionType ? pose : (_spawnPose.Count > 0 ? _spawnPose[0] : default);
        }
    }


}
