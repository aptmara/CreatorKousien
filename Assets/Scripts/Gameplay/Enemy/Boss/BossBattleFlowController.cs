/*
 * 寺田
 * ボスの動作のフローを管理する
 * 
 * 
 */

using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using Game.Data.Enemy.Boss;
using Game.Core.Events;

namespace Game.Gameplay.Enemy.Boss
{
    public enum BossBattleFlowState
    {
        Inactive,
        Intro,
        InBattle,
        Down,
        PhaseTransition,
        Victory,
        Defeat,
    }

    public enum BossDamageAcceptance
    {
        AlwaysDamageable,
        DownOnly,
    }


    [System.Serializable]
    public struct SocketBinding
    {
        public BossSocket socket;
        public Transform transform;
    }


    public class BossBattleFlowController : MonoBehaviour
    {
        [Header("==== 基本ステータス =====")]
        [SerializeField] private BossBattleDataSO _battleData;
        [SerializeField] private float _maxHP = 1000.0f;
        [SerializeField] private float _currentHP;

        [Header("==== 勝利条件 ====")]
        [SerializeField] private bool _triggerVictoryOnZeroHp = true;
        [SerializeField,Tooltip("0は無制限")]
        private float _timeLimit = 0.0f; // 0は無制限

        [Header("==== ダウンシステムの設定 ====")]
        [SerializeField] private bool _useDownSystem = true;
        [SerializeField] private float _downDuration = 5.0f;
        [SerializeField] private string _downAnimTrigger = "Down";
        [SerializeField] private string _recoverAnimTrigger = "Recover";
        [SerializeField] private BossDownPresentationController _downPresentationController;

        [Header("===== ダメージルール =====")]
        [SerializeField] private BossDamageAcceptance _damageAcceptance = BossDamageAcceptance.AlwaysDamageable;
        [SerializeField, Min(1.0f)] private float _downDamageMultiplier = 1.0f;


        [Header("==== 参照 ====")]
        [SerializeField] private Animator _bossAnimator;
        [SerializeField] private List<SocketBinding> _socketBindings;

        [Header("==== 演出 =====")]
        [SerializeField] private BossIntroSequenceController _introSequenceController;
        [SerializeField] private BossIntroSequenceData _introSequenceData;

        [Header("==== ギミック登録リスト ====")]
        [SerializeField] private List<GimmickSlot> _gimmickSlots = new List<GimmickSlot>();

        //======= ランタイム状態 =======
        private string _bossInstanceId = string.Empty;
        private BossBattleFlowState _currentState = BossBattleFlowState.Inactive;
        private int _currentPhaseIndex = -1;
        private BossPhaseData _currentPhaseData;
        private bool _isInitialized;
        private bool _isBattleActive;

        private float _battleTimer = 0.0f;
        private float _downTimer = 0.0f;
        private Coroutine _stateRoutine;
        private BossContext _bossContext;

        //====== ギミック実行状態 ========
        private BossGimmickSO _currentWaitingGimmick = null;
        private BossGimmickData _currentWaitingData = null;
        private float _waitingTimeoutTimer = 0.0f;
        private Queue<GimmickSlot> _interruptQueue = new Queue<GimmickSlot>();


        //====== 公開 =======
        public string BossInstanceId => _bossInstanceId;
        public BossBattleFlowState CurrentState => _currentState;
        public int CurrentPhaseIndex => _currentPhaseIndex;
        public BossPhaseData CurrentPhaseData => _currentPhaseData;
        public float CurrentHP => _currentHP;
        public float MaxHP => _maxHP;
        public float HpPercentage => Mathf.Clamp01(_currentHP / _maxHP);
        public bool IsDown => _currentState == BossBattleFlowState.Down;
        public bool IsBattleActive => _isBattleActive;
        public List<GimmickSlot> GimmickSlots => _gimmickSlots;

        public bool CanReceiveBodyDamage =>
            _isBattleActive &&
            CurrentState != BossBattleFlowState.Victory &&
            CurrentState != BossBattleFlowState.Defeat &&
            (_damageAcceptance == BossDamageAcceptance.AlwaysDamageable || IsDown);

        //======= イベント ========
        public event Action<BossBattleFlowState, BossBattleFlowState> OnStateChanged;
        public event Action<int, BossPhaseData> OnPhaseStarted;
        public event Action<float, float> OnHpChanged; // <current , max>
        public event Action OnVictory;
        public event Action OnDefeat;
        public event Action OnDownStart;
        public event Action OnDownEnd;
        public event Action<string> OnBossBattleCompleted;

        private void Awake()
        {
            _currentHP = _maxHP;

            // Dictionary化してContextに渡す
            var socketDict = new Dictionary<BossSocket, Transform>();
            foreach (var binding in _socketBindings)
            {
                if (binding.transform != null && !socketDict.ContainsKey(binding.socket))
                {
                    socketDict.Add(binding.socket, binding.transform);
                }
            }

            _bossContext = new BossContext(this, _bossAnimator, transform, socketDict);
        }

