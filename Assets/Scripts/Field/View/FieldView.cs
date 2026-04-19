// ------------------------------------------------------------
// File		: FieldView.cs
// Summary	: FieldStateの状態を元に、盤面全体の見た目を管理するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-13
//
// Notes	:
// - 盤面全体の見た目を管理するクラス。
// - 左上(0,0)を原点とし、右方向へ+X、下方向へ+Y(Unity空間では-Z)として配置するよう修正 (4/13)
// - 乗ったマス、過去のマスを記録して、マスの見た目を変える機能追加 (4/15)
// - カメラの位置をStageに対応する形で調整
// ------------------------------------------------------------
using UnityEngine;
using System.Collections.Generic;
using CreatorKousien.Field;
using CreatorKousien.Data;

namespace CreatorKousien.Field
{
    /// <summary>
    /// 盤面全体の情報を読み取り、画面上に可視化する表示層のクラス
    /// </summary>
    public class FieldView : MonoBehaviour
    {
        [Header("参照オブジェクト")]
        [Tooltip("マスを生成する際の親となる空のGameObject")]
        [SerializeField] private Transform _gridParent;         // ここがマスの親（0,0）になる！

        [Tooltip("1マスのPrefab。GridCellViewコンポーネントがアタッチされている必要がある")]
        [SerializeField] private GridCellView _cellPrefab;      // 1マスのPrefab


        [Header("カメラ設定")]
        [Tooltip("カメラの高さ")]
        [SerializeField] private float _cameraHeight = 4.5f;      // カメラの高さ
        [Tooltip("カメラのZ方向の引き")]
        [SerializeField] private float _cameraOffsetZ = -7f;    // カメラのZ方向の引き
        [Tooltip("カメラのX軸の回転角度")]
        [SerializeField] private float _cameraAngleX = 35f;   // カメラのX軸の回転角度


        // 生成したセルのリスト
        private Dictionary<Vector2Int, GridCellView> _cellViews = new Dictionary<Vector2Int, GridCellView>();

        private Vector2Int _lastOccupiedPos = new Vector2Int(-1, -1); // 最後にキャラクターがいたマスの座標を保存する変数

        /// <summary>
        /// FieldStateのデータに基づいて初期の盤面モデルを生成・配置
        /// 盤面を左上から順に配置するように修正 (4/13)
        /// 引数をStageDataに変更 (4/15)
        /// </summary>
        /// <param name="state">盤面の実行時データ</param>
        /// <param name="stageData">盤面の静的データ</param>
        public void BuildView(FieldState state, StageData stageData)
        {
            // ----- 1. 既存のセルを削除 -----
            foreach (Transform child in _gridParent)
            {
                Destroy(child.gameObject);
            }
            _cellViews.Clear(); // 辞書もクリア

            float cellSize = stageData.CellSize;           // StageDataからセルのサイズを取得
            float borderGap = stageData.BorderGap;          // StageDataから境界線の隙間の広さを取得
            int borderX = stageData.BorderX;            // StageDataから境界線のX座標を取得


            // ----- 2. 新しいセルを生成 -----
            for (int x = 0; x < state.Width; x++)
            {
                for (int y = 0; y < state.Height; y++)
                {
                    // 敵陣のマスは、BorderGap分だけ右にオフセットして配置する (4/15)
                    float offsetX = (x >= borderX) ? borderGap : 0f;

                    // 座標計算の修正：
                    // 右方向へ+X、
                    // 下方向へ+Y = Unity空間では-Zとして配置するように修正 (4/13)
                    Vector3 localPos = new Vector3(x * cellSize + offsetX, 0, -y * cellSize);   // マスの位置を計算

                    // プレハブを生成
                    GridCellView cellView = Instantiate(_cellPrefab, _gridParent);
                    cellView.transform.localPosition = localPos;                        // 位置を設定
                    cellView.transform.localRotation = Quaternion.identity;             // 回転をリセット

                    cellView.Initialize(x, y);                                          // セルの初期化

                    // 初期状態を反映
                    var cellData = state.GetCell(x, y);
                    if (cellData != null)
                    {
                        cellView.SetTile(cellData.CurrentTile);
                    }

                    _cellViews.Add(new Vector2Int(x, y), cellView); // 生成したセルを保存
                }
            }

            // ----- 3. 境界線のオブジェクトを配置 (4/15) -----
            if (stageData.BorderPrefab != null && borderGap > 0f)
            {
                // 隙間の真ん中のX座標を計算
                float borderCenterX = (borderX * cellSize) - (cellSize / 2f) + (borderGap / 2f);
                // Y軸の真ん中のZ座標を計算
                float centerZ = -((state.Height - 1) * cellSize) / 2f;

                Vector3 borderPos = new Vector3(borderCenterX, 0, centerZ);                 // 境界線の位置を計算
                GameObject borderObj = Instantiate(stageData.BorderPrefab, _gridParent);
                borderObj.transform.localPosition = borderPos;                              // 位置を設定
                borderObj.name = "BorderLineObject";                                        // オブジェクトの名前を設定
            }

            // カメラの位置を調整
            SetupCamera(state, stageData);

            Debug.Log($"[FieldView] {state.Width}x{state.Height}の盤面を生成しました。");
        }


