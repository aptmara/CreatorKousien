using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ItemPhysicsStabilizer : MonoBehaviour
{
    private Rigidbody _rb;

    [Header("物理挙動の安定化設定")]
    [Tooltip("アイテム同士が重なった際、押し出そうとする力")]
    [SerializeField] private float _maxDepenetration = 1.0f;

    [Tooltip("アイテムが吹き飛ぶ最大速度")]
    [SerializeField] private float _maxSpeed = 10.0f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        _rb.maxDepenetrationVelocity = _maxDepenetration;

        _rb.linearDamping = 0.25f;
    }

    private void FixedUpdate()
    {
        Vector3 velocity = _rb.linearVelocity;

        // 水平方向の速度を制限
        Vector2 horizontalVelocity = new Vector2(velocity.x, velocity.z);
        if (horizontalVelocity.magnitude > _maxSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * _maxSpeed;
            velocity.x = horizontalVelocity.x;
            velocity.z = horizontalVelocity.y;
        }

        if (velocity.y > _maxSpeed)
        {
            velocity.y = _maxSpeed;
        }

        _rb.linearVelocity = velocity;
    }
}