        private void OnDisable()
        {
            if(_isBattleActive)
            {
                StopBattle();
            }
        }

        public bool Initialize(string bossInstanceId)
        {
            if (_isBattleActive) return false;
            if(string.IsNullOrEmpty(bossInstanceId)) return false;

            _bossInstanceId = bossInstanceId;
            _isInitialized = true;
            return true;
        }

        public bool StartBattle(string bossInstanceId)
        {
            if(!Initialize(bossInstanceId)) return false;
            
            return BeginBattle();
        }

        public bool BeginBattle()
        {
            if(_isBattleActive || !_isInitialized) return false;

            _battleTimer = 0.0f;
            _currentHP = _maxHP;
            _currentPhaseIndex = -1;
            _currentPhaseData = null;
            _currentWaitingGimmick = null;
            _currentWaitingData = null;
            _interruptQueue.Clear();
            _isBattleActive = true;

            foreach(var slot in _gimmickSlots)
            {
                slot?.InitializeSlot(_battleTimer, _bossContext);
            }

            ChangeState(BossBattleFlowState.Intro);
            _stateRoutine = StartCoroutine(PlayIntroSequence());

            return true;
        }


        private IEnumerator PlayIntroSequence()
        {
            if (_introSequenceController != null && _introSequenceData != null)
            {
                yield return StartCoroutine(_introSequenceController.PlayPresentation(_introSequenceData,_bossAnimator));
            }
            else if(_bossAnimator != null)
            {
                _bossAnimator.SetTrigger("Intro");
                yield return new WaitForSeconds(3.0f);
            }

            if (!_isBattleActive) yield break;

            _stateRoutine = null;

            BeginPhase(0);
        }

        public bool BeginPhase(int phaseIndex)
        {
            if(_battleData != null && _battleData.TryGetPhaseData(phaseIndex,out BossPhaseData phaseData))
            {
                _currentPhaseIndex = phaseIndex;
                _currentPhaseData = phaseData;
            }

            ChangeState(BossBattleFlowState.InBattle);
            OnPhaseStarted?.Invoke(_currentPhaseIndex, _currentPhaseData);
            return true;
        }

        private void Update()
        {
            if (!_isBattleActive) return;

            //======== タイムリミット =========
            if(_timeLimit > 0.0f && _battleTimer >= _timeLimit)
            {
                TriggerDefeat();
                return;
            
            }

            //======== ダウン中の処理 =========
            if(_useDownSystem &&IsDown)
            {
                _downTimer -= Time.deltaTime;
                if (_downTimer <= 0.0f)
                {
                    EndDown();
                }
                return;
            }

            //========= 完了待ち処理 ==========
            if(_currentWaitingGimmick != null)
            {
                _waitingTimeoutTimer += Time.deltaTime;

                if(_currentWaitingGimmick.IsTick)
                {
                    _currentWaitingGimmick.Tick(Time.deltaTime);
                }

                bool isTimeout = _currentWaitingData != null &&
                    _currentWaitingData.timeoutDuration > 0.0f &&
                    _waitingTimeoutTimer >= _currentWaitingData.timeoutDuration;

                if(_currentWaitingGimmick.IsComplete || isTimeout)
                {
                    var nextOverride = _currentWaitingGimmick.NextOverrideGimmick;
                    if (nextOverride != null)
                    {
                        EnqueueInterruptGimmick(nextOverride);
                    }
                    _currentWaitingGimmick = null;
                    _currentWaitingData = null;
                }
                else
                {
                    return;
                }
            }

            //======= 割り込み処理 =========
            if (_interruptQueue.Count > 0)
            {
                var interruptSlot = _interruptQueue.Dequeue();
                ExecuteSlot(interruptSlot);
                return;
            }

            _battleTimer += Time.deltaTime;
            //========= ギミックの処理 ========
            EvaluateIntervalGimmicks();
            EvaluateTimelineGimmicks();
        }

        /*
         * 
         * ダメージ処理
         * 
         */

        public void TakeDamage(float amount)
        {
            if (!CanReceiveBodyDamage) return;

            float multiplier = IsDown ? _downDamageMultiplier : 1.0f;
            _currentHP = Mathf.Max(0.0f, _currentHP - amount * multiplier);
            OnHpChanged?.Invoke(_currentHP,_maxHP);

            if(_currentHP <= 0.0f && _triggerVictoryOnZeroHp)
            {
                TriggerVictory();
            }
        }

        public void TriggerVictory()
        {
            if (CurrentState == BossBattleFlowState.Victory) return;

            ChangeState(BossBattleFlowState.Victory);
            StopBattle();
            Debug.Log("[BattleFlow] <color=green>勝利条件達成!</color>");
            OnVictory?.Invoke();
        }

