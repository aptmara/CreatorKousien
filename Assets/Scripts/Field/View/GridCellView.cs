// ------------------------------------------------------------
// File		: GridCellView1.cs
// Summary	: ここのマスの見た目を管理するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-14
//
// Notes	:
// - 攻撃予告用の関数を追加 (4/17)
// ------------------------------------------------------------
using UnityEngine;

public class GridCellView : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("モデルを生成して配置する親オブジェクト")]
    [SerializeField] private Transform _modelContainer;

    [Header("インタラクション用の変数")]
    [Tooltip("沈む深さ")]
    [SerializeField] private float _sinkDepth = -0.2f;      // 沈む深さ

    [Tooltip("沈む速度")]
    [SerializeField] private float _animSpeed = 10f;        // 沈む速度


    private GameObject _currentModel;                       // 現在表示しているモデル
    private bool _isOccupied;                               // キャラクターがいるかどうか
    private Vector3 _targetLocalPos;                        // キャラクターの有無に応じた目標位置
    private Renderer _renderer;                             // マスの見た目を変えるためのRendererコンポーネント
    private Color _originalEmissionColor;                   // 元のエミッションカラーを保存する変数
    public float CurrentVisualOffset => _currentModel != null ? _currentModel.transform.localPosition.y : 0f; // 現在の視覚的なオフセット（沈み具合）

    /// <summary>
    /// 初期化処理。マスの座標を名前に設定してわかりやすくする。
    /// </summary>
    /// <param name="x">X座標</param>
    /// <param name="y">Y座標</param>
    public void Initialize(int x, int y)
    {
        gameObject.name = $"Cell({x},{y})";
    }


    /// <summary>
    /// 指定された床タイプのプレファブを生成し、見た目を更新する
    /// </summary>
    /// <param name="tile">指定したい床タイル</param>
    public void SetTile(TileTypeDefinition tile)
    {
        if (tile == null)
            return;

        // 既存のモデルがあれば削除
        if (_currentModel != null)
        {
            Destroy(_currentModel);
        }

        // プレハブが設定されている場合のみ生成
        if (tile.ModelPrefab != null)
        {
            _currentModel = Instantiate(tile.ModelPrefab, _modelContainer != null ? _modelContainer : transform);
            _currentModel.transform.localPosition = Vector3.zero;
            _currentModel.transform.localRotation = Quaternion.identity;


            // 子要素からRendererを探しておく
            _renderer = GetComponentInChildren<Renderer>();
            if (_renderer != null && _renderer.material.HasProperty("_EmissionColor"))
            {
                _originalEmissionColor = _renderer.material.GetColor("_EmissionColor");
            }
        }
        else
        {
            Debug.LogWarning($"[GridCellView] {tile.TileName} にモデルプレハブが設定されていません。");
        }
    }


    /// <summary>
    /// このマスに誰かが乗っている状態をセット
    /// </summary>
    /// <param name="isOccupied">乗っているかどうか</param>
    public void SetOccupied(bool isOccupied)
    {
        Debug.Log($"{gameObject.name} の SetOccupied が {isOccupied} で呼ばれました！");
        _isOccupied = isOccupied;

        // キャラクターがいる場合は沈む位置、いない場合は元の位置を目標に設定
        _targetLocalPos = isOccupied ? new Vector3(0, _sinkDepth, 0) : Vector3.zero;

        if (_renderer != null)
        {
            // 乗っているときは明るく、いないときは元の色にする
            Color targetColor = isOccupied ? Color.white * 0.5f : _originalEmissionColor;
            _renderer.material.SetColor("_EmissionColor", targetColor);

            if (isOccupied)
            {
                _renderer.material.EnableKeyword("_EMISSION");
            }
            else
            {
                _renderer.material.DisableKeyword("_EMISSION");
            }
        }
    }

    /// <summary>
    /// 更新処理。毎フレーム、現在の位置を目標位置に向かって滑らかに移動させる
    /// </summary>
    private void Update()
    {
        // 毎フレーム、現在の位置を目標位置に向かって滑らかに移動させる
        if (_currentModel != null)
        {
            _currentModel.transform.localPosition = Vector3.Lerp(
                _currentModel.transform.localPosition,
                _targetLocalPos,
                Time.deltaTime * _animSpeed
            );
        }
    }

    /// <summary>
    /// 攻撃予告などの危険状態をセットする
    /// </summary>
    /// <param name="isWarning">警告状態かどうか</param>
    public void SetWarning(bool isWarning)
    {
        if (_renderer != null)
        {
            if (isWarning)
            {
                // 危険な赤色に光らせる！
                _renderer.material.SetColor("_EmissionColor", Color.red * 1.5f);
                _renderer.material.EnableKeyword("_EMISSION");
            }
            else
            {
                // 警告解除時は元の状態に戻す
                SetOccupied(_isOccupied);
            }
        }
    }
}
