using System.Collections;
using System.Collections.Generic;
using Game.Gameplay.Collectibles;
using UnityEngine;

namespace Game.Core.Enemy
{
    /// <summary>
    /// ステージアンカーの範囲内に、収集物を排出する蔦を生成する。
    /// </summary>
    public sealed class StageCollectibleVineSpawner : MonoBehaviour
    {
        [Header("蔦の生成")]
        [SerializeField] private GameObject _vinePrefab;
        [SerializeField, Min(0.1f)] private float _spawnInterval = 8f;
        [SerializeField, Min(1)] private int _maxActiveVines = 6;
        [SerializeField, Min(0f)] private float _edgeInset = 2f;
        [SerializeField] private Vector3 _spawnOffset;

        [Header("アンカー")]
        [SerializeField] private string _leftFrontAnchorTag = "Field_LeftFront";
        [SerializeField] private string _rightFrontAnchorTag = "Field_RightFront";
        [SerializeField] private string _leftBackAnchorTag = "Field_LeftBack";
        [SerializeField] private string _rightBackAnchorTag = "Field_RightBack";
        [SerializeField] private string _centerAnchorTag = "Field_Center";

        [Header("収集物")]
        [SerializeField, Min(0)] private int _collectibleCount = 12;

        private readonly List<StageCollectibleVine> _activeVines = new();
        private CollectibleSpawner _collectibleSpawner;
        private Coroutine _spawnCoroutine;

        private void OnEnable()
        {
            _spawnCoroutine = StartCoroutine(SpawnRoutine());
        }

        private void OnDisable()
        {
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }
        }

        private IEnumerator SpawnRoutine()
        {
            while (enabled)
            {
                SpawnVine();
                yield return new WaitForSeconds(_spawnInterval);
            }
        }

        private void SpawnVine()
        {
            _activeVines.RemoveAll(vine => vine == null);
            if (_vinePrefab == null || _activeVines.Count >= _maxActiveVines)
            {
                return;
            }

            if (!TryGetStageBounds(out Bounds bounds))
            {
                return;
            }

            Vector3 position = new Vector3(
                Random.Range(bounds.min.x + _edgeInset, bounds.max.x - _edgeInset),
                bounds.max.y,
                Random.Range(bounds.min.z + _edgeInset, bounds.max.z - _edgeInset));
            position += _spawnOffset;

            GameObject vineObject = Instantiate(_vinePrefab, position, Quaternion.identity);
            if (!vineObject.TryGetComponent(out StageCollectibleVine vine))
            {
                Destroy(vineObject);
                return;
            }

            vine.Initialize(this);
            _activeVines.Add(vine);
        }

        private bool TryGetStageBounds(out Bounds bounds)
        {
            Transform[] anchors = new[]
            {
                FindAnchor(_leftFrontAnchorTag),
                FindAnchor(_rightFrontAnchorTag),
                FindAnchor(_leftBackAnchorTag),
                FindAnchor(_rightBackAnchorTag)
            };

            bounds = default;
            bool hasAnchor = false;
            foreach (Transform anchor in anchors)
            {
                if (anchor == null)
                {
                    continue;
                }

                if (!hasAnchor)
                {
                    bounds = new Bounds(anchor.position, Vector3.zero);
                    hasAnchor = true;
                }
                else
                {
                    bounds.Encapsulate(anchor.position);
                }
            }

            if (!hasAnchor)
            {
                return false;
            }

            Transform center = FindAnchor(_centerAnchorTag);
            float surfaceY = center != null ? center.position.y : bounds.max.y;
            bounds.SetMinMax(
                new Vector3(bounds.min.x, surfaceY, bounds.min.z),
                new Vector3(bounds.max.x, surfaceY, bounds.max.z));
            return true;
        }

        public void HandleVineBroken(StageCollectibleVine vine, Vector3 position)
        {
            _activeVines.Remove(vine);
            _collectibleSpawner ??= FindFirstObjectByType<CollectibleSpawner>();
            _collectibleSpawner?.SpawnCollectiblesAt(position, _collectibleCount);
        }

        private Transform FindAnchor(string tag)
        {
            try
            {
                GameObject anchor = GameObject.FindGameObjectWithTag(tag);
                return anchor != null ? anchor.transform : null;
            }
            catch (UnityException)
            {
                return null;
            }
        }
    }
}
