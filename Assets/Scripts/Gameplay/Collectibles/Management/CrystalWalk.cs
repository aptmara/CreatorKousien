// 描画している線上をcrystalが練り歩きます
// 2026/6/29
// 山本郁也
// 2026/6/29 - Fieldの傾きに対応するように修正、統合を行いました！浅野勇生
//             プレイヤーの殴りで壊せるクリスタルの共通インターフェース ICrystalBreakable を追加しました
using Game.Gameplay.Collectibles;
using Game.Gameplay.Stage;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class CrystalWalk : MonoBehaviour, ICrystalBreakable
{
    public Vector3 centerPos;
    public Vector3 rightPos;
    public Vector3 leftPos;
    public Vector3 rightControllPos;
    public Vector3 leftControllPos;

    private int segmentCount = 32;

    [Header("スタート位置(0秒or最大の半分の時間推奨)")]
    [SerializeField]
    public float startCount;
    [Header("一周にかかる時間(秒)")]
    [SerializeField]
    public float maxCount;
    private float _currentCount;
    private float _baseCount;
    [Header("ヒットストップ")]
    [SerializeField] private float _hitStop = 0.5f;
    private float _currentHitStop = 0.0f;

    [Header("殴られた時の揺れ")]
    [Tooltip("揺らす見た目")]
    [SerializeField] private Transform _model;
    [Tooltip("揺れ幅")]
    [SerializeField] private float _shakeAmplitude = 0.1f;
    private Vector3 _modelBaseLocalPos;

    [Header("欠片関連")]
    [SerializeField]
    private CrystalShardEmitter _emitter;
    [Tooltip("欠片の散らばり角度(度)")]
    [SerializeField] private float _spreadAngle = 25.0f;
    [Header("欠片の基礎発射数")]
    [SerializeField] public int shardBaseCount;
    [Header("実際に出る数")]
    [SerializeField] public int curShardCount;
    [Header("欠片の発射力")]
    [Tooltip("欠片の発射力。大きいほど遠くへ飛ぶ")]
    [SerializeField] public float power;
    [Header("欠片の種類と重み")]
    [SerializeField] public CrystalShardEmitter.WeightedShardData[] _shard;
    [Header("欠片の発生エフェクト")]
    [SerializeField] private GameObject VFX;
    [Tooltip("欠片の発生エフェクトの大きさ")]
    [SerializeField] private Vector3 _scale;

    [Header("フィールドの傾き")]
    [Tooltip("経路を傾けるために参照する FieldData(SO)。未設定なら実行時の FieldContext を使用")]
    [SerializeField] private FieldData _fieldData;


    [Header("欠片のあつまる中心")]
    [Tooltip("欠片が集まる中心の Transform。未設定ならワールド原点(0,0,0)")]
    [SerializeField] private Transform _fieldCenter;
    [Tooltip("欠片を殴られた点からどれだけ外へ出すか")]
    [SerializeField] private float _emitOffset = 1.5f;
    [Tooltip("発射方向に足す上向きの強さ(大きいほど高く打ち上がる)")]
    [SerializeField] private float _upBias = 1.5f;

    // フィールド中心のワールド座標
    private Vector3 FieldCenter => _fieldCenter != null ? _fieldCenter.position : Vector3.zero;


    // ICrystalBreakable インターフェースの実装
    public void Break(Vector3 hitPoint, Vector3 hitDirection)
    {
        Emits(hitPoint);
    }


    /// <summary>
    /// 経路に適用するフィールドの傾き回転。
    /// SO が設定されていればその傾き角を、なければ実行時の FieldContext を参照する。
    /// </summary>
    private Quaternion FieldRotation
    {
        get
        {
            if (_fieldData != null)
            {
                return Quaternion.Euler(_fieldData.FieldTilt, 0f, 0f);
            }
            if (FieldContext.IsReady)
            {
                return FieldContext.Rotation;
            }
            return Quaternion.identity;
        }
    }

    void Start()
    {
        _currentCount = startCount;
        _baseCount = maxCount / 4.0f;
        if(_scale.x <= 0.0f || _scale.y <= 0.0f || _scale.z <= 0.0f)
        {
            _scale = new Vector3(1, 1, 1);
        }

        if (_model != null)
        {
            _modelBaseLocalPos = _model.localPosition;
        }

        curShardCount = shardBaseCount;
    }

    // Update is called once per frame
    void Update()
    {
        // 水平面上で算出した経路上の点
        Vector3 flatPoint;
        if(_currentCount < maxCount / 4.0f)
        {
            flatPoint =
            CalculateQuadraticBezierPoint(
                (_currentCount)/_baseCount,
                centerPos,
                leftControllPos,
                leftPos);
        }
        else if(_currentCount > maxCount / 4.0f && _currentCount <= maxCount / 2.0f)
        {
            flatPoint =
            CalculateQuadraticBezierPoint(
                (_currentCount - (_baseCount)) / _baseCount,
                leftPos,
                leftControllPos,
                centerPos);
        }
        else if(_currentCount > maxCount / 2.0f && _currentCount <= 3.0f * maxCount/ 4.0f)
        {
            flatPoint =
            CalculateQuadraticBezierPoint(
                (_currentCount - (_baseCount * 2.0f)) / _baseCount,
                centerPos,
                rightControllPos,
                rightPos);
        }
        else if(_currentCount > 3.0f * maxCount / 4.0f)
        {
            flatPoint =
            CalculateQuadraticBezierPoint(
                (_currentCount - (_baseCount * 3.0f)) / _baseCount,
                rightPos,
                rightControllPos,
                centerPos);
        }
        else
        {
            flatPoint = Vector3.zero;
        }

        // フィールドの傾きを反映してワールド座標に変換(斜め移動)
        Vector3 worldPoint = FieldRotation * flatPoint;
        Vector3 moveDir = worldPoint - transform.position;
        gameObject.transform.position = worldPoint;

        if (_currentHitStop > 0.0f)
        {
            _currentHitStop -= Time.deltaTime;
        }
        else
        {
            _currentCount += Time.deltaTime;
            if (_currentCount >= maxCount) _currentCount = 0.0f;
        }

        if (_currentHitStop <= 0.0f) _currentHitStop = 0.0f;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Emits(transform.position);
        }

        Vector3 fieldUp = FieldRotation * Vector3.up;   // 床に合わせた上向き

        if (moveDir.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(moveDir.normalized, fieldUp);
        }

        // 殴られた時の揺れ
        if (_model != null)
        {
            if (_currentHitStop > 0f)
            {
                // 残り時間で減衰
                float k = _currentHitStop / Mathf.Max(0.0001f, _hitStop);
                _model.localPosition = _modelBaseLocalPos + UnityEngine.Random.insideUnitSphere * _shakeAmplitude * k;
            }
            else
            {
                _model.localPosition = _modelBaseLocalPos;
            }
        }
    }


    private void OnDrawGizmos()
    {
        if (centerPos == null ||
            rightPos == null ||
            leftPos == null ||
            leftControllPos == null ||
            rightControllPos == null)
        {
            return;
        }

        Gizmos.color = Color.green;

        Vector3 previousPoint = FieldRotation * leftPos;

        for (int i = 1; i <= segmentCount; i++)
        {
            float t = (float)i / segmentCount;

            Vector3 currentPoint = FieldRotation * CalculateQuadraticBezierPoint(
                t,
                leftPos,
                leftControllPos,
                centerPos
            );

            Gizmos.DrawLine(previousPoint, currentPoint);

            previousPoint = currentPoint;
        }

        previousPoint = FieldRotation * centerPos;

        for (int i = 1; i <= segmentCount; i++)
        {
            float t = (float)i / segmentCount;

            Vector3 currentPoint = FieldRotation * CalculateQuadraticBezierPoint(
                t,
                centerPos,
                rightControllPos,
                rightPos
            );

            Gizmos.DrawLine(previousPoint, currentPoint);

            previousPoint = currentPoint;
        }

        // 欠片の集まる中心(シアン)と発射方向(マゼンタ)の確認用
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(FieldCenter, 0.5f);

        Vector3 toCenter = Vector3.ProjectOnPlane(FieldCenter - transform.position, Vector3.up);
        if (toCenter.sqrMagnitude > 0.0001f)
        {
            Vector3 launchDir = (toCenter.normalized + Vector3.up * _upBias).normalized;
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, transform.position + launchDir * 3.0f);
        }
    }

    private Vector3 CalculateQuadraticBezierPoint(
        float t,
        Vector3 p0,
        Vector3 p1,
        Vector3 p2
    )
    {
        float u = 1.0f - t;

        return
            u * u * p0 +
            2.0f * u * t * p1 +
            t * t * p2;
    }

    [ContextMenu("DEBUG: Emit Shards")]
    private void DebugEmit() => Emits(transform.position);

    public void Emits(Vector3 hitPoint)
    {
        if (_emitter != null)
        {
            // 水平方向だけ「中心へ」向ける
            Vector3 toCenter = FieldCenter - transform.position;
            Vector3 horizontal = Vector3.ProjectOnPlane(toCenter, Vector3.up);
            horizontal = horizontal.sqrMagnitude > 0.0001f ? horizontal.normalized : Vector3.zero;

            // 水平(中心へ) + 上向き = 上に弾けて中心側へ飛ぶ
            Vector3 dir = (horizontal + Vector3.up * _upBias).normalized;

            // 発生位置: クリスタル中心から外へ押し出す
            Vector3 outward = hitPoint - transform.position;
            outward = outward.sqrMagnitude > 0.0001f ? outward.normalized : Vector3.up;
            Vector3 spawnPos = hitPoint + outward * _emitOffset;

            for (int i = 0; i < curShardCount; i++)
                _emitter.EmitFromHit(spawnPos, dir, _spreadAngle, power, _shard);
        }
        _currentHitStop = _hitStop;
        PlayEffect(hitPoint, _scale.x);
    }


    public void PlayEffect(Vector3 position, float size)
    {
        GameObject effect = Instantiate(VFX, position, Quaternion.identity);

        ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem particle in particles)
        {
            var main = particle.main;
            main.startSizeMultiplier *= size;
        }

        Destroy(effect, 2.0f);
    }

    // 現在数に倍率をかける
    public void Multiply(float multiply)
    {
        curShardCount = (int)(curShardCount * multiply);
    }

    // 基礎数に倍率をかける
    public void BaseMultiply(float multiply)
    {
        curShardCount = (int)(shardBaseCount * multiply);
    }

    // 現在数を基礎数に戻す
    public void InitShardCount()
    {
        curShardCount = shardBaseCount;
    }

}
