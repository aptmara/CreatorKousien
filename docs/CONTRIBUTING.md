# 開発・運用ガイドライン (CreatorKousien)

本リポジトリでは、`master` ブランチを保護し、すべての作業を Issue 起点の短命ブランチで行う運用を採用しています。

## 1. 目的
* 作業内容をブランチ名から一目で判別可能にする
* Issue / PR / Project の対応関係を明確にし、進捗管理を円滑にする
* `master` への直接コミットを禁止し、レビュー経由で品質を担保する
* Unity / ゲーム開発特有の競合リスクを最小限に抑える

## 2. ブランチ構成
* **`master`**
  * 常に動作可能な状態（デプロイ可能）を維持する基準ブランチ
  * 直接 push 禁止（Branch Protection 推奨）
* **作業ブランチ**
  * すべて `master` から作成し、1ブランチにつき1つの Issue/目的のみを扱う
  * 作業完了後は PR を作成し、マージ後に削除する

## 3. ブランチ命名規則
形式: `<type>/#<issue番号>-<内容>`

| Type | 内容 |
| :--- | :--- |
| `feature` | 新機能追加 |
| `fix` | バグ修正 |
| `refactor` | 挙動を変えないコード整理・設計改善 |
| `docs` | ドキュメント修正 |
| `chore` | 環境整備、設定変更、依存更新、CI 修正 |
| `test` | テスト追加・修正 |
| `hotfix` | 緊急修正（本番相当ブランチへの即時反映が必要なもの） |

### 具体的な例
* `feature/#12-player-move`
* `fix/#18-enemy-spawn-bug`
* `docs/#31-readme-update`

### 詳細ルール
* 英小文字のみを使用し、単語区切りは `-`（ハイフン）とする
* Issue 番号は必須（作業の透明性を確保するため）
* 内容は「作業対象」が分かるよう、2～5語程度で簡潔に記述する

---

## 4. 開発フロー
### 作業の開始
1. **Issue を作成する**（または既存の Issue を担当する）
2. `master` ブランチを最新にする
3. `master` から作業ブランチを切る

```bash
git switch master
git pull origin master
git switch -c feature/#12-player-move
```

### コミット運用
メッセージ形式: `<type>: <変更内容>`
* 1 commit = 1 意味のある変更単位
* `feat: add player move logic` のような Conventional Commits 形式も推奨

### プルリクエスト (PR)
* タイトル形式: `[#<issue番号>] <概要>`
* 説明欄には必ず `Closes #番号` を含め、関連する Issue を紐付ける
* 1人以上のレビューを経てマージする

## 5. マージ方式
原則として **Squash and merge** を採用します。
* コミット履歴を Issue 単位で綺麗に保ち、追跡性を高めるため
* 作業中の細かい WIP コミットを統合するため

## 6. 禁止事項
* `master` ブランチへの直接 push
* Issue を作成せずに作業ブランチを作成すること
* 1つのブランチに無関係な修正を混ぜること
* 動作確認（ビルド・実機確認）が未完了の状態での PR 作成
* `final`, `test2` といった意図の不明な名前の使用

---

## ⚡ コピペ用：クイックレファレンス

### ブランチ命名規則
` <type>/#<issue番号>-<内容> `

**例:**
- `feature/#12-player-move`
- `fix/#18-enemy-spawn-bug`
- `refactor/#25-turn-manager`
- `docs/#31-readme-update`
- `chore/#40-gitignore-update`

### ルール
- **英小文字のみ**、単語区切りは `-`
- **Issue 番号必須**
- **1 ブランチ = 1 Issue = 1 目的**
- `master` から分岐し、`master` へ直接 push しない
- 作業完了後は PR を作成し、`Closes #番号` を含める
- `Squash and merge` でマージし、ブランチを削除する
