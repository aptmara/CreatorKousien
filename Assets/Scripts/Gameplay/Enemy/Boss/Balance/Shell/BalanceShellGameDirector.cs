/**
 * File: BalanceShellGameDirector
 * 
 * 天秤ボスの演出の進行役
 * 見せる=>容器を被せる=>シャッフル=>落ち物で開封=>中身稼働=>後片付け
 * 
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Data.Collectibles;
using Game.Gameplay.Collectibles;

namespace Game.Gameplay.Enemy.Boss
{

    [DisallowMultipleComponent]
    public sealed class BalanceShellGameDirector : MonoBehaviour
    {
        private sealed class Entry
        {
            public TraySide Slot;
            public BalanceTrayContentBase Content;
            public BalanceShellCover Cover;
            public Vector3 ContentBaseScale;
            public bool Revealed;

            // 皿への追従
            public bool CoverFollow;
            public bool ContentFollowCover;
            public bool ContentFollowPan;
            public Vector3 CoverLocalOffset;
            public Quaternion CoverLocalRot = Quaternion.identity;
            public Vector3 PanLocalOffset;
            public Quaternion PanLocalRot;
        }

        [Header("===== 参照 =====")]
        [SerializeField]
        private BossBalanceBeamController _beam;
        [SerializeField]
        private RealisticBalanceScale _scale;
        [SerializeField]
        private BalanceShellCover _coverPrefab;

        [Header("===== 配置 ======")]
        [SerializeField]
        private Vector3 _contentOffset = new Vector3(0.0f, 0.3f, 0.0f);
        [SerializeField]
        private Vector3 _coverRestOffset = new Vector3(0.0f, 0.3f, 0.0f);
        [SerializeField, Tooltip("Onなら皿の傾きに合わせてふたと中身も傾く")]
        private bool _followPanRotation;
        [SerializeField, Min(0.0f)]
        private float _showcaseHeight = 4.0f;
        [SerializeField, Min(0.0f)]
        private float _coverDropHeight = 8.0f;

        [Header("===== 演出 ======")]
        [SerializeField, Min(0.1f)]
        private float _showcaseDuration = 2.0f;
        [SerializeField, Min(0.05f)]
        private float _coverDropDuration = 0.6f;
        [SerializeField, Min(0.0f)]
        private float _coverStagger = 0.25f;
        [SerializeField, Min(0.0f)]
        private float _preShuffleWait = 0.5f;
        [SerializeField, Min(0.0f)]
        private float _postShuffleWait = 0.6f;

        [Header("==== シャッフル ======")]
        [SerializeField]
        private Vector2Int _swapCountRange = new Vector2Int(5, 8);
        [SerializeField, Min(0.05f)]
        private float _swapDurationStart = 0.6f;
        [SerializeField, Min(0.05f)]
        private float _swapDurationEnd = 0.25f;
        [SerializeField, Tooltip("入れ替えずに途中で戻るフェイントの確率。最初と最後の入れ替えでは出ない")]
        [Range(0.0f, 1.0f)] private float _feintChange = 0.2f;
        [SerializeField]
        private float _swapArcHeight = 1.5f;
        [SerializeField, Tooltip("すれ違う時の奥行。片方が手前、片方が奥を通る")]
        private float _swapDepth = 1.2f;
        [SerializeField]
        private Vector3 _swapPassAxis = Vector3.forward;

        [Header("===== 開封 ======")]
        [SerializeField]
        private CollectibleData _revealRainData;
        [SerializeField, Min(0)]
        private int _revealRainCountPerCover = 6;
        [SerializeField, Min(0.0f)]
        private float _revealRainInterval = 0.06f;
        [SerializeField, Min(0.0f)]
        private float _revealRainHeight = 8.0f;
        [SerializeField, Min(0.0f)]
        private float _revealRainSpread = 1.5f;
        [SerializeField, Min(0.1f)]
        private float _revealRainScale = 1.0f;
        [SerializeField, Tooltip("この時間内に開かなければ強制的に空ける(進行停止防止)")]
        [Min(1.0f)] private float _revealTimeout = 6.0f;

        private readonly List<Entry> _entries = new List<Entry>();
        private BalanceShellContext _ctx;
        private Coroutine _routine;

        public BalanceShellPhase Phase { get; private set; } = BalanceShellPhase.Idle;
        public bool IsRoundFinished { get; private set; } = true;
        public bool IsReady => _ctx != null && _beam != null && _scale != null && _coverPrefab != null;

        public System.Action<BalanceShellPhase> OnPhaseChanged;
        public event System.Action<TraySide, BalanceTrayContentBase> OnItemShowcased;
        public event System.Action<int, int> OnSwap;
        public event System.Action<TraySide, BalanceTrayContentBase> OnContentRevealed;

        public void Initialize(BossContext bossContext)
        {
            Transform root = bossContext.Transform;
            if (_beam == null) _beam = root.GetComponentInChildren<BossBalanceBeamController>();
            if (_scale == null) _scale = root.GetComponentInChildren<RealisticBalanceScale>();

            var collectibles = FindFirstObjectByType<CollectibleSpawner>();
            _ctx = new BalanceShellContext(bossContext, _beam, _scale, collectibles);
        }

        public float EstimateMaxDuration(float resultDuration)
        {
            float cover = 0.35f + _coverDropDuration + _coverStagger;
            float shuffle = _preShuffleWait + _swapCountRange.y * _swapDurationStart + _postShuffleWait;
            return _showcaseDuration + cover + shuffle + _revealTimeout + resultDuration + 0.5f;
        }

        public bool BeginRound(BalanceTrayContentBase leftPrefab, BalanceTrayContentBase rightPrefab, float resultDuration)
        {
            if (!IsReady)
            {
                Debug.LogWarning("[ShellGame] Directorの参照(Beam/Scale/CoverPrefab)が揃っていません", this);
                return false;
            }

            if (leftPrefab == null || rightPrefab == null) return false;

            if (!IsRoundFinished) Abort();

            IsRoundFinished = false;
            _routine = StartCoroutine(RoundRoutine(leftPrefab, rightPrefab, resultDuration));
            return true;
        }

        public void Abort()
        {
            StopAllCoroutines();
            _routine = null;

            if (_scale != null) _scale.SetFrozen(false);
            DestroyEntries();

            if(!IsRoundFinished)
            {
                IsRoundFinished = true;
                SetPhase(BalanceShellPhase.Idle);
            }
        }

        private void OnDisable() => Abort();


        private IEnumerator RoundRoutine(BalanceTrayContentBase leftPrefab, BalanceTrayContentBase rightPrefab, float resultDuration)
        {
            _scale.SetFrozen(true);

            yield return ShowcasePhase(leftPrefab, rightPrefab);
            yield return CoverPhase();
            yield return ShufflePhase();

            _scale.SetFrozen(false);

            yield return RevealPhase();
            yield return ResultPhase(resultDuration);
            yield return CleanupPhase();

            _routine = null;
            IsRoundFinished = true;
            SetPhase(BalanceShellPhase.Idle);
        }

        private IEnumerator ShowcasePhase(BalanceTrayContentBase leftPrefab,BalanceTrayContentBase rightPrefab)
        {
            SetPhase(BalanceShellPhase.Showcase);

            DestroyEntries();
            CreateEntry(TraySide.Left, leftPrefab);
            CreateEntry(TraySide.Right, rightPrefab);

            foreach (var e in _entries)
            {
                e.Content.transform.position = ContentRestPos(e.Slot) + Vector3.up * _showcaseHeight;
                e.Content.transform.localScale = Vector3.zero;
                OnItemShowcased?.Invoke(e.Slot, e.Content);
            }

            const float popIn = 0.4f;
            yield return Tween(popIn, t =>
            {
                float s = EaseOutBack(t);
                foreach (var e in _entries) e.Content.transform.localScale = e.ContentBaseScale * s;
            });

            float hold = Mathf.Max(0.0f, _showcaseDuration - popIn);
            float elapsed = 0.0f;
            while (elapsed < hold)
            {
                elapsed += Time.deltaTime;
                float bob = Mathf.Sin(Time.time * 3.0f) * 0.15f;
                foreach(var e in _entries)
                {
                    e.Content.transform.position = ContentRestPos(e.Slot) + Vector3.up * (_showcaseHeight + bob);
                }
                yield return null;
            }
        }

        private IEnumerator CoverPhase()
        {
            SetPhase(BalanceShellPhase.CoverIn);

            var startPositions = new Vector3[_entries.Count];
            for (int i = 0; i < _entries.Count; ++i) startPositions[i] = _entries[i].Content.transform.position;

            yield return Tween(0.35f, t =>
            {
                float k = EaseOutCubic(t);
                for (int i = 0; i < _entries.Count; ++i)
                {
                    var e = _entries[i];
                    e.Content.transform.position = Vector3.Lerp(startPositions[i], ContentRestPos(e.Slot), k);
                }
            });

            foreach(var e in _entries)
            {
                e.Cover  = Instantiate(_coverPrefab,CoverRestPos(e.Slot) + Vector3.up * _coverDropHeight,TrayRot(e.Slot));
                e.Cover.SetImpactEnable(false);
                e.Cover.Broken += HandleCoverBroken;
            }

            float total = _coverDropDuration + _coverStagger * (_entries.Count - 1);
            yield return Tween(total, t =>
            {
                float elapsed = t * total;
                for (int i = 0; i < _entries.Count; ++i)
                {
                    var e = _entries[i];
                    float local = Mathf.Clamp01((elapsed - i * _coverStagger) / _coverDropDuration);
                    Vector3 rest = CoverRestPos(e.Slot);
                    e.Cover.transform.SetPositionAndRotation(
                        Vector3.LerpUnclamped(rest + Vector3.up * _coverDropHeight, rest, EaseOutBounce(local)),
                        TrayRot(e.Slot)
                        );
                }
            });

            foreach(var e in _entries)
            {
                e.Cover.transform.SetPositionAndRotation(CoverRestPos(e.Slot), TrayRot(e.Slot));
                e.Cover.NotifyLanded();
                // 親子化するとスケールを継承してしまうため、機銃の相対姿勢を記録して追従
                Transform coverSocket = e.Cover.ContentSocket;
                Quaternion coverInv = Quaternion.Inverse(coverSocket.rotation);
                e.CoverLocalOffset = coverInv * (e.Content.transform.position - coverSocket.position);
                e.CoverLocalRot = coverInv * e.Content.transform.rotation;
                e.ContentFollowCover = true;
                e.CoverFollow = true;

                e.Content.SetVisible(false);
            }

        }

        private IEnumerator ShufflePhase()
        {
            SetPhase(BalanceShellPhase.Shuffle);

            yield return new WaitForSeconds(_preShuffleWait);

            int min = Mathf.Max(1, _swapCountRange.x);
            int max = Mathf.Max(min, _swapCountRange.y);
            int swaps = UnityEngine.Random.Range(min, max + 1);

            for(int i = 0; i < swaps;++i)
            {
                float progress = swaps <= 1 ? 1.0f : i / (float)(swaps - 1);
                float duration = Mathf.Lerp(_swapDurationStart,_swapDurationEnd, progress);
                bool feint = i > 0 && i < swaps - 1 && UnityEngine.Random.value < _feintChange;

                OnSwap?.Invoke(i, swaps);
                yield return SwapRoutine(duration,feint);
            }

            yield return new WaitForSeconds(_postShuffleWait);
        }

        private IEnumerator SwapRoutine(float duration, bool feint)
        {
            Entry a = _entries[0];
            Entry b = _entries[1];

            TraySide aSlot = a.Slot;
            TraySide bSlot = b.Slot;
            a.CoverFollow = false;
            b.CoverFollow = false;
            Vector3 axis = _swapPassAxis.sqrMagnitude > 0.0001f ? _swapPassAxis.normalized : Vector3.forward;

            yield return Tween(duration, t =>
            {
                float k = EaseInOut(t);
                float arc = Mathf.Sin(k * Mathf.PI);
                float move = feint ? arc * 0.5f : k;

                Vector3 aFrom = CoverRestPos(aSlot);
                Vector3 bFrom = CoverRestPos(bSlot);
                Quaternion aRot = TrayRot(aSlot);
                Quaternion bRot = TrayRot(bSlot);

                if (a.Cover != null)
                {
                    a.Cover.transform.SetPositionAndRotation(
                        Vector3.Lerp(aFrom, bFrom, move) + Vector3.up * (_swapArcHeight * arc) + axis * (_swapDepth * arc),
                        Quaternion.Slerp(aRot, bRot, move)
                        );
                }
                if (b.Cover != null)
                {
                    b.Cover.transform.SetPositionAndRotation(
                        Vector3.Lerp(bFrom, aFrom, move) + Vector3.up * (_swapArcHeight * arc) - axis * (_swapDepth * arc),
                        Quaternion.Slerp(bRot, aRot, move)
                        );
                }
            });

            if(!feint)
            {
                (a.Slot,b.Slot) = (b.Slot,a.Slot);
            }

            if (a.Cover != null) a.Cover.transform.SetPositionAndRotation(CoverRestPos(a.Slot), TrayRot(a.Slot));
            if (a.Cover != null) b.Cover.transform.SetPositionAndRotation(CoverRestPos(b.Slot), TrayRot(b.Slot));
            a.CoverFollow = true;
            b.CoverFollow = true;
        }

        private IEnumerator RevealPhase()
        {
            SetPhase(BalanceShellPhase.Reveal);

            foreach(var e in _entries)
            {
                if (e.Cover != null) e.Cover.SetImpactEnable(true);
                StartCoroutine(RainRoutine(e));
            }

            float elapsed = 0.0f;
            while(elapsed < _revealTimeout && !AllRevealed())
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            foreach (var e in _entries.ToArray())
            {
                if (e.Revealed) continue;
                if (e.Cover != null) e.Cover.Break();
                else RevealEntry(e);
            }
        }


        private IEnumerator RainRoutine(Entry e)
        {
            if (_revealRainData == null || _ctx.Collectibles == null) yield break;

            yield return new WaitForSeconds(_revealTimeout);
            var wait = new WaitForSeconds(_revealRainInterval);
            for (int i = 0; i  < _revealRainCountPerCover;++i)
            {
                Vector2 o = UnityEngine.Random.insideUnitCircle * _revealRainSpread;
                Vector3 pos = CoverRestPos(e.Slot)
                    + new Vector3(o.x,_revealRainHeight + UnityEngine.Random.Range(0.0f,2.0f),o.y);

                _ctx.Collectibles.SpawnWithVelocity(_revealRainData, pos, Vector3.down * 2.0f, _revealRainScale);
                yield return wait;
            }
        }

        private void HandleCoverBroken(BalanceShellCover cover)
        {
            foreach(var e in _entries)
            {
                if(e.Cover == cover && !e.Revealed)
                {
                    RevealEntry(e);
                    return;
                }
            }
        }

        private void RevealEntry(Entry e)
        {
            e.Revealed = true;

            Transform socket = _beam.GetTraySocket(e.Slot);

            Quaternion inv = Quaternion.Inverse(_followPanRotation ? socket.rotation : Quaternion.identity);
            e.PanLocalOffset = inv * (e.Content.transform.position - socket.position);
            e.PanLocalRot = inv * e.Content.transform.rotation;
            e.ContentFollowCover = false;
            e.ContentFollowPan = true;

            e.Content.SetVisible(true);

            _beam.RegisterItem(e.Slot, e.Content);
            e.Content.Reveal(e.Slot);

            if(_beam.CurrentRaisedSide == e.Slot)
            {
                e.Content.OnTrayRaised();
            }

            OnContentRevealed?.Invoke(e.Slot, e.Content);
        }

        private IEnumerator ResultPhase(float duration)
        {
            SetPhase(BalanceShellPhase.Result);

            float elapsed = 0.0f;
            while(elapsed < duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator CleanupPhase()
        {
            SetPhase(BalanceShellPhase.Cleanup);

            var startScales = new Vector3[_entries.Count];
            for(int i = 0;i < _entries.Count;++i)
            {
                var e = _entries[i];
                startScales[i] = e.Content != null ? e.Content.transform.localScale : Vector3.one;
                if (e.Content != null) e.Content.EndRound();
                if (e.Revealed && _beam != null) _beam.UnregisterItem(e.Slot);
            }

            yield return Tween(0.25f, t =>
            {
                float k = 1.0f - EaseInOut(t);
                for (int i = 0; i < _entries.Count; ++i)
                {
                    var e = _entries[i];
                    if (e.Content != null) e.Content.transform.localScale = startScales[i] * k;
                }
            });

            DestroyEntries();
        }

        private void CreateEntry(TraySide slot, BalanceTrayContentBase prefab)
        {
            var content = Instantiate(prefab);
            content.Bind(_ctx);

            _entries.Add(new Entry
            {
                Slot = slot,
                Content = content,
                ContentBaseScale = content.transform.localScale,
            });
        }

        private bool AllRevealed()
        {
            foreach (var e in _entries)
            {
                if (!e.Revealed) return false;
            }
            return true;
        }

        private void DestroyEntries()
        {
            foreach (var e in _entries)
            {
                if (e.Revealed && _beam != null) _beam.UnregisterItem(e.Slot);

                if(e.Cover != null)
                {
                    e.Cover.Broken -= HandleCoverBroken;
                    Destroy(e.Cover.gameObject);
                }
                if(e.Content != null)
                {
                    e.Content.EndRound();
                    Destroy(e.Content.gameObject);
                }
            }

            _entries.Clear();
        }

        private void LateUpdate()
        {
            if(_beam == null || _entries.Count == 0) return;

            foreach(var e in _entries)
            {
                if(e.Cover != null && e.CoverFollow && !e.Cover.IsBroken)
                {
                    e.Cover.transform.SetPositionAndRotation(CoverRestPos(e.Slot), TrayRot(e.Slot));
                }

                if (e.Content == null) continue;

                if(e.ContentFollowCover && e.Cover != null)
                {
                    Transform socket = e.Cover.ContentSocket;
                    e.Content.transform.SetPositionAndRotation(
                        socket.position + socket.rotation * e.CoverLocalOffset,
                        socket.rotation * e.CoverLocalRot);
                }
                else
                {
                    Transform pan = _beam.GetTraySocket(e.Slot);
                    Quaternion rot = _followPanRotation ? pan.rotation : Quaternion.identity;
                    e.Content.transform.SetPositionAndRotation(pan.position + rot * e.PanLocalOffset, rot * e.PanLocalRot);
                }
            }
        }

        private Quaternion TrayRot(TraySide slot) => _followPanRotation ? _beam.GetTraySocket(slot).rotation : Quaternion.identity;

        private Vector3 ContentRestPos(TraySide slot) => _beam.GetTraySocket(slot).position + TrayRot(slot) * _contentOffset;
        private Vector3 CoverRestPos(TraySide slot) => _beam.GetTraySocket(slot).position + TrayRot(slot) * _coverRestOffset;

        private void SetPhase(BalanceShellPhase phase)
        {
            Phase = phase;
            OnPhaseChanged?.Invoke(phase);
        }

        private static IEnumerator Tween(float duration, System.Action<float> onStep)
        {
            if(duration < 0.0f)
            {
                onStep(1.0f);
                yield break;
            }

            float elapsed = 0.0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                onStep(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
        }

        private static float EaseInOut(float t) => t * t * (3.0f - 2.0f * t);
        private static float EaseOutCubic(float t) => 1.0f - Mathf.Pow(1.0f - t, 3.0f);

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1.0f;
            float u = t - 1.0f;
            return 1.0f + c3 * u * u * u + c1 * u * u;
        }

        private static float EaseOutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;
            if (t < 1.0f / d1) return n1 * t * t;
            if (t < 2.0f / d1) { t -= 1.5f / d1;return n1 * t * t + 0.75f; }
            if(t < 2.5f / d1) { t -= 2.25f / d1;return n1 * t * t + 0.9375f; }
            t -= 2.625f / d1;
            return n1 * t * t + 0.984375f;
        }
    }
}