        /// <summary>
        /// カメラを盤面の中心に合わせるメソッド
        /// </summary>
        /// <param name="state">    フィールドの状態</param>
        /// <param name="stageData">ステージのデータ</param>
        private void SetupCamera(FieldState state, StageData stageData)
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                Debug.LogWarning("[FieldView] メインカメラが設定されていません。カメラの位置を調整できません。");
                return;
            }

            // 盤面の中心を計算
            float centerX = (state.Width - 1) * stageData.CellSize / 2f;
            if (stageData.BorderGap > 0f)
            {
                centerX += stageData.BorderGap / 2f;
            }
            float centerZ = -(state.Height - 1) * stageData.CellSize / 2f;

            // カメラの位置と角度をセット
            mainCam.transform.position = new Vector3(centerX, _cameraHeight, centerZ + _cameraOffsetZ);
            mainCam.transform.rotation = Quaternion.Euler(_cameraAngleX, 0, 0);

            Debug.Log($"[FieldView] カメラの位置を調整しました。位置: {mainCam.transform.position}, 角度: {mainCam.transform.rotation.eulerAngles}");
        }


        /// <summary>
        /// マスのアニメショーンを行うメソッド
        /// </summary>
        /// <param name="x">X座標</param>
        /// <param name="y">Y座標</param>
        public void HighlightCell(int x, int y)
        {
            // 前のマスを元に戻す
            if (_cellViews.TryGetValue(_lastOccupiedPos, out var oldCell))
            {
                oldCell.SetOccupied(false); // 前のマスの見た目を元に戻す
            }

            // 新しいマスをアニメーション
            Vector2Int newPos = new Vector2Int(x, y);
            if (_cellViews.TryGetValue(newPos, out var newCell))
            {
                newCell.SetOccupied(true);
            }

            _lastOccupiedPos = newPos; // 最後にキャラクターがいたマスの座標を更新
        }


        /// <summary>
        /// 座標から対応するGridCellViewを取得するメソッド
        /// </summary>
        /// <param name="pos">対応する座標</param>
        /// <returns></returns>
        public GridCellView GetCellView(Vector2Int pos)
        {
            if (_cellViews.TryGetValue(pos, out var cell))
            {
                return cell;
            }
            return null;
        }

        /// <summary>
        /// マスの実際の3D空間上の位置を取得するメソッド
        /// </summary>
        /// <param name="x">マスのX座標</param>
        /// <param name="y">マスのY座標</param>
        /// <returns>実際の3D空間上の位置</returns>
        public Vector3 GetCellWorldPosition(int x, int y)
        {
            var cell = GetCellView(new Vector2Int(x, y));
            return cell != null ? cell.transform.position : Vector3.zero;
        }


        /// <summary>
        /// 特定の座標のマスのモデルを更新する
        /// </summary>
        /// <param name="x">マスのX座標</param>
        /// <param name="y">マスのY座標</param>
        /// <param name="newTile">新しいタイルの定義</param>
        public void UpdateCellTileModel(int x, int y, TileTypeDefinition newTile)
        {
            Vector2Int pos = new Vector2Int(x, y);
            if (_cellViews.TryGetValue(pos, out var cellView))
            {
                cellView.SetTile(newTile);

                // TODO: ここで、マスの見た目を更新するためのアニメーションやエフェクトを追加できるぜよ
            }
        }


        /// <summary>
        /// 指定された複数のマスを攻撃予告にするメソッド
        /// </summary>
        /// <param name="position">攻撃予告を表示するマスのリスト</param>
        /// <param name="isWarning">警告状態かどうか</param>
        /// <param name="stepOrder">何手目か</param>
        public void ShowTelegraph(List<Vector2Int> position, bool isWarning, int stepOrder = 0)
        {
            foreach (var pos in position)
            {
                var cell = GetCellView(pos);
                if (cell != null)
                {
                    cell.SetWarning(isWarning, stepOrder);
                }
            }
        }


        /// <summary>
        /// 移動予告を表示するメソッド
        /// </summary>
        /// <param name="position">移動予告を表示するマス</param>
        /// <param name="isWarning">警告状態かどうか</param>
        /// <param name="stepOrder">何手目か</param>
        public void ShowMoveTelegraph(Vector2Int position, bool isWarning, int stepOrder = 0)
        {
            var cell = GetCellView(position);
            if (cell != null)
            {
                cell.SetMoveIntent(isWarning, stepOrder);
            }
        }


        public void ClearAllTelegraph()
        {
            foreach (var cell in _cellViews.Values)
            {
                cell.SetWarning(false);
                cell.SetMoveIntent(false);
            }
        }


        /// <summary>
        /// アクションフェーズで攻撃が発動したマスを一斉に光らせるメソッド
        /// </summary>
        /// <param name="positions"></param>
        public void ShowAttackArea(List<Vector2Int> positions, bool show)
        {
            if (positions == null)
                return;

            foreach (var pos in positions)
            {
                var cell = GetCellView(pos);
                if (cell != null)
                {
                    cell.SetAttackHighlight(show);
                }
            }
        }
    }
}
