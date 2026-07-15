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


    private int _currentIndex = 0;
    private float _cooldownTimer = 0.0f;
    private bool _isNavigateReleased = true;

    private void OnEnable()
    {
        _currentIndex = 0;
        _selectionUI.SetFocusIndex(_currentIndex);
    }


    private void Update()
    {
        HandleNavigate();
        HandleSubmit();
        HandleExit();

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
            _cooldownTimer -= Time.unscaledDeltaTime;
        }


        // ニュートラルになったら次の入力を受付
        if(nav.sqrMagnitude < _navigateDeadzone * _navigateDeadzone)
        {
            _isNavigateReleased = true;
            return;
        }


        if (!_isNavigateReleased || _cooldownTimer > 0.0f) return;

        // 入力操作
        int columnCount = (int)Mathf.Max(1, _selectionUI.ColumnCount);
        int cardCount = _selectionUI.CardCount;
        if (cardCount == 0) return;

        int col = _currentIndex % columnCount;
        int row = _currentIndex / columnCount;
        int rowCount = Mathf.CeilToInt((float)cardCount / columnCount);

        bool horizontalDominant = Mathf.Abs(nav.x) >= Mathf.Abs(nav.y);

        if (horizontalDominant)
        {
            int dir = nav.x > 0 ? 1 : -1;
            col = (col + dir + columnCount) % columnCount;
        }
        else
        {
            // 上入力で前の行、下入力で次の行へ
            int dir = nav.y > 0 ? -1 : 1;
            row = (row + dir + rowCount) % rowCount;
        }

        int newIndex = row * columnCount + col;

        // 最終行が9未満で欠けている場合は異動させない
        if(newIndex < cardCount)
        {
            _currentIndex = newIndex;
            _selectionUI.SetFocusIndex(_currentIndex);
        }


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

    private void HandleExit()
    {
        if(_input.ConsumeExit())
        {
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
