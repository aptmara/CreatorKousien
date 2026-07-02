//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : S_PlayerUpgradeResolver.cs
// brief  : ローグライク終了イベントを受け取り、SO_UpgradeRuntimeStateを呼んで
//          UpgradeApplyContext経由で全教科を適応する
//
// auther : Shohei Takitani
// date   : 2026/06/30 - begin.
//          2026/07/01 - PlayerRuntimeDataベースの構成に合わせて実装
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using UnityEngine;
using Game.Gameplay.Player.Progression;
using Game.Gameplay.Combo;

public class S_PlayerUpgradeResolver : MonoBehaviour
{
    [Header("Event")]
    [Tooltip("ローグライクシーン終了イベント")]
    [SerializeField] private SO_RoguelikeEndEvent _roguelikeEndEvent;

    [Header("Runtime State")]
    [Tooltip("取得済み強化とレベルを保存しているSO")]
    [SerializeField] private SO_UpgradeRuntimeState _upgradeRuntimeState;

    // 強化項目
    private PlayerRuntimeData _runtimeData;
    private ComboManager _comboManager;

    private UpgradeApplyContext _context;

    /// <summary>
    /// プレイヤーのランタイムデータを設定する関数
    /// (PlayerMoter.SetRuntimeDataと同じタイミングで呼ばれる想定)
    /// </summary>
    /// <param name="runtimedData">プレイヤーのランタイムデータ</param>
    public void SetRuntimeData(PlayerRuntimeData runtimedData)
    {
        _runtimeData = runtimedData;
        _context = new UpgradeApplyContext(_runtimeData);
    }

    private void OnEnable()
    {
        if (_roguelikeEndEvent != null)
        {
            _roguelikeEndEvent.OnRaised += ApplyAllUpgrades;
        }
    }

    private void OnDisable()
    {
        if(_roguelikeEndEvent != null)
        {
            _roguelikeEndEvent.OnRaised += ApplyAllUpgrades;
        }
    }


    private void ApplyAllUpgrades()
    {
        if (_context == null)
        {
            Debug.LogWarning("[S_PlayerUpgradeResolver] RuntimeDataが未設定のため強化を適用できません。");
            return;
        }

        foreach (var entry in _upgradeRuntimeState.Entries)
        {
            if (entry.CardData == null) continue;
            if (entry.Level <= 0) continue;

        }
    }

}
