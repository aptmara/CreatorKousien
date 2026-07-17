using UnityEngine;

[CreateAssetMenu(fileName = "MoneyData", menuName = "Scriptable Objects/MoneyData")]
public class MoneyData : ScriptableObject
{
    // 所持金
    [SerializeField]
    private int _initMoeny = 100;

    public int moneyOnHand;

    [SerializeField,Range(0.0f,1.0f),Tooltip("コンボボーナスがかかる割合")]
    private float _moneyMagni;

    [SerializeField, Tooltip("コンボ数に応じて倍率が上昇する際の伸びの勢い")]
    private float _inflationGrowthCoefficient = 0.04f;

    [SerializeField, Range(0.0f, 3.0f), Tooltip("インフレし過ぎを防ぐための最大ボーナス倍率の上限")]
    private float _maxMoneyMagniLimit = 1.0f;

    public void Initialize()
    {
        moneyOnHand = _initMoeny;
    }

    public void AddMoney(int Combo)
    {
        if (Combo <= 0) return;

        // 素のコンボ報酬
        float basePayout = Combo;

        // 平方根カーブを用いたインフレ倍率
        float dynamicMagni = _moneyMagni + (Mathf.Sqrt(Combo) * _inflationGrowthCoefficient);

        // 倍率制限を適用
        if (dynamicMagni > _maxMoneyMagniLimit)
        {
            dynamicMagni = _maxMoneyMagniLimit;
        }

        // コンボボーナス分
        float bonusPayout = Combo * dynamicMagni;
        int addValue = Mathf.RoundToInt(basePayout + bonusPayout);

        // １ゴールドは保証
        if (addValue <= 0) addValue = 1;

        moneyOnHand += addValue;

        Debug.Log($"[MoneyData] 💰 コンボ精算：累進インフレ報酬が加算されました 💰\n" +
                  $" - 確定グローバルコンボ数: {Combo}\n" +
                  $" - 適用された動的ボーナス倍率: {dynamicMagni * 100:F1}% (初期基準: {_moneyMagni * 100}%, 上限値: {_maxMoneyMagniLimit * 100}%)\n" +
                  $" - 報酬内訳 -> 基本額: {basePayout} / インフレボーナス額: {bonusPayout:F2}\n" +
                  $" - 四捨五入後合計加算額: {addValue}\n" +
                  $" - 現在の総所持金 (moneyOnHand): {moneyOnHand}");
    }

    public void SubtractMoney(int subVal)
    {
        moneyOnHand -= subVal;
        Debug.Log($"[MoneyData] 💸 お金が消費されました。消費額: {subVal} / 残高: {moneyOnHand}");
    }
}
