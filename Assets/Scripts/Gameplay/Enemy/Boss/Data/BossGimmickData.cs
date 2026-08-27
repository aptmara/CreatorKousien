using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Polybrush;

public enum GimmickExecutionType
{
    interval,
    Timeline,
}


[CreateAssetMenu(fileName = "BossGimmickData", menuName = "Scriptable Objects/BossGimmickData")]
public class BossGimmickData : ScriptableObject
{
    [Header("===== 基本情報 =====")]
    public string BossGimmickName = "NewGimmick";
    public Color editColor = Color.cyan;
    public GimmickExecutionType ExecutionType = GimmickExecutionType.interval;

    [Header("Animation")]
    public string animTriggerName;

    [Header("==== Settings Interval ====")]
    [Tooltip("クールタイムがMinMaxになります")]
    public bool isRandomInterval = false;

    [Tooltip("固定インターバル or ランダムの最小時間")]
    public float minInterval = 5.0f;

    [Tooltip("ランダムの最大時間")]
    public float maxInterval = 10.0f;


    [Header("==== タイムライン設定 ====")]
    [Tooltip("戦闘開始からの発動タイミング")]
    public List<float> triggerTimes = new List<float>();

    private Queue<float> _runtimeQueue = new Queue<float>();

    [Header("==== シーケンス進行制御 ====")]
    [Tooltip("Onの場合子のギミックが完了後するまで次のギミックに移行しない")]
    public bool waitForCompletion = true;

    [Tooltip("完了を待つ場合のタイムアウト時間(0で無制限)")]
    public float timeoutDuration = 15.0f;

    public void InitializeQueue()
    {
        _runtimeQueue.Clear();

        triggerTimes.Sort();

        foreach (var item in triggerTimes)
        {
            _runtimeQueue.Enqueue(item);
        }
    }

    public bool TryPeekNextItem(out float nextTime)
    {
        if(_runtimeQueue != null && _runtimeQueue.Count > 0)
        {
            nextTime = _runtimeQueue.Peek();
            return true;
        }
        nextTime = -1.0f;
        return false;
    }

    public float DequeueNextTime()
    {
        return _runtimeQueue.Dequeue();
    }

    public bool IsQueueEmpty => _runtimeQueue == null || _runtimeQueue.Count == 0;

    /// <summary>
    /// 次のインターバルの時間を返す
    /// </summary>
    /// <returns></returns>
    public float GetNextInterval()
    {
        if(isRandomInterval)
        {
            return UnityEngine.Random.Range(minInterval, maxInterval);
        }
        return minInterval;
    }
}

[System.Serializable]
public class GimmickSlot
{
    public BossGimmickData data;

    [Tooltip("実行するギミックのSOアセット")]
    public BossGimmickSO gimmick;

    [NonSerialized] public BossGimmickSO runtimeGimmick;
    [NonSerialized] public Queue<float> runtimeTimelineQueue;
    [NonSerialized] public float nextExecuteTime;

    public void InitializeSlot(float currentBattleTimer,BossContext context)
    {
        if(gimmick != null)
        {
            runtimeGimmick = UnityEngine.Object.Instantiate(gimmick);
            runtimeGimmick.Initialize(context);
        }

        if (data == null) return;

        runtimeTimelineQueue = new Queue<float>();
        if(data.ExecutionType == GimmickExecutionType.Timeline && data.triggerTimes != null)
        {
            var sortedTimes = new List<float>(data.triggerTimes);
            sortedTimes.Sort();
            foreach (var time in sortedTimes)
            {
                runtimeTimelineQueue.Enqueue(time);
            }
        }
        else if(data.ExecutionType == GimmickExecutionType.interval)
        {
            nextExecuteTime = currentBattleTimer + data.GetNextInterval();
        }
    }
}
