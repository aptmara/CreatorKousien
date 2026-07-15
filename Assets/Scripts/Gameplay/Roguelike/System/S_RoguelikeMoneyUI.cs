//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : S_RoguelikeMoneyUI.cs
// brief  : ローグライクシーンでのお金表示
//          お金の実データは保持しない
//
// auther : Takitani Shohei
// date   : 2026/07/14 - begin.
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using TMPro;
using UnityEngine;

public class S_RoguelikeMoneyUI : MonoBehaviour
{
    [Header("UIで表示するデータ")]
    [Tooltip("生成する実データ")]
    [SerializeField] private GameObject _moneyPrefab;
    [Tooltip("生成先の親")]
    [SerializeField] private Transform _spawnParent;

    private TextMeshProUGUI _text;
    private GameObject _spawnedInstance;


    //____________________________________
    // public funtion

    /// <summary>
    /// UIを丸ごと生成
    /// </summary>
    /// <param name="money">所持金</param>
    public void SpawnMoneyUI(int money)
    {
        if(_spawnedInstance != null)
        {
            // 既に生成済みであれば処理を更新するのみ
            ChangeMoneyUI(money);
            return;
        }

        if(_moneyPrefab == null || _spawnParent == null)
        {
            Debug.LogError("[S_RoguelikeMoneyUI] Prefabまたは生成先が未設定です");
            return;
        }

        _spawnedInstance = Instantiate(_moneyPrefab, _spawnParent);
        _text = _spawnedInstance.GetComponentInChildren<TextMeshProUGUI>();

        if(_text == null)
        {
            Debug.LogError("[S_RoguelikeMoneyUI] Prefab内にTextMeshProUGUIが見つかりませんでした");
            return;
        }

        ChangeMoneyUI(money);
    }

    /// <summary>
    /// 表示する残金を更新
    /// 強化選択後に呼び出し
    /// </summary>
    /// <param name="money">変更後のお金</param>
    public void ChangeMoneyUI(int money)
    {
        if(_text == null)
        {
            Debug.LogWarning("[S_RoguelikeMoneyUI] まだSpawnMoneyUIが呼ばれていません");
            return;
        }

        _text.text = money.ToString();
    }


}
