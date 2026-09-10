// ------------------------------------------------------------
// File		: BocchaScatterSettings.cs
// Summary	: フィールへ偏りなくアイテムを散布させるための座標計算クラス
//
// Author	: [浅野 勇生]
// Created	: 2026-09-07
//
// Notes	:
// - Random.insideUnitCircle は方向が偏り、かつ中心に密集するため使用しない。
// - 円周を散布数で等分し各セクターへ1個ずつ置くことで、方向の偏りを原理的に無くす。
// - 半径は面積均等になるよう平方根補正を行う。
// - 呼び出しごとに開始角へ黄金角を加算し、連続実行時も同じ位置に落ちないようにする。
// - オカシ / 石化キャンディ / トゲ玉 / ドクロ の全てで使い回す。
// ------------------------------------------------------------
using Game.Gameplay.Collectibles;
using Game.Gameplay.Stage;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// 散布パターン。
    /// </summary>
    public enum BocchaScatterPattern
    {
        /// <summary>円周を等分し、各セクターへ1個ずつ配置する（既定）</summary>
        EvenSector = 0,

        /// <summary>黄金角で並べる。数が多い時に自然な均等さになる</summary>
        GoldenSpiral = 1,

        /// <summary>外周のリング状に等間隔で並べる</summary>
        Ring = 2,
    }

    /// <summary>
    /// 散布座標の計算設定。ギミックSOへ埋め込んで使用する。
    /// </summary>
    [System.Serializable]
    public class BocchaScatterSettings
    {
        /// <summary>黄金角（度）</summary>
        private const float GoldenAngleDegrees = 137.50776f;


        [SerializeField]
        [Tooltip("散布パターン")]
        private BocchaScatterPattern _pattern = BocchaScatterPattern.EvenSector;

        [SerializeField]
        [Min(0f)]
        [Tooltip("この半径より内側には落とさない（ボス直下を避ける）")]
        private float _innerRadius = 3.0f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("散布する最大半径")]
        private float _outerRadius = 12.0f;

        [SerializeField]
        [Tooltip("楕円化の倍率。x=横方向(左右) / y=奥行き方向。横長のフィールドはxを大きくする")]
        private Vector2 _axisScale = Vector2.one;

        [SerializeField]
        [Tooltip("散布中心のオフセット。奥へ寄せたい場合はzを増やす")]
        private Vector3 _centerOffset = Vector3.zero;

        [Header("--- フィールド自動フィット ---")]

        [SerializeField]
        [Tooltip("ONにするとシーン上のCollectibleSpawnAreaを包含する範囲へ散布を合わせる。Inner/Outer RadiusとAxis Scaleは無視される")]
        private bool _fitToSpawnAreas = false;

        [SerializeField]
        [Range(0.1f, 1.5f)]
        [Tooltip("自動フィット時、範囲に対してどこまで広げるか")]
        private float _fitMargin = 0.95f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("0=完全に等間隔 / 1=セクター幅いっぱいにランダム")]
        private float _angleJitter = 0.35f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("半径方向のばらつき")]
        private float _radiusJitter = 0.25f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("点同士の最低距離。0で判定しない")]
        private float _minSeparation = 1.5f;

        [SerializeField]
        [Min(1)]
        [Tooltip("最低距離を満たす点を探す試行回数")]
        private int _separationRetryCount = 6;

        [SerializeField]
        [Tooltip("地面へスナップするかどうか")]
        private bool _snapToGround = true;

        [SerializeField]
        [Tooltip("地面が見つからない点を捨てるかどうか。ONだと場外へ落とさなくなるが、Ground Maskの設定ミスで何も出なくなるので注意")]
        private bool _discardWhenNoGround = false;

        [SerializeField]
        [Tooltip("地面判定に使うレイヤー")]
        private LayerMask _groundMask = ~0;

        [SerializeField]
        [Min(1f)]
        [Tooltip("地面を探すレイの高さ")]
        private float _groundRayHeight = 50.0f;

        [SerializeField]
        [Tooltip("地面から浮かせる高さ。ピボットが中心にあるモデルは、半径ぶん上げると埋まらなくなる")]
        private float _groundClearance = 0.0f;


        /// <summary>呼び出しごとに黄金角ぶん回して、毎回同じ配置にならないようにする</summary>
        private float _rotationSeed;

        /// <summary>フィールド範囲のキャッシュ</summary>
        private Bounds _cachedBounds;

        /// <summary>フィールド範囲をキャッシュ済みかどうか</summary>
        private bool _hasCachedBounds;

        /// <summary>半径の割り当て順。角度と半径が相関して渦巻きになるのを防ぐ</summary>
        private readonly List<int> _radiusOrder = new List<int>();


        /// <summary>
        /// 散布の内側半径。
        /// </summary>
        public float InnerRadius => _innerRadius;

        /// <summary>
        /// 散布の外側半径。
        /// </summary>
        public float OuterRadius => _outerRadius;


        /// <summary>
        /// 散布座標を計算して、 results へ追加する。
        /// </summary>
        /// <param name="center">散布の中心</param>
        /// <param name="count">散布する数</param>
        /// <param name="results">結果を追加するリスト</param>
        /// <returns>実際に追加した数</returns>
        public int BuildPoints(Vector3 center, int count, List<Vector3> results)
        {
            if (results == null)
            {
                return 0;
            }

            results.Clear();


            if (count <= 0)
            {
                return 0;
            }

            // 半径の割り当て順を作る
            float inner = Mathf.Min(_innerRadius, _outerRadius);
            float outer = Mathf.Max(_innerRadius, _outerRadius);

            BuildShuffledRadiusOrder(count);

            // 散布の中心と各軸の広がりを決める
            Vector2 axisScale = _axisScale;
            Vector3 origin = center;

            if (_fitToSpawnAreas && TryResolveFieldBounds(out Bounds fieldBounds))
            {
                // 正規化半径1に対し各軸の半径を倍率として与えることで、フィールドの縦横比へそのまま合わせる
                float halfX = Mathf.Max(0.01f, fieldBounds.extents.x) * _fitMargin;
                float halfZ = Mathf.Max(0.01f, fieldBounds.extents.z) * _fitMargin;

                axisScale = new Vector2(halfX, halfZ);
                outer = 1.0f;
                inner = Mathf.Clamp01(_innerRadius / Mathf.Min(halfX, halfZ));

                origin = fieldBounds.center;
            }

            origin += FieldContext.IsReady ? FieldContext.Rotation * _centerOffset : _centerOffset;

            for (int i = 0; i < count; i++)
            {
                Vector3 candidate = Vector3.zero;

                int retryCount = Mathf.Max(1, _separationRetryCount);

                // 最低距離を満たす点を探す。見つからなければ最後の候補をそのまま使う
                for (int retry = 0; retry < retryCount; ++retry)
                {
                    candidate = CreateCandidate(origin, i, count, _radiusOrder[i], inner, outer, axisScale);

                    if (IsFarEnough(candidate, results)) break;
                }

                if (_snapToGround && !TrySnapToGround(ref candidate) && _discardWhenNoGround) continue;

                results.Add(candidate);
            }

            // 次回呼び出しでは、開始角度をずらす
            _rotationSeed = Mathf.Repeat(_rotationSeed + GoldenAngleDegrees, 360f);

            return results.Count;
        }



        // 内部処理
        // ------------------------------------------------------

        /// <summary>
        /// 半径の割り当て順をシャッフルして作る
        /// </summary>
        /// <param name="count">要素数</param>
        private void BuildShuffledRadiusOrder(int count)
        {
            _radiusOrder.Clear();

            for (int i = 0; i < count; ++i)
            {
                _radiusOrder.Add(i);
            }

            // Fisher-Yatesアルゴリズムでシャッフルする
            // Fisher-Yatesアルゴリズムとは、配列の要素をランダムに並べ替えるための効率的なアルゴリズムらしい！
            // 各要素を一度だけ処理するため、O(n)の時間でシャッフルが可能と！
            for (int i = count - 1; i > 0; --i)
            {
                // 0からiまでの範囲でランダムなインデックスを選ぶ
                int j = UnityEngine.Random.Range(0, i + 1);

                // 選ばれたインデックスの要素と現在の要素を入れ替える
                (_radiusOrder[i], _radiusOrder[j]) = (_radiusOrder[j], _radiusOrder[i]);
            }
        }


        /// <summary>
        /// 直交座標系の候補点を作る
        /// </summary>
        /// <param name="center">中心の座標</param>
        /// <param name="index">点のインデックス</param>
        /// <param name="count">総数</param>
        /// <param name="radiusIndex">半径のインデックス</param>
        /// <param name="inner">内半径</param>
        /// <param name="outer">外半径</param>
        /// <returns></returns>
        private Vector3 CreateCandidate(Vector3 center, int index, int count, int radiusIndex, float inner, float outer, Vector2 axisScale)
        {
            float angle = CreateAngle(index, count);
            float radius = CreateRadius(radiusIndex, count, inner, outer);

            // FieldContextが初期化されていない場合はワールド座標系の右方向と前方向を使う
            Vector3 axisRight = FieldContext.IsReady ? FieldContext.Rotation * Vector3.right : Vector3.right;
            Vector3 axisForward = FieldContext.IsReady ? FieldContext.Rotation * Vector3.forward : Vector3.forward;

            // 角度をラジアンに変換して、極座標から直交座標へ変換する
            float radian = angle * Mathf.Deg2Rad;

            // 一様な円を軸ごとに拡大しても密度の一様さは保たれるため、楕円化しても偏りは生じない
            Vector3 offset =
                axisRight * (Mathf.Cos(radian) * radius * axisScale.x) +
                axisForward * (Mathf.Sin(radian) * radius * axisScale.y);

            return center + offset;
        }



        /// <summary>
        /// 散布する点の候補を作る
        /// </summary>
        /// <param name="index">散布する点のインデックス</param>
        /// <param name="count">総数</param>
        /// <returns>できた角度</returns>
        private float CreateAngle(int index, int count)
        {
            if (_pattern == BocchaScatterPattern.GoldenSpiral)
            {
                return _rotationSeed + index * GoldenAngleDegrees;
            }

            float sectorWidth = 360f / count;
            float sectorCenter = _rotationSeed + sectorWidth * index;

            if (_pattern == BocchaScatterPattern.Ring)
            {
                return sectorCenter;
            }

            // セクターの幅の中だけで揺らすので、隣のセクターへはみ出さない
            return sectorCenter + UnityEngine.Random.Range(-0.5f, 0.5f) * sectorWidth * _angleJitter;
        }


        /// <summary>
        /// 半径の割り当て順をシャッフルして作る
        /// </summary>
        /// <param name="radiusIndex">半径インデックス</param>
        /// <param name="count">総数</param>
        /// <param name="inner">内半径</param>
        /// <param name="outer">外半径</param>
        /// <returns>半径</returns>
        private float CreateRadius(int radiusIndex, int count, float inner, float outer)
        {
            if (_pattern == BocchaScatterPattern.Ring)
                return outer;

            float t = (radiusIndex + 0.5f + UnityEngine.Random.Range(-0.5f, 0.5f) * _radiusJitter) / count;

            t = Mathf.Clamp01(t);

            // 半径を一様にとると内側が密になるため、面積が均等になるよう平方根で補正する
            return Mathf.Sqrt(Mathf.Lerp(inner * inner, outer * outer, t));
        }


        /// <summary>
        /// 候補点が、すでに置いた点との最低距離を満たすかどうかを判定する
        /// </summary>
        /// <param name="candidate">候補点</param>
        /// <param name="placed">すでに置いた点のリスト</param>
        /// <returns>置けるかどうか</returns>
        private bool IsFarEnough(Vector3 candidate, List<Vector3> placed)
        {
            if (_minSeparation <= 0.0f) return true;

            float sqrMin = _minSeparation * _minSeparation;

            // すでに置いた点との距離をチェックする
            for (int i = 0; i < placed.Count; ++i)
            {
                if ((placed[i] - candidate).sqrMagnitude < sqrMin) return false;
            }

            return true;
        }


        private bool TrySnapToGround(ref Vector3 position)
        {
            // フィールドの上下方向を取得する。FieldContextが未初期化の場合はワールドの上方向を使う
            Vector3 up = FieldContext.IsReady ? FieldContext.Up : Vector3.up;
            Vector3 origin = position + up * _groundRayHeight;

            // 地面を探す
            if (!Physics.Raycast(origin, -up, out RaycastHit hit, _groundRayHeight * 2f, _groundMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            position = hit.point + up * _groundClearance;

            return true;
        }

        /// <summary>
        /// シーン上のCollectibleSpawnAreaを全て包含する範囲を取得する。
        /// 毎回検索すると重いため一度だけキャッシュする。
        /// </summary>
        /// <param name="bounds">取得した範囲</param>
        /// <returns>1つ以上見つかった場合はtrue</returns>
        private bool TryResolveFieldBounds(out Bounds bounds)
        {
            if (_hasCachedBounds)
            {
                bounds = _cachedBounds;

                return true;
            }

            CollectibleSpawnArea[] areas =
                UnityEngine.Object.FindObjectsByType<CollectibleSpawnArea>(FindObjectsSortMode.None);

            if (areas == null || areas.Length == 0)
            {
                bounds = default;

                return false;
            }

            Bounds combined = areas[0].Bounds;

            for (int i = 1; i < areas.Length; ++i)
            {
                combined.Encapsulate(areas[i].Bounds);
            }

            _cachedBounds = combined;
            _hasCachedBounds = true;

            bounds = combined;

            return true;
        }

        /// <summary>
        /// フィールド範囲のキャッシュを破棄する。シーンを切り替えた場合に呼ぶ。
        /// </summary>
        public void InvalidateFieldBoundsCache() => _hasCachedBounds = false;
    }
}
