//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// @file   HpGaugeView.cs
// @brief  HPゲージの更新描画。分ける必要性を感じなかったのでUpdate内でデータ更新
// @author 山本郁也
// @date   2026/04/15
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using UnityEngine;
using UnityEngine.UI;

public class HpGaugeView : MonoBehaviour
{
    [SerializeField] private Image hpGauge;

    [SerializeField] public float currentHp;
    [SerializeField] public float MaxHp = 100.0f;
    private float oldHp;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        oldHp = currentHp = MaxHp;
    }

    // Update is called once per frame
    void Update()
    {
        if(currentHp != oldHp)
        {
            currentHp = currentHp >= MaxHp ? MaxHp : currentHp <= 0.0f ? 0 : currentHp;
            currentHp = Mathf.Clamp(currentHp, 0, MaxHp);
            oldHp = currentHp;
            UpdateHPBar(currentHp,MaxHp);
        }
    }
    public void UpdateHPBar(float currentHP, float maxHP)
    {
        // 0.0f ~ 1.0f の値に変換
        float fillValue = currentHP / maxHP;

        // Imageの表示を更新
        hpGauge.fillAmount = fillValue;
    }
}
