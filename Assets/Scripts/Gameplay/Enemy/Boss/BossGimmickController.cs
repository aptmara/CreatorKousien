/*
 * 寺田
 * ボスの動作のフローを管理する
 * 
 * 
 */

using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Multiplayer.Center.Common;

/// <summary>
/// 各ギミック処理
/// </summary>
public interface IBossGimmick
{
    /// <summary>
    /// ギミック実行
    /// </summary>
    void Execute();

    bool IsComplete { get; }

    void Cancel();
}


public class BossGimmickController : MonoBehaviour
{
    [Header("==== 共通設定 =====")]
    [SerializeField] private Animator _bossAnimator;

    [Header("==== ダウンシステムの設定 ====")]
    [SerializeField] private bool _useDownSystem = true;
    [SerializeField] private float _downDuration = 5.0f;
    [SerializeField] private string _downAnimTrigger = "Down";
    [SerializeField] private string _recoverAnimTrigger = "Recover";

    [Header("==== ギミック登録リスト ====")]
    [SerializeField] private List<GimmickSlot> _gimmickSlots = new List<GimmickSlot>();
    public List<GimmickSlot> GimmickSlots => _gimmickSlots;

    private float _battleTimer = 0.0f;
    private bool _isBattleActive = false;

    public bool IsDown { get; private set; } = false;
    private float _downTimer = 0.0f;

    private IBossGimmick _currentWaitingGimmick = null;
    private float _waitingTimeoutTimer = 0.0f;

    public event Action OnDownStart;
    public event Action OnDownEnd;

    private void Start()
    {
        StartBattle();
    }

    public void StartBattle()
    {
        _battleTimer = 0.0f;
        _isBattleActive = true;
        IsDown = false;
        _currentWaitingGimmick = null;

        for (int i = 0; i < _gimmickSlots.Count; ++i)
        {
            var slot = _gimmickSlots[i];
            if (slot.data == null) continue;

            if(slot.data.ExecutionType == GimmickExecutionType.interval)
            {
                slot.nextExecuteTime = Time.time + slot.data.GetNextInterval();
            }
            else
            {
                slot.isExecutedInTimeline = false;
            }

            _gimmickSlots[i] = slot;
        }

    }

    private void Update()
    {
        if (!_isBattleActive) return;

        if(_useDownSystem &&IsDown)
        {
            _downTimer -= Time.deltaTime;
            if (_downTimer <= 0.0f)
            {
                EndDown();
            }
            return;
        }

        bool iswaiting = false;
        if(_currentWaitingGimmick != null)
        {
            _waitingTimeoutTimer += Time.deltaTime;

            if(_currentWaitingGimmick.IsComplete || _waitingTimeoutTimer >= 15.0f)
            {
                _currentWaitingGimmick = null;
            }
            else
            {
                iswaiting = true;
            }
        }

        if(!iswaiting)
        {
            _battleTimer += Time.deltaTime;
        }
        EvaluateGimmicks();
    }

    /*
     * 
     * DownSystem
     * 
     */
    public void TriggerDown()
    {
        if (!_useDownSystem || IsDown) return;

        IsDown = true;
        _downTimer = _downDuration;

        if(_currentWaitingGimmick != null)
        {
            _currentWaitingGimmick.Cancel();
            _currentWaitingGimmick = null;
        }

        Debug.Log($"[BattleFlow] ボスがダウンしました！ ({_downDuration}秒間)");

        if (_bossAnimator != null && !string.IsNullOrEmpty(_downAnimTrigger))
        {
            _bossAnimator.SetTrigger(_downAnimTrigger);
        }

        OnDownStart?.Invoke();
    }


    private void EndDown()
    {
        IsDown = false;
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


    private void EvaluateGimmicks()
    {
        for(int i = 0;i < _gimmickSlots.Count;++i)
        {
            var slot = _gimmickSlots[i];
            if(slot.data == null) continue;

            switch (slot.data.ExecutionType)
            {
                case GimmickExecutionType.interval:
                    if(Time.time >= slot.nextExecuteTime)
                    {
                        ExecuteGimmick(slot);
                        slot.nextExecuteTime = Time.time + slot.data.GetNextInterval();
                        _gimmickSlots[i] = slot;
                    }
                    break;

                case GimmickExecutionType.Timeline:
                    if (!slot.isExecutedInTimeline && _battleTimer >= slot.data.triggerTime)
                    {
                        ExecuteGimmick(slot);
                        slot.isExecutedInTimeline = true;
                        _gimmickSlots[i] = slot;

                        if(slot.data.waitForCompletion && slot.gimmickTarget != null)
                        {
                            _currentWaitingGimmick = slot.gimmickTarget.GetComponent<IBossGimmick>();
                            _waitingTimeoutTimer = 0.0f;
                        }
                    }
                    break;

            }

        }
    }

    public void ExecuteGimmick(GimmickSlot slot)
    {
        if (IsDown) return;

        if(!string.IsNullOrEmpty(slot.data.animTriggerName) && _bossAnimator != null)
        {
            _bossAnimator.SetTrigger(slot.data.animTriggerName);
        }

        if(slot.gimmickTarget != null)
        {
            var gimmick = slot.gimmickTarget.GetComponent<IBossGimmick>();

            gimmick?.Execute();
        }
    }

    public void StopBattle()
    {
        _isBattleActive = false;
    }
}
