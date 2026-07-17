using System;
using System.Collections.Generic;
using Game.Core.Events;
using UnityEngine;


namespace Game.Core.DefenceLine
{
    /// <summary>
    /// 防衛ラインの被弾、損傷、破壊リアクションを制御する。
    /// </summary>
    public class DefenseLineReaction : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        protected readonly struct HitReactionData
        {
            public readonly float Damage;
            public readonly float RemainingHpRatio;
            public readonly Vector3 AttackPosition;

            public HitReactionData(float damage, float remainingHpRatio, Vector3 attackPosition)
            {
                Damage = damage;
                RemainingHpRatio = remainingHpRatio;
                AttackPosition = attackPosition;
            }
        }

        protected readonly struct BreakReactionData
        {
            public readonly float Damage;
            public readonly Vector3 AttackPosition;

            public BreakReactionData(float damage, Vector3 attackPosition)
            {
                Damage = damage;
                AttackPosition = attackPosition;
            }
        }

        private readonly struct SurfaceHit
        {
            public readonly Vector3 LocalPosition;
            public readonly Vector3 WorldPosition;
            public readonly Vector3 WorldNormal;

            public SurfaceHit(Vector3 localPosition, Vector3 worldPosition, Vector3 worldNormal)
            {
                LocalPosition = localPosition;
                WorldPosition = worldPosition;
                WorldNormal = worldNormal;
            }
        }

        [Header("参照")]
        [SerializeField] private Renderer _barrierRenderer;
        [SerializeField] private Collider _surfaceCollider;
        [SerializeField] private GameObject _hitVfxPrefab;
        [SerializeField] private Material _crackMaterial;

        [Header("攻撃位置Ray")]
        [SerializeField, Min(0.0f)] private float _attackRayStartDownOffset = 0.25f;
        [SerializeField, Min(0.01f)] private float _attackRayDistance = 10.0f;

        [Header("バリア表面")]
        [SerializeField] private bool _useAttackerSide = true;
        [SerializeField] private bool _useAttackerHeight = true;
        [SerializeField] private Vector3 _surfaceLocalPosition = new Vector3(0.0f, 0.0f, -0.51f);
        [SerializeField] private Vector2 _surfaceLocalXRange = new Vector2(-0.48f, 0.48f);
        [SerializeField] private Vector2 _surfaceLocalYRange = new Vector2(-0.48f, 0.48f);
        [SerializeField, Range(0.0f, 0.5f)] private float _overallCrackVerticalRange = 0.32f;
        [SerializeField, Min(0.0f)] private float _surfaceNormalOffset = 0.02f;

        [Header("HPによる色遷移")]
        [SerializeField, ColorUsage(true, true)] private Color _healthyColor = new Color(0.48f, 1.0f, 0.94f, 0.28f);
        [SerializeField, ColorUsage(true, true)] private Color _damagedColor = new Color(0.45f, 0.28f, 1.15f, 0.25f);
        [SerializeField, ColorUsage(true, true)] private Color _criticalColor = new Color(1.2f, 0.12f, 0.58f, 0.18f);
        [SerializeField, Range(0.01f, 0.99f)] private float _damagedColorHpRatio = 0.5f;

        [Header("バリア表示")]
        [SerializeField, Min(0.01f)] private float _hitVisibleDuration = 0.55f;
        [SerializeField, Min(0.0f)] private float _hitFadeInDuration = 0.08f;
        [SerializeField, Min(0.0f)] private float _hitFadeOutDuration = 0.22f;
        [SerializeField, Range(0.0f, 1.0f)] private float _alwaysVisibleHpRatio = 0.25f;

        [Header("瀕死時の明滅")]
        [SerializeField, Range(0.0f, 1.0f)] private float _criticalHpRatio = 0.25f;
        [SerializeField, Min(0.0f)] private float _criticalFlickerSpeed = 8.0f;
        [SerializeField, Range(0.0f, 1.0f)] private float _criticalFlickerAmount = 0.35f;

        [Header("被弾フラッシュ")]
        [SerializeField, ColorUsage(true, true)] private Color _hitFlashColor = new Color(2.0f, 2.4f, 2.4f, 0.65f);
        [SerializeField, Min(0.01f)] private float _hitFlashDuration = 0.12f;
        [SerializeField, Range(0.0f, 1.0f)] private float _hitFlashStrength = 0.85f;

