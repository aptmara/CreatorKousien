using UnityEngine;

[CreateAssetMenu(fileName = "MoneyData", menuName = "Scriptable Objects/MoneyData")]
public class MoneyData : ScriptableObject
{
    // 所持金
    [SerializeField]
    private int _initMoeny = 100;

    public int moneyOnHand;

    [SerializeField,Range(0.0f,1.0f),Tooltip("コンボボーナスでかかる割合")]
    private float _moneyMagni;

    void Initialize()
    {
        moneyOnHand = _initMoeny;
    }

    void AddMoney(int Combo)
    {
        int addValue = 0;
        addValue += (int)((float)Combo * (1.0f + (float)Combo / 10.0f * _moneyMagni));
        moneyOnHand+= addValue;
        Debug.Log("Money On Hand : " + moneyOnHand);
    }

    void SubtractMoney(int subVal)
    {
        moneyOnHand -= subVal;
        Debug.Log("Money On Hand : " + moneyOnHand);
    }
}
