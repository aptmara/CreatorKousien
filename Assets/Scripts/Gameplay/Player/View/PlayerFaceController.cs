// ------------------------------------------------------------
// File		: PlayerFaceController.cs
// Summary	: プレイヤーの表情を制御するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-07-06
//
// Notes	:
// - 顔メッシュのBlendShapeを名前で切り替える
// - 表情を増やすときはInspector の _expressions に1要素足すだけでOK！
// ------------------------------------------------------------
using UnityEngine;
using System.Collections;

namespace Game.Gameplay.Player
{
    public sealed class PlayerFaceController : MonoBehaviour
    {
        /// <summary>
        /// 表情の情報
        /// </summary>
        [System.Serializable]
        public struct Expression
        {
            [Tooltip("表情の名前(コードから指定する識別子)")]
            public string Name;
            [Tooltip("動かすBlendShapeの番号(Inspectorの上から0, 1, 2....)")]
            public int BlendShapeIndex;
            [Tooltip("表情の強さ(0.0~100.0)")]
            [Range(0.0f, 100.0f)] public float Weight;
        }


        [Header("参照")]
        [Tooltip("顔メッシュのSkinnedMeshRenderer")]
        [SerializeField] private SkinnedMeshRenderer _faceRenderer;

        [Header("表情プリセット(増やすときはここに追加)")]
        [SerializeField] private Expression[] _expressions;

        [Header("補間")]
        [Tooltip("表情の変化速度。大きいほどパッと変わる")]
        [SerializeField] private float _weightPerSecond = 800.0f;


        private int _activeIndex = -1;              ///< 現在の表情のBlendShape番号
        private float _currentWeight;               ///< 現在の表情の強さ(0~100)
        private float _targetWeight;                ///< 目標の表情の強さ(0~100)
        private Coroutine _autoResetCo;             ///< 自動リセットのコルーチン
        private string _heldFace;                   ///< 押しっぱなしで維持する表情


        /// <summary>
        /// 指定した表情に切り替える
        /// </summary>
        public void SetFace(string faceName)
        {
            if (_faceRenderer == null)
            {
                // Debug.LogWarning($"[PlayerFaceController] SkinnedMeshRenderer が設定されていません", this);
                return;
            }
            Debug.Log($"[PlayerFaceController] 表情を切り替えます: {faceName}", this);

            for (int i = 0; i < _expressions.Length; i++)
            {
                if (_expressions[i].Name == faceName)
                {
                    Apply(_expressions[i]);
                    return;
                }
            }
#if UNITY_EDITOR
            // Debug.LogWarning($"[PlayerFaceController] 表情が見つかりません: {faceName}", this);
#endif
        }


        /// <summary>
        /// 表情を一定時間だけ出して、そのあと0に戻す
        /// </summary>
        /// <param name="faceName">表情する顔の名前</param>
        /// <param name="seconds">表示する時間（秒）</param>
        public void SetFaceForSeconds(string faceName, float seconds)
        {
            SetFace(faceName);
            if (_autoResetCo != null)
            {
                StopCoroutine(_autoResetCo);
            }
            _autoResetCo = StartCoroutine(ResetAfter(seconds));
        }


        /// <summary>
        /// 表情を戻す(BlendShapeを0にする)
        /// </summary>
        public void ResetFace()
        {
            // 維持表情があるならそこへ戻す
            if (!string.IsNullOrEmpty(_heldFace))
            {
                SetFace(_heldFace);
            }
            else
            {
                _targetWeight = 0.0f;
            }
        }



        // ----- 表情維持用の処理 -----
        /// <summary>
        /// 押している間ずっと維持する表情を設定する
        /// </summary>
        /// <param name="faceName"></param>
        public void SetHeldFace(string faceName)
        {
            _heldFace = faceName;
            SetFace(faceName);
        }


        /// <summary>
        /// 押している間ずっと維持する表情を解除する
        /// </summary>
        public void ClearHeldFace()
        {
            _heldFace = null;
            ResetFace();
        }



        // ----- 内部処理 -----

        /// <summary>
        /// 表情の情報を適用する
        /// </summary>
        /// <param name="e">表情の情報</param>
        private void Apply(Expression e)
        {
            if (_activeIndex != e.BlendShapeIndex && _activeIndex >= 0)
            {
                _faceRenderer.SetBlendShapeWeight(_activeIndex, 0.0f);
            }

            _activeIndex = e.BlendShapeIndex;
            _currentWeight = _faceRenderer.GetBlendShapeWeight(_activeIndex);
            _targetWeight = e.Weight;
        }


        /// <summary>
        /// 自動リセットのコルーチン
        /// </summary>
        /// <param name="seconds">時間</param>
        /// <returns>コルーチン</returns>
        private IEnumerator ResetAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            ResetFace();
            _autoResetCo = null;
        }


        /// <summary>
        /// 毎フレーム呼ばれる更新処理
        /// </summary>
        private void LateUpdate()
        {
            if (_faceRenderer == null || _activeIndex < 0) return;

            _currentWeight = Mathf.MoveTowards(_currentWeight, _targetWeight, _weightPerSecond * Time.deltaTime);
            _faceRenderer.SetBlendShapeWeight(_activeIndex, _currentWeight);
        }
    }
}