        [Header("波紋")]
        [SerializeField, Min(3)] private int _rippleSegments = 32;
        [SerializeField, Min(0.01f)] private float _rippleDuration = 0.28f;
        [SerializeField, Min(0.01f)] private float _rippleMaxRadius = 1.1f;
        [SerializeField, Min(0.001f)] private float _rippleStartWidth = 0.08f;
        [SerializeField, Min(0.001f)] private float _rippleEndWidth = 0.015f;
        [SerializeField, ColorUsage(true, true)] private Color _rippleColor = new Color(1.2f, 2.0f, 2.0f, 0.9f);

        [Header("全体ひび")]
        [SerializeField, Min(1)] private int _overallCrackCount = 12;
        [SerializeField, Min(0.05f)] private float _overallCrackLength = 1.25f;
        [SerializeField, Min(2)] private int _overallCrackSegments = 6;
        [SerializeField, Min(0)] private int _overallCrackBranches = 2;
        [SerializeField] private int _overallCrackRandomSeed = 2157;

        [Header("局所ひび")]
        [SerializeField, Min(1)] private int _maxLocalCracks = 8;
        [SerializeField, Min(0.05f)] private float _localCrackLength = 0.75f;
        [SerializeField, Min(2)] private int _localCrackSegments = 5;
        [SerializeField, Min(0)] private int _localCrackBranches = 2;

        [Header("ひび共通")]
        [SerializeField, Min(0.001f)] private float _crackWidth = 0.035f;
        [SerializeField, Range(0.0f, 90.0f)] private float _crackBendAngle = 28.0f;
        [SerializeField, ColorUsage(true, true)] private Color _crackHealthyColor = new Color(0.8f, 2.0f, 2.0f, 0.75f);
        [SerializeField, ColorUsage(true, true)] private Color _crackDamagedColor = new Color(1.0f, 0.45f, 2.0f, 0.9f);
        [SerializeField, ColorUsage(true, true)] private Color _crackCriticalColor = new Color(2.0f, 0.1f, 0.65f, 1.0f);

        [Header("被弾VFX")]
        [SerializeField] private bool _alignHitVfxToSurfaceNormal = true;
        [SerializeField] private Vector3 _hitVfxLocalOffset = Vector3.zero;
        [SerializeField] private Vector3 _hitVfxRotationOffset = Vector3.zero;
        [SerializeField, Min(0.01f)] private float _hitVfxBaseScale = 1.0f;
        [SerializeField, Min(0.01f)] private float _hitVfxSizeMultiplier = 3.0f;
        [SerializeField, Min(0.01f)] private float _referenceDamage = 10.0f;
        [SerializeField, Min(0.01f)] private float _minimumDamageVfxScale = 0.8f;
        [SerializeField, Min(0.01f)] private float _maximumDamageVfxScale = 1.4f;
        [SerializeField, Min(0.01f)] private float _hitVfxLifetime = 2.0f;
        [SerializeField, Min(0.01f)] private float _breakVfxScaleMultiplier = 1.8f;
        [SerializeField, Range(0.0f, 1.0f)] private float _hitVfxColorBlend = 0.65f;
        [SerializeField, Min(0.0f)] private float _hitVfxBrightness = 1.8f;

        [Header("破壊")]
        [SerializeField, Min(0.01f)] private float _breakDuration = 0.4f;
        [SerializeField, Min(0.0f)] private float _breakWhiteFlashDuration = 0.08f;
        [SerializeField, ColorUsage(true, true)] private Color _breakFlashColor = new Color(2.5f, 2.5f, 2.5f, 1.0f);

        private readonly List<CrackGroup> _overallCracks = new List<CrackGroup>();
        private readonly Queue<CrackGroup> _localCracks = new Queue<CrackGroup>();
        private MaterialPropertyBlock _propertyBlock;
        private Material _runtimeCrackMaterial;
        private MeshFilter _meshFilter;
        private DefenseLineFracture _fracture;
        private Bounds _surfaceBounds;
        private LineRenderer _rippleRenderer;
        private float _remainingHpRatio = 1.0f;
        private float _hitFlashRemaining;
        private float _barrierVisibleRemaining;
        private float _rippleElapsed;
        private Vector3 _rippleCenter;
        private SurfaceHit _rippleSurface;
        private bool _isRipplePlaying;
        private bool _isBreaking;
        private bool _isBroken;
        private float _breakElapsed;
        private int _localCrackSeed;

        private sealed class CrackGroup
        {
            public readonly GameObject Root;
            public readonly List<LineRenderer> Lines;

