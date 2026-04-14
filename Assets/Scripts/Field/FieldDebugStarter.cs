// ------------------------------------------------------------
// File		: FieldDebugStarter.cs
// Summary	: フィールド関連のクラスの動作確認用のスクリプト
//
// Author	: [浅野勇生]
// Created	: 2026-04-13
//
// Notes	:
// - デバック用に作成しマスタ。GameManager実装後は削除予定
// ------------------------------------------------------------
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// デバック用に作成した、フィールド関連のクラスの動作確認用のスクリプト
/// </summary>
public class FieldDebugStarter : MonoBehaviour
{
    [Header("フィールド情報")]
    [Tooltip("読み込むステージデータ")]
    [SerializeField] private StageData _debugStageData; // 読み込むステージデータ

    [Tooltip("盤面を描画するViewコンポーネント")]
    [SerializeField] private FieldView _fieldView;      // 盤面を描画するViewコンポーネント


    [Header("プレイヤー情報")]
    [Tooltip("プレイヤーの情報")]
    [SerializeField] private PlayerData _debugPlayerData;    // プレイヤーの情報

    [Tooltip("プレイヤーのViewコンポーネント")]
    [SerializeField] private PlayerView _playerView;      // プレイヤーのViewコンポーネント

    // テスト用のFieldServiceインスタンス
    private FieldService _fieldService;                     // テスト用のFieldServiceインスタンス
    private TileEffectSystem _tileEffectSystem;             // テスト用のTileEffectSystemインスタンス
    private PlayerSystem _playerSystem;                     // テスト用のPlayerSystemインスタンス


    /// <summary>
    /// 初期化処理。ステージデータを読み込み、FieldServiceを初期化し、FieldViewに盤面を描画させる
    /// </summary>
    void Start()
    {
        if (_debugStageData == null || _fieldView == null)
        {
            Debug.LogError("[FieldDebug] ステージデータまたはFieldViewがアサインされていません!");
            return;
        }

        Debug.Log("[FieldDebug]盤面の初期化を開始します...");

        // ----- 1. FieldServiceの初期化 -----
        _fieldService = new FieldService();
        _fieldService.Initialize(_debugStageData);
        _tileEffectSystem = new TileEffectSystem(_fieldService.State);

        // ----- 2. FieldViewに盤面を描画させる -----
        _fieldView.BuildView(_fieldService.State, _debugStageData.CellSize);


        // ----- 3. PlayerSystemの初期化とPlayerViewへの反映 -----
        if (_debugPlayerData != null && _playerView != null)
        {
            Vector2Int startPos = _debugStageData.PlayerStartPosition;

            _playerSystem = new PlayerSystem();
            _playerSystem.Initialize(_debugPlayerData, startPos);

            // FieldでActorが動いたらPlayerSystemの位置を更新する
            _fieldService.OnActorMoved += (actorId, x, y) =>
            {
                if (actorId == _playerSystem.RuntimeData.ActorId)
                {
                    _playerSystem.SyncPosition(new Vector2Int(x, y));
                }
            };

            // PlayerSystemの座標が変わったらPlayerViewに反映する
            float cellSize = _debugStageData.CellSize; // セルのサイズ
            _playerSystem.OnPositionChanged += (gridPos) =>
            {
               _playerView.UpdateTargetPosition(gridPos, cellSize);
            };

            // PlayerSystemが死亡判定を受けたらPlayerViewに反映する
            _playerSystem.OnDeathEvent += () =>
            {
                _playerView.OnDeath();
            };

            // ----- 4. PlayerViewの初期位置をセットアップ -----
            _playerView.Initialize(startPos, cellSize);
            _fieldService.UpdateOccupancy(_playerSystem.RuntimeData.ActorId, -1, -1, startPos.x, startPos.y);
            _tileEffectSystem.TriggerOnEnter(_playerSystem.RuntimeData.ActorId, startPos.x, startPos.y);
        }
        else
        {
            Debug.LogWarning("[FieldDebug] プレイヤーデータまたはPlayerViewがアサインされていません。Playerの初期化をスキップします。");
        }

        Debug.Log("[FieldDebug]盤面の初期化が完了しました!");
    }


    private void Update()
    {
        if (_playerSystem == null)
            return;

        // EffectManagerの代わりとして、キーボード入力を受け付ける(デバッグ用)
        if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame) TryMoveActor(0, -1);
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame) TryMoveActor(0, 1);
        else if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame) TryMoveActor(-1, 0);
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame) TryMoveActor(1, 0);

        // スペースキーでターン終了（床効果の評価テスト）
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("<color=cyan>--- ターン終了 ---</color>");
            var pos = _playerSystem.RuntimeData.Position;
            _tileEffectSystem.TriggerOnTurnEnd(_playerSystem.RuntimeData.ActorId, pos.x, pos.y);
        }
    }

    /// <summary>
    /// EffectManagerからの移動リクエストを受け取るためのメソッド。FieldServiceに移動リクエストを送り、成功したら床効果の呼び出しも行う
    /// </summary>
    /// <param name="dx"></param>
    /// <param name="dy"></param>
    private void TryMoveActor(int dx, int dy)
    {
        var pos = _playerSystem.RuntimeData.Position;
        int actorId = _playerSystem.RuntimeData.ActorId;

        // ----- 1. FieldServiceに移動リクエストを送る -----
        bool isSuccess = _fieldService.TryMoveActor(actorId, pos.x, pos.y, dx, dy);

        if (isSuccess)
        {
            // ----- 2. 移動が成功したら床効果の呼び出し -----
            _tileEffectSystem.TriggerOnExit(actorId, pos.x, pos.y);
            _tileEffectSystem.TriggerOnEnter(actorId, pos.x + dx, pos.y + dy);
        }
        else
        {
            Debug.Log("<color=yellow>[FieldDebug] 移動に失敗しました。移動先が通行不可か、盤面外の可能性があります。</color>");
        }
    }
}
