// ------------------------------------------------------------
// File		: BocchaHazardGimmickBase.cs
// Summary	: 石化キャンディ。着地後に地面へ少し埋まり、一定時間フィールドを塞ぐ
//
// Author	: [浅野 勇生]
// Created	: 2026-09-07
//
// Notes	:
// - ただの邪魔者らしい！
// - 一定時間鎮座するようにする！
// ------------------------------------------------------------
using Game.Core.Events;
using Game.Gameplay.Stage;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// 石化した巨大キャンディ
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BocchaStoneCandy : BocchaHazardBase
    {
        [Header("==== 石化キャンディ ====")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("着地時に地面へ埋まる深さ")]
        private float _sinkDepth = 0.4f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("埋まりきるまでの時間")]
        private float _sinkDuration = 0.25f;


        private Vector3 _sinkStartPosition;
        private Vector3 _sinkEndPosition;
        private float _sinkTimer;
        private bool _isSinking;

        // お邪魔アイテムの種類を返す
        public override BocchaHazardType HazardType => BocchaHazardType.StoneCandy;


        protected override void OnLanded()
        {
            Vector3 up = FieldContext.IsReady ? FieldContext.Up : Vector3.up;

            _sinkStartPosition = transform.position;
            _sinkEndPosition = transform.position - up * _sinkDepth;
            _sinkTimer = 0f;
            _isSinking = _sinkDepth > 0.0f && _sinkDuration > 0.0f;

            if (!_isSinking)
            {
                // 埋まらない場合は即座に埋まった位置にする
                transform.position = _sinkEndPosition;
            }
        }


        protected override void Update()
        {
            base.Update();

            if (!_isSinking)
                return;

            _sinkTimer += Time.deltaTime;

            float t = Mathf.Clamp01(_sinkTimer / _sinkDuration);

            transform.position = Vector3.Lerp(_sinkStartPosition, _sinkEndPosition, t);

            if (t < 1.0f)
                return;

            _isSinking = false;
        }
    }
}
