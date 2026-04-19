// ------------------------------------------------------------
// File		: PlayerView.cs
// Summary	: プレイヤーのViewクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-14
//
// Notes	:
// -
// ------------------------------------------------------------
using System.Collections;
using UnityEngine;
using CreatorKousien.Field;

namespace CreatorKousien.Player
{
    /// <summary>
    /// プレイヤーの見た目とアニメーションを担当するクラス
    /// </summary>
    public class PlayerView : MonoBehaviour
    {
        // ----- Inspectorで設定する変数 -----
        [Header("Playerの描画に関する設定")]
        [Tooltip("アニメーターコンポーネントをアタッチしてください")]
        [SerializeField] private Animator _animator;                // プレイヤーのアニメーター

        [Tooltip("目標座標へ向かう際の滑らかさ（Lerpの補間速度）")]
        [SerializeField] private float _lerpSpeed = 10f;

        [Tooltip("モデルが床に埋まる場合、ここを0.5等に設定して高さを持ち上げる")]
        [SerializeField] private float _modelYOffset = 0.5f;        // モデルの高さオフセット

        [Tooltip("ジャンプの高さ")]
        [SerializeField] private float _jumpHeight = 1f;            // ジャンプの高さ
        [Tooltip("1マスの移動にかかる時間")]
        [SerializeField] private float _moveDuration = 0.25f;       // ジャンプの時間


        [Header("エフェクト設定")]
        [Tooltip("ダメージを受けた時に赤く点滅させるRenderer")]
        [SerializeField] private Renderer[] _renderers;
        [Tooltip("ダメージ時ののけぞり時間")]
        [SerializeField] private float _damageEffectDuration = 0.2f; // 将来的にアニメーションに変更予定


        private Vector3 _targetWorldPos;    // 目標座標（ワールド座標）
        private GridCellView _standingTile; // プレイヤーが立っているマスの参照
        private float _baseHeight;          // プレイヤーの基本的な高さ（マスの高さを考慮する前の高さ）
        private Coroutine _moveCoroutine;   // 移動中のコルーチンの参照
        private Color[] _originalColors;    // 元の色を保存する配列

        private void Start()
        {
            // 元の色を保存
            if (_renderers != null && _renderers.Length > 0)
            {
                _originalColors = new Color[_renderers.Length];
                for (int i = 0; i < _renderers.Length; i++)
                {
                    if (_renderers[i] != null)
                        _originalColors[i] = _renderers[i].material.color;
                }
            }
        }



        /// <summary>
        /// 初期位置のセットアップ
        /// </summary>
        /// <param name="startGridPos"> 初期のプレイヤー座標</param>
        public void Initialize(Vector3 startWorldPos)
        {
            _baseHeight = startWorldPos.y + _modelYOffset;

            UpdateTargetPosition(startWorldPos);
            transform.position = _targetWorldPos;           // 初期位置を設定
        }


        /// <summary>
        /// 立っている床をセットする
        /// </summary>
        /// <param name="tile">立っている床の参照</param>
        public void SetStandingTile(GridCellView tile)
        {
            _standingTile = tile;
        }



        /// <summary>
        /// PlayerSystemからの移動通知を受け取る
        /// </summary>
        /// <param name="cellWorldPos">FieldViewから取得したマスのワールド座標</param>
        public void UpdateTargetPosition(Vector3 cellWorldPos)
        {
            _targetWorldPos = new Vector3(cellWorldPos.x, _baseHeight, cellWorldPos.z);

            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
            }
            _moveCoroutine = StartCoroutine(JumpRoutine(_targetWorldPos));
        }


