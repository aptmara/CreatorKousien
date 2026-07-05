using Game.Core.Enemy;
using UnityEngine;
using Game.Core.Events;
using Unity.Mathematics;

public class EnemyBodyController : MonoBehaviour
{
    EnemyAnimation _animation;
    EnemyHitReceiver _receiver;
    EnemyBodyPose _pose;
    string _enemyID;


    private void OnEnable()
    {
        EventBus.Subscribe<EnemyDropEvent>(OnDropBatch);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<EnemyDropEvent>(OnDropBatch);
    }

    public void Initialize(string enemyID)
    {
        _enemyID = enemyID;

        // 各要素を取得
        _animation = GetComponent<EnemyAnimation>();
        _receiver = GetComponent<EnemyHitReceiver>();
        _pose = GetComponent<EnemyBodyPose>();


        // リカバリーを初期化
        _receiver.Initialize(enemyID);
        _receiver.OnHitAction = HandleHitDamage;
    }

    void HandleHitDamage()
    {
        // アニメーションを再生
        _animation.bodyHit();
    }

    void OnDropBatch(EnemyDropEvent dropEvent)
    {
        if (dropEvent.EnemyId != _enemyID) return;

        _pose.DropPose(transform);
    }
}
