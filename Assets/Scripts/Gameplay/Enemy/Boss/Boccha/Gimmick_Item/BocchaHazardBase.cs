// ------------------------------------------------------------
// File		: BocchaHazardBase.cs
// Summary	: お邪魔アイテムの落下・着地・寿命の共通処理をまとめた基底クラス
//
// Author	: [浅野 勇生]
// Created	: 2026-09-07
//
// Notes	:
// - 物理落下だと転がって狙った位置に残らないため、等速降下で制御！！
// - 着地・消滅のタイミングを派生クラスへ通知して実装しますお！
// ------------------------------------------------------------
using Game.Core.Events;
using Game.Gameplay.Stage;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// お邪魔アイテムの落下方式
    /// </summary>
    public enum BocchaHazardFallMode
    {
        /// <summary>接地点の真上から等速で降ってくる</summary>
        StraightDown = 0,

        /// <summary>ボスの発射口から山なりに投げられる</summary>
        ArcFromSource = 1,
    }

    /// <summary>
    /// お邪魔アイテムの基底クラス
    /// </summary>
    public abstract class BocchaHazardBase : MonoBehaviour
    {
        [Header("==== 落下 ====")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("落下を開始する高さ")]
        private float _spawnHeight = 14.0f;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("落下速度")]
        private float _fallSpeed = 18.0f;

        [SerializeField]
        [Tooltip("落下方式。ArcFromSourceならボスから山なりに投げる")]
        private BocchaHazardFallMode _fallMode = BocchaHazardFallMode.ArcFromSource;

        [SerializeField]
        [Min(0.5f)]
        [Tooltip("山なり軌道の頂点の高さ。大きいほどふわっと飛ぶ")]
        private float _arcApexHeight = 8.0f;

        [SerializeField]
        [Range(0f, 0.5f)]
        [Tooltip("頂点の高さのばらつき。1個ずつ軌道を変えて単調さを消す")]
        private float _arcApexRandomness = 0.15f;


        [Header("==== 寿命 ====")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("着地してから消えるまでの時間。0で無限")]
        private float _lifeTime = 12.0f;


        [Header("==== 当たり判定 ====")]

        [SerializeField]
        [Tooltip("落下中は無効にしておくコライダー。未設定なら何もしない")]
        private Collider _bodyCollider;


        [Header("==== 演出(後付け用・空でOK) ====")]

        [SerializeField]
        [Tooltip("着地時のVFX")]
        private GameObject _landingVfxPrefab;

        [SerializeField]
        [Tooltip("消滅時のVFX")]
        private GameObject _despawnVfxPrefab;

        [SerializeField]
        [Min(0f)]
        [Tooltip("VFXを破棄するまでの時間")]
        private float _vfxLifeTime = 3.0f;


        private Vector3 _groundPosition;
        private float _lifeTimer;
        private bool _isFalling;
        private bool _isDespawned;
        private Vector3 _arcStartPosition;
        private Vector3 _arcVelocity;
        private Vector3 _arcGravity;
        private float _arcFlightTime;
        private float _arcElapsed;


        /// <summary>着地済みかどうか。</summary>
        public bool IsLanded { get; private set; }

        /// <summary>お邪魔アイテムの種類。</summary>
        public abstract BocchaHazardType HazardType { get; }


        /// <summary>
        /// 指定した接地点へ向けて投射を開始する。
        /// </summary>
        /// <param name="sourcePosition">投げ出す位置。ArcFromSource以外では使用しない</param>
        /// <param name="groundPosition">最終的に着地する位置</param>
        /// <param name="scaleMultiplier">大きさの倍率</param>
        public void Launch(Vector3 sourcePosition, Vector3 groundPosition, float scaleMultiplier)
        {
            _groundPosition = groundPosition;

            // 山なりに投げる場合はボスの発射口から、そうでない場合は接地点の真上から始める
            bool isArc = _fallMode == BocchaHazardFallMode.ArcFromSource;
            Vector3 startPosition = isArc ? sourcePosition : groundPosition + GetUp() * _spawnHeight;

            transform.localScale *= Mathf.Max(0.1f, scaleMultiplier);
            transform.position = startPosition;

            _isFalling = true;
            IsLanded = false;
            _isDespawned = false;
            _lifeTimer = _lifeTime;

            if (_bodyCollider != null) _bodyCollider.enabled = false;

            if (isArc) SetupArc(startPosition, groundPosition);

            BocchaHazardRegistry.Register(this);
        }


        /// <summary>
        /// 山なり軌道の初速と到達時間を求める。
        /// フィールドが傾いても正しい放物線になるよう、重力は上方向から作り直す。
        /// </summary>
        private void SetupArc(Vector3 from, Vector3 to)
        {
            float gravityMagnitude = Physics.gravity.magnitude;

            if (gravityMagnitude < 0.0001f) gravityMagnitude = 9.81f;

            _arcGravity = -GetUp() * gravityMagnitude;

            // 頂点の高さを1個ずつ揺らして、同じ軌道が並ばないようにする
            float apex = _arcApexHeight * (1.0f + Random.Range(-_arcApexRandomness, _arcApexRandomness));

            _arcVelocity = BocchaBallistics.SolveVelocityByApex(from, to, apex, _arcGravity, out _arcFlightTime);
            _arcStartPosition = from;
            _arcElapsed = 0.0f;
        }


        /// <summary>
        /// 外部から強制的に消滅させる
        /// </summary>
        public void ForceDespawn() => Despawn();


        /// <summary>
        /// 更新処理。落下中は落下処理、着地後は寿命のカウントダウンを行う
        /// </summary>
        protected virtual void Update()
        {
            if (_isDespawned)
                return;

            // 落下中は寿命のカウントダウンをしない
            if (_isFalling)
            {
                UpdateFall();

                return;
            }

            if (_lifeTime <= 0.0f)
            {
                return;
            }

            // 寿命のカウントダウン
            _lifeTimer -= Time.deltaTime;

            if (_lifeTimer > 0.0f)
            {
                return;
            }

            Despawn();
        }


        // 消滅処理
        private void OnDestroy() => BocchaHazardRegistry.Unregister(this);



        // 派生クラス向けフック
        // ------------------------------------------------------------

        /// <summary>
        /// 着地した直後に呼ばれる
        /// </summary>
        protected virtual void OnLanded() { }

        /// <summary>
        /// 寿命が尽きて消える直前に呼ばれる
        /// </summary>
        protected virtual void OnDespawning() { }



        // 内部処理
        // ------------------------------------------------------------

        private void UpdateFall()
        {
            // 山なりに投げる場合は放物線の位置計算を行う
            if (_fallMode == BocchaHazardFallMode.ArcFromSource)
            {
                UpdateArcFall();

                return;
            }

            Vector3 up = GetUp();
            float remaining = Vector3.Dot(_groundPosition - transform.position, -up);

            float step = _fallSpeed * Time.deltaTime;

            // 接地点に到達する前なら落下処理
            if (step < remaining)
            {
                transform.position -= up * step;

                return;
            }

            Land();
        }


        /// <summary>
        /// 放物線の位置を時刻から直接求めて動かす。
        /// 速度を積分せず位置を直接出すため、必ず狙った接地点へ着地する。
        /// </summary>
        private void UpdateArcFall()
        {
            _arcElapsed += Time.deltaTime;

            if (_arcElapsed < _arcFlightTime)
            {
                transform.position = _arcStartPosition
                    + _arcVelocity * _arcElapsed
                    + 0.5f * _arcGravity * _arcElapsed * _arcElapsed;

                return;
            }

            Land();
        }


        /// <summary>
        /// 着地処理。落下方式に関わらず共通で通る。
        /// </summary>
        private void Land()
        {
            // 接地点に到達したら着地処理
            transform.position = _groundPosition;
            _isFalling = false;
            IsLanded = true;

            if (_bodyCollider != null)
            {
                _bodyCollider.enabled = true;
            }

            PlayVfx(_landingVfxPrefab);

            EventBus.Publish(new BocchaHazardSpawnedEvent(HazardType, transform.position));

            OnLanded();
        }


        private void Despawn()
        {
            if (_isDespawned)
            {
                return;
            }

            _isDespawned = true;

            OnDespawning();

            PlayVfx(_despawnVfxPrefab);

            BocchaHazardRegistry.Unregister(this);

            Destroy(gameObject);
        }


        /// <summary>
        /// VFXを再生する。nullなら何もしない。
        /// </summary>
        /// <param name="prefab"></param>
        protected void PlayVfx(GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }

            GameObject vfx = Instantiate(prefab, transform.position, Quaternion.identity);

            Destroy(vfx, _vfxLifeTime);
        }

        /// <summary>
        /// 落下中の上方向を取得する。フィールドが生成されていない場合はワールド上方向を返す。
        /// </summary>
        /// <returns></returns>
        private static Vector3 GetUp() => FieldContext.IsReady ? FieldContext.Up : Vector3.up;
    }
}
