using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.DefenceLine
{
    public sealed class DefenseLineFracture : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("破片生成")]
        [SerializeField, Range(8, 96)] private int _fragmentCount = 48;
        [SerializeField] private int _randomSeed = 4815;
        [SerializeField, Min(0.005f)] private float _fragmentWorldThickness = 0.08f;
        [SerializeField, Range(0, 31)] private int _fragmentLayer;

        [Header("破壊伝播")]
        [SerializeField, Min(0.0f)] private float _handoffDelay = 0.055f;
        [SerializeField, Min(0.01f)] private float _propagationDuration = 0.2f;
        [SerializeField, Min(0.0f)] private float _releaseRandomDelay = 0.025f;
        [SerializeField] private AnimationCurve _propagationCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);

        [Header("破片物理")]
        [SerializeField] private bool _enableFragmentCollisions = true;
        [SerializeField] private PhysicsMaterial _fragmentPhysicsMaterial;
        [SerializeField, Min(0.0f)] private float _normalImpulse = 2.8f;
        [SerializeField, Min(0.0f)] private float _radialImpulse = 1.7f;
        [SerializeField, Min(0.0f)] private float _randomImpulse = 0.75f;
        [SerializeField, Min(0.0f)] private float _angularImpulse = 0.45f;
        [SerializeField, Min(0.001f)] private float _massPerSquareUnit = 0.035f;
        [SerializeField, Min(0.001f)] private float _minimumMass = 0.025f;
        [SerializeField, Min(0.0f)] private float _linearDamping = 0.12f;
        [SerializeField, Min(0.0f)] private float _angularDamping = 0.08f;
        [SerializeField] private bool _useGravity = true;

        [Header("破砕境界")]
        [SerializeField, Min(0.001f)] private float _seamWidth = 0.045f;
        [SerializeField, ColorUsage(true, true)] private Color _seamColor = new Color(2.0f, 0.1f, 0.65f, 1.0f);
        [SerializeField, ColorUsage(true, true)] private Color _seamIgnitionColor = new Color(3.0f, 3.0f, 3.0f, 1.0f);
        [SerializeField, Min(0.01f)] private float _seamGlowDuration = 0.18f;

        [Header("二重衝撃波")]
        [SerializeField, Min(12)] private int _shockwaveSegments = 48;
        [SerializeField, Min(0.01f)] private float _primaryShockwaveDuration = 0.22f;
        [SerializeField, Min(0.01f)] private float _primaryShockwaveRadius = 2.4f;
        [SerializeField, Min(0.001f)] private float _primaryShockwaveStartWidth = 0.13f;
        [SerializeField, ColorUsage(true, true)] private Color _primaryShockwaveColor = new Color(2.8f, 3.0f, 3.0f, 1.0f);
        [SerializeField, Min(0.0f)] private float _secondaryShockwaveDelay = 0.08f;
        [SerializeField, Min(0.01f)] private float _secondaryShockwaveDuration = 0.4f;
        [SerializeField, Min(0.01f)] private float _secondaryShockwaveRadius = 4.2f;
        [SerializeField, Min(0.001f)] private float _secondaryShockwaveStartWidth = 0.1f;
        [SerializeField, ColorUsage(true, true)] private Color _secondaryShockwaveColor = new Color(2.2f, 0.2f, 1.4f, 0.9f);

        [Header("二次VFX")]
        [SerializeField, Range(0, 12)] private int _secondaryVfxCount = 5;
        [SerializeField] private Vector2 _secondaryVfxScaleRange = new Vector2(0.45f, 0.75f);
        [SerializeField, Min(0.01f)] private float _secondaryVfxLifetime = 2.0f;
        [SerializeField, Range(0.0f, 1.0f)] private float _secondaryVfxColorBlend = 0.65f;
        [SerializeField, Min(0.0f)] private float _secondaryVfxBrightness = 1.8f;

        [Header("破片消滅")]
        [SerializeField, Min(0.0f)] private float _fragmentFadeDelay = 0.85f;
        [SerializeField, Min(0.01f)] private float _fragmentFadeDuration = 0.65f;
        [SerializeField, Range(0.1f, 1.0f)] private float _fragmentEndScale = 0.88f;
        [SerializeField, Min(0.0f)] private float _collisionDisableDelay = 0.9f;

        [Header("破壊SE")]
        [SerializeField, Min(0.0f)] private float _breakTailDelay = 0.11f;

        private readonly List<Fragment> _fragments = new List<Fragment>();
        private GameObject _fragmentRoot;
        private bool _hasFractured;

        private sealed class Fragment
        {
            public GameObject GameObject;
            public Mesh Mesh;
            public MeshRenderer Renderer;
            public LineRenderer Seam;
            public MeshCollider Collider;
            public Rigidbody Rigidbody;
            public float ReleaseDelay;
            public float DistanceRatio;
        }

        public void PlayFracture(
            Renderer sourceRenderer,
            Bounds sourceLocalBounds,
            float surfaceLocalZ,
            Vector2 surfaceLocalXRange,
            Vector2 surfaceLocalYRange,
            Vector3 impactPoint,
            Vector3 impactNormal,
            Color fragmentColor,
            Material seamMaterial,
            GameObject secondaryVfxPrefab)
        {
            if (_hasFractured || sourceRenderer == null) return;

            _hasFractured = true;
            Rect fractureArea = GetFractureArea(sourceLocalBounds, surfaceLocalXRange, surfaceLocalYRange);
            List<DefenseLineFractureMeshBuilder.Cell> cells =
                DefenseLineFractureMeshBuilder.BuildVoronoi(fractureArea, _fragmentCount, _randomSeed);
            if (cells.Count == 0) return;

            CreateFragments(
                cells,
                fractureArea,
                sourceRenderer,
                surfaceLocalZ,
                impactPoint,
                impactNormal,
                fragmentColor,
                seamMaterial);
            StartCoroutine(PlayShockwaves(fractureArea, surfaceLocalZ, impactPoint, seamMaterial));
            StartCoroutine(PlayFractureSequence(
                sourceRenderer,
                impactPoint,
                impactNormal.normalized,
                fragmentColor,
                secondaryVfxPrefab));
            PlayBreakAudio();
        }

        private IEnumerator PlayShockwaves(
            Rect fractureArea,
            float surfaceLocalZ,
            Vector3 impactPoint,
            Material shockwaveMaterial)
        {
            GameObject primaryObject = new GameObject("DefenseLinePrimaryShockwave");
            GameObject secondaryObject = new GameObject("DefenseLineSecondaryShockwave");
            LineRenderer primary = CreateShockwaveRenderer(primaryObject, shockwaveMaterial);
            LineRenderer secondary = CreateShockwaveRenderer(secondaryObject, shockwaveMaterial);
            Vector3 localCenter = transform.InverseTransformPoint(impactPoint);
            localCenter.x = Mathf.Clamp(localCenter.x, fractureArea.xMin, fractureArea.xMax);
            localCenter.y = Mathf.Clamp(localCenter.y, fractureArea.yMin, fractureArea.yMax);
            localCenter.z = surfaceLocalZ;
            Vector3 worldCenter = transform.TransformPoint(localCenter);
            float elapsed = 0.0f;
            float totalDuration = Mathf.Max(
                _primaryShockwaveDuration,
                _secondaryShockwaveDelay + _secondaryShockwaveDuration);

            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;
                UpdateShockwave(
                    primary,
                    elapsed,
                    0.0f,
                    _primaryShockwaveDuration,
                    _primaryShockwaveRadius,
                    _primaryShockwaveStartWidth,
                    _primaryShockwaveColor,
                    worldCenter,
                    fractureArea,
                    surfaceLocalZ);
                UpdateShockwave(
                    secondary,
                    elapsed,
                    _secondaryShockwaveDelay,
                    _secondaryShockwaveDuration,
                    _secondaryShockwaveRadius,
                    _secondaryShockwaveStartWidth,
                    _secondaryShockwaveColor,
                    worldCenter,
                    fractureArea,
                    surfaceLocalZ);
                yield return null;
            }

            Destroy(primaryObject);
            Destroy(secondaryObject);
        }

        private LineRenderer CreateShockwaveRenderer(GameObject shockwaveObject, Material material)
        {
            LineRenderer line = shockwaveObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = _shockwaveSegments;
            line.numCornerVertices = 2;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.sharedMaterial = material;
            line.enabled = false;
            return line;
        }

        private void UpdateShockwave(
            LineRenderer line,
            float elapsed,
            float delay,
            float duration,
            float maximumRadius,
            float startWidth,
            Color baseColor,
            Vector3 worldCenter,
            Rect fractureArea,
            float surfaceLocalZ)
        {
            float localElapsed = elapsed - delay;
            if (localElapsed < 0.0f || localElapsed > duration)
            {
                line.enabled = false;
                return;
            }

            line.enabled = true;
            float normalizedTime = Mathf.Clamp01(localElapsed / duration);
            float radius = maximumRadius * (1.0f - Mathf.Pow(1.0f - normalizedTime, 3.0f));
            Vector3 right = transform.TransformDirection(Vector3.right).normalized;
            Vector3 up = transform.TransformDirection(Vector3.up).normalized;
            Vector3 normal = transform.TransformDirection(Vector3.forward).normalized;

            for (int i = 0; i < _shockwaveSegments; i++)
            {
                float angle = (i / (float)_shockwaveSegments) * Mathf.PI * 2.0f;
                Vector3 worldPoint = worldCenter
                    + right * (Mathf.Cos(angle) * radius)
                    + up * (Mathf.Sin(angle) * radius);
                Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
                localPoint.x = Mathf.Clamp(localPoint.x, fractureArea.xMin, fractureArea.xMax);
                localPoint.y = Mathf.Clamp(localPoint.y, fractureArea.yMin, fractureArea.yMax);
                localPoint.z = surfaceLocalZ;
                line.SetPosition(i, transform.TransformPoint(localPoint) + normal * 0.015f);
            }

            Color color = baseColor;
            color.a *= 1.0f - normalizedTime;
            SetLineColor(line, color);
            line.widthMultiplier = Mathf.Lerp(startWidth, startWidth * 0.08f, normalizedTime);
        }

        private void CreateFragments(
            List<DefenseLineFractureMeshBuilder.Cell> cells,
            Rect fractureArea,
            Renderer sourceRenderer,
            float surfaceLocalZ,
            Vector3 impactPoint,
            Vector3 impactNormal,
            Color fragmentColor,
            Material seamMaterial)
        {
            _fragmentRoot = new GameObject("DefenseLineFractureFragments");
            Material fragmentMaterial = sourceRenderer.sharedMaterial;
            float maximumDistance = 0.0001f;

            for (int i = 0; i < cells.Count; i++)
            {
                DefenseLineFractureMeshBuilder.FragmentMesh fragmentMesh =
                    DefenseLineFractureMeshBuilder.BuildFragment(
                        cells[i],
                        transform,
                        surfaceLocalZ,
                        _fragmentWorldThickness,
                        fractureArea);
                float distance = Vector3.ProjectOnPlane(
                    fragmentMesh.WorldCenter - impactPoint,
                    impactNormal).magnitude;
                maximumDistance = Mathf.Max(maximumDistance, distance);

                GameObject fragmentObject = new GameObject($"DefenseLineFragment_{i:00}");
                fragmentObject.layer = _fragmentLayer;
                fragmentObject.transform.SetPositionAndRotation(fragmentMesh.WorldCenter, Quaternion.identity);
                fragmentObject.transform.SetParent(_fragmentRoot.transform, true);

                MeshFilter meshFilter = fragmentObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = fragmentMesh.Mesh;
                MeshRenderer meshRenderer = fragmentObject.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = fragmentMaterial;
                meshRenderer.enabled = false;
                ApplyRendererColor(meshRenderer, fragmentColor);

                MeshCollider meshCollider = fragmentObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = fragmentMesh.Mesh;
                meshCollider.convex = true;
                meshCollider.sharedMaterial = _fragmentPhysicsMaterial;
                meshCollider.enabled = false;

                Rigidbody rigidbody = fragmentObject.AddComponent<Rigidbody>();
                rigidbody.isKinematic = true;
                rigidbody.useGravity = _useGravity;
                rigidbody.mass = Mathf.Max(_minimumMass, fragmentMesh.WorldArea * _massPerSquareUnit);
                rigidbody.linearDamping = _linearDamping;
                rigidbody.angularDamping = _angularDamping;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

                LineRenderer seam = CreateSeam(fragmentObject, fragmentMesh.FrontLoop, seamMaterial);
                seam.enabled = false;

                _fragments.Add(new Fragment
                {
                    GameObject = fragmentObject,
                    Mesh = fragmentMesh.Mesh,
                    Renderer = meshRenderer,
                    Seam = seam,
                    Collider = meshCollider,
                    Rigidbody = rigidbody,
                    ReleaseDelay = distance,
                    DistanceRatio = distance
                });
            }

            System.Random random = new System.Random(_randomSeed + 7919);
            foreach (Fragment fragment in _fragments)
            {
                fragment.DistanceRatio = Mathf.Clamp01(fragment.DistanceRatio / maximumDistance);
                float propagatedDistance = _propagationCurve != null
                    ? _propagationCurve.Evaluate(fragment.DistanceRatio)
                    : fragment.DistanceRatio;
                fragment.ReleaseDelay = Mathf.Max(0.0f, propagatedDistance * _propagationDuration)
                    + RandomRange(random, 0.0f, _releaseRandomDelay);
            }

            _fragments.Sort((left, right) => left.ReleaseDelay.CompareTo(right.ReleaseDelay));
        }

        private IEnumerator PlayFractureSequence(
            Renderer sourceRenderer,
            Vector3 impactPoint,
            Vector3 impactNormal,
            Color fragmentColor,
            GameObject secondaryVfxPrefab)
        {
            if (_handoffDelay > 0.0f)
            {
                yield return new WaitForSeconds(_handoffDelay);
            }

            foreach (Fragment fragment in _fragments)
            {
                fragment.Renderer.enabled = true;
                fragment.Seam.enabled = true;
            }
            sourceRenderer.enabled = false;

            float elapsed = 0.0f;
            int releasedCount = 0;
            int spawnedVfxCount = 0;
            int vfxStep = _secondaryVfxCount > 0
                ? Mathf.Max(1, _fragments.Count / _secondaryVfxCount)
                : int.MaxValue;
            System.Random random = new System.Random(_randomSeed + 1543);

            while (releasedCount < _fragments.Count)
            {
                elapsed += Time.deltaTime;
                float seamT = Mathf.Clamp01(elapsed / _seamGlowDuration);
                Color currentSeamColor = Color.Lerp(_seamIgnitionColor, _seamColor, seamT);

                foreach (Fragment fragment in _fragments)
                {
                    if (fragment.Seam.enabled)
                    {
                        SetLineColor(fragment.Seam, currentSeamColor);
                    }
                }

                while (releasedCount < _fragments.Count
                    && _fragments[releasedCount].ReleaseDelay <= elapsed)
                {
                    Fragment fragment = _fragments[releasedCount];
                    ReleaseFragment(fragment, impactPoint, impactNormal, random);

                    if (secondaryVfxPrefab != null
                        && spawnedVfxCount < _secondaryVfxCount
                        && releasedCount % vfxStep == 0)
                    {
                        SpawnSecondaryVfx(
                            secondaryVfxPrefab,
                            fragment.GameObject.transform.position,
                            impactNormal,
                            fragmentColor,
                            random);
                        spawnedVfxCount++;
                    }

                    releasedCount++;
                }

                yield return null;
            }

            float collisionElapsed = 0.0f;
            while (collisionElapsed < _fragmentFadeDelay)
            {
                collisionElapsed += Time.deltaTime;
                if (collisionElapsed >= _collisionDisableDelay)
                {
                    DisableFragmentCollisions();
                }
                yield return null;
            }

            DisableFragmentCollisions();
            float fadeElapsed = 0.0f;
            while (fadeElapsed < _fragmentFadeDuration)
            {
                fadeElapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(fadeElapsed / _fragmentFadeDuration);
                float alpha = 1.0f - Mathf.SmoothStep(0.0f, 1.0f, normalizedTime);
                Color fadedFragmentColor = fragmentColor;
                fadedFragmentColor.a *= alpha;
                Color fadedSeamColor = _seamColor;
                fadedSeamColor.a *= alpha;
                float scale = Mathf.Lerp(1.0f, _fragmentEndScale, normalizedTime);

                foreach (Fragment fragment in _fragments)
                {
                    ApplyRendererColor(fragment.Renderer, fadedFragmentColor);
                    SetLineColor(fragment.Seam, fadedSeamColor);
                    fragment.GameObject.transform.localScale = Vector3.one * scale;
                }

                yield return null;
            }

            DestroyFragments();
        }

        private void ReleaseFragment(
            Fragment fragment,
            Vector3 impactPoint,
            Vector3 impactNormal,
            System.Random random)
        {
            fragment.Rigidbody.isKinematic = false;
            fragment.Collider.enabled = _enableFragmentCollisions;

            Vector3 radial = Vector3.ProjectOnPlane(
                fragment.GameObject.transform.position - impactPoint,
                impactNormal).normalized;
            Vector3 randomDirection = RandomUnitVector(random);
            float impactStrength = Mathf.Lerp(1.0f, 0.38f, fragment.DistanceRatio);
            Vector3 impulse = (
                -impactNormal * _normalImpulse
                + radial * _radialImpulse
                + randomDirection * _randomImpulse) * impactStrength;
            fragment.Rigidbody.AddForce(impulse, ForceMode.Impulse);
            fragment.Rigidbody.AddTorque(RandomUnitVector(random) * _angularImpulse, ForceMode.Impulse);
        }

        private LineRenderer CreateSeam(GameObject fragmentObject, Vector3[] frontLoop, Material seamMaterial)
        {
            LineRenderer seam = fragmentObject.AddComponent<LineRenderer>();
            seam.useWorldSpace = false;
            seam.loop = true;
            seam.positionCount = frontLoop.Length;
            seam.SetPositions(frontLoop);
            seam.widthMultiplier = _seamWidth;
            seam.numCornerVertices = 2;
            seam.numCapVertices = 1;
            seam.alignment = LineAlignment.View;
            seam.textureMode = LineTextureMode.Stretch;
            seam.sharedMaterial = seamMaterial;
            SetLineColor(seam, _seamIgnitionColor);
            return seam;
        }

        private void SpawnSecondaryVfx(
            GameObject prefab,
            Vector3 position,
            Vector3 impactNormal,
            Color effectColor,
            System.Random random)
        {
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, impactNormal);
            GameObject instance = Instantiate(prefab, position, rotation);
            DefenseLineReaction.ApplyParticleColor(
                instance,
                effectColor,
                _secondaryVfxColorBlend,
                _secondaryVfxBrightness);
            float minimumScale = Mathf.Min(_secondaryVfxScaleRange.x, _secondaryVfxScaleRange.y);
            float maximumScale = Mathf.Max(_secondaryVfxScaleRange.x, _secondaryVfxScaleRange.y);
            instance.transform.localScale *= RandomRange(random, minimumScale, maximumScale);
            Destroy(instance, _secondaryVfxLifetime);
        }

        private void PlayBreakAudio()
        {
            SoundManager.instance?.StopBGM();
            SoundManager.instance?.PlaySE("Barrier_01");
            StartCoroutine(PlayTailAudio());
        }

        private IEnumerator PlayTailAudio()
        {
            if (_breakTailDelay > 0.0f)
            {
                yield return new WaitForSeconds(_breakTailDelay);
            }
            SoundManager.instance?.PlaySE("Barrier_02");
        }

        private void DisableFragmentCollisions()
        {
            foreach (Fragment fragment in _fragments)
            {
                if (fragment.Collider != null)
                {
                    fragment.Collider.enabled = false;
                }
            }
        }

        private void DestroyFragments()
        {
            foreach (Fragment fragment in _fragments)
            {
                if (fragment.Mesh != null)
                {
                    Destroy(fragment.Mesh);
                }
            }
            _fragments.Clear();

            if (_fragmentRoot != null)
            {
                Destroy(_fragmentRoot);
                _fragmentRoot = null;
            }
        }

        private void OnDestroy()
        {
            DestroyFragments();
        }

        private static Rect GetFractureArea(Bounds bounds, Vector2 xRange, Vector2 yRange)
        {
            float minimumX = Mathf.Max(bounds.min.x, Mathf.Min(xRange.x, xRange.y));
            float maximumX = Mathf.Min(bounds.max.x, Mathf.Max(xRange.x, xRange.y));
            float minimumY = Mathf.Max(bounds.min.y, Mathf.Min(yRange.x, yRange.y));
            float maximumY = Mathf.Min(bounds.max.y, Mathf.Max(yRange.x, yRange.y));
            if (minimumX >= maximumX)
            {
                minimumX = bounds.min.x;
                maximumX = bounds.max.x;
            }
            if (minimumY >= maximumY)
            {
                minimumY = bounds.min.y;
                maximumY = bounds.max.y;
            }

            return Rect.MinMaxRect(minimumX, minimumY, maximumX, maximumY);
        }

        private static void ApplyRendererColor(Renderer renderer, Color color)
        {
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            renderer.SetPropertyBlock(propertyBlock);
        }

        private static void SetLineColor(LineRenderer line, Color color)
        {
            line.startColor = color;
            line.endColor = color;
        }

        private static Vector3 RandomUnitVector(System.Random random)
        {
            float z = RandomRange(random, -1.0f, 1.0f);
            float angle = RandomRange(random, 0.0f, Mathf.PI * 2.0f);
            float radius = Mathf.Sqrt(Mathf.Max(0.0f, 1.0f - z * z));
            return new Vector3(radius * Mathf.Cos(angle), z, radius * Mathf.Sin(angle));
        }

        private static float RandomRange(System.Random random, float minimum, float maximum)
        {
            return Mathf.Lerp(minimum, maximum, (float)random.NextDouble());
        }
    }
}
