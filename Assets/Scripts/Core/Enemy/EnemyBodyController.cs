using Game.Core.Enemy;
using UnityEngine;

public class EnemyBodyController : MonoBehaviour
{
    EnemyAnimation _animation;
    EnemyHitReceiver _receiver;


    public void Initialize(string enemyID)
    {
        // 各要素を取得
        _animation = GetComponent<EnemyAnimation>();
        _receiver = GetComponent<EnemyHitReceiver>();
        
        // リカバリーを初期化
        _receiver.Initialize(enemyID);
        _receiver.OnHitAction = HandleHitDamage;
    }

    void HandleHitDamage()
    {
        // アニメーションを再生
        _animation.bodyHit();
    }

}
