# CreatorKousien documentation

[← Repository README](../README.md)

このディレクトリには、CreatorKousienの正式な開発文書だけを置きます。リポジトリの入口は常にルートの[README](../README.md)です。

## 読み順

### 初めて参加する

1. [Getting started](GETTING_STARTED.md)
2. [Project structure](PROJECT_STRUCTURE.md)
3. [Development](DEVELOPMENT.md)
4. [Testing](TESTING.md)

### ゲームシステムを実装する

1. [Gameplay](GAMEPLAY.md)
2. [Architecture](ARCHITECTURE.md)
3. [Scenes](SCENES.md)
4. [Testing](TESTING.md)

### デザイナー素材を取り込む

1. [Asset workflow](ASSET_WORKFLOW.md)
2. [Project structure](PROJECT_STRUCTURE.md)
3. [Troubleshooting](TROUBLESHOOTING.md)

## 文書一覧

| 文書 | 責務 | 主な読者 |
| --- | --- | --- |
| [Getting started](GETTING_STARTED.md) | 開発環境を再現し、Title Sceneを起動する | 新規参加者 |
| [Architecture](ARCHITECTURE.md) | 依存方向、レイヤー、契約、データと物量の設計を定義する | Programmer、Tech Lead |
| [Project structure](PROJECT_STRUCTURE.md) | ファイル・フォルダの正式な置き場所を定義する | 全員 |
| [Asset workflow](ASSET_WORKFLOW.md) | UnityPackageとデザイナー素材の安全な受け入れを定義する | Designer、Implementer |
| [Scenes](SCENES.md) | Scene構成、Build Settings、所有権、Additiveロードを定義する | Programmer、Level Designer |
| [Gameplay](GAMEPLAY.md) | コアループと各メカニクスの設計基準を定義する | Game Designer、Programmer |
| [Development](DEVELOPMENT.md) | Issue、Branch、Commit、PR、並列開発の手順を定義する | Contributor |
| [Testing](TESTING.md) | 自動テスト、Scene検査、meta／GUID検査の合格基準を定義する | Contributor、Reviewer |
| [Troubleshooting](TROUBLESHOOTING.md) | よくあるUnity固有問題の調査・復旧順を定義する | 全員 |

## 更新ルール

- コードや設定から確認できる事実と、将来の設計案を同じ書き方で混ぜない。
- ファイル名、Scene名、Menu名、Unityバージョンは実在を確認して記載する。
- 新しい正式文書を追加したら、ルートREADMEとこの一覧の両方へリンクする。
- 同じルールを複数文書へコピーしない。責務を持つ文書へ書き、他文書からリンクする。
- 一時的な調査ログ、個人メモ、チャットのコピーを正式文書として追加しない。
- 古い文書を残して新旧を併存させず、差分を確認したうえで正式文書を更新する。
- HTML版、PDF版、別インデックスを手作業で複製しない。

## 文書変更のレビュー項目

- READMEから到達できるか。
- ローカルリンクが存在するか。
- 現在のコードや設定と矛盾していないか。
- 現在の仕様と将来の計画が混同されていないか。
- 同じ説明が別文書と重複していないか。
- 削除・移動したファイルへの参照が残っていないか。
