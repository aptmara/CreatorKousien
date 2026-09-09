// ------------------------------------------------------------
// File		: BocchaIntroPresenter.cs
// Summary	: キング・ボッチャの登場演出
//
// Author	: [浅野 勇生]
// Created	: 2026-09-09
//
// Notes	:
// - 分身フェーズと同じく煙でボンボン移動する感じ！
// - 勝手に作ってるから没になる可能性あり(´;ω;｀)
// - ステップはリストにするから変更可能！
// ------------------------------------------------------------
using System;
using System.Collections;
using Game.Gameplay.Stage;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    [Serializable]
    public class BocchaIntroStep
    {
        [SerializeField]
        [Tooltip("Inspectorで識別するための名前")]
        private string _stepName = "Step";

        [SerializeField]
        [Tooltip("フィールド中心からの横方向オフセット。マイナスで左")]
        private float _offsetX = -18.0f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("煙を出してから姿を見せるまでの時間")]
        private float _appearDelay = 0.35f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("姿を見せてから消えるまでの時間")]
        private float _stayDuration = 0.6f;

        [SerializeField]
        [Tooltip("ONならここで留まる")]
        private bool _isFinal = false;


        // --- 公開プロパティ---
        public string StepName => _stepName;
        public float OffsetX => _offsetX;
        public float AppearDelay => _appearDelay;
        public float StayDuration => _stayDuration;
        public bool IsFinal => _isFinal;
    }



    /// <summary>
    /// キング・ボッチャの登場演出
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BocchaIntroPresenter : MonoBehaviour, IBossIntroPresentation
    {
        [Header("==== 手順 ====")]

        [SerializeField]
        [Tooltip("上から順に実行する。最後のステップのIs Finalを必ずONにすること")]
        private BocchaIntroStep[] _steps = new BocchaIntroStep[0];

        [SerializeField]
        [Min(0f)]
        [Tooltip("最初のステップに入る前の待ち時間")]
        private float _startDelay = 0.3f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("最後に姿を見せてから戦闘開始までの間")]
        private float _finishDelay = 0.5f;


        [Header("==== 演出 ====")]

        [SerializeField]
        [Tooltip("出現・消滅で出す煙")]
        private GameObject _smokeVfxPrefab;

        [SerializeField]
        [Tooltip("VFXの位置補正。ルート原点と見た目のズレを吸収する")]
        private Vector3 _vfxOffset = new Vector3(0.0f, -4.89f, 0.0f);

        [SerializeField]
        [Min(0f)]
        [Tooltip("VFXを破棄するまでの時間")]
        private float _vfxLifeTime = 3.0f;

        [SerializeField]
        [Tooltip("最後に姿を見せた時のAnimatorトリガー名。空なら何もしない")]
        private string _appearAnimTrigger = string.Empty;

        [SerializeField]
        [Tooltip("アニメーター。未設定なら同じGameObjectから取得する")]
        private Animator _animator;


        private Renderer[] _renderers;
        private Collider[] _colliders;


        private void Awake()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }
        }


        /// <summary>
        /// 登場演出を再生する
        /// </summary>
        public IEnumerator PlayIntro()
        {
            // 演出が終わるまで姿を隠す
            SetVisible(false);

            if (_startDelay > 0.0f)
            {
                yield return new WaitForSeconds(_startDelay);
            }

            foreach (BocchaIntroStep step in _steps)
            {
                if (step == null)
                {
                    continue;
                }

                yield return StartCoroutine(PlayStep(step));
            }

            // ステップが1つもない場合の保険
            SetVisible(true);

            if (_finishDelay > 0.0f)
            {
                yield return new WaitForSeconds(_finishDelay);
            }
        }



        // 内部処理
        // ------------------------------------------------------------

        /// <summary>
        /// ステップを順に実行する
        /// </summary>
        /// <param name="step">オープニングの演出</param>
        private IEnumerator PlayStep(BocchaIntroStep step)
        {
            // ステップの位置を求める
            Vector3 stepPosition = ResolveStepPosition(step.OffsetX);

            // 煙を出す
            PlayVfx(stepPosition);

            if (step.AppearDelay > 0.0f)
            {
                yield return new WaitForSeconds(step.AppearDelay);
            }

            // 姿を見せる
            SetVisible(true);

            // 最後のステップならアニメーターにトリガーを送る
            if (step.IsFinal && _animator != null && !string.IsNullOrEmpty(_appearAnimTrigger))
            {
                _animator.SetTrigger(_appearAnimTrigger);
            }

            if (step.StayDuration > 0.0f)
            {
                yield return new WaitForSeconds(step.StayDuration);
            }

            // 最後のステップはそのまま残る
            if (step.IsFinal)
                yield break;

            PlayVfx(transform.position);

            SetVisible(false);
        }


        /// <summary>
        /// フィールド中心を基準にステップの位置を求める
        /// </summary>
        /// <param name="offsetX">中心からの横方向オフセット</param>
        /// <returns>与えを返す</returns>
        private Vector3 ResolveStepPosition(float offsetX)
        {
            Vector3 right = FieldContext.IsReady ? FieldContext.Rotation * Vector3.right : Vector3.right;

            // 高さは今の値を保つ
            Vector3 center = FieldContext.IsReady ? new Vector3(FieldContext.Center.x, transform.position.y, transform.position.z) : transform.position;

            return center + right * offsetX;
        }


        /// <summary>
        /// 姿と当たり判定をまとめて切り替える
        /// </summary>
        /// <param name="isVisible">表示するかどうか</param>
        private void SetVisible(bool isVisible)
        {
            _renderers ??= GetComponentsInChildren<Renderer>(true);
            _colliders ??= GetComponentsInChildren<Collider>(true);

            // レンダラーをON/OFFして姿を見せる/隠す
            foreach (Renderer targetRenderer in _renderers)
            {
                if (targetRenderer != null)
                {
                    targetRenderer.enabled = isVisible;
                }
            }

            // 演出中に殴られてキャパが溜まらないようにする
            foreach (Collider targetCollider in _colliders)
            {
                if (targetCollider != null)
                {
                    targetCollider.enabled = isVisible;
                }
            }
        }


        /// <summary>
        /// 姿を見せるか隠すか
        /// </summary>
        /// <param name="position">生成位置</param>
        private void PlayVfx(Vector3 position)
        {
            if (_smokeVfxPrefab == null)
            {
                return;
            }

            GameObject vfx = Instantiate(_smokeVfxPrefab, position + _vfxOffset, Quaternion.identity);

            Destroy(vfx, _vfxLifeTime);
        }
    }
}
