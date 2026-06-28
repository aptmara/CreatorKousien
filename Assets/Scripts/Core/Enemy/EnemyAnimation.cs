using UnityEngine;
// 制作者 越智晴彦


public class EnemyAnimation : MonoBehaviour
{
    // 使用するアニメーター
    [SerializeField]
    private Animator _animator;

    [Tooltip("ヒット時に増加するアニメーション継続時間の最大値")]
    [SerializeField] private float _maxHitAnimationTime;

    [Tooltip("1ヒットでのアニメーション継続時間の増加量")]
    [SerializeField] private float _addHitAnimationTime;
    // ヒットアニメーション継続時間、この値が0の状態でアニメーションが再生し終わると通常状態へ移行する
    float _hitAnimationTime;

    private void Update()
    {
        // 一応Nullチェック
        if (_animator == null)
        {
            Debug.Log(this.gameObject.name + "にAnimatorがついてないよ!");
            return;
        }
        // 継続時間を消費
        if (_hitAnimationTime > 0.0f)
        {
            _hitAnimationTime -= Time.deltaTime;
            _hitAnimationTime = Mathf.Max(_hitAnimationTime, 0.0f);
        }
        // アニメーターに数値を反映
        _animator.SetFloat("HitTime", _hitAnimationTime);
    }
    public void bodyHit()
    {
        // アニメーション継続時間を加算
        _hitAnimationTime += _addHitAnimationTime;
        _hitAnimationTime = Mathf.Min(_hitAnimationTime, _maxHitAnimationTime);
        // トリガーをオンにする
        _animator.SetTrigger("HitAnimeEvent");
    }

}
