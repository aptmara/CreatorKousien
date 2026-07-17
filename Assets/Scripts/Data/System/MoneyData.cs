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

    public void Initialize()
    {
        moneyOnHand = _initMoeny;
    }

    public void AddMoney(int Combo)
    {
        if (Combo <= 0) return;

        // 素のコンボ報酬
        float basePayout = Combo;
        // コンボボーナス分
        float bonusPayout = Combo * _moneyMagni;
        int addValue = Mathf.RoundToInt(basePayout + bonusPayout);

        // １ゴールドは保証
        if (addValue <= 0) addValue = 1;

        moneyOnHand += addValue;

        Debug.Log($"[MoneyData] 💰 お金が加算されました 💰\n" +
                  $" - 精算グローバルコンボ数: {Combo}\n" +
                  $" - 基本報酬: {basePayout} / ボーナス内訳: {bonusPayout:F2} (倍率: {_moneyMagni * 100}%)\n" +
                  $" - 四捨五入後合計加算額: {addValue}\n" +
                  $" - 現在の総所持金 (moneyOnHand): {moneyOnHand}");
    }

    public void SubtractMoney(int subVal)
    {
        moneyOnHand -= subVal;
        Debug.Log($"[MoneyData] 💸 お金が消費されました。消費額: {subVal} / 残高: {moneyOnHand}");
    }
}
