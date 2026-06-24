/**
 * 作成：寺田晴
 * 
 * 内容：敵がフィールドまで上昇する処理
 * 
 * 
 */
using System;
using System.Collections;
using System.Xml.Serialization;
using UnityEngine;

namespace Game.Core.Enemy
{
    /// <summary>
    /// 敵の上昇処理を行う
    /// </summary>
    public class EnemyRising : MonoBehaviour
    {
        //　敵の目標地点
        private Vector3 _targetPosition;

        [Tooltip("上昇にかかる時間(調整中)")]
        [SerializeField] private float _riseDuration = 1.5f;
        [Tooltip("上昇の際の動き(調整中)")]
        [SerializeField] private AnimationCurve _riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // ゴールに到達した際のコールバック
        public Action OnEnemyReachedGoal;

        // 何らかの方法でゴールから引きはがされた際のコールバック
        public Action OnLeftReachedGoal;

        /// <summary>
        /// 初期化。インスタンス生成後に必ず呼ぶ。
        /// </summary>
        /// <param name="riseDuration">敵が上るのにかかる秒数</param>
        public void Initialize(float riseDuration)
        {
            _riseDuration = riseDuration;
        }

        /// <summary>
        /// 指定した目標地点へ敵を上昇させる。
        /// </summary>
        /// <param name="targetPos">上昇後に到達する目標座標。</param>
        /// <param name="startYOffset">開始時に目標位置から下方向へ下げる距離（正値で下方向へ移動）。</param>
        public void StartRise(Vector3 targetPos, float startYOffset, Transform enemyTransform)
        {
            _targetPosition = targetPos;
            // 上昇目標と目標までの距離から初期位置設定
            enemyTransform.position = _targetPosition + Vector3.down * startYOffset;
            // 上昇開始
            StartCoroutine(RiseRoutine(enemyTransform));
        }

        private IEnumerator RiseRoutine(Transform enemyTransform)
        {
            Vector3 startPos = enemyTransform.position;
            float elapsed = 0.0f;
            // カーブも考慮した目標地点までの滑らかな動き
            while (elapsed < _riseDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _riseDuration;
                float curveT = _riseCurve.Evaluate(t);
                //-イージングにより位置の更新
                enemyTransform.position = Vector3.Lerp(startPos, _targetPosition, curveT);
                yield return null;
            }
            //-イージング処理完了後目標地点に位置を補正
            enemyTransform.position = _targetPosition;
            // コールバックを発行
            OnEnemyReachedGoal();
        }
    }

}
