/**
 * 作成：寺田晴
 * 
 * 内容：敵がフィールドまで上昇する処理
 * 
 * 
 */
using Game.Core.Enemy;
using System.Collections;
using UnityEngine;

namespace Game.Core.Enemy
{
    /// <summary>
    /// 敵の上昇処理を行う
    /// </summary>
    public class EnemyRising : MonoBehaviour
    {
        private EnemyController _controller;
        //　敵の目標地点
        private Vector3 _targetPosition;

        [Tooltip("上昇にかかる時間(調整中)")]
        [SerializeField] private float _riseDuration = 1.5f;
        [Tooltip("上昇の際の動き(調整中)")]
        [SerializeField] private AnimationCurve _riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        public void StartRise(EnemyController controller,Vector3 targetPos,float startYOffset)
        {
            // コントローラーと上昇目標の設定
            _controller = controller;
            _targetPosition = targetPos;
            // 上昇目標と目標までの距離から初期位置設定
            transform.position = _targetPosition + Vector3.down * startYOffset;
            // 上昇開始
            StartCoroutine(RiseRoutine());
        }

        private IEnumerator RiseRoutine()
        {
            Vector3 startPos = transform.position;
            float elapsed = 0.0f;
            // カーブも考慮した目標地点までの滑らかな動き
            while (elapsed < _riseDuration)
            {
                // 以下なぜかインスペクターの値が反映されない
                //-カーブの値を取得し、割合を計算
                elapsed += Time.deltaTime;
                float t = elapsed / _riseDuration;
                float curveT = _riseCurve.Evaluate(t);
                //-イージングにより位置の更新
                transform.position = Vector3.Lerp(startPos, _targetPosition, curveT);
                yield return null;
            }
            //-イージング処理完了後目標地点に位置を補正
            transform.position = _targetPosition;

            Debug.Log("[Rising] 到着");
        }
}

}
