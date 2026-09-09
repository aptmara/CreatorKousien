// ------------------------------------------------------------
// File		: BocchaGimmick_HalloweenFever.cs
// Summary	: キャパオーバー後の分身フェーズを制御するギミック
//
// Author	: [浅野 勇生]
// Created	: 2026-09-09
//
// Notes	:
// - 後始末はCancel側で行う！
// - フィールドのお邪魔は煙と一緒に一掃する
// - ラウンドごとの調整値はApplyTuningで受け取る！
// - 勝手に何もない瞬間をつくって煙→待ち→晴れるときに出現みたいにするねー
// 　仕様違ったらもどすけど一旦作って甲斐に提案しますお
// ------------------------------------------------------------
using System.Collections.Generic;
using Game.Core.Events;
using Game.Gameplay.Collectibles;
using Game.Gameplay.Stage;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// 「ハロウィン・ナイト・フィーバーじゃ！」の制御をするクラス！
    /// </summary>
    [CreateAssetMenu(fileName = "Gimmick_BocchaFever", menuName = "Boss/Gimmicks/Boccha/HalloweenFever")]
    public sealed class BocchaGimmick_HalloweenFever : BossGimmickSO, IBocchaTunable
    {
        /// <summary>
        /// 分身フェーズの進行段階
        /// </summary>
        private enum FeverPhase
        {
            // 爆発
            Explode = 0,

            // 誰もいない状態を見せる
            Hidden = 1,

            // 煙
            Smoke = 2,

            // 分身戦
            Battle = 3,

            // 撃破後の咆哮
            Rcar = 4,

            // 攻撃後に消えて戻る
            Recover = 5,

            // 戻り位置で煙を出して待つ
            RecoverSmoke = 6,

            // 完了
            Done = 7,
        }


        [Header("--- 分身 ---")]

        [SerializeField]
        [Tooltip("ダミーのPrefab")]
        private GameObject _cloneUnitPrefab;

        [SerializeField]
        [Min(0)]
        [Tooltip("ダミーの数。ラウンド調整値で上書きされる")]
        private int _cloneCount = 2;

        [SerializeField]
        [Min(1f)]
        [Tooltip("本体とダミーを並べる全体の幅。一番外側どうしの距離")]
        private float _lineupTotalWidth = 30.0f;

        [SerializeField]
        [Min(1f)]
        [Tooltip("隣どうしの最低間隔。数が増えてもこれ以上は近づかない")]
        private float _minLineupSpacing = 10.0f;

        [SerializeField]
        [Tooltip("VFXの位置補正。ルート原点と見た目がずれている場合に使う")]
        private Vector3 _vfxOffset = new Vector3(0.0f, -4.5f, 0.0f);

        [SerializeField]
        [Tooltip("ONならフィールド中心を並びの基準にする。OFFならボスの現在位置")]
        private bool _lineupFromFieldCenter = true;

        [SerializeField]
        [Min(0f)]
        [Tooltip("フィールド端からこれだけ内側に収める")]
        private float _lineupMargin = 6.0f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("爆発してから、誰もいない状態を見せる時間")]
        private float _hiddenDuration = 0.4f;

        [SerializeField]
        [Tooltip("爆発時にボスの見た目を隠すかどうか")]
        private bool _hideRendererOnExplode = true;

        [SerializeField]
        [Min(0f)]
        [Tooltip("時間切れで攻撃したあと、姿を消してから戻るまでの時間")]
        private float _recoverDuration = 0.8f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("出現時の煙だけ、この高さぶん上に出す")]
        private float _spawnVfxExtraHeight = 2.5f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("分身フェーズ終了後、ボスが無敵になる時間。連続でフィーバーに入るのを防ぐ")]
        private float _invincibleAfterFever = 2.0f;

        [SerializeField]
        [Tooltip("ONならボスの向きをダミーへ引き継ぐ。二重に回転してしまう場合はOFF")]
        private bool _inheritBossRotation = false;

        [SerializeField]
        [Tooltip("ONなら分身フェーズ後に必ずフィールド中央へ戻る。OFFなら開始位置へ戻る")]
        private bool _returnToFieldCenter = true;

        [SerializeField]
        [Tooltip("並び幅をフィールド範囲でクランプするかどうか。OFFならLineup Total Widthをそのまま使う")]
        private bool _clampLineupToField = false;



        [Header("--- 体力・攻撃力 ---")]

        [SerializeField]
        [Min(1f)]
        [Tooltip("分身後本体体力。ラウンド調整値で上書きされる")]
        private float _mainHp = 100.0f;

        [SerializeField]
        [Min(1f)]
        [Tooltip("分身後ダミー体力。ラウンド調整値で上書きされる")]
        private float _cloneHp = 30.0f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("分身後本体攻撃力。時間切れ時にバリアへ与える")]
        private float _mainAttack = 25.0f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("分身後ダミー攻撃力。残っている数だけ加算される")]
        private float _cloneAttack = 10.0f;


        [Header("--- 見た目差分 ---")]

        [SerializeField]
        [Tooltip("本体の見た目")]
        private BocchaAppearance _mainAppearance = new BocchaAppearance();

        [SerializeField]
        [Tooltip("ダミーの見た目。数が足りない場合は末尾を使い回す")]
        private List<BocchaAppearance> _cloneAppearances = new List<BocchaAppearance>();

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("0で本体とダミーが見分けられない、1で設定した差分そのまま。難易度に直結する")]
        private float _appearanceDistinctiveness = 0.5f;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("分身フェーズ中の全体の大きさ")]
        private float _feverBaseScale = 0.7f;


        [Header("--- 時間 ---")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("煙が晴れるまでの時間")]
        private float _smokeDuration = 1.0f;

        [SerializeField]
        [Min(1f)]
        [Tooltip("本体を倒しきる制限時間。BossGimmickDataのTimeout Durationより短くすること")]
        private float _challengeDuration = 12.0f;


        [Header("--- 演出 ---")]

        [SerializeField]
        [Tooltip("爆発時のVFX")]
        private GameObject _explosionVfxPrefab;

        [SerializeField]
        [Tooltip("煙のVFX。分身の出現とお邪魔の一掃で共用する")]
        private GameObject _smokeVfxPrefab;

        [SerializeField]
        [Min(0f)]
        [Tooltip("VFXを破棄するまでの時間")]
        private float _vfxLifeTime = 3.0f;

        [SerializeField]
        [Tooltip("見た目を隠している間、当たり判定も切るかどうか")]
        private bool _disableColliderWhileHidden = true;


        [Header("--- 撃破後の咆哮 ---")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("撃破後、怒って咆哮している時間")]
        private float _roarDuration = 1.5f;

        [SerializeField]
        [Tooltip("咆哮時に鳴らすAnimatorのTrigger名。空なら何もしない")]
        private string _roarAnimTrigger = "Roar";

        [SerializeField]
        [Tooltip("咆哮時のVFX")]
        private GameObject _roarVfxPrefab;

        [SerializeField]
        [Tooltip("咆哮VFXの位置補正。ボスから見て右上に出したい場合は x と y をプラスにする")]
        private Vector3 _roarVfxOffset = new Vector3(4.0f, 5.0f, 0.0f);

        [SerializeField]
        [Tooltip("咆哮時にカメラを揺らすかどうか")]
        private bool _shakeCameraOnRoar = true;

        [SerializeField]
        [Min(0f)]
        [Tooltip("咆哮の揺れの長さ")]
        private float _roarShakeDuration = 0.4f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("咆哮の揺れの強さ")]
        private float _roarShakeStrength = 0.35f;



        // ランタイム状態
        // ------------------------------------------------------------

        // 分身のインスタンスを保持する
        private readonly List<BocchaCloneUnit> _clones = new List<BocchaCloneUnit>();

        // 分身の出現位置を保持する
        private BocchaFeverBody _feverBody;

        // 分身の揺れを制御する
        private BocchaSwayMover _swayMover;

        // 分身の体力ゲージを制御する
        private BocchaCapacityGauge _capacityGauge;

        // 分身フェーズの進行段階を保持する
        private FeverPhase _phase;

        // 分身フェーズの経過時間を保持する
        private float _phaseTimer;

        // 分身フェーズが完了したかどうかを保持する
        private bool _isComplete;

        // 並べ替えで動かす前のボスの位置を保持する
        private Vector3 _originalPosition;

        // ボス本体のRendererをキャッシュする
        private Renderer[] _bossRenderers;

        // フィールド範囲のキャッシュ
        private Bounds _fieldBounds;
        private bool _hasFieldBounds;

        // ボス本体のColliderをキャッシュする
        private Collider[] _bossColliders;

        // 復帰演出のあとにダウンさせるかどうか
        private bool _pendingDown;

        // 本体に実際に掛けたスケール
        private float _appliedMainScale = 1.0f;



        /// <summary>
        /// 分身フェーズが完了したかどうか
        /// </summary>
        public override bool IsComplete => _isComplete;


        /// <summary>
        /// 分身フェーズがTickするかどうか
        /// </summary>
        public override bool IsTick => true;


        /// <summary>
        /// 初期化する
        /// </summary>
        /// <param name="context">ボスのコンテキスト</param>
        public override void Initialize(BossContext context)
        {
            base.Initialize(context);

            _feverBody = context.Transform.GetComponentInChildren<BocchaFeverBody>();
            _swayMover = context.Transform.GetComponentInChildren<BocchaSwayMover>();
            _capacityGauge = context.Transform.GetComponentInChildren<BocchaCapacityGauge>();

            if (_feverBody == null)
            {
                Debug.LogWarning("[Fever] BocchaFeverBodyが見つかりません。本体HPを削れません。");
            }
        }


        /// <summary>
        /// 爆発してフィールドを一掃し、煙で隠す
        /// </summary>
        public override void Execute()
        {
            _isComplete = false;
            _phaseTimer = 0.0f;
            _clones.Clear();
            _pendingDown = false;

            // 並べ替えで動かす前の位置を覚えておく
            _originalPosition = Context.Transform.position;

            EventBus.Publish(new BocchaFeverStartedEvent(_cloneCount, _challengeDuration));

            _swayMover?.SetMoveEnable(false);

            PlayVfx(_explosionVfxPrefab, Context.Transform.position);

            // 爆発と同時に見た目を消し、いなくなったことを見せる
            SetBossVisible(false);

            // フィールドのお邪魔も煙と一緒に消す
            BocchaHazardRegistry.DespawnAll(_smokeVfxPrefab, _vfxLifeTime);

            _phase = FeverPhase.Hidden;
        }


        /// <summary>
        /// 段階を進める
        /// </summary>
        /// <param name="dt">経過時間</param>
        public override void Tick(float dt)
        {
            if (_isComplete)
            {
                return;
            }

            _phaseTimer += dt;

            switch (_phase)
            {
                case FeverPhase.Hidden:
                    if (_phaseTimer < _hiddenDuration)
                        return;

                    // 何もいない状態を見せた後、煙を出す
                    PlayVfx(_smokeVfxPrefab, Context.Transform.position);

                    _phase = FeverPhase.Smoke;
                    _phaseTimer = 0.0f;
                    return;

                case FeverPhase.Smoke:
                    if (_phaseTimer < _smokeDuration)
                        return;

                    SpawnClones();

                    // 煙が晴れると同時に姿を見せる
                    SetBossVisible(true);

                    _phase = FeverPhase.Battle;
                    _phaseTimer = 0.0f;
                    return;

                case FeverPhase.Battle:
                    if (_feverBody != null && _feverBody.IsDefeated)
                    {
                        Resolve(true);
                        return;
                    }

                    if (_phaseTimer >= _challengeDuration)
                    {
                        Resolve(false);
                    }

                    return;

                case FeverPhase.Rcar:
                    if (_phaseTimer < _roarDuration)
                        return;

                    // 咆哮しきってから爆発して消える
                    PlayVfx(_explosionVfxPrefab, Context.Transform.position);

                    SetBossVisible(false);

                    _phase = FeverPhase.Recover;
                    _phaseTimer = 0.0f;
                    return;

                case FeverPhase.Recover:
                    if (_phaseTimer < _recoverDuration)
                        return;

                    // 透明のまま元の位置へ戻し、そこに煙だけを出す
                    Context.Transform.position = ResolveReturnPosition();

                    PlayVfx(_smokeVfxPrefab, Context.Transform.position + GetSpawnVfxLift());

                    _phase = FeverPhase.RecoverSmoke;
                    _phaseTimer = 0.0f;
                    return;

                case FeverPhase.RecoverSmoke:
                    if (_phaseTimer < _smokeDuration)
                        return;

                    // 煙が晴れると同時に姿を見せる
                    Cleanup();

                    _isComplete = true;

                    // 復帰しきってからダウンさせる
                    if (_pendingDown)
                    {
                        _pendingDown = false;

                        Context.Controller.TriggerDown();
                    }

                    return;
            }
        }


        /// <summary>
        /// ギミックの中断, TriggerDown()からも呼ばれる
        /// </summary>
        public override void Cancel() => Cleanup();


        /// <summary>
        /// ラウンド調整値を適用する
        /// </summary>
        /// <param name="tuning">適用する調整値</param>
        public void ApplyTuning(BocchaRoundTuning tuning)
        {
            if (tuning == null)
                return;

            _cloneCount = Mathf.Max(0, tuning.CloneCount);
            _mainHp = tuning.MainHp;
            _cloneHp = tuning.CloneHp;
            _mainAttack = tuning.MainAttack;
            _cloneAttack = tuning.CloneAttack;
        }


        // 内部処理
        // ------------------------------------------------------------

        /// <summary>
        /// 本体とダミーを横一列に並べる
        /// </summary>
        private void SpawnClones()
        {
            _feverBody?.BeginFever(_mainHp);

            ApplyMainAppearance();

            int total = _cloneCount + 1; // 本体 + ダミーの合計数

            // 並びの立ち位置を作る
            List<Vector3> slots = new List<Vector3>(total);
            Vector3 right = FieldContext.IsReady ? FieldContext.Rotation * Vector3.right : Vector3.right;

            bool hasBounds = TryResolveFieldBounds(out Bounds fieldBounds);

            // 並びの基準
            Vector3 center = Context.Transform.position;

            if (_lineupFromFieldCenter && hasBounds)
            {
                // 高さはボスのものを保つ
                center = new Vector3(fieldBounds.center.x, center.y, center.z);
            }

            // 全体幅で割った間隔を使い、最低間隔と必ず確保する
            float spacing = total > 1 ? Mathf.Max(_minLineupSpacing, _lineupTotalWidth / (total - 1)) : 0.0f;

            // 一番外側が場外へ出るなら間隔を詰める
            if (_clampLineupToField && total > 1 && hasBounds)
            {
                // rightがどの軸を向いていても効くように、両輪ぶんを合成する
                float halfWidth = Mathf.Abs(right.x) * fieldBounds.extents.x + Mathf.Abs(right.z) * fieldBounds.extents.z;

                float halfLimit = Mathf.Max(0.0f, halfWidth - _lineupMargin);
                float maxSpacing = halfLimit * 2.0f / (total - 1);

                spacing = Mathf.Min(spacing, maxSpacing);
            }

            for (int i = 0; i < total; ++i)
            {
                float offset = (i - (total - 1) * 0.5f) * spacing;

                slots.Add(center + right * offset);
            }


            // シャッフルして本体の立ち位置をランダムにする
            for (int i = slots.Count - 1; i > 0; --i)
            {
                int swap = Random.Range(0, i + 1);

                (slots[i], slots[swap]) = (slots[swap], slots[i]);
            }

            // 出現の煙だけ少し上に出す
            Vector3 spawnVfxLift = GetSpawnVfxLift();

            Context.Transform.position = slots[0];

            PlayVfx(_smokeVfxPrefab, slots[0] + spawnVfxLift);

            if (_cloneUnitPrefab == null)
            {
                Debug.LogWarning("[Fever] 分身のPrefabが設定されていません");
                return;
            }

            // Prefab側にモデルの回転が焼き込まれているため、既定では引き継がない
            Quaternion cloneRotation = _inheritBossRotation ? Context.Transform.rotation : _cloneUnitPrefab.transform.rotation;


            for (int i = 0; i < _cloneCount; ++i)
            {
                GameObject instance = Instantiate(_cloneUnitPrefab, slots[i + 1], cloneRotation);

                PlayVfx(_smokeVfxPrefab, slots[i + 1] + spawnVfxLift);

                if (!instance.TryGetComponent(out BocchaCloneUnit clone))
                {
                    Debug.LogError("[Fever] PrefabにBocchaCloneUnitがありません。", instance);

                    Destroy(instance);

                    continue;
                }

                clone.Initialize(Context.Controller.BossInstanceId, _cloneHp, ResolveCloneAppearance(i), _appearanceDistinctiveness, _feverBaseScale);

                _clones.Add(clone);
            }
        }


        /// <summary>
        /// 本体側の見た目差分を反映する
        /// </summary>
        private void ApplyMainAppearance()
        {
            float scale = _feverBaseScale;

            if (_mainAppearance != null)
            {
                scale *= _mainAppearance.GetScale(_appearanceDistinctiveness);
            }

            if (Mathf.Approximately(scale, 1.0f))
                return;

            // 戻すときに使うので実際に掛けた値を覚えておく
            _appliedMainScale = scale;

            Context.Transform.localScale *= scale;
        }


        /// <summary>
        /// ダミーの見た目差分を取得する
        /// </summary>
        /// <param name="index">ダミーの番号</param>
        private BocchaAppearance ResolveCloneAppearance(int index)
        {
            if (_cloneAppearances.Count == 0)
                return null;

            return _cloneAppearances[Mathf.Min(index, _cloneAppearances.Count - 1)];
        }


        /// <summary>
        /// ダミーの見た目差分を取得する
        /// </summary>
        /// <param name="index">ダミーの番号</param>
        /// <returns></returns>
        private void RestoreMainAppearance()
        {
            if (Mathf.Approximately(_appliedMainScale, 1.0f) || _appliedMainScale <= 0.0f)
                return;

            Context.Transform.localScale /= _appliedMainScale;

            _appliedMainScale = 1.0f;
        }


        /// <summary>
        /// 決着処理
        /// </summary>
        /// <param name="isSuccess">本体を倒しきれたかどうか</param>
        private void Resolve(bool isSuccess)
        {
            int remaining = CountAliveClones();

            // 成功か失敗かを通知する
            EventBus.Publish(new BocchaFeverResolvedEvent(isSuccess, remaining));

            // 成否にかかわらず、消えてから中央へ戻る演出を通す
            _pendingDown = isSuccess;

            // ダミーはここで消える
            DestroyClones();

            // 咆哮から再出現までは一切殴られないようにする
            float invincibleTotal = _roarDuration + _recoverDuration + _smokeDuration + _invincibleAfterFever;

            _feverBody?.BeginInvincible(invincibleTotal);

            if (isSuccess)
            {
                Debug.Log("[Fever] <color=green>本体撃破！ 咆哮してから仕切り直します</color>");

                PlayRoar();

                _phase = FeverPhase.Rcar;
                _phaseTimer = 0.0f;

                return;
            }

            // 本体とダミーが一緒に攻撃してくる
            float damage = _mainAttack + remaining * _cloneAttack;

            EventBus.Publish(new RuleBarrierAttackEvent(damage, Context.Transform.position));

            Debug.Log("[Fever] <color=orange>時間切れ！</color> 残りダミー数: " + remaining + " 攻撃力: " + damage);

            // 失敗時は爆発してから姿を消す。咆哮はしない
            PlayVfx(_explosionVfxPrefab, Context.Transform.position);

            SetBossVisible(false);

            _phase = FeverPhase.Recover;
            _phaseTimer = 0.0f;
        }


        /// <summary>
        /// 後始末。何度呼ばれても問題ないようにする
        /// </summary>
        private void Cleanup()
        {
            DestroyClones();

            // 中断された場合も含め、必ず姿を戻す
            SetBossVisible(true);

            // 体力ゲージを消す
            RestoreMainAppearance();

            _feverBody?.EndFever();
            _feverBody?.BeginInvincible(_invincibleAfterFever);
            _capacityGauge?.ResetGauge();

            // 並べ替えでずらした位置を戻す。これをやらないと左右移動の中心が毎回ずれていく
            Context.Transform.position = ResolveReturnPosition();

            _swayMover?.ResetCenter();
            _swayMover?.SetMoveEnable(true);

            _phase = FeverPhase.Done;
        }



        /// <summary>
        /// 生きている分身の数を数える
        /// </summary>
        /// <returns>生きてる分身の数</returns>
        private int CountAliveClones()
        {
            int count = 0;

            foreach (BocchaCloneUnit clone in _clones)
            {
                if (clone != null && clone.IsAlive)
                {
                    count++;
                }
            }

            return count;
        }


        /// <summary>
        /// VFXを再生する
        /// </summary>
        /// <param name="prefab">プレファブ</param>
        /// <param name="position">位置</param>
        private void PlayVfx(GameObject prefab, Vector3 position)
        {
            if (prefab == null)
                return;

            GameObject vfx = Instantiate(prefab, position + _vfxOffset, Quaternion.identity);

            Destroy(vfx, _vfxLifeTime);

        }


        /// <summary>
        /// 撃破後の咆哮演出を再生する
        /// </summary>
        private void PlayRoar()
        {
            if (Context.Animator != null && !string.IsNullOrEmpty(_roarAnimTrigger))
            {
                Context.Animator.SetTrigger(_roarAnimTrigger);
            }

            // フィールドが傾いても右上を保つよう、フィールドの軸で補正する
            Vector3 right = FieldContext.IsReady ? FieldContext.Rotation * Vector3.right : Vector3.right;
            Vector3 up = FieldContext.IsReady ? FieldContext.Up : Vector3.up;
            Vector3 forward = FieldContext.IsReady ? FieldContext.Rotation * Vector3.forward : Vector3.forward;

            Vector3 roarPosition = Context.Transform.position + right * _roarVfxOffset.x + up * _roarVfxOffset.y + forward * _roarVfxOffset.z;

            PlayVfx(_roarVfxPrefab, roarPosition);

            if (!_shakeCameraOnRoar)
                return;

            EventBus.Publish(new CameraShakeRequestedEvent(_roarShakeDuration, _roarShakeStrength, _roarShakeStrength, 30.0f));
        }


        /// <summary>
        /// 出現時の煙を上に持ち上げる量を取得する
        /// </summary>
        private Vector3 GetSpawnVfxLift()
        {
            Vector3 up = FieldContext.IsReady ? FieldContext.Up : Vector3.up;

            return up * _spawnVfxExtraHeight;
        }


        /// <summary>
        /// ダミーを煙とともに消す
        /// </summary>
        private void DestroyClones()
        {
            foreach (BocchaCloneUnit clone in _clones)
            {
                if (clone == null)
                    continue;

                PlayVfx(_smokeVfxPrefab, clone.transform.position);

                Destroy(clone.gameObject);
            }

            _clones.Clear();
        }


        /// <summary>
        /// ボス本体の見た目を切り替える
        /// </summary>
        /// <param name="isVisible">表示するかどうか</param>
        private void SetBossVisible(bool isVisible)
        {
            if (_hideRendererOnExplode)
            {
                if (_bossRenderers == null)
                {
                    _bossRenderers = Context.Transform.GetComponentsInChildren<Renderer>(true);
                }

                foreach (Renderer renderer in _bossRenderers)
                {
                    if (renderer != null)
                        renderer.enabled = isVisible;
                }
            }

            if (!_disableColliderWhileHidden)
            {
                return;
            }

            if (_bossColliders == null)
            {
                _bossColliders = Context.Transform.GetComponentsInChildren<Collider>(true);
            }

            // 隠れている間に殴られて通常HPが減るのを防ぐ
            foreach (Collider bossCollider in _bossColliders)
            {
                if (bossCollider != null)
                    bossCollider.enabled = isVisible;
            }
        }


        /// <summary>
        /// シーン上のCollectibleSpawnAreaを包む範囲を取得する
        /// </summary>
        /// <param name="bounds">取得した範囲</param>
        /// <returns>1つ以上見つかった場合はtrue</returns>
        private bool TryResolveFieldBounds(out Bounds bounds)
        {
            if (_hasFieldBounds)
            {
                bounds = _fieldBounds;
                return true;
            }

            CollectibleSpawnArea[] areas = UnityEngine.Object.FindObjectsByType<CollectibleSpawnArea>(FindObjectsSortMode.None);

            // 見つからなかった場合は失敗
            if (areas == null || areas.Length == 0)
            {
                bounds = default;
                return false;
            }

            Bounds combined = areas[0].Bounds;

            // 2つ目以降の範囲を包む
            for (int i = 1; i < areas.Length; ++i)
            {
                combined.Encapsulate(areas[i].Bounds);
            }

            _fieldBounds = combined;
            _hasFieldBounds = true;
            bounds = combined;

            return true;
        }


        /// <summary>
        /// 分身フェーズ後にボスが戻る位置を決定する
        /// </summary>
        /// <returns></returns>
        private Vector3 ResolveReturnPosition()
        {
            if (!_returnToFieldCenter)
            {
                return _originalPosition;
            }

            if (!TryResolveFieldBounds(out Bounds fieldBounds))
            {
                return _originalPosition;
            }

            // 高さは開始時のものを保つ
            return new Vector3(fieldBounds.center.x, _originalPosition.y, _originalPosition.z);
        }
    }
}
