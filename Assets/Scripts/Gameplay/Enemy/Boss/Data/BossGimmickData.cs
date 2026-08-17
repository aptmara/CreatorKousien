using UnityEngine;

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
    public float triggerTime = 10.0f;

    [Header("==== シーケンス進行制御 ====")]
    [Tooltip("Onの場合子のギミックが完了後するまで次のギミックに移行しない")]
    public bool waitForCompletion = true;

    [Tooltip("完了を待つ場合のタイムアウト時間(0で無制限)")]
    public float timeoutDuration = 15.0f;

    /// <summary>
    /// 次のインターバルの時間を返す
    /// </summary>
    /// <returns></returns>
    public float GetNextInterval()
    {
        if(isRandomInterval)
        {
            return Random.Range(minInterval, maxInterval);
        }
        return minInterval;
    }
}

[System.Serializable]
public struct GimmickSlot
{
    [Tooltip("ギミックのパラメータ・条件データ")]
    public BossGimmickData data;

    [Tooltip("IBossGimmick コンポーネントを持ったアタッチ用ゲームオブジェクト")]
    public GameObject gimmickTarget;

    [HideInInspector] public float nextExecuteTime;
    [HideInInspector] public bool isExecutedInTimeline;
}
