/**
 * 作成：寺田晴
 * 
 * 内容：敵がフィールドまで上昇する処理
 * 
 * 追加実装 : 落下、ずり落ちを追加
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

        // ずり落ちステータス
        float _breakDropStart = 0.0f;
        float _breakDropTarget = 0.0f;
        float _breakDropDistance = 0.2f;

        float _damageDropStart = 0.0f;
        float _damageDropTarget = 0.0f;
        float _damageDropDistance = 0.02f;

        [Tooltip("上昇にかかる時間(調整中)")]
        [SerializeField] private float _riseDuration = 1.5f;
        [Tooltip("落下にかかる時間(調整中)")]
        [SerializeField] private float _dropDuration = 5.0f;
        [Tooltip("ずり落ちにかかる時間(調整中)")]
        [SerializeField] private float _barrierBreakDuration = 5.0f;
        [Tooltip("被弾ずり落ちにかかる時間(調整中)")]
        [SerializeField] private float _damageDropDuration = 0.02f;

        [Tooltip("上昇の際の動き")]
        [SerializeField] private AnimationCurve _riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Tooltip("ずり落ちの際の動き")]
        [SerializeField] private AnimationCurve _barrierBreakCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Tooltip("被ダメージ時の動き")]
        [SerializeField] private AnimationCurve _damageDropCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Tooltip("落下の際の動き")]
        [SerializeField] private AnimationCurve _dropCurve = AnimationCurve.Linear(0, 0, 1, 1);

        // ゴールに到達した際のコールバック
        public Action OnEnemyReachedGoal;

        // 何らかの方法でゴールから引きはがされた際のコールバック
        public Action OnLeftReachedGoal;

        public Action OnEnemyDroped;

        // 進行時間
        float _elapsedTime = 0.0f;
        // 落下進行時間
        float _dropElapsedTime = 0.0f;
        // バリア破壊ずり落ち時間
        float _breakDropElapsedTime = 0.0f;

        float _damageDropElapsedTime = 0.0f;

        // 各コルーチン
        Coroutine _riseCoroutine = null;
        Coroutine _dropCoroutine = null;
        Coroutine _barrierBreakCoroutine = null;
        Coroutine _damageDropCoroutine = null;


        /// <summary>
        /// 初期化。インスタンス生成後に必ず呼ぶ。
        /// </summary>
        /// <param name="riseDuration">敵が上るのにかかる秒数</param>
        public void Initialize(float riseDuration, float dropDuration, float breakDuration, float damageDuration,
                               AnimationCurve riseCurve, AnimationCurve dropCurve, AnimationCurve breakCurve, AnimationCurve damageCurve,
                               float breakDropDistance, float damageDropDistance)
        {
            if (_riseCoroutine != null) StopCoroutine(_riseCoroutine);
            if (_dropCoroutine!= null) StopCoroutine(_dropCoroutine);
            if (_barrierBreakCoroutine != null) StopCoroutine(_barrierBreakCoroutine);
            _riseCoroutine = null;
            _dropCoroutine = null;
            _barrierBreakCoroutine = null;
            // 値の設定
            _riseDuration = riseDuration;
            _dropDuration = dropDuration;
            _barrierBreakDuration = breakDuration;
            _damageDropDuration = damageDuration;

            _riseCurve = riseCurve;
            _dropCurve = dropCurve;
            _barrierBreakCurve = breakCurve;
            _damageDropCurve = damageCurve;

            _breakDropDistance = breakDropDistance;
            _damageDropDistance = damageDropDistance;
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
            _elapsedTime = 0.0f;
            // 上昇開始
            _riseCoroutine = StartCoroutine(RiseRoutine(enemyTransform));
        }


        /// <summary>
        /// 上昇のコルーチン
        /// </summary>
        /// <param name="enemyTransform"></param>
        /// <returns>コルーチン用の戻り値 </returns>
        private IEnumerator RiseRoutine(Transform enemyTransform)
        {

            // カーブも考慮した目標地点までの滑らかな動き
            while (_elapsedTime < 1.0f)
            {
                _elapsedTime += Time.deltaTime / _riseDuration;
                _elapsedTime = Mathf.Clamp(_elapsedTime, 0.0f, 1.0f);
                float t = _elapsedTime;
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

        /// <summary>
        /// 落下を開始する
        /// </summary>
        /// <param name="enemyTransform"></param>
        public void DropStart(Transform enemyTransform)
        {
            float currentValue;

            if (_barrierBreakCoroutine != null)
            {
                StopCoroutine(_barrierBreakCoroutine);
                float finalCurve = _barrierBreakCurve.Evaluate(_breakDropElapsedTime);
                finalCurve = Mathf.Clamp(finalCurve, 0.0f, 1.0f);
                currentValue = Mathf.Lerp(_breakDropTarget, _breakDropStart, finalCurve);
                _dropElapsedTime = ValueToCurveTime(currentValue, _dropCurve, 12);
            }
            else
            {
                StopCoroutine(_riseCoroutine);
                currentValue = _riseCurve.Evaluate(_elapsedTime);
                currentValue = Mathf.Clamp(currentValue, 0.0f, 1.0f);
                _dropElapsedTime = ValueToCurveTime(currentValue, _dropCurve, 12);
            }
            // 落下中だったら抜ける
            if (_dropCoroutine != null) return;

            _riseCoroutine = null;
            _barrierBreakCoroutine = null;
            // 現在の進行度の値を上昇カーブから見た進行度から落下カーブから見た進行度に変換する
            float newValue = _dropCurve.Evaluate(_dropElapsedTime);
            // 落下開始
            _dropCoroutine = StartCoroutine(DropRoutine(enemyTransform));
        }

        /// <summary>
        /// バリア破壊時のずり落ちを開始する
        /// </summary>
        /// <param name="_enemyTransform">トランスフォーム</param>
        public void BreakDrop(Transform _enemyTransform)
        {
            if(_riseCoroutine !=null) StopCoroutine(_riseCoroutine);
            if(_barrierBreakCoroutine != null) StopCoroutine(_barrierBreakCoroutine);

            // 現在の値を取得
            float currentValue = _riseCurve.Evaluate(_elapsedTime);

            // 移動後の値を決定
            _breakDropStart = currentValue;
            _breakDropTarget = Mathf.Max(currentValue - _breakDropDistance, 0.0f);

            _breakDropElapsedTime = 0.0f;
            _barrierBreakCoroutine = StartCoroutine(BreakDropRoutine(transform));

            OnLeftReachedGoal();
        }

        /// <summary>
        /// 被ダメージ時のずり落ちを開始する
        /// </summary>
        /// <param name="_enemyTransform">トランスフォーム</param>
        public void DamageDrop(Transform _enemyTransform)
        {
            if (_barrierBreakCoroutine != null) return;

            if (_riseCoroutine != null) StopCoroutine(_riseCoroutine);
            if (_damageDropCoroutine != null) StopCoroutine(_damageDropCoroutine);

            // 現在の値を取得
            float currentValue = _riseCurve.Evaluate(_elapsedTime);

            // 移動後の値を決定
            _damageDropStart = currentValue;
            _damageDropTarget = Mathf.Max(currentValue - _damageDropDistance, 0.0f);

            _damageDropElapsedTime = 0.0f;
            _damageDropCoroutine = StartCoroutine(DamageDropRoutine(transform));


            OnLeftReachedGoal();
        }

        /// <summary>
        /// 落下用コルーチン
        /// </summary>
        /// <param name="enemyTransform">移動させる敵のトランスフォーム</param>
        /// <returns>コルーチン用の戻り値</returns>
        private IEnumerator DropRoutine(Transform enemyTransform)
        {

            while (_dropElapsedTime < 1.0f)
            {
                _dropElapsedTime += Time.deltaTime / _dropDuration;
                _dropElapsedTime = Mathf.Clamp(_dropElapsedTime, 0.0f, 1.0f);
                float curveT = _dropCurve.Evaluate(_dropElapsedTime);
                curveT = Mathf.Clamp(curveT, 0.0f, 1.0f);
                // 値が1から0に落ちるカーブを使用するため、ターゲットからスタートに向けてLeapする
                enemyTransform.position = Vector3.Lerp(_startPosition, _targetPosition, curveT);
                yield return null;
            }

            //-イージング処理完了後開始地点に位置を補正
            enemyTransform.position = _startPosition;
            OnEnemyDroped();
        }

        /// <summary>
        /// 被ダメージ時ずり落ちコルーチン
        /// </summary>
        /// <param name="enemyTransform">移動する敵のトランスフォーム</param>
        /// <returns>コルーチン用の戻り値</returns>
        private IEnumerator DamageDropRoutine(Transform enemyTransform)
        {
            while(_damageDropElapsedTime < 1.0f)
            {
                _damageDropElapsedTime += Time.deltaTime / _damageDropDuration;
                _damageDropElapsedTime = Mathf.Clamp(_damageDropElapsedTime, 0.0f, 1.0f);

                float curveT = _damageDropCurve.Evaluate(_damageDropElapsedTime);
                curveT = Mathf.Clamp(curveT, 0.0f, 1.0f);
                // 値が1から0に落ちるカーブを使用するため、ターゲットからスタートに向けてLeapする
                curveT = Mathf.Lerp(_damageDropTarget, _damageDropStart, curveT);
                //-イージングにより位置の更新
                enemyTransform.position = Vector3.Lerp(_startPosition, _targetPosition, curveT);
                yield return null;
            }

            // 現在の値を取得
            float finalCurve = _damageDropCurve.Evaluate(_damageDropElapsedTime);
            finalCurve = Mathf.Clamp(finalCurve, 0.0f, 1.0f);
            finalCurve = Mathf.Lerp(_damageDropTarget, _damageDropStart, finalCurve);
            _elapsedTime = ValueToCurveTime(finalCurve, _riseCurve, 5);
            _damageDropCoroutine = null;

            Debug.Log("OldValue" + finalCurve + "Value" + _riseCurve.Evaluate(_elapsedTime));

            _riseCoroutine = StartCoroutine(RiseRoutine(enemyTransform));
        }


        /// <summary>
        /// バリア破壊時ずり落ちコルーチン
        /// </summary>
        /// <param name="enemyTransform">移動する敵のトランスフォーム</param>
        /// <returns>コルーチン用の戻り値</returns>
        private IEnumerator BreakDropRoutine(Transform enemyTransform)
        {
            while (_breakDropElapsedTime < 1.0f)
            {
                _breakDropElapsedTime += Time.deltaTime / _barrierBreakDuration;
                _breakDropElapsedTime = Mathf.Clamp(_breakDropElapsedTime, 0.0f, 1.0f);

                float curveT = _barrierBreakCurve.Evaluate(_breakDropElapsedTime);
                curveT = Mathf.Clamp(curveT, 0.0f, 1.0f);
                // 値が1から0に落ちるカーブを使用するため、ターゲットからスタートに向けてLeapする
                curveT = Mathf.Lerp(_breakDropTarget, _breakDropStart, curveT);
                //-イージングにより位置の更新
                enemyTransform.position = Vector3.Lerp(_startPosition, _targetPosition, curveT);
                yield return null;
            }

            // 現在の値を取得
            float finalCurve = _barrierBreakCurve.Evaluate(_breakDropElapsedTime);
            finalCurve = Mathf.Clamp(finalCurve, 0.0f, 1.0f);
            finalCurve = Mathf.Lerp(_breakDropTarget, _breakDropStart, finalCurve);
            _elapsedTime = ValueToCurveTime(finalCurve, _riseCurve, 12);
            _barrierBreakCoroutine = null;

            _riseCoroutine = StartCoroutine(RiseRoutine(enemyTransform));
        }

        /// <summary>
        /// カーブの範囲内の値からその値が手に入る時間を逆算する
        /// </summary>
        /// <param name="value">カーブから取得できる値</param>
        /// <param name="curve">使用するカーブ</param>
        /// <returns>指定された値が手に入る時間</returns>
        private float ValueToCurveTime(float value, AnimationCurve curve, float count)
        {
            float minValue = 0.0f;
            float maxValue = 1.0f;

            for (int i = 0; i < count; i++)
            {
                if (!FindValueRangeByStepping(out minValue, out maxValue, value, 3, curve, minValue, maxValue)) break;
            }
            return (minValue + maxValue) / 2.0f;
        }

        /// <summary>
        /// カーブの時間範囲を受け取り、指定された値の存在する時間範囲を絞る
        /// ※全然二分探索でも良かったかも
        /// </summary>
        /// <param name="outMinPoint">値が存在する最低範囲</param>
        /// <param name="outMaxPoint">値が存在する最高範囲</param>
        /// <param name="value">カーブの値</param>
        /// <param name="findCount">何分割するか</param>
        /// <param name="curve">確認するカーブ</param>
        /// <param name="findMinPoint">探索最低範囲</param>
        /// <param name="findMaxPoint">探索最大範囲</param>
        /// <returns>探索が成功したかどうか</returns>
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

                float minValue = Mathf.Min(currentValue, nextValue);
                float maxValue = Mathf.Max(currentValue, nextValue);

                float minPoint = Mathf.Min(currentPoint, nextPoint);
                float maxPoint = Mathf.Max(currentPoint, nextPoint);

                if (minValue <= value && value <= maxValue)
                {
                    outMinPoint = minPoint;
                    outMaxPoint = maxPoint;
                    return true;
                }

                if(i == findCount - 1)
                {
                    Debug.Log("Value = " + value + "MaxValue = " + maxValue);
                }
                
            }
            // 探索中見つからなかった場合失敗を返す
            outMinPoint = findMinPoint;
            outMaxPoint = findMaxPoint;

            Debug.Log("探索失敗！");
            return false;
        }

        public void MoveStop()
        {
            if (_riseCoroutine != null) StopCoroutine(_riseCoroutine);
            if (_dropCoroutine != null) StopCoroutine(_dropCoroutine);
            if (_barrierBreakCoroutine != null) StopCoroutine(_barrierBreakCoroutine);
        }
    }
}
