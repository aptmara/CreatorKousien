using Game.Gameplay.Enemy.Boss;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BalanceCatapultBall : MonoBehaviour
{
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private BossBattleFlowController _battleFlowController;
    [SerializeField] private float _baseDamage = 5.0f;
    [SerializeField] private float _speedDamageMultiplier = 2.0f;
    [Tooltip("これ未満の速度での接触はダメージ無し")]
    [SerializeField] private float _minLaunchSpeed = 3.0f;
    [SerializeField] private LayerMask _bossLayer;

    private void Awake()
    {
        if(_rigidbody == null )_rigidbody = GetComponent<Rigidbody>();
    }

    public void Initialize(BossContext context)
    {
        _battleFlowController = context.Controller;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & _bossLayer) == 0) return;
        if (!collision.transform.GetComponentInParent<BossHitReceiver>()) return;

        float speed = _rigidbody.linearVelocity.magnitude;
        if (speed < _minLaunchSpeed) return;

        float damage = _baseDamage + speed * _speedDamageMultiplier;
        _battleFlowController.TakeDamage(damage);
    }
}
