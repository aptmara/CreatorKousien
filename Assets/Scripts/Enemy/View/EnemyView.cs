// ================================================================================
// File         : EnemyData.cs
// Author       : Iwai Shogo
//
// Description  : エネミーの見た目とアニメーションを担当するクラス。
//                ジャンプ移動、ダメージ、死亡演出を管理します。
// Created      : 2026-04-13
//
// Note         : プランナーはこのデータをインスペクターから作成・調整します。
// ================================================================================

using System.Collections;
using UnityEngine;
using CreatorKousien.Field;

namespace CreatorKousien.Enemy
{
    public class EnemyView : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private Animator _animator;
        [SerializeField] private float _moveDuration = 0.25f;
        [SerializeField] private float _jumpHeight = 1.0f;
        [SerializeField] private float _modelYOffset = 0.5f;

        [Header("VFX Settings")]
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private float _damageDuration = 0.2f;

        private int _actorId;
        private Vector3 _targetPos;
        private Color[] _originalColors;
        private GridCellView _standingTile;
        private Coroutine _activeMoveRoutine;

        public int ActorId => _actorId;

        public void Initialize(int actorId, Vector3 startPos, GridCellView initialTile)
        {
            _actorId = actorId;
            _standingTile = initialTile;

            // 向きの補正
            transform.rotation = Quaternion.Euler(0, 180f, 0);

            // 初期位置を即座に反映
            float yOffset = initialTile != null ? initialTile.CurrentVisualOffset : 0;
            _targetPos = new Vector3(startPos.x, startPos.y + _modelYOffset + yOffset, startPos.z);
            transform.position = _targetPos;

            // マテリアルカラーの保存
            if (_renderers != null)
            {
                _originalColors = new Color[_renderers.Length];
                for (int i = 0; i < _renderers.Length; i++)
                    _originalColors[i] = _renderers[i].material.color;
            }
        }

        public void SetStandingTile(GridCellView tile) => _standingTile = tile;

        /// <summary>
        /// 移動通知を受けてジャンプを開始する
        /// </summary>
        public void MoveTo(Vector3 worldPos)
        {
            _targetPos = new Vector3(worldPos.x, worldPos.y + _modelYOffset, worldPos.z);

            if (_activeMoveRoutine != null) StopCoroutine(_activeMoveRoutine);
            _activeMoveRoutine = StartCoroutine(JumpRoutine(_targetPos));
        }

        private IEnumerator JumpRoutine(Vector3 target)
        {
            if (_animator != null) _animator.SetBool("IsMoving", true);

            Vector3 start = transform.position;
            float elapsed = 0;

            while (elapsed < _moveDuration)
            {
                elapsed += Time.deltaTime;
                float p = elapsed / _moveDuration;

                Vector3 current = Vector3.Lerp(start, target, p);
                float jump = Mathf.Sin(p * Mathf.PI) * _jumpHeight;
                float tileOffset = _standingTile != null ? _standingTile.CurrentVisualOffset : 0;

                current.y += jump + tileOffset;
                transform.position = current;
                yield return null;
            }

            transform.position = target + Vector3.up * (_standingTile != null ? _standingTile.CurrentVisualOffset : 0);
            if (_animator != null) _animator.SetBool("IsMoving", false);
        }

        public void PlayDamageEffect() => StartCoroutine(DamageRoutine());

        private IEnumerator DamageRoutine()
        {
            foreach (var r in _renderers) r.material.color = Color.red;
            yield return new WaitForSeconds(_damageDuration);
            for (int i = 0; i < _renderers.Length; i++) _renderers[i].material.color = _originalColors[i];
        }

        public void PlayDeathEffect()
        {
            // TODO: パーティクルの生成など
            Destroy(gameObject, 0.5f);
        }


        /// <summary>
        /// 移動失敗時に、少しだけ前に出て戻る演出を再生する
        /// </summary>
        /// <param name="failTargetWorldPos">意向としていた目標のワールド座標</param>
        public void PlayMoveFailEffect(Vector3 failTargetWorldPos)
        {
            StartCoroutine(BumpRoutine(failTargetWorldPos));
        }


        /// <summary>
        /// 移動失敗の演出ルーチン。目標に向かって少しだけ移動し、すぐに元の位置に戻る。
        /// </summary>
        /// <param name="failTargetWorldPos"></param>
        /// <returns></returns>
        private System.Collections.IEnumerator BumpRoutine(Vector3 failTargetWorldPos)
        {
            Vector3 startPos = transform.position;

            // 目標に向かって「20%」だけ進んだ位置をぶつかるポインタにする
            Vector3 bumpPos = Vector3.Lerp(startPos, failTargetWorldPos, 0.2f);

            // ぶつかる位置まで移動
            float duration = 0.08f;
            float elapsed = 0;

            // ----- 1. 目標に向かって少しだけ移動 -----
            while (elapsed < duration)
            {
                transform.position = Vector3.Lerp(startPos, bumpPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // ----- 2. 元の位置に戻る -----
            elapsed = 0;
            while (elapsed < duration)
            {
                transform.position = Vector3.Lerp(bumpPos, startPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = startPos;
        }
    }
}
