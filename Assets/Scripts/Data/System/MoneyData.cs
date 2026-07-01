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
        int addValue = 0;
        addValue += (int)(Combo + (Combo * _moneyMagni));
        moneyOnHand+= addValue;
        Debug.Log("Money On Hand : " + moneyOnHand +
            "\nComboVal : " + Combo +
            "\nAddVal : " + (Combo * _moneyMagni));
    }

    public void SubtractMoney(int subVal)
    {
        moneyOnHand -= subVal;
        Debug.Log("Money On Hand : " + moneyOnHand);
    }
}
