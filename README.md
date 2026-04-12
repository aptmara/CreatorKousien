# CreatorKousien

![Unity Version](https://img.shields.io/badge/Unity-6000.3.7f1-black.svg?style=flat&logo=unity)
![CI Status](https://github.com/aptmara/CreatorKousien/actions/workflows/ci.yml/badge.svg)
![License](https://img.shields.io/badge/License-Private-blue.svg)

本リポジトリは、戦略的なフィールド移動とカードベースの戦闘を組み合わせたゲーム、**『CreatorKousien』** の開発プロジェクトです。
疎結合な設計（Decoupling）とドメイン駆動の責務分離を徹底し、拡張性とメンテナンス性を重視しています。

---

## 🎮 ゲーム概要と特徴

- **フィールド移動**: 盤面上の座標、移動可否、占有状態を管理。
- **カードシステム**: 表裏で効果が変化するカードによる戦略的なバトル。
- **床効果 (Tile Effects)**: 踏み込んだ際や滞在時に発生する多様な効果。
- **敵AIと予告**: 敵の行動パターンを選択し、攻撃範囲を事前に予告する仕組み。

---

## 🛠️ 技術スタックと設計方針

### 開発環境
- **Unity**: `6000.3.7f1`
- **Scripting**: C# 11+
- **Rendering**: Universal Render Pipeline (URP)

### 設計思想: "Decoupling Logic from View"
1. **ロジックの分離**: ゲームコアロジックは可能な限り `Pure C#` で記述し、Unity API (`MonoBehaviour`) への依存を最小限に抑えています。
2. **所有権の徹底**: 各データ（RuntimeData）を書き換えてよいシステム（Owner）を厳格に定義しています。
3. **仲介者パターン (Mediator)**: システム間の直接参照を避け、`GameMediator` を介したイベント通信や順序制御を行います。

---

## 👥 担当メンバーとドメイン範囲

| 担当 | メンバー | 主なドメインと責務 | 関連システム |
| :--- | :--- | :--- | :--- |
| **進行/バトル** | **寺田 / 滝谷** | ゲームライフサイクル、フェーズ管理、戦闘解決 | `GameManager`, `BattleManager`, `TurnManager` |
| **盤面/効果** | **浅野** | 盤面座標管理、プレイヤー移動、床効果基盤 | `FieldService`, `TileEffectSystem`, `StageData` |
| **カード/効果** | **越智** | カードデッキ・手札管理、汎用効果定義 | `CardSystem`, `CardData`, `EffectSystem` |
| **エネミー** | **岩井** | 敵行動AI、攻撃範囲予告の生成と更新 | `EnemySystem`, `EnemyAI`, `AttackTelegraphSystem` |
| **UI/表示** | **山本** | HP/手札/フィールド等の表示・演出・入力 | `UIManager`, `FieldView`, `CardView` |

---

## 📁 ディレクトリ構造

```text
Assets/
├── Scripts/            # プログラムソースコード
│   ├── Core/           # ゲーム進行管理、システム仲介
│   ├── Field/          # 盤面、移動ロジック
│   ├── Battle/         # 戦闘解決ロジック
│   ├── Card/           # カード管理
│   ├── Enemy/          # 敵AI、攻撃予告
│   ├── UI/             # 表示、演出、入力
│   └── Data/           # ScriptableObject 定義
├── Prefabs/            # プレハブ（ドメイン毎に分割）
├── Settings/           # プロジェクト、描画設定
└── TutorialInfo/       # テンプレート用（初期フォルダ）

docs/                   # 詳細ドキュメント・設計書
```

---

## 📖 開発・設計リソース

開発を開始する前に、以下のドキュメントを必ず確認してください。

- 🗺️ **[詳細設計ドキュメント (Markdown)](./docs/ARCHITECTURE.md)**: 責務、データの所有権、参照フローの解説。
- 🧭 **[設計ナビゲーター (HTML)](./docs/architecture_navigator.html)**: 検索・フィルタ可能な詳細設計ブラウザ。
- ✍️ **[コード・アセット命名規則 (STYLE_GUIDE.md)](./docs/STYLE_GUIDE.md)**: 接尾辞のルール、配置ディレクトリの指定。
- 📋 **[開発・運用ガイドライン (CONTRIBUTING.md)](./docs/CONTRIBUTING.md)**: 命名、フロー、PR手順。

---

## 🚀 開発の進め方

### 1. セットアップ
1. Unity `6000.3.7f1` をインストールします。
2. 本リポジトリをクローンし、Unityで開きます。
3. IDE（VS/Rider等）で `.editorconfig` が読み込まれていることを確認してください。

### 2. 開発フロー
- すべての作業は **Issue 起点** です。
- ブランチ作成: `git switch -c <type>/#<issue番号>-<内容>`
  - 例: `feature/#12-player-move`
- コミットメッセージ: `<type>: <変更内容>`

### 3. プルリクエスト (PR)
- PR作成時に自動で **PRテンプレート** が適用されます。
- チェックリストを確認し、セルフチェックを行ってください。
- **CI チェック**: GitHub Actions によりコード整形とメタファイル漏れが自動チェックされます。

### 4. マージ
- 1人以上のレビュー承認後、`Squash and merge` でマージしてください。
