// ------------------------------------------------------------
// File		: BigHandAuraController.cs
// Summary	: BigHandのスケールを監視し、しきい値を跨いだら
//              Meshの表示切り替え + オーラエフェクトを再生
//
// Author	: [浅野勇生]
// Created	: 2026-07-08
//
// Notes	:
// - ベースの作成
// - 7/8: 親子付けをやめてLateUpdate追従に変更（スケール継承と置き去り対策）
// ------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace Game.Gameplay.Player
{
    public class BigHandAuraController : MonoBehaviour
    {
        [Header("エフェクトプレハブ")]
        [Tooltip("出現時に一度だけ再生するエフェクト")]
        [SerializeField] private GameObject _startVfxPrefab;

        [Tooltip("出現中ずっとループ再生するエフェクト")]
        [SerializeField] private GameObject _loopVfxPrefab;

        [Tooltip("消滅時に一度だけ再生するエフェクト")]
        [SerializeField] private GameObject _finishVfxPrefab;


        [Header("BigHandの追従先ソケット")]
        [SerializeField] private Transform _leftSocket;
        [SerializeField] private Transform _rightSocket;


        [Header("BigHandの表示切り替え用ソケット")]
        [Tooltip("表示を切り替えるBigHandのソケット")]
        [SerializeField] private Transform _modelRoot;


        [Header("しきい値")]
        [Tooltip("BigHandのスケールがこの値を超えたら表示切り替え")]
        [SerializeField] private float _visibleThreshold = 0.15f;

        [Tooltip("Finishエフェクトを破棄するまでの時間")]
        [SerializeField] private float _finishLifeTime = 1.5f;

        [Tooltip("Startエフェクトを見せてからループを出すまでの時間")]
        [SerializeField] private float _loopDelay = 0.4f;

        [Header("エフェクトのサイズ")]
        [Tooltip("エフェクト全体の大きさ倍率（プレハブのスケール × この値）")]
        [SerializeField] private float _effectScale = 1f;

        [Tooltip("Startエフェクトだけに掛かる追加の倍率")]
        [SerializeField] private float _startScale = 0.7f;


        [Header("再生速度")]
        [Tooltip("Startエフェクトの再生速度倍率")]
        [SerializeField] private float _startSpeed = 1f;

        [Tooltip("Finishエフェクトの再生速度倍率")]
        [SerializeField] private float _finishSpeed = 2f;


        [Header("追従設定")]
        [Tooltip("腕が出たあと、Startエフェクトがソケットに吸い付く速さ")]
        [SerializeField] private float _blendSpeed = 10f;


        private Renderer[] _renderers;              ///< 表示切替対象のレンダラー
        private bool _isVisible;                    ///< BigHandの表示状態
        private GameObject _leftLoopInstance;       ///< 左手のループエフェクトインスタンス
        private GameObject _rightLoopInstance;      ///< 右手のループエフェクトインスタンス

        private Coroutine _loopSpawnRoutine;        ///< ループ遅延スポーンのコルーチン


        /// <summary>
        /// エフェクトの追従方法
        /// </summary>
        private enum FollowMode
        {
            Socket,         // ソケットに追従
            PlayerOffset,   // スポーン時の位置関係を保ってプレイヤーに追従
        }


        private class Follower
        {
            public Transform Effect;            // 追従させるエフェクト
            public Transform Socket;            // 追従先ソケット
            public Vector3 FixedOffset;         // ルート基準の固定
            public Quaternion FixedRotation;    // ルート基準の固定回転
            public Transform PendingSocket;     // 腕が出たら追従先になるソケット（Start用）
            public bool Blending;               // ソケットへ滑らかに移動中か
        }


        /// <summary>
        /// ソケットに追従させるエフェクトのリスト（親子付けの代わりに位置・回転だけコピーする）
        /// </summary>
        private readonly List<Follower> _followers = new();


        /// <summary>
        /// 出現の溜めエフェクトを再生する（腕が大きくなる前に呼ばれる）
        /// </summary>
        public void PlayStartEffect(Transform leftPoint, Transform rightPoint)
        {
            // すでに腕が出ているときは重複再生しない
            if (_isVisible)
            {
                return;
            }

            SpawnOneShot(_startVfxPrefab, leftPoint  != null ? leftPoint  : _leftSocket,  FollowMode.PlayerOffset, _startSpeed);
            SpawnOneShot(_startVfxPrefab, rightPoint != null ? rightPoint : _rightSocket, FollowMode.PlayerOffset, _startSpeed);
        }


        private void Awake()
        {
            // モデル配下のレンダラーをキャッシュ
            Transform root = _modelRoot != null ? _modelRoot : transform;
            _renderers = root.GetComponentsInChildren<Renderer>(true);

            // 初期状態は非表示
            _isVisible = false;
            SetMeshVisible(false);
        }


        private void Update()
        {
            // スケールを監視
            bool shouldBeVisible = transform.localScale.x >= _visibleThreshold;

            if (shouldBeVisible == _isVisible)
            {
                return;
            }

            _isVisible = shouldBeVisible;

            if (shouldBeVisible)
            {
                OnExpand();
            }
            else
            {
                OnShrink();
            }
        }


        /// <summary>
        /// アニメーション適用後のソケット位置にエフェクトを追従させる
        /// </summary>
        private void LateUpdate()
        {
            for (int i = _followers.Count - 1; i >= 0; i--)
            {
                var follower = _followers[i];

                // 破棄済みのエフェクトはリストから除去
                if (follower.Effect == null)
                {
                    _followers.RemoveAt(i);
                    continue;
                }

                if (follower.Socket != null)
                {
                    // 手に追従
                    follower.Effect.SetPositionAndRotation(follower.Socket.position, follower.Socket.rotation);
                }
                else
                {
                    // スポーン時の位置関係を保ったまま追従
                    Vector3 pos = transform.position + transform.rotation * follower.FixedOffset;

                    follower.Effect.SetPositionAndRotation(pos, transform.rotation * follower.FixedRotation);
                }
            }
        }


        /// <summary>
        /// しきい値を上回った瞬間の処理
        /// </summary>
        private void OnExpand()
        {
            SetMeshVisible(true);

            // ループはStartを見せ終わってから出す
            _loopSpawnRoutine = StartCoroutine(SpawnLoopDelayed());
        }


        /// <summary>
        /// しきい値を下回った瞬間の処理
        /// </summary>
        private void OnShrink()
        {
            if (_loopSpawnRoutine != null)
    {
        StopCoroutine(_loopSpawnRoutine);
        _loopSpawnRoutine = null;
    }

            SetMeshVisible(false);

            StopLoop(ref _leftLoopInstance);
            StopLoop(ref _rightLoopInstance);

            SpawnOneShot(_finishVfxPrefab, _leftSocket, FollowMode.PlayerOffset, _finishSpeed);
            SpawnOneShot(_finishVfxPrefab, _rightSocket, FollowMode.PlayerOffset, _finishSpeed);
        }


        /// <summary>
        /// メッシュの描画切り替え
        /// </summary>
        /// <param name="visible"></param>
        private void SetMeshVisible(bool visible)
        {
            foreach (var renderer in _renderers)
            {
                if (renderer != null)
                {
                    renderer.gameObject.SetActive(visible);
                }
            }
        }


        /// <summary>
        /// 一回だけ再生するエフェクトの生成
        /// </summary>
        /// <param name="prefab">プレハブ</param>
        /// <param name="socket">ソケット</param>
        /// <param name="follow">ソケットに追従させるか</param>
        private void SpawnOneShot(GameObject prefab, Transform socket, FollowMode mode,float speed)
        {
            if (prefab == null || socket == null)
                return;

            // 親子付けしないのでスケールを継承せず、プレハブ本来のサイズで出る
            var obj = Instantiate(prefab, socket.position, socket.rotation);
            obj.transform.localScale = prefab.transform.localScale * _effectScale;
            SetSimulationSpeed(obj, speed);

            if (mode == FollowMode.Socket)
            {
                _followers.Add(new Follower { Effect = obj.transform, Socket = socket });
            }
            else
            {
                _followers.Add(new Follower
                {
                    Effect = obj.transform,
                    Socket = null,
                    FixedOffset = Quaternion.Inverse(transform.rotation) * (socket.position - transform.position),
                    FixedRotation = Quaternion.Inverse(transform.rotation) * socket.rotation,
                });
            }

            Destroy(obj, _finishLifeTime);
        }


        /// <summary>
        /// ループエフェクトの生成
        /// </summary>
        /// <param name="prefab">プレハブ</param>
        /// <param name="socket">ソケット</param>
        /// <returns></returns>
        private GameObject SpawnLoop(GameObject prefab, Transform socket)
        {
            if (prefab == null || socket == null)
                return null;

            var obj = Instantiate(prefab, socket.position, socket.rotation);
            obj.transform.localScale = prefab.transform.localScale * _effectScale;

            // 追従リストに登録するので、パンチで動いても付いていく
            _followers.Add(new Follower { Effect = obj.transform, Socket = socket });

            return obj;
        }


        /// <summary>
        /// ループエフェクトのストップ
        /// </summary>
        /// <param name="instance"></param>
        private void StopLoop(ref GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            // 置き去り防止のため即座に消す（消え際の見た目はFinishエフェクトに任せる）
            RemoveFollower(instance.transform);

            foreach (var ps in instance.GetComponentsInChildren<ParticleSystem>())
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            Destroy(instance);
            instance = null;
        }


        /// <summary>
        /// 追従リストから指定のエフェクトを外す
        /// </summary>
        /// <param name="effect"></param>
        private void RemoveFollower(Transform effect)
        {
            for (int i = _followers.Count - 1; i >= 0; i--)
            {
                if (_followers[i].Effect == effect)
                {
                    _followers.RemoveAt(i);
                }
            }
        }


        /// <summary>
        /// 親子付けしていないため、アタッチメント破棄時にエフェクトも道連れにする
        /// </summary>
        private void OnDestroy()
        {
            foreach (var follower in _followers)
            {
                if (follower.Effect != null)
                {
                    Destroy(follower.Effect.gameObject);
                }
            }

            _followers.Clear();
        }

        /// <summary>
        /// エフェクトの再生速度を変更する
        /// </summary>
        /// <param name="obj">オブジェクト</param>
        /// <param name="speed">スピード</param>
        private void SetSimulationSpeed(GameObject obj, float speed)
        {
            foreach (var ps in obj.GetComponentsInChildren<ParticleSystem>())
            {
                var main = ps.main;
                main.simulationSpeed = speed;
            }
        }


        /// <summary>
        /// ループスポーンディレイ
        /// </summary>
        /// <returns></returns>
        private IEnumerator SpawnLoopDelayed()
        {
            yield return new WaitForSeconds(_loopDelay);

            _leftLoopInstance = SpawnLoop(_loopVfxPrefab, _leftSocket);
            _rightLoopInstance = SpawnLoop(_loopVfxPrefab, _rightSocket);

            _loopSpawnRoutine = null;
        }
    }
}