        public void TriggerDefeat()
        {
            if (CurrentState == BossBattleFlowState.Defeat) return;

            ChangeState(BossBattleFlowState.Defeat);
            StopBattle();
            Debug.Log("[BattleFlow] <color=red>敗北条件達成!(バリア破壊またはタイムアップ)</color>");
            OnDefeat?.Invoke();
        }

    

        /*
         * 
         * DownSystem
         * 
         */
        public void TriggerDown()
        {
            if (!_useDownSystem || IsDown) return;

            _downTimer = _downDuration;
            ChangeState(BossBattleFlowState.Down);

            _currentWaitingGimmick?.Cancel();
            _currentWaitingGimmick = null;
            _currentWaitingData = null;

            Debug.Log($"[BattleFlow] ボスがダウンしました！ ({_downDuration}秒間)");

            //if(_downPresentationController != null && _currentPhaseData?.DownPresentationData != null)
            //{
            //    StartCoroutine(_downPresentationController.PlayPresentation(_currentPhaseData.DownPresentationData));
            //}
            if (_bossAnimator != null && !string.IsNullOrEmpty(_downAnimTrigger))
            {
                _bossAnimator.SetTrigger(_downAnimTrigger);
            }

            OnDownStart?.Invoke();
        }


        private void EndDown()
        {
            ChangeState(BossBattleFlowState.InBattle);
            
            Debug.Log("[BattleFlow] ボスがダウンから復帰しました");

            if(_bossAnimator != null && !string.IsNullOrEmpty(_recoverAnimTrigger))
            {
                _bossAnimator.SetTrigger(_recoverAnimTrigger);
            }

            OnDownEnd?.Invoke();
        }


        /*
         * 
         * Gimmick
         * 
         */

        private void EvaluateIntervalGimmicks()
        {
            for (int i = 0; i < _gimmickSlots.Count; ++i)
            {
                var slot = _gimmickSlots[i];
                if (slot.data == null || slot.data.ExecutionType != GimmickExecutionType.interval) continue;

                if(_battleTimer >= slot.nextExecuteTime)
                {
                    ExecuteSlot(slot);
                    slot.nextExecuteTime = _battleTimer + slot.data.GetNextInterval();

                    if (_currentWaitingGimmick != null) break;
                }
            }
        }

        private void EvaluateTimelineGimmicks()
        {
            for(int i = 0;i < _gimmickSlots.Count;++i)
            {
                var slot = _gimmickSlots[i];
                if(slot.data == null || slot.data.ExecutionType != GimmickExecutionType.Timeline) continue;

                if(slot.runtimeTimelineQueue != null && slot.runtimeTimelineQueue.Count > 0)
                {
                    float nextTriggerTime = slot.runtimeTimelineQueue.Peek();
                    if(_battleTimer >= nextTriggerTime)
                    {
                        slot.runtimeTimelineQueue.Dequeue();
                        ExecuteSlot(slot);

                        if (_currentWaitingGimmick != null) break;
                    }
                }
            }
        }

        /*
         * 
         * 割り込み処理
         * 
         */

        public void EnqueueInterruptGimmick(BossGimmickData gimmickData)
        {
            if(gimmickData == null) return;

            GimmickSlot targetSlot = _gimmickSlots.Find(s => s.data == gimmickData);

            if (targetSlot.data != null)
            {
                if(!_interruptQueue.Contains(targetSlot))
                {
                    _interruptQueue.Enqueue(targetSlot);
                }
            }
        }

        public void ExecuteSlot(GimmickSlot slot)
        {
            if (IsDown) return;

            if (slot == null || slot.runtimeGimmick == null) return;

            if(slot.data != null &&
                !string.IsNullOrEmpty(slot.data.animTriggerName) &&
                _bossAnimator != null)
            {
                _bossAnimator.SetTrigger(slot.data.animTriggerName);
            }

            slot.runtimeGimmick.Execute();

            if(slot.data != null && slot.data.waitForCompletion)
            {
                _currentWaitingGimmick = slot.runtimeGimmick;
                _currentWaitingData = slot.data;
                _waitingTimeoutTimer = 0.0f;
            }
        }

        public void StopBattle()
        {
            if(_stateRoutine != null)
            {
                StopCoroutine(_stateRoutine);
                _stateRoutine = null;
            }

            _isBattleActive = false;
            EventBus.Publish(new EnemyDefeatedEvent(_bossInstanceId));
            OnBossBattleCompleted?.Invoke(_bossInstanceId);
        }

        private void ChangeState(BossBattleFlowState newState)
        {
            if(_currentState == newState) return;

            BossBattleFlowState previousState = _currentState;
            _currentState = newState;
            OnStateChanged?.Invoke(previousState, newState);
        }
    }

}
