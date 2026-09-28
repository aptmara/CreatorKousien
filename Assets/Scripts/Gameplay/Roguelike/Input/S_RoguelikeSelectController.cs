//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : S_RoguelikeSelectController.cs
// brief  : Roguelikeショップ画面の初期選択制御
//
// auther : Takitani Shohei
// date   : 2026/09/27 - re
//_/_/_/_/_/_/_/_/_/_/_/
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;



public class S_RoguelikeSelectController : MonoBehaviour
{
    [Header("InputActionの取得")]
    [SerializeField] private S_RoguelikeSelectInput _input;

    [Header("カード一覧の取得元")]
    [SerializeField] private S_ShopMenuUI _shopMenu;

    // trueならマウス、falseならキーボード／コントローラを優先する
    public static bool IsPointerMode { get; private set; }

    private void OnEnable()
    {
        IsPointerMode = false;
    }

    private void Update()
    {
        DetectInputMode();
        EnsureNavigationSelection();
    }

    /// <summary>
    /// 最後に操作された入力機器に表示フォーカスを切り替える
    /// </summary>
    private void DetectInputMode()
    {
        bool pointerInput =
            Mouse.current != null
            && (Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f
                || Mouse.current.leftButton.wasPressedThisFrame);

        if (pointerInput)
        {
            IsPointerMode = true;
            EventSystem.current?.SetSelectedGameObject(null);
        }

        // Navigateはキーボード十字キー／WASD／スティック／D-padを含む
        bool navigationInput =
            _input != null
            && _input.Navigate.sqrMagnitude > 0.01f;

        // 独自Submit Actionはモード切替の検出だけに使う。
        // Buttonの決定処理はEventSystemが一度だけ行う。
        bool submitInput = _input != null && _input.ConsumeSubmit();

        if (navigationInput || submitInput)
            IsPointerMode = false;
    }

    /// <summary>
    /// ナビゲーション操作へ切り替わった際、選択対象がなければ最初のカードを選ぶ
    /// </summary>
    private void EnsureNavigationSelection()
    {
        if (IsPointerMode || EventSystem.current == null || _shopMenu == null)
            return;

        if (EventSystem.current.currentSelectedGameObject != null)
            return;

        if (_shopMenu.SpawnedCards.Count == 0)
            return;

        S_UpgradeCard firstCard = _shopMenu.SpawnedCards[0];
        if (firstCard == null || firstCard.Selectable == null)
            return;

        EventSystem.current.SetSelectedGameObject(firstCard.Selectable.gameObject);
    }
}
