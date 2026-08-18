//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : S_RoguelikeResultController.cs
// brief  : シーンで選ばれた強化をSO_UpgradeRuntimeStateに記録
//          同時に、PlayerFacade.ApplyUpgrade()で実ステータスへ即時反映する
//
// auther : Shohei Takitani
// date   : 2026/06/30 - begin.
//          2026/07/02 - GameProgressionManagerに合わせる
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using UnityEngine;
using Game.Gameplay.Player;
using Game.Data.Player;
using Game.Core.Roguelike;
using Game.Gameplay.Roguelike.Effects;
using Game.Data.Collectibles;


public class S_RoguelikeResultController : MonoBehaviour
{
    //____________________________________
    // variables

    [Header("Runtime State")]
    [Tooltip("取得済み強化とレベルを保存するSO")]
    [SerializeField] private SO_UpgradeRuntimeState _upgradeRuntimeState;

    [Header("Player")]
    [Tooltip("実ステータス適応先、シーン内のPlayerFacadeを指定")]
    [SerializeField] private PlayerFacade _playerFacade;

    [Header("Event")]
    [Tooltip("ローグライクシーン終了イベント")]
    [SerializeField] private SO_RoguelikeEndEvent _roguelikeEndEvent;

    [Tooltip("終了時呼び出し")]
    [SerializeField] private S_CameraSetUp _cameraSetUp;



    //____________________________________
    // basic functions

    private void Awake()
    {
        if (_playerFacade == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            _playerFacade = player?.GetComponent<PlayerFacade>();
            if (_playerFacade == null)
            {
                Debug.LogError("[S_RoguelikeResultController] PlayerFacadeが見つかりません。");
            }
        }
    }



    //____________________________________
    // public functions

    /// <summary>
    /// 選択された強化を取得済みリストに反映する関数
    /// </summary>
    /// <param name="selectedCard">選択された強化データ</param>
    public bool SelectUpgrade(
        UpgradeData selectedCard,
        CollectibleData focusedCollectible = null,
        int levelGain = 1)
    {
        if(selectedCard == null)
        {
            Debug.LogWarning("[S_RoguelikeResultController] 選択された強化がnullです。");
            return false;
        }

        bool hasPlayerModifiers = selectedCard.Modifiers != null && selectedCard.Modifiers.Length > 0;
        if(hasPlayerModifiers && _playerFacade == null)
        {
            Debug.LogError("[S_RoguelikeResultController] PlayerFacadeが未設定です。");
            return false;
        }

        int appliedLevels = _upgradeRuntimeState.AddLevels(selectedCard, Mathf.Max(1, levelGain));
        if (appliedLevels <= 0)
            return false;

        int level = _upgradeRuntimeState.GetLevel(selectedCard);

        if (hasPlayerModifiers)
        {
            for (int index = 0; index < appliedLevels; index++)
                _playerFacade.ApplyUpgrade(selectedCard);
        }
        RoguelikeUpgradeRuntime.Apply(selectedCard.Id, level, selectedCard.GameplayValue);
        RoguelikeEffectRuntime.Register(selectedCard, level);

        if (selectedCard.OfferType == UpgradeOfferType.CombatPressureRule)
        {
            int outputType = (int)selectedCard.CombatPressureOutputType;
            RoguelikeBuildRuntime.SetCombatRule(
                selectedCard.CombatPressureRuleId,
                level,
                outputType);
            RoguelikeUpgradeRuntime.UnlockCollectible(outputType);
        }
        return true;
    }

    /// <summary>
    /// ローグライクシーンを終了する
    /// </summary>
    public void FinishRoguelikeScene()
    {
        _cameraSetUp?.SceneEnd();

        if(_roguelikeEndEvent != null)
        {
            _roguelikeEndEvent.Raise();
        }
        // ゲーム進行マネージャーにローグライク終了を通知
        if (Game.Core.Management.GameProgressionManager.Instance != null)
        {
            Game.Core.Management.GameProgressionManager.Instance.CompleteRoguelikeSequence();
        }
        else
        {
            Debug.LogError("[S_RoguelikeResultController] GameProgressionManager.Instanceが見つかりません。");
        }

    }
}
