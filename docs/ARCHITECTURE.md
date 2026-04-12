# プロジェクトアーキテクチャ (CreatorKousien)

本プロジェクトは、システム、定義データ、実行時状態、コマンドの4つの要素を軸に、疎結合な設計を目指しています。

---

## 🏛️ 主要システムと責務 (Core Systems)

| システム名 | 担当者 | 責務概要 | 関連ファイル |
| :--- | :--- | :--- | :--- |
| **GameManager** | 寺田 | シーン初期化、全体ライフサイクル管理 | `Scripts/Core/GameManager.cs` |
| **GameMediator** | 寺田 | システム間の仲介・イベント順序制御 | `Scripts/Core/GameMediator.cs` |
| **FieldService** | 浅野 | 盤面操作API、移動可否判定、占有管理 | `Scripts/Field/FieldService.cs` |
| **BattleManager** | 寺田 | 戦闘解決、ダメージ計算、命中判定 | `Scripts/Battle/BattleManager.cs` |
| **CardSystem** | 越智 | 手札・デッキ管理、使用要求生成 | `Scripts/Card/CardSystem.cs` |
| **EnemyAI** | 岩井 | 敵行動決定、予告情報生成 | `Scripts/Enemy/EnemyAI.cs` |
| **UIManager** | 山本 | HP、手札、フィールド更新等の表示層 | `Scripts/UI/UIManager.cs` |

---

## 💾 データ構造と所有権 (Data Ownership)

「定義データ（静的）」と「実行時状態（動的）」を分離し、書き換え権限を明確にしています。

| データ種別 | 所有システム (書き換え権) | 参照システム |
| :--- | :--- | :--- |
| **FieldState** | FieldService | BattleManager / EnemyAI / TileEffectSystem |
| **PlayerRuntimeData** | PlayerSystem | BattleManager / UIManager / EffectSystem |
| **EnemyRuntimeData** | EnemySystem | EnemyAI / BattleManager / UIManager |
| **CardRuntimeData** | CardSystem | UIManager / GameMediator |
| **TelegraphRuntimeData**| AttackTelegraphSystem | FieldView / BattleManager |

---

## 👥 担当メンバーと担当範囲

| メンバー | 担当範囲 | 主なシステム |
| :--- | :--- | :--- |
| **浅野** | プレイヤー / フィールド / 床効果 | `FieldService`, `TileEffectSystem`, `StageData` |
| **寺田** | ゲームマネージャー / 進行管理 / バトル | `GameManager`, `GameMediator`, `BattleManager` |
| **山本** | UI全般 | `UIManager`, `FieldView`, `CardView` |
| **滝谷** | ターン管理 / フェーズ制御 | `TurnManager`, `PhaseManager` |
| **越智** | カード / 効果定義 | `CardSystem`, `CardData`, `EffectSystem` |
| **岩井** | エネミー / 攻撃予告 | `EnemySystem`, `EnemyAI`, `AttackTelegraphSystem` |

---

## 🔍 実装時の参照フロー

特定の機能を実装・修正する際の推奨される確認順序です。

1. **移動まわり**: `MoveCommand` → `FieldService` → `FieldState` → `TileEffectSystem`
2. **戦闘・攻撃**: `AttackCommand` → `BattleManager` → `EffectSystem` → 各 `RuntimeData`
3. **カード追加**: `CardData` → `CardRuntimeData` → `CardSystem` → `CardView`
4. **敵行動**: `EnemyData` → `EnemyAI` → `AttackTelegraphSystem` → `BattleManager`

---

## 🗺️ インタラクティブ・ナビゲーター
より詳細な要素間の依存関係やファイルパスを検索したい場合は、以下のHTMLファイルを開いてください。

👉 **[docs/architecture_navigator.html](./architecture_navigator.html)**
*(ブラウザで開くと検索・フィルタリングが可能です)*
