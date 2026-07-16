//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : S_UpgradeDetail.cs
// brief  : 強化項目の詳細を描画
//
// auther : Takitani Shohei
// date   : 2026/07/15
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using Game.Data.Player;
using TMPro;
using UnityEngine;

public class S_UpgradeDetail : MonoBehaviour
{
    [Header("生成するプレハブ")]
    [SerializeField] private GameObject _detailPrefab;
    [Tooltip("生成先の親")]
    [SerializeField] private Transform _spawnParent;

    [Header("Prefab内のオブジェクト名(検索用)")]
    [SerializeField] private string _levelTextName = "LevelText";
    [SerializeField] private string _descriptionTextName = "DescriptionText";
    [SerializeField] private string _nameTextName = "NameText";
    [SerializeField] private string _costTextName = "CostText";


    [Header("強化データ")]
    [SerializeField] private SO_UpgradeRuntimeState _upgradeRuntimeState;


    private TextMeshProUGUI _levelText;
    private TextMeshProUGUI _descriptionText;
    private TextMeshProUGUI _nameText;
    private TextMeshProUGUI _costText;
    private GameObject _spawnedInstance;

    // 余りにも取得の処理がめんどいのでS_UpgrateSelectionUIから取得
    private int _currentCost;
    private int _currentLevel;


    public void SpawnDetail(UpgradeData upgrade)
    {
        if (_spawnedInstance != null)
        {
            ChangeDetail(upgrade);
            return;
        }

        if(_detailPrefab == null || _spawnParent == null)
        {
            Debug.LogError("[S_UpgradeDetail] Prefabまたは生成元が未設定です。");
            return;
        }

        _spawnedInstance = Instantiate(_detailPrefab, _spawnParent);

        foreach(var text in _spawnedInstance.GetComponentsInChildren<TextMeshProUGUI>())
        {
            if(text.gameObject.name == _levelTextName)
            {
                _levelText = text;
            }
            else if(text.gameObject.name == _descriptionTextName)
            {
                _descriptionText = text;
            }
            else if(text.gameObject.name == _nameTextName)
            {
                _nameText = text;
            }
            else if(text.gameObject.name == _costTextName)
            {
                _costText = text;
            }
        }

        if(_levelText == null || _descriptionText == null)
        {
            Debug.LogError($"[S_UpgradeDetail] Prefab内に'{_levelTextName}'、'{_descriptionTextName}'、'{_nameTextName}'または'{_costTextName}'の名前は存在していません");
        }

        ChangeDetail(upgrade);
    }

    /// <summary>
    /// 強化の詳細を描画する
    /// カード選択更新時に呼び出し
    /// </summary>
    /// <param name="upgrade"></param>
    public void ChangeDetail(UpgradeData upgrade)
    {
        if(upgrade == null)
        {
            Debug.LogWarning("[S_UpgradeDetail] upgradeのデータがnullです");
            return;
        }

        if(_descriptionText == null || _levelText == null ||
            _nameText == null || _costText == null)
        {
            Debug.LogWarning("[S_UpgradeDetail] まだSpawnDetailが呼ばれていません");
            return;
        }


        _descriptionText.text = upgrade.Description;
        _nameText.text = upgrade.DisplayName;

        int level = _upgradeRuntimeState.GetLevel(upgrade);
        int cost = upgrade.GetCost(_currentLevel);

        if (level == upgrade.MaxLevel)
        {
            _levelText.text = "Level : Max";
            _costText.text = "Cost : None";
        }
        else
        {
            _levelText.text = $"Level : {level} / {upgrade.MaxLevel}";
            _costText.text = $"Cost : {cost}";
        }

    }

    public void SetCost(int cost) => _currentCost = cost;

    public void SetCurrentLevel(int level) => _currentLevel = level;

}
