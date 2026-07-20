# Development

[← README](../README.md) | [Documentation index](README.md) | [Testing →](TESTING.md)

## 1. 開発原則

- 1 Issue = 1目的
- 1 Branch = 1 Issue
- 変更範囲をOwnerと契約で区切る
- 共有Sceneを複数人で同時編集しない
- Unity Assetは`.meta`と一緒に扱う
- 動作確認できない状態をレビューへ渡さない
- 仕様、保存形式、公開契約の変更を隠さない

## 2. Issue

着手前にIssueへ次を記載します。

- 背景／問題
- 期待する結果
- 対象Scene、Prefab、System
- 変更しない範囲
- 完了条件
- 検証方法
- Screenshot／Video／再現手順
- 依存Issue

Bugでは、期待値、実際の結果、再現率、最小再現手順、Unityバージョンを記載します。

## 3. Branch

形式:

```text
<type>/#<issue-number>-<short-description>
```

例:

```text
feature/#123-boss-intake
fix/#124-result-double-transition
refactor/#125-enemy-gauge-owner
docs/#126-asset-workflow
chore/#127-update-unity-package
test/#128-wave-runtime-tests
```

Type:

| Type | 用途 |
| --- | --- |
| `feature` | 機能追加 |
| `fix` | 不具合修正 |
| `refactor` | 外部挙動を変えない構造改善 |
| `docs` | 文書のみ |
| `chore` | 環境、Package、CI、設定 |
| `test` | テスト追加・修正 |
| `hotfix` | 緊急修正 |

## 4. 実装開始前

1. `master`を最新化する。
2. 作業Branchを作る。
3. Unityを正しいバージョンで開く。
4. Console Errorが0件か確認する。
5. 対象Scene／テストのBaselineを確認する。
6. `git status`で既存の未コミット変更を把握する。
7. 変更するOwner、公開契約、保存データを特定する。

既存の未コミット変更をstash、reset、削除して作業開始しないでください。

## 5. 実装単位

変更は次の順で小さく検証します。

1. Data／契約
2. Runtime logic
3. Unity component／Prefab
4. Development Scene
5. Gameplay Sceneへの統合
6. UI／Feedback
7. Tests
8. Documentation

公開API、保存形式、asmdef、Scene flowが当初計画から変わる場合、実装を続ける前に影響範囲を更新します。

## 6. 並列開発

### 6.1 Ownerの例

| 領域 | 主な成果物 | 接続先 |
| --- | --- | --- |
| Player | Movement、Collection、Attachment | Stage、UI、Roguelike |
| Release／Hit | Payload release、collision、batch | Player、Enemy |
| Enemy／Boss | State、gauge、damage、visual | Stage、UI、Result |
| Collectibles | Definition、Prefab、mutation | Player、Stage |
| Stage／Wave | Field、spawn、wave、gimmick | Enemy、Result |
| UI／Feedback | HUD、Result、Camera、VFX | Event／Snapshot |
| Infrastructure | Boot、Loading、Scene flow | 全Scene |
| Asset integration | Asset Intake、Prefab setup | 各Feature Owner |

### 6.2 接続契約

担当間の接続は次を優先します。

- C# interface／公開API
- Event／Event Channel
- PrefabルートComponent
- ScriptableObject definition
- Scene内の明示的なSerializeField

相手のprivate field、Scene子階層名、`Find`結果、Editor上の偶然のロード順へ依存しません。

### 6.3 共有Scene

- GameplayShellは統合担当が最終編集する。
- StageとUI_HUDの同時編集予定を共有する。
- Prefabへ切り出せる変更はSceneへ直接作り込まない。
- Scene変更前後のHierarchyをScreenshotで残す。
- Scene競合をYAMLの片側採用だけで解決しない。

## 7. Code style

### Naming

- Type、method、property: `PascalCase`
- private field: 周辺コードの規約に合わせる
- interface: `I` prefix
- ScriptableObject class: 責務が分かる`Data`／`Definition`等
- Runtime stateとDefinitionを名前で区別する
- Eventは発生済みの事実として読める名前

### MonoBehaviour

- Unity message methodに複数責務を詰め込まない。
- `Update`内の検索、LINQ、文字列生成、Instantiateを避ける。
- `Find`や`GetComponent`を毎フレーム実行しない。
- Inspector必須参照の設定方法をPrefabで統一する。
- `OnEnable`で購読し、対応する`OnDisable`で解除する。
- Scene unload後のEvent購読を残さない。

### Logging

- 通常フレームで大量に出るLogを追加しない。
- Errorには対象Asset／Scene／IDを含める。
- 個人情報、Token、完全な外部入力を記録しない。
- Debug専用Logを製品ロジックの成立条件にしない。

## 8. Asset変更

- Unity Editorまたは専用ツールで移動する。
- `.meta`を維持する。
- GUIDと参照元を確認する。
- UnityPackageは[Asset workflow](ASSET_WORKFLOW.md)に従う。
- 外部アセットとゲーム固有派生物を分ける。
- Import Settings変更もレビュー対象に含める。

## 9. Commit

コミットは機能単位で、日本語で簡潔にします。

例:

```text
敵アセットの安全な受け入れ処理を追加
ステージシーンの参照切れを修正
アセット運用ドキュメントを再構成
```

避ける例:

```text
update
fix
いろいろ修正
最終
```

コミット前:

- `git status`で対象外差分がないか。
- Scene、Prefab、ProjectSettingsに意図しない差分がないか。
- 大容量BinaryがGit LFS対象か。
- 新規Assetにmetaがあるか。
- Generated fileを不要に含めていないか。

## 10. Pull request

PR本文に含める内容:

- 変更目的
- 変更内容
- 主要な設計判断
- 変更Scene／Prefab／Data
- Screenshot／動画
- 実行したテスト
- 未検証事項
- 既知のリスク
- `Closes #<issue>`

Scene変更がある場合:

- 開いたScene
- 変更Hierarchy
- Missing Script検査結果
- Build Settingsへの影響

Asset移動がある場合:

- Source → Destination
- GUID維持結果
- 依存GUID検査結果
- Resources／Addressablesへの影響

## 11. Review

Reviewerは差分量だけでなく次を確認します。

- Ownerと依存方向が明確か。
- Prefab／Scene契約を壊していないか。
- DefinitionとRuntime stateが混ざっていないか。
- Eventを命令や戻り値に使っていないか。
- 毎フレーム処理や大量生成が増えていないか。
- Testが要件を検証しているか。
- テストを通すために期待値を弱めていないか。
- 文書と実装が一致するか。

## 12. Merge

- 1人以上のReview承認を得る。
- Required checksが成功している。
- 未解決Review threadがない。
- 原則`Squash and merge`を使う。
- Merge後にBranchを削除する。
- 共有Sceneを触る次の担当へ完了を共有する。

## 13. Definition of done

- [ ] Issueの完了条件を満たした
- [ ] Console Error 0件
- [ ] 対象Scene／Prefabに新規Missing Script 0件
- [ ] 関連EditModeテスト成功
- [ ] 必要なPlayModeテスト成功
- [ ] meta／GUID検査成功
- [ ] Title起点の回帰確認完了
- [ ] PRに未検証事項を記載
- [ ] 変更した仕様を文書へ反映
- [ ] 対象外変更を含めていない
