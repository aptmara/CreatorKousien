/**
 * 作成：寺田晴
 * 
 * 内容：敵がフィールドまで上昇する処理
 * 
 * 
 */
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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

        //　敵の開始地点
        private Vector3 _startPosition;

        [Tooltip("上昇にかかる時間(調整中)")]
        [SerializeField] private float _riseDuration = 1.5f;
        [Tooltip("上昇にかかる時間(調整中)")]
        [SerializeField] private float _dropDuration = 5.0f;

        [Tooltip("上昇の際の動き(調整中)")]
        [SerializeField] private AnimationCurve _riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Tooltip("ずり落ちの際の動き(調整中)")]
        [SerializeField] private AnimationCurve _downCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Tooltip("落下の際の動き")]
        [SerializeField] private AnimationCurve _dropCurve = AnimationCurve.Linear(0, 0, 1, 1);

        // ゴールに到達した際のコールバック
        public Action OnEnemyReachedGoal;

        // 何らかの方法でゴールから引きはがされた際のコールバック
        public Action OnLeftReachedGoal;

        public Action OnEnemyDroped;

        // 進行度
        float _elapsed = 0.0f;



        Coroutine _riseCoroutine = null;
        Coroutine _dropCoroutine = null;

        /// <summary>
        /// 初期化。インスタンス生成後に必ず呼ぶ。
        /// </summary>
        /// <param name="riseDuration">敵が上るのにかかる秒数</param>
        public void Initialize(float riseDuration, float dropDuration, AnimationCurve riseCurve, AnimationCurve dropCurve)
        {
            if (_riseCoroutine != null) StopCoroutine(_riseCoroutine);
            if (_dropCoroutine!= null) StopCoroutine(_dropCoroutine);
            _riseCoroutine = null;
            _dropCoroutine = null;
            _riseDuration = riseDuration;
            _dropDuration = dropDuration;
            if(riseCurve != null) _riseCurve = riseCurve;

            if(dropCurve != null)_dropCurve = dropCurve;
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
            _startPosition = _targetPosition + Vector3.down * startYOffset;
            enemyTransform.position = _startPosition;
            _elapsed = 0.0f;
            // 上昇開始
            _riseCoroutine = StartCoroutine(RiseRoutine(enemyTransform));
        }

        private IEnumerator RiseRoutine(Transform enemyTransform)
        {

            // カーブも考慮した目標地点までの滑らかな動き
            while (_elapsed < 1.0f)
            {
                _elapsed += Time.deltaTime / _riseDuration;
                _elapsed = Mathf.Clamp(_elapsed, 0.0f, 1.0f);
                float t = _elapsed;
                float curveT = _riseCurve.Evaluate(t);
                curveT = Mathf.Clamp(curveT, 0.0f, 1.0f);
                //-イージングにより位置の更新
                enemyTransform.position = Vector3.Lerp(_startPosition, _targetPosition, curveT);
                yield return null;
            }
            //-イージング処理完了後目標地点に位置を補正
            enemyTransform.position = _targetPosition;
            // コールバックを発行
            OnEnemyReachedGoal();
        }

        public void DropStart(Transform enemyTransform)
        {
            if(_riseCoroutine != null) StopCoroutine(_riseCoroutine);
            _riseCoroutine = null;
            // 現在の進行度の値を上昇カーブから見た進行度から落下カーブから見た進行度に変換する
            float currentValue = _riseCurve.Evaluate(_elapsed);
            _elapsed = ValueToCurveTime(currentValue, _dropCurve);
            float newValue = _dropCurve.Evaluate(_elapsed);
            Debug.Log("newValue = " + newValue + " oldValue = " + currentValue ); 
            // 落下開始
            _dropCoroutine = StartCoroutine(DropRoutine(enemyTransform));
        }

        private IEnumerator DropRoutine(Transform enemyTransform)
        {

            while (_elapsed > 0.0f)
            {
                _elapsed -= Time.deltaTime / _dropDuration;
                _elapsed = Mathf.Clamp(_elapsed, 0.0f, 1.0f);
                float curveT = _dropCurve.Evaluate(_elapsed);
                curveT = Mathf.Clamp(curveT, 0.0f, 1.0f);
                //-イージングにより位置の更新
                enemyTransform.position = Vector3.Lerp(_startPosition, _targetPosition, curveT);
                yield return null;
            }

            //-イージング処理完了後開始地点に位置を補正
            enemyTransform.position = _startPosition;
            OnEnemyDroped();
        }


        private float ValueToCurveTime(float value, AnimationCurve curve)
        {
            float minValue = 0.0f;
            float maxValue = 1.0f;

            for (int i = 0; i < 15; i++)
            {
                if (!FindValueRangeByStepping(out minValue, out maxValue, value, 3, curve, minValue, maxValue)) break;
            }
            return (minValue + maxValue) / 2.0f;
        }




        private bool FindValueRangeByStepping(out float outMinPoint, out float outMaxPoint, float value, int findCount, AnimationCurve curve, float findMinPoint, float findMaxPoint)
        {

            // 前から探索するか後ろから探索するかを値が半分以上進んでいるかどうかで推定(カーブの曲がり方によって最高率ではなくなるがないよりマシ)
            bool isFowerdFind = value < (findMaxPoint + findMinPoint) * 0.5f;

            // 1ループでの探索範囲を決める
            float step;
            float startPoint;
            float endPoint;
            if (isFowerdFind)
            {
                // 前方向に探索する
                step = (findMaxPoint - findMinPoint) / (float)findCount;
                startPoint = findMinPoint;
                endPoint = findMaxPoint;
            }
            else
            {
                // 後ろ方向に探索する
                step = ((findMaxPoint - findMinPoint) * -1.0f) / (float)findCount;
                startPoint = findMaxPoint;
                endPoint = findMinPoint;
            }

            int i;
            float currentPoint;
            for (i = 0, currentPoint = startPoint; i < findCount; i++, currentPoint += step)
            {
                // 値を取る
                float nextPoint = currentPoint + step;
                float currentValue = curve.Evaluate(currentPoint);
                float nextValue = curve.Evaluate(nextPoint);
                
                if(isFowerdFind)
                {
                    // 前方向に探索、範囲内か確認
                    if(currentValue <= value && value <= nextValue )
                    {
                        outMinPoint = currentPoint;
                        outMaxPoint = nextPoint;
                        return true;
                    }
                }
                else
                {
                    // 後方向に探索している場合小さくなっていくため符号や戻り値が逆になる
                    if(currentValue >= value && value >= nextValue)
                    {
                        outMaxPoint = currentPoint;
                        outMinPoint = nextPoint;
                        return true;
                    }
                }
            }
            // 探索中見つからなかった場合失敗を返す
            outMinPoint = findMinPoint;
            outMaxPoint = findMaxPoint;
            Debug.Log("探索失敗！");
            return false;
        }
    }
}