        /// <summary>
        /// ジャンプ移動のコルーチン。現在の位置から目標位置へ向かって、ジャンプしながら移動するアニメーションを再生する
        /// </summary>
        /// <param name="targetPos">目標位置</param>
        /// <returns>IEnumerator</returns>
        private IEnumerator JumpRoutine(Vector3 targetPos)
        {
            if (_animator != null)
            {
                _animator.SetBool("IsMoving", true);

                Vector3 startPos = transform.position;
                float elapsed = 0f;

                while (elapsed < _moveDuration)
                {
                    elapsed += Time.deltaTime;
                    float percent = elapsed / _moveDuration;

                    // 直線的な移動位置
                    Vector3 currentPos = Vector3.Lerp(startPos, targetPos, percent);

                    // ジャンプの高さを加算
                    float jumpOffset = Mathf.Sin(percent * Mathf.PI) * _jumpHeight; // ジャンプの高さをサイン波で計算

                    // 床の高さを考慮してY座標を調整
                    float tileSinkOffset = _standingTile != null ? _standingTile.CurrentVisualOffset : 0f;

                    // 最終的な位置を計算
                    currentPos.y = currentPos.y + jumpOffset + tileSinkOffset;

                    transform.position = currentPos;
                    yield return null;
                }

                // 最終位置を確実にセット
                float finalTileOffset = _standingTile != null ? _standingTile.CurrentVisualOffset : 0f;
                transform.position = new Vector3(targetPos.x, targetPos.y + finalTileOffset, targetPos.z);

                if (_animator != null)
                {
                    _animator.SetBool("IsMoving", false);
                }
            }
        }


        /// <summary>
        /// Event Busから呼ばれる被ダメージエフェクト再生のメソッド
        /// </summary>
        /// <param name="damageAmount">ダメージ量</param>
        public void PlayDamageEffect(int damageAmount)
        {
            // TODO: 将来的にアニメーションに変更予定。今は単純に赤く点滅させるだけのエフェクト
            // if (_animator != null) _animator.SetTrigger("Damage");

            // 赤く点滅してピクッと震えるコルーチンを開始
            StartCoroutine(DamageRoutine());

            Debug.Log($"[PlayerView] ダメージエフェクト再生: {damageAmount}ダメージ");

            // TODO: ダメージ量のポップアップUIとかもここで出してもいいかも？？
        }


        /// <summary>
        /// ダメージエフェクトのコルーチン。一定時間、プレイヤーのモデルを赤く点滅させる
        /// </summary>
        /// <returns></returns>
        private IEnumerator DamageRoutine()
        {
            // ----- 1. 赤くする -----
            foreach (var r in _renderers)
            {
                if (r != null)
                    r.material.color = Color.red; // 赤く点滅
            }

            // ----- 2. のけぞりモーション -----
            Vector3 originalPos = transform.position;
            transform.position = originalPos + (Random.insideUnitSphere * 0.2f);

            yield return new WaitForSeconds(_damageEffectDuration);

            // ----- 3. 元に戻す -----
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                    _renderers[i].material.color = _originalColors[i]; // 元の色に戻す
            }

            transform.position = _targetWorldPos; // 元の位置に戻す
        }


        public void PlayDeathEffect()
        {
            // if (_animator != null) _animator.SetTrigger("Die");

            Debug.Log("[PlayerView] 死亡アニメーション再生！バタンキュー！！");

            // TODO: 死亡エフェクトを再生して、プレイヤーが倒れるアニメーションを再生する
        }


        /// <summary>
        /// アクションを再生するためのメソッド
        /// </summary>
        /// <param name="triggerName"></param>
        public void PlayAction(string triggerName)
        {
            if (_animator != null)
            {
                _animator.SetTrigger(triggerName);
            }
        }

        /// <summary>
        /// PlayerSystemからの死亡通知を受け取る
        /// </summary>
        public void OnDeath()
        {
            if (_animator != null) _animator.SetTrigger("Die");
            Debug.Log("[PlayerView] 死亡アニメーション再生");
        }

        /// <summary>
        /// 更新処理：目標位置へ向かって滑らかに移動し、アニメーションの状態を更新する
        /// </summary>
        private void Update()
        {
            // 基本の目標座標
            Vector3 finalTargetPos = _targetWorldPos;

            // もし床が沈んでいたら
            if (_standingTile != null)
            {
                // 立っているマスの高さを考慮して目標座標を調整
                finalTargetPos.y += _standingTile.CurrentVisualOffset; // マスの高さ + プレイヤーの半分の高さ（仮）
            }

            // 現在の位置から目標位置へ滑らかに移動
            transform.position = Vector3.Lerp(transform.position, finalTargetPos, Time.deltaTime * _lerpSpeed);

            // 移動中かどうかの判定をアニメーターに送る
            if (_animator != null)
            {
                float distance = Vector3.Distance(transform.position, _targetWorldPos);
                _animator.SetBool("IsMoving", distance > 0.01f);
            }
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
