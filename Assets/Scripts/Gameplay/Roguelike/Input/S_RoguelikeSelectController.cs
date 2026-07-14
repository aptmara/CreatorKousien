//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : S_RoguelikeSelectionController.cs
// brief  : コントローラ操作の実装
//
// auther : Takitani Shohei
// date   : 2026/07/12 - begin.
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using UnityEngine;

public class S_RoguelikeSelectController : MonoBehaviour
{
    [Header("InputActionの取得")]
    [SerializeField] private S_RoguelikeSelectInput _input;

    [Header("選択画面への参照")]
    [SerializeField] private S_UpgradeSelectionUI _selectionUI;

    [Header("Navigate感度設定")]
    [Tooltip("この値を超えたら移動")]
    [SerializeField] private float _navigateDeadzone = 0.5f;
    [Tooltip("移動後、入力受付までの待機時間(秒)")]
    [SerializeField] private float _navigateCooldown = 0.2f;

    [Header("Exit長押し設定")]
    [SerializeField] private float _exitHoldDuration = 1.5f;

    private int _currentIndex = 0;
    private float _cooldownTimer = 0.0f;
    private bool _isNavigateReleased = true;
    private bool _exitTriggered = false;        // 重複防止

    private void OnEnable()
    {
        _currentIndex = 0;
        _selectionUI.SetFocusIndex(_currentIndex);
    }


    private void Update()
    {
        Debug.Log($"[S_RoguelikeController] Navigate={_input.Navigate}, CardCount={_selectionUI.CardCount}, CurrentIndex={_currentIndex}");

        HandleNavigate();
        HandleSubmit();
        HandleExitHold();

        // 二重処理の防止
        // 入力を消費させる
        _input.ConsumeClick();
    }

    /// <summary>
    /// スティック/十字キーの入力を「一回分の移動」に変換
    /// (倒しっぱなしで連続移動しないようにクールダウン)
    /// </summary>
    private void HandleNavigate()
    {
        Vector2 nav = _input.Navigate;

        if(_cooldownTimer > 0.0f)
        {
//            _cooldownTimer -= Time.deltaTime;   // deltaTimeが0なので定数で減算します
            _cooldownTimer -= (1.0f / 60.0f);
        }


        // ニュートラルになったら次の入力を受付
        if(nav.sqrMagnitude < _navigateDeadzone * _navigateDeadzone)
        {
            _isNavigateReleased = true;
            return;
        }


        if (!_isNavigateReleased || _cooldownTimer > 0.0f) return;

        // 横並びの入力を優先する
        int direction = nav.y > 0.0f
            ? -1 : nav.y < 0.0f
                ? 1 : 0;
        if (direction == 0) return;


        MoveFocus(direction);

        _isNavigateReleased = false;
        _cooldownTimer = _navigateCooldown;
    }

    private void HandleSubmit()
    {
        if(_input.ConsumeSubmit())
        {
            _selectionUI.TriggerSelect(_currentIndex);
        }
    }

    private void HandleExitHold()
    {
        float held = _input.ExitHeldSeconds;

        if(held <= 0.0f)
        {
            // ボタンを離したらリセット
            _exitTriggered = false;
            return;
        }

        if (_exitTriggered) return;

//        Debug.Log($"[S_RoguelikeSelectController] held = '{held}'");
        if(held >= _exitHoldDuration)
        {
            _exitTriggered = true;
            _selectionUI.OnFinishButtonPressed();
        }

    }

    private void MoveFocus(int dir)
    {
        int count = _selectionUI.CardCount;
        if (count == 0)
        {
            return;
        }
        _currentIndex = (_currentIndex + dir + count) % count;
        Debug.Log($"[S_RoguelikeController] フォーカス移動: _currentIndex={_currentIndex}");
        _selectionUI.SetFocusIndex(_currentIndex);
    }

}
