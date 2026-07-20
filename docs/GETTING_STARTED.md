# Getting started

[← README](../README.md) | [Documentation index](README.md) | [Project structure →](PROJECT_STRUCTURE.md)

## 目的

新しい開発環境でリポジトリを取得し、正しいUnityバージョンで開き、Title SceneからPlayできる状態にします。

## 前提環境

| 項目 | 必須／推奨 | 備考 |
| --- | --- | --- |
| Unity Hub | 必須 | Editorのインストールとプロジェクト登録に使用 |
| Unity `6000.3.7f1` | 必須 | `ProjectSettings/ProjectVersion.txt`と一致させる |
| Git | 必須 | リポジトリ取得と差分確認 |
| Git LFS | 必須 | FBX、PNG、Audio、FontなどをLFS管理している |
| Windows | 現行検証環境 | 他OSではパスや外部DLLの差を確認する |
| Rider／Visual Studio等 | 推奨 | C#編集、参照検索、デバッグ |

## 1. リポジトリを取得する

```bash
git lfs install
git clone <repository-url>
cd CreatorKousien
git lfs pull
```

取得後、LFSポインタのままの大容量アセットがないことを確認します。

```bash
git lfs status
```

画像やFBXを開いたとき、数行のテキストとして表示される場合はLFS本体を取得できていません。Unityを開く前に`git lfs pull`を再実行してください。

## 2. Unity Editorを準備する

1. Unity Hubの`Installs`から`6000.3.7f1`をインストールする。
2. 対象プラットフォーム向けBuild Supportは、実際にビルドする場合だけ追加する。
3. Unity Hubの`Projects`で`Add project from disk`を選ぶ。
4. `Assets`ではなく、`Assets`、`Packages`、`ProjectSettings`を含むリポジトリルートを指定する。
5. Editor versionの警告が出た場合は、別バージョンへアップグレードせず`6000.3.7f1`を選ぶ。

> [!WARNING]
> Unityバージョンの変更は、Scene、Prefab、ProjectSettingsの大規模な再シリアライズを発生させます。アップグレードは通常の機能実装と同じPRへ混ぜないでください。

## 3. 初回インポートを待つ

初回起動では、Package解決、Library作成、Shader Import、Script Compileが走ります。

- Import中にUnityを終了しない。
- Consoleへ一時的なエラーが出ても、コンパイル完了まで待つ。
- Package ManagerのGit依存解決にはネットワークが必要。
- Unity MCPは`Packages/manifest.json`で特定コミットへ固定されている。

完了後に次を確認します。

1. 右下のImport／Compile表示が消えている。
2. Consoleの`Error`が0件。
3. Projectウィンドウに`Assets/CreatorKousien`が表示される。
4. Test RunnerでEditMode／PlayModeのテスト一覧を取得できる。

## 4. プロジェクトを起動する

1. `Assets/CreatorKousien/Scenes/Application/Title.unity`を開く。
2. SceneがDirtyでないことを確認する。
3. Playボタンを押す。
4. TitleからLoading、Boot、GameplayShellへ遷移できることを確認する。

直接検証したい場合はBuild Settings登録SceneまたはDevelopment Sceneを開けますが、最終確認はTitle起点で行います。

## 5. IDEを接続する

Unityの`Edit > Preferences > External Tools`で使用するIDEを選びます。

プロジェクトには次のアセンブリがあります。

- `Assembly-CSharp`: `Assets/Scripts`を中心とする既存コード
- `Assembly-CSharp-Editor`: `Assets/Editor`など
- `Game.Runtime`: `Assets/_Project/Runtime`
- `Game.Tests`: `Assets/_Project/Tests`
- `CreatorKousien.AssetOrganization.Editor`
- `CreatorKousien.AssetOrganization.Editor.Tests`

`.slnx`や`.csproj`はUnityが生成します。手動でソース一覧を編集しないでください。

## 6. Unity MCPを使う場合

Unity MCPはEditorを操作・検査する開発支援です。ゲーム実行時の依存ではありません。

1. Unityでプロジェクトを開く。
2. MCP packageが解決されていることを確認する。
3. MCP側で接続対象が`CreatorKousien`になっていることを確認する。
4. 複数Unity Editorが起動している場合は、対象Instanceを明示する。
5. Scene変更前にEditorがCompile中／Play中でないことを確認する。

MCPが接続できない場合は[Troubleshooting](TROUBLESHOOTING.md#mcpが接続できない)を参照してください。

## 7. 初回チェックリスト

- [ ] Unity `6000.3.7f1`で開いた
- [ ] Git LFS本体を取得した
- [ ] Package解決が完了した
- [ ] Console Errorが0件
- [ ] Title Sceneを開けた
- [ ] Title起点でPlayできた
- [ ] EditModeテストを実行できた
- [ ] PlayModeテストを実行できた
- [ ] 自分が変更していないScene／ProjectSettings差分がない

## 次に読む

- ファイル配置: [Project structure](PROJECT_STRUCTURE.md)
- 日常の開発手順: [Development](DEVELOPMENT.md)
- 検証方法: [Testing](TESTING.md)