            public CrackGroup(GameObject root, List<LineRenderer> lines)
            {
                Root = root;
                Lines = lines;
            }

            public void SetEnabled(bool enabled)
            {
                Root.SetActive(enabled);
            }

            public void SetColor(Color color)
            {
                foreach (LineRenderer line in Lines)
                {
                    line.startColor = color;
                    Color endColor = color;
                    endColor.a *= 0.35f;
                    line.endColor = endColor;
                }
            }
        }

        private void Awake()
        {
            if (_barrierRenderer == null)
            {
                _barrierRenderer = GetComponent<Renderer>();
            }

            if (_surfaceCollider == null)
            {
                _surfaceCollider = GetComponent<Collider>();
            }

            _meshFilter = GetComponent<MeshFilter>();
            _fracture = GetComponent<DefenseLineFracture>();
            _surfaceBounds = _meshFilter != null && _meshFilter.sharedMesh != null
                ? _meshFilter.sharedMesh.bounds
                : new Bounds(Vector3.zero, Vector3.one);

            _propertyBlock = new MaterialPropertyBlock();
            CreateCrackMaterial();
            CreateRippleRenderer();
            ApplyDamageState();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<DefLineHitReactionEvent>(OnPlayHitReaction);
            EventBus.Subscribe<DefLineBreakReactionEvent>(OnPlayBreakReaction);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DefLineHitReactionEvent>(OnPlayHitReaction);
            EventBus.Unsubscribe<DefLineBreakReactionEvent>(OnPlayBreakReaction);
        }

        private void OnDestroy()
        {
            if (_runtimeCrackMaterial != null)
            {
                Destroy(_runtimeCrackMaterial);
            }
        }

        private void Update()
        {
            if (_isBroken)
            {
                return;
            }

            if (_isBreaking)
            {
                UpdateBreakReaction();
                return;
            }

            if (_hitFlashRemaining > 0.0f)
            {
                _hitFlashRemaining = Mathf.Max(0.0f, _hitFlashRemaining - Time.deltaTime);
            }

            if (_barrierVisibleRemaining > 0.0f)
            {
                _barrierVisibleRemaining = Mathf.Max(0.0f, _barrierVisibleRemaining - Time.deltaTime);
            }

            UpdateRipple();
            ApplyDamageState();
        }

        private void OnPlayHitReaction(DefLineHitReactionEvent reactionEvent)
        {
            HitReactionData data = new HitReactionData(
                reactionEvent.Damage,
                reactionEvent.RemainingHpRatio,
                reactionEvent.AttackPosition);
            PlayHitReaction(data);
            SoundManager.instance?.PlaySE("BarrierBreak_01");
        }

        private void OnPlayBreakReaction(DefLineBreakReactionEvent breakEvent)
        {
            BreakReactionData data = new BreakReactionData(breakEvent.Damage, breakEvent.AttackPosition);
            PlayBreakReaction(data);
        }

        protected virtual void PlayHitReaction(HitReactionData data)
        {
            if (_isBreaking || _isBroken) return;

            _remainingHpRatio = Mathf.Clamp01(data.RemainingHpRatio);
            SurfaceHit surfaceHit = GetSurfaceHit(data.AttackPosition);

            _hitFlashRemaining = _hitFlashDuration;
            _barrierVisibleRemaining = _hitVisibleDuration;
            EnsureOverallCracks();
            StartRipple(surfaceHit);
            AddLocalCrack(surfaceHit);
            Color effectColor = EvaluateColor(_healthyColor, _damagedColor, _criticalColor, _remainingHpRatio);
            PlayHitVfx(surfaceHit, data.Damage, 1.0f, effectColor);
            UpdateCrackVisibility();
            ApplyDamageState();
        }

        protected virtual void PlayBreakReaction(BreakReactionData data)
        {
            if (_isBreaking || _isBroken) return;

            _remainingHpRatio = 0.0f;
            SurfaceHit surfaceHit = GetSurfaceHit(data.AttackPosition);

            EnsureOverallCracks();
            AddLocalCrack(surfaceHit);
            PlayHitVfx(surfaceHit, data.Damage, _breakVfxScaleMultiplier, _criticalColor);
            if (_fracture != null)
            {
                float fractureSurfaceZ = Mathf.Clamp(
                    _surfaceLocalPosition.z,
                    _surfaceBounds.min.z,
                    _surfaceBounds.max.z);
                _fracture.PlayFracture(
                    _barrierRenderer,
                    _surfaceBounds,
                    fractureSurfaceZ,
                    _surfaceLocalXRange,
                    _surfaceLocalYRange,
                    surfaceHit.WorldPosition,
                    surfaceHit.WorldNormal,
                    _criticalColor,
                    _crackMaterial,
                    _hitVfxPrefab);
            }
            SetAllCracksEnabled();
            _isRipplePlaying = false;
            if (_rippleRenderer != null)
            {
                _rippleRenderer.enabled = false;
            }

            _isBreaking = true;
            _breakElapsed = 0.0f;
        }

