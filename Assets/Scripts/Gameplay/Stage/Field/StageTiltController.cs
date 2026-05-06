// ================================================================================
// File         : StageTiltController.cs
// Author       : Terada Haru
//
// Description  : フィールドの傾き処理と崩落処理を行う
// Created      : 2026-05-06
// ================================================================================
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;


namespace Game.Gameplay.Stage
{

    public class StageTiltController : MonoBehaviour
    {
        enum FieldState
        {
            Stable,
            Preparing,
            Tilting,
            Avalanche,
            Recovering,
            Locked,
        }



        [Tooltip("生成オブジェクト")]
        [SerializeField]
        private StageData _stageData;

    
        private Queue<GameObject> _tiltFieldQueue = new Queue<GameObject>();

        private List<GameObject> _spawners = new List<GameObject>();

        [Header("傾き設定")]
        [Tooltip("最大傾き角度")]
        [SerializeField]
        private float _tiltAngle = 12.0f;
        [Tooltip("傾きが完了するまでの時間")]
        [SerializeField]
        private float _tiltDuration = 0.45f;
        [Tooltip("傾きを保持する時間")]
        [SerializeField]
        private float _holdDuration = 0.35f;
        [Tooltip("傾きから復帰するまでの時間")]
        [SerializeField]
        private float _recoverDuration = 0.65f;


    
        // 方向を反映した傾きの目標角度
        private Quaternion _tiltTargetAngle;

        // 現在の傾き状態の経過時間
        private float _pastTiltTime;
        // 現在の傾き状態の必要時間
        private float _durationTime;
        private FieldState _fieldState;


        private float _debugFallCycle;

        private bool _isFall;


        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _fieldState = FieldState.Stable;

            for(var i = 0; i < _stageData.height; ++i)
            {
                var obj = Instantiate(_stageData.FieldGroundPrefab);
                obj.transform.position = new Vector3(0.0f,0.0f,i - _stageData.height / 2.0f);
                obj.transform.localScale = new Vector3(_stageData.width,1.0f,1.0f);
                obj.transform.SetParent(transform);
                _tiltFieldQueue.Enqueue(obj);
            }
        
            /*
            foreach(var obj in _stageData.collectibleSpawner)
            {
                var spawnObj = Instantiate(obj.SpawnerPrefab, obj.position, Quaternion.identity);
                var spawner = spawnObj.GetComponent<FieldTestSpawner>();
                spawner.Initialize(obj.SpawnInterval);
                _spawners.Add(spawnObj);
            }
            */
            _debugFallCycle = 3.0f;
        }

        // Update is called once per frame
        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            if(mouse.leftButton.isPressed)
            {
                Vector2 mousePos = mouse.position.ReadValue();

                Ray ray = Camera.main.ScreenPointToRay(mousePos);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    TiltStage(hit.point);
                }
            }
            else
            {
            }

            _debugFallCycle -= Time.deltaTime;
            if(_debugFallCycle <= 0.0f)
            {
                FieldFall();
                // デバッグ用
                _debugFallCycle = 3.0f;
            }

            TiltUpdate();
        
        }

        /// <summary>
        /// 傾きを開始する
        /// </summary>
        /// <param name="tiltTargetPos">傾かせる方向を算出するための位置</param>
        public void TiltStage(Vector3 tiltTargetPos)
        {
            _pastTiltTime = 0.0f;
            _fieldState = FieldState.Tilting;
            _durationTime = _tiltDuration;

            Vector3 localDir = transform.InverseTransformPoint(tiltTargetPos);
            localDir.y = 0.0f;

            if (localDir.sqrMagnitude < 0.0f) return;

            Vector3 tiltAxis = Vector3.Cross(Vector3.up, localDir.normalized);

            _tiltTargetAngle = Quaternion.AngleAxis(_tiltAngle, tiltAxis);
        }
        /// <summary>
        /// 現在の状態に合わせた傾きの更新
        /// </summary>
        private void TiltUpdate()
        {
            switch (_fieldState)
            {
                case FieldState.Tilting:
                    _pastTiltTime += Time.deltaTime;
                    if (_pastTiltTime >= _tiltDuration)
                    {
                        _durationTime = _holdDuration;
                        _fieldState = FieldState.Avalanche;
                        _pastTiltTime = 0.0f;
                    }
                    break;

                case FieldState.Avalanche:
                    // 角度を変えないようにしつつホールド
                    _pastTiltTime += Time.deltaTime;
                    // ここら辺に後々雪崩システムが入るのかな？

                    if (_pastTiltTime >= _holdDuration)
                    {// 状態更新
                        _durationTime = _recoverDuration;
                        _fieldState = FieldState.Recovering;
                        _pastTiltTime = 0.0f;
                    }
                    break;

                case FieldState.Recovering:
                    // 目標角度を更新
                    _tiltTargetAngle = Quaternion.identity;
                    _pastTiltTime += Time.deltaTime;
                    if(_pastTiltTime >= _recoverDuration)
                    {
                        _fieldState = FieldState.Stable;
                    }
                    break;

                default: break;
            }
            transform.rotation = Quaternion.Slerp(transform.rotation,
                _tiltTargetAngle, _pastTiltTime / _durationTime);
        }

        private void FieldFall()
        {
            foreach (var moveObj in _tiltFieldQueue)
            {
                moveObj.transform.Translate(new Vector3(0.0f, 0.0f, -1.0f));
            }
            // 一番手前のフィールドを落下させる
            var obj = _tiltFieldQueue.Dequeue();
            Vector3 fallObjPos = obj.transform.position;
            for(int i = 0;i < _stageData.width;++i)
            {
                fallObjPos.x = i - _stageData.width / 2.0f + 0.5f;
                var fallObj = Instantiate(_stageData.FieldGroundPrefab, fallObjPos, Quaternion.identity);
                fallObj.AddComponent<Rigidbody>();
                Destroy(fallObj, 2.0f);
            }

            // 新たなフィールド用の地面の生成
            obj.transform.localPosition = new Vector3(0.0f, 0.0f, _stageData.height / 2.0f - 1.0f);
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = new Vector3(_stageData.width, 1.0f, 1.0f);
            _tiltFieldQueue.Enqueue(obj);

            _isFall = true;
        }
    }

}
