// ------------------------------------------------------------
// File		: ItemPhysicsStabilizer.cs
// Summary	: アイテムの物理挙動を安定化させるクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-05-24
//
// Notes	:
// - 5/24: ベース作成
// ------------------------------------------------------------
using UnityEngine;
using Game.Gameplay.Player;

[RequireComponent(typeof(Rigidbody))]
public class ItemPhysicsStabilizer : MonoBehaviour
{
    private Rigidbody _rb;

    [Header("物理挙動の安定化設定")]
    [Tooltip("アイテム同士が重なった際、押し出そうとする力")]
    [SerializeField] private float _maxDepenetration = 1.0f;

    [Tooltip("アイテムが吹き飛ぶ最大速度")]
    [SerializeField] private float _maxSpeed = 10.0f;

    [Header("プレイヤー接触時の吹き飛び制御")]
    [Tooltip("プレイヤーアタッチメント接触時の吹き飛び抑制を使うか")]
    [SerializeField] private bool _usePlayerAttachmentImpactLimit = true;

    [Tooltip("プレイヤーアタッチメントに当たった直後の最大水平速度")]
    [SerializeField] private float _playerAttachmentMaxSpeed = 2.5f;

    [Tooltip("吹き飛び抑制を続ける時間")]
    [SerializeField] private float _impactLimitDuration = 0.2f;

    [Tooltip("プレイヤーアタッチメント接触時に速度を弱める倍率")]
    [SerializeField, Range(0f, 1f)] private float _impactVelocityMultiplier = 0.35f;

    private float _impactLimitTimer;        ///< プレイヤーアタッチメント接触後の吹き飛び抑制タイマー

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        _rb.maxDepenetrationVelocity = _maxDepenetration;

        _rb.linearDamping = 0.25f;
    }

    private void FixedUpdate()
    {
        Vector3 velocity = _rb.linearVelocity;

        float currentMaxSpeed = _maxSpeed;

        if (_impactLimitTimer > 0f)
        {
            _impactLimitTimer -= Time.fixedDeltaTime;
            currentMaxSpeed = _playerAttachmentMaxSpeed;
        }

        // 水平方向の速度を制限
        Vector2 horizontalVelocity = new Vector2(velocity.x, velocity.z);
        if (horizontalVelocity.magnitude > currentMaxSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * currentMaxSpeed;
            velocity.x = horizontalVelocity.x;
            velocity.z = horizontalVelocity.y;
        }

        if (velocity.y > currentMaxSpeed)
        {
            velocity.y = currentMaxSpeed;
        }

        _rb.linearVelocity = velocity;
    }


    private void OnCollisionEnter(Collision collision)
    {
        TryStartPlayerAttachmentImpactLimit(collision);
    }


    private void TryStartPlayerAttachmentImpactLimit(Collision collision)
    {
        if (!_usePlayerAttachmentImpactLimit) return;
        if (_impactLimitTimer > 0f) return; // すでに制限中

        if (collision.transform.GetComponentInParent<PhysicalAttachment>() == null) return;

        _impactLimitTimer = _impactLimitDuration;

        Vector3 velocity = _rb.linearVelocity;
        velocity.x *= _impactVelocityMultiplier;
        velocity.z *= _impactVelocityMultiplier;
        _rb.linearVelocity = velocity;
    }
}