        private void CreateCrackMaterial()
        {
            if (_crackMaterial != null) return;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            }

            if (shader != null)
            {
                _runtimeCrackMaterial = new Material(shader)
                {
                    name = "DefenseLineCrack_Runtime"
                };
                _crackMaterial = _runtimeCrackMaterial;
            }
        }

        private void CreateRippleRenderer()
        {
            GameObject rippleObject = new GameObject("DefenseLineHitRipple");
            rippleObject.transform.SetParent(transform, false);
            _rippleRenderer = rippleObject.AddComponent<LineRenderer>();
            ConfigureLineRenderer(_rippleRenderer, _rippleSegments + 1);
            _rippleRenderer.loop = true;
            _rippleRenderer.enabled = false;
        }

        private void EnsureOverallCracks()
        {
            if (_overallCracks.Count > 0) return;

            GetSurfaceRanges(out float minimumX, out float maximumX, out float minimumY, out float maximumY);
            minimumY = Mathf.Max(minimumY, _surfaceLocalPosition.y - _overallCrackVerticalRange);
            maximumY = Mathf.Min(maximumY, _surfaceLocalPosition.y + _overallCrackVerticalRange);
            float surfaceZ = Mathf.Clamp(_surfaceLocalPosition.z, _surfaceBounds.min.z, _surfaceBounds.max.z);
            float normalSign = surfaceZ >= _surfaceBounds.center.z ? 1.0f : -1.0f;
            Vector3 worldNormal = transform.TransformDirection(Vector3.forward * normalSign).normalized;
            System.Random random = new System.Random(_overallCrackRandomSeed);
            for (int i = 0; i < _overallCrackCount; i++)
            {
                float normalizedX = ((i + 0.5f) / _overallCrackCount) + RandomRange(random, -0.025f, 0.025f);
                float localX = Mathf.Lerp(minimumX, maximumX, Mathf.Clamp01(normalizedX));
                float localY = RandomRange(random, minimumY, maximumY);
                Vector3 localCenter = new Vector3(localX, localY, surfaceZ);
                SurfaceHit crackSurface = new SurfaceHit(
                    localCenter,
                    transform.TransformPoint(localCenter) + worldNormal * _surfaceNormalOffset,
                    worldNormal);
                CrackGroup crack = CreateCrackGroup(
                    "OverallCrack",
                    crackSurface,
                    _overallCrackLength,
                    _overallCrackSegments,
                    _overallCrackBranches,
                    random.Next());
                crack.SetEnabled(false);
                _overallCracks.Add(crack);
            }
        }

        private void AddLocalCrack(SurfaceHit surface)
        {
            CrackGroup crack = CreateCrackGroup(
                "LocalCrack",
                surface,
                _localCrackLength,
                _localCrackSegments,
                _localCrackBranches,
                ++_localCrackSeed + _overallCrackRandomSeed);
            _localCracks.Enqueue(crack);

            while (_localCracks.Count > _maxLocalCracks)
            {
                CrackGroup oldest = _localCracks.Dequeue();
                Destroy(oldest.Root);
            }
        }

        private CrackGroup CreateCrackGroup(
            string objectName,
            SurfaceHit surface,
            float length,
            int segmentCount,
            int branchCount,
            int seed)
        {
            GameObject root = new GameObject(objectName);
            root.transform.SetParent(transform, true);
            List<LineRenderer> lines = new List<LineRenderer>();
            System.Random random = new System.Random(seed);
            GetSurfaceAxes(surface, out Vector3 right, out Vector3 up);

            float angle = RandomRange(random, 0.0f, 360.0f);
            Vector3 direction = DirectionOnSurface(right, up, angle);
            Vector3[] mainPoints = new Vector3[segmentCount + 1];
            mainPoints[0] = surface.WorldPosition;
            float stepLength = length / segmentCount;

            for (int i = 1; i <= segmentCount; i++)
            {
                angle += RandomRange(random, -_crackBendAngle, _crackBendAngle);
                direction = DirectionOnSurface(right, up, angle);
                mainPoints[i] = ClampPointToSurface(mainPoints[i - 1] + direction * stepLength, surface);
            }

            lines.Add(CreateCrackLine(root.transform, "Main", mainPoints));

            for (int branchIndex = 0; branchIndex < branchCount; branchIndex++)
            {
                int originIndex = random.Next(1, segmentCount);
                Vector3 origin = mainPoints[originIndex];
                float branchAngle = angle + (random.NextDouble() < 0.5 ? -1.0f : 1.0f) * RandomRange(random, 45.0f, 85.0f);
                float branchLength = length * RandomRange(random, 0.2f, 0.42f);
                Vector3[] branchPoints = new Vector3[3];
                branchPoints[0] = origin;
                branchPoints[1] = ClampPointToSurface(
                    origin + DirectionOnSurface(right, up, branchAngle) * branchLength * 0.55f,
                    surface);
                branchAngle += RandomRange(random, -_crackBendAngle, _crackBendAngle);
                branchPoints[2] = ClampPointToSurface(
                    branchPoints[1] + DirectionOnSurface(right, up, branchAngle) * branchLength * 0.45f,
                    surface);
                lines.Add(CreateCrackLine(root.transform, $"Branch_{branchIndex}", branchPoints));
            }

            return new CrackGroup(root, lines);
        }

        private LineRenderer CreateCrackLine(Transform parent, string objectName, Vector3[] points)
        {
            GameObject lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(parent, true);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            ConfigureLineRenderer(line, points.Length);
            line.SetPositions(points);
            return line;
        }

        private void ConfigureLineRenderer(LineRenderer line, int positionCount)
        {
            line.useWorldSpace = true;
            line.positionCount = positionCount;
            line.widthMultiplier = _crackWidth;
            line.numCornerVertices = 2;
            line.numCapVertices = 1;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.sharedMaterial = _crackMaterial;
            if (_barrierRenderer != null)
            {
                line.sortingLayerID = _barrierRenderer.sortingLayerID;
                line.sortingOrder = _barrierRenderer.sortingOrder + 1;
            }
        }

        private void StartRipple(SurfaceHit surface)
        {
            if (_rippleRenderer == null) return;

            _rippleSurface = surface;
            _rippleCenter = surface.WorldPosition;
            _rippleElapsed = 0.0f;
            _isRipplePlaying = true;
            _rippleRenderer.enabled = true;
        }

        private void UpdateRipple()
        {
            if (!_isRipplePlaying || _rippleRenderer == null) return;

            _rippleElapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(_rippleElapsed / _rippleDuration);
            float radius = Mathf.Lerp(0.0f, _rippleMaxRadius, 1.0f - Mathf.Pow(1.0f - normalizedTime, 3.0f));
            GetSurfaceAxes(_rippleSurface, out Vector3 right, out Vector3 up);

            for (int i = 0; i <= _rippleSegments; i++)
            {
                float angle = (i / (float)_rippleSegments) * Mathf.PI * 2.0f;
                Vector3 point = _rippleCenter + right * (Mathf.Cos(angle) * radius) + up * (Mathf.Sin(angle) * radius);
                _rippleRenderer.SetPosition(i, ClampPointToSurface(point, _rippleSurface));
            }

            Color rippleColor = _rippleColor;
            rippleColor.a *= 1.0f - normalizedTime;
            _rippleRenderer.startColor = rippleColor;
            _rippleRenderer.endColor = rippleColor;
            _rippleRenderer.widthMultiplier = Mathf.Lerp(_rippleStartWidth, _rippleEndWidth, normalizedTime);

            if (normalizedTime >= 1.0f)
            {
                _isRipplePlaying = false;
                _rippleRenderer.enabled = false;
            }
        }

        private void PlayHitVfx(SurfaceHit surface, float damage, float scaleMultiplier, Color effectColor)
        {
            if (_hitVfxPrefab == null) return;

            Vector3 position = surface.WorldPosition + transform.TransformVector(_hitVfxLocalOffset);
            Quaternion baseRotation = _alignHitVfxToSurfaceNormal
                ? Quaternion.FromToRotation(Vector3.up, surface.WorldNormal)
                : transform.rotation;
            Quaternion rotation = baseRotation * Quaternion.Euler(_hitVfxRotationOffset);
            GameObject instance = Instantiate(_hitVfxPrefab, position, rotation);
            ApplyParticleColor(instance, effectColor, _hitVfxColorBlend, _hitVfxBrightness);
            float damageScale = Mathf.Lerp(
                _minimumDamageVfxScale,
                _maximumDamageVfxScale,
                Mathf.Clamp01(damage / _referenceDamage));
            instance.transform.localScale *= _hitVfxBaseScale * _hitVfxSizeMultiplier * damageScale * scaleMultiplier;
            Destroy(instance, _hitVfxLifetime);
        }

        internal static void ApplyParticleColor(
            GameObject instance,
            Color color,
            float colorBlend,
            float brightness)
        {
            Color tint = new Color(color.r, color.g, color.b, 1.0f);
            float blend = Mathf.Clamp01(colorBlend);
            float intensity = Mathf.Max(0.0f, brightness);
            ParticleSystem[] particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                ParticleSystem.MainModule main = particleSystem.main;
                main.startColor = TintParticleColor(main.startColor, tint, blend, intensity);
            }
        }

        private static ParticleSystem.MinMaxGradient TintParticleColor(
            ParticleSystem.MinMaxGradient source,
            Color tint,
            float blend,
            float brightness)
        {
            switch (source.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return new ParticleSystem.MinMaxGradient(
                        BlendRgb(source.color, tint, blend, brightness));
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(
                        BlendRgb(source.colorMin, tint, blend, brightness),
                        BlendRgb(source.colorMax, tint, blend, brightness));
                case ParticleSystemGradientMode.Gradient:
                    return new ParticleSystem.MinMaxGradient(
                        TintGradient(source.gradient, tint, blend, brightness));
                case ParticleSystemGradientMode.TwoGradients:
                    return new ParticleSystem.MinMaxGradient(
                        TintGradient(source.gradientMin, tint, blend, brightness),
                        TintGradient(source.gradientMax, tint, blend, brightness));
                case ParticleSystemGradientMode.RandomColor:
                    ParticleSystem.MinMaxGradient randomColor =
                        new ParticleSystem.MinMaxGradient(
                            TintGradient(source.gradient, tint, blend, brightness));
                    randomColor.mode = ParticleSystemGradientMode.RandomColor;
                    return randomColor;
                default:
                    return source;
            }
        }

        private static Gradient TintGradient(
            Gradient source,
            Color tint,
            float blend,
            float brightness)
        {
            GradientColorKey[] colorKeys = source.colorKeys;
            for (int i = 0; i < colorKeys.Length; i++)
            {
                colorKeys[i].color = BlendRgb(colorKeys[i].color, tint, blend, brightness);
            }

            Gradient gradient = new Gradient
            {
                mode = source.mode
            };
            gradient.SetKeys(colorKeys, source.alphaKeys);
            return gradient;
        }

        private static Color BlendRgb(Color source, Color tint, float blend, float brightness)
        {
            Color blended = Color.Lerp(source, tint, blend);
            return new Color(
                blended.r * brightness,
                blended.g * brightness,
                blended.b * brightness,
                source.a);
        }

        private SurfaceHit GetSurfaceHit(Vector3 attackPosition)
        {
            if (_surfaceCollider != null)
            {
                Vector3 rayOrigin = attackPosition + Vector3.down * _attackRayStartDownOffset;
                Ray attackRay = new Ray(rayOrigin, Vector3.up);
                if (_surfaceCollider.Raycast(attackRay, out RaycastHit raycastHit, _attackRayDistance))
                {
                    Vector3 hitNormal = raycastHit.normal.normalized;
                    return new SurfaceHit(
                        transform.InverseTransformPoint(raycastHit.point),
                        raycastHit.point + hitNormal * _surfaceNormalOffset,
                        hitNormal);
                }
            }

            Vector3 localPosition = transform.InverseTransformPoint(attackPosition);
            GetSurfaceRanges(out float minimumX, out float maximumX, out float minimumY, out float maximumY);
            localPosition.x = Mathf.Clamp(localPosition.x, minimumX, maximumX);
            localPosition.y = _useAttackerHeight
                ? Mathf.Clamp(localPosition.y, minimumY, maximumY)
                : Mathf.Clamp(_surfaceLocalPosition.y, minimumY, maximumY);

            if (_useAttackerSide)
            {
                localPosition.z = localPosition.z >= _surfaceBounds.center.z
                    ? _surfaceBounds.max.z
                    : _surfaceBounds.min.z;
            }
            else
            {
                localPosition.z = Mathf.Clamp(_surfaceLocalPosition.z, _surfaceBounds.min.z, _surfaceBounds.max.z);
            }

            float normalSign = localPosition.z >= _surfaceBounds.center.z ? 1.0f : -1.0f;
            Vector3 worldNormal = transform.TransformDirection(Vector3.forward * normalSign).normalized;
            Vector3 worldPosition = transform.TransformPoint(localPosition) + worldNormal * _surfaceNormalOffset;
            return new SurfaceHit(localPosition, worldPosition, worldNormal);
        }

        private Vector3 ClampPointToSurface(Vector3 worldPoint, SurfaceHit surface)
        {
            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
            GetSurfaceRanges(out float minimumX, out float maximumX, out float minimumY, out float maximumY);
            int surfaceAxis = GetDominantAxis(transform.InverseTransformDirection(surface.WorldNormal));

            if (surfaceAxis == 0)
            {
                localPoint.x = surface.LocalPosition.x;
                localPoint.y = Mathf.Clamp(localPoint.y, minimumY, maximumY);
                localPoint.z = Mathf.Clamp(localPoint.z, _surfaceBounds.min.z, _surfaceBounds.max.z);
            }
            else if (surfaceAxis == 1)
            {
                localPoint.x = Mathf.Clamp(localPoint.x, minimumX, maximumX);
                localPoint.y = surface.LocalPosition.y;
                localPoint.z = Mathf.Clamp(localPoint.z, _surfaceBounds.min.z, _surfaceBounds.max.z);
            }
            else
            {
                localPoint.x = Mathf.Clamp(localPoint.x, minimumX, maximumX);
                localPoint.y = Mathf.Clamp(localPoint.y, minimumY, maximumY);
                localPoint.z = surface.LocalPosition.z;
            }

            return transform.TransformPoint(localPoint) + surface.WorldNormal * _surfaceNormalOffset;
        }

        private void GetSurfaceAxes(SurfaceHit surface, out Vector3 right, out Vector3 up)
        {
            right = Vector3.ProjectOnPlane(transform.right, surface.WorldNormal).normalized;
            if (right.sqrMagnitude < 0.001f)
            {
                right = Vector3.ProjectOnPlane(transform.forward, surface.WorldNormal).normalized;
            }

            up = Vector3.Cross(surface.WorldNormal, right).normalized;
        }

        private static int GetDominantAxis(Vector3 direction)
        {
            direction = new Vector3(Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z));
            if (direction.x >= direction.y && direction.x >= direction.z) return 0;
            return direction.y >= direction.z ? 1 : 2;
        }

        private void GetSurfaceRanges(
            out float minimumX,
            out float maximumX,
            out float minimumY,
            out float maximumY)
        {
            minimumX = Mathf.Max(_surfaceBounds.min.x, Mathf.Min(_surfaceLocalXRange.x, _surfaceLocalXRange.y));
            maximumX = Mathf.Min(_surfaceBounds.max.x, Mathf.Max(_surfaceLocalXRange.x, _surfaceLocalXRange.y));
            minimumY = Mathf.Max(_surfaceBounds.min.y, Mathf.Min(_surfaceLocalYRange.x, _surfaceLocalYRange.y));
            maximumY = Mathf.Min(_surfaceBounds.max.y, Mathf.Max(_surfaceLocalYRange.x, _surfaceLocalYRange.y));

            if (minimumX > maximumX)
            {
                minimumX = _surfaceBounds.min.x;
                maximumX = _surfaceBounds.max.x;
            }

            if (minimumY > maximumY)
            {
                minimumY = _surfaceBounds.min.y;
                maximumY = _surfaceBounds.max.y;
            }
        }

        private void ApplyDamageState()
        {
            Color barrierColor = EvaluateColor(_healthyColor, _damagedColor, _criticalColor, _remainingHpRatio);
            float visibility = GetBarrierVisibility();

            if (_remainingHpRatio <= _criticalHpRatio && _criticalFlickerAmount > 0.0f)
            {
                float noise = Mathf.PerlinNoise(Time.time * _criticalFlickerSpeed, 0.0f);
                barrierColor.a *= Mathf.Lerp(1.0f - _criticalFlickerAmount, 1.0f, noise);
            }

            if (_hitFlashRemaining > 0.0f)
            {
                float flash = (_hitFlashRemaining / _hitFlashDuration) * _hitFlashStrength;
                barrierColor = Color.Lerp(barrierColor, _hitFlashColor, flash);
            }

            barrierColor.a *= visibility;

            ApplyBarrierColor(barrierColor);
            UpdateCrackVisibility();
            UpdateCrackColors();
        }

        private void ApplyBarrierColor(Color color)
        {
            if (_barrierRenderer == null) return;

            _barrierRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _propertyBlock.SetColor(ColorId, color);
            _barrierRenderer.SetPropertyBlock(_propertyBlock);
        }

        private Color EvaluateColor(Color healthy, Color damaged, Color critical, float remainingHpRatio)
        {
            if (remainingHpRatio >= _damagedColorHpRatio)
            {
                float t = Mathf.InverseLerp(_damagedColorHpRatio, 1.0f, remainingHpRatio);
                return Color.Lerp(damaged, healthy, t);
            }

            float criticalT = Mathf.InverseLerp(0.0f, _damagedColorHpRatio, remainingHpRatio);
            return Color.Lerp(critical, damaged, criticalT);
        }

        private void UpdateCrackVisibility()
        {
            bool isVisible = GetBarrierVisibility() > 0.001f;
            int visibleCount = isVisible
                ? Mathf.CeilToInt((1.0f - _remainingHpRatio) * _overallCracks.Count)
                : 0;
            for (int i = 0; i < _overallCracks.Count; i++)
            {
                _overallCracks[i].SetEnabled(i < visibleCount);
            }

            foreach (CrackGroup crack in _localCracks)
            {
                crack.SetEnabled(isVisible);
            }
        }

        private float GetBarrierVisibility()
        {
            if (_remainingHpRatio <= _alwaysVisibleHpRatio)
            {
                return 1.0f;
            }

            if (_barrierVisibleRemaining <= 0.0f)
            {
                return 0.0f;
            }

            float elapsed = Mathf.Max(0.0f, _hitVisibleDuration - _barrierVisibleRemaining);
            float fadeIn = _hitFadeInDuration > 0.0f
                ? Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp01(elapsed / _hitFadeInDuration))
                : 1.0f;
            float fadeOut = _hitFadeOutDuration > 0.0f
                ? Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp01(_barrierVisibleRemaining / _hitFadeOutDuration))
                : 1.0f;
            return Mathf.Min(fadeIn, fadeOut);
        }

        private void SetAllCracksEnabled()
        {
            foreach (CrackGroup crack in _overallCracks)
            {
                crack.SetEnabled(true);
            }

            UpdateCrackColors();
        }

        private void UpdateCrackColors()
        {
            Color crackColor = EvaluateColor(_crackHealthyColor, _crackDamagedColor, _crackCriticalColor, _remainingHpRatio);
            crackColor.a *= GetBarrierVisibility();
            foreach (CrackGroup crack in _overallCracks)
            {
                crack.SetColor(crackColor);
            }

            foreach (CrackGroup crack in _localCracks)
            {
                crack.SetColor(crackColor);
            }
        }

        private void UpdateBreakReaction()
        {
            _breakElapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(_breakElapsed / _breakDuration);
            Color color;

            if (_breakWhiteFlashDuration > 0.0f && _breakElapsed <= _breakWhiteFlashDuration)
            {
                float flashTime = _breakElapsed / _breakWhiteFlashDuration;
                color = Color.Lerp(_criticalColor, _breakFlashColor, Mathf.Sin(flashTime * Mathf.PI));
            }
            else
            {
                color = _criticalColor;
                color.a *= 1.0f - normalizedTime;
            }

            ApplyBarrierColor(color);
            Color crackColor = _crackCriticalColor;
            crackColor.a *= 1.0f - normalizedTime;
            foreach (CrackGroup crack in _overallCracks)
            {
                crack.SetColor(crackColor);
            }
            foreach (CrackGroup crack in _localCracks)
            {
                crack.SetColor(crackColor);
            }

            if (normalizedTime >= 1.0f)
            {
                _isBreaking = false;
                _isBroken = true;
                if (_barrierRenderer != null)
                {
                    _barrierRenderer.enabled = false;
                }
                foreach (CrackGroup crack in _overallCracks)
                {
                    crack.SetEnabled(false);
                }
                foreach (CrackGroup crack in _localCracks)
                {
                    crack.SetEnabled(false);
                }
            }
        }

        private static Vector3 DirectionOnSurface(Vector3 right, Vector3 up, float angleDegrees)
        {
            float angle = angleDegrees * Mathf.Deg2Rad;
            return (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)).normalized;
        }

        private static float RandomRange(System.Random random, float minimum, float maximum)
        {
            return Mathf.Lerp(minimum, maximum, (float)random.NextDouble());
        }
    }
}
