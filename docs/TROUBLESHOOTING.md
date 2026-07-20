# Troubleshooting

[← README](../README.md) | [Getting started](GETTING_STARTED.md) | [Testing](TESTING.md)

## 1. 調査の基本順序

1. 直前の操作を止める。
2. Sceneを保存せず、Dirty状態を確認する。
3. Consoleの最初のErrorを確認する。
4. `git status`で意図しない変更を確認する。
5. Asset pathとGUIDを確認する。
6. 問題がCompile、Asset、Scene、Runtimeのどこかを分ける。
7. 最小のDevelopment SceneまたはTestで再現する。

複数の推測修正を同時に行わず、原因を1つずつ確認します。

## 2. Compile error

### 確認

- Consoleの最初のC# error
- 対象Scriptのasmdef
- Editor APIをRuntime assemblyで参照していないか
- Namespace／型名変更
- Package解決状態
- `.cs`とClass名の不一致

### 対応

1. Errorを発生させた最初のScriptを直す。
2. UnityのCompile完了を待つ。
3. Consoleを再確認する。
4. 後続Errorが連鎖的に消えたか確認する。

Assembly-CSharpとGame.Runtime間の参照問題を、asmdef削除で回避しないでください。

## 3. Missing Script

### 原因候補

- Script assetを削除した。
- `.meta`を失いMonoScript GUIDが変わった。
- Scriptを別asmdefへ移動し、型解決できなくなった。
- Class名／Namespaceを変更した。
- Package内Scriptを除外したPrefabをそのまま採用した。
- Compile errorで型が一時的に解決できない。

### 調査

1. 先にCompile Errorを0にする。
2. Missing ScriptのGameObject名とScene／Prefab pathを記録する。
3. Gitの過去版YAMLから`m_Script` GUIDを確認する。
4. GUIDを持つ`.meta`が現在どこにあるか検索する。
5. ScriptのAssembly、Namespace、Class名を確認する。
6. 正しいScriptを特定できた場合だけ再割当てする。

### 禁止

- 原因確認前に`Remove Missing Scripts`を全体実行する。
- 見た目が似たComponentを推測で割り当てる。
- GUIDを合わせるため別Scriptの`.meta`を書き換える。

既知Baselineは[Testing](TESTING.md#既知のmissing-script)を参照してください。

## 4. Broken Prefab／Missing Prefab

### 確認

- 元Prefab Assetが存在するか。
- Prefab `.meta`のGUIDが変わっていないか。
- VariantのBase Prefabが存在するか。
- Model PrefabのFBXがLFS pointerになっていないか。
- Asset Intakeで依存Assetを一部だけ採用していないか。

### 対応

1. Source GUIDを確認する。
2. Git履歴またはPackage原本から同じGUIDのAssetを復元する。
3. 復元できない場合、利用箇所と必要Componentを特定して置換計画を立てる。
4. 全利用Sceneを検証する。

## 5. Materialがピンクになる

### 原因候補

- Built-in／HDRP ShaderをURPで使用している。
- Shader Graph依存Packageがない。
- Shader assetを移動・削除した。
- MaterialのShader GUIDが欠落した。
- PlatformでShader compileに失敗した。

### 対応

1. Material InspectorでShader名を確認する。
2. ConsoleのShader errorを確認する。
3. 同じ用途の既存URP Materialと比較する。
4. Texture割当てとSurface設定を移植する。
5. Prefab／Sceneで表示確認する。

一括Material変換は対象を限定し、ThirdParty原本を直接破壊しないようにします。

## 6. Texture／Modelが正しく表示されない

### Texture

- Texture Type
- sRGB
- Alpha
- Normal Map変換
- Sprite Mode／Pixels Per Unit
- Max Size／Compression

### Model

- Scale Factor
- Axis／Bake Axis Conversion
- Rig／Avatar
- Animation clip範囲
- Material remap
- Read/Write

Import Settings差分は`.meta`に保存されるため、意図しない再Import差分を確認します。

## 7. Sceneをロードできない

### 確認

- Build SettingsにSceneが登録されているか。
- Scene名文字列が実ファイル名と一致するか。
- path移動後もGUIDが同じか。
- `LoadSceneMode.Single`／`Additive`が意図どおりか。
- 現在Sceneに未保存変更がないか。
- 同名Sceneが複数ないか。

検索対象:

```text
SceneManager.LoadScene
SceneManager.LoadSceneAsync
_sceneName
SceneCatalog
```

Scene名だけでなく、SerializeFieldに保存された文字列もInspectorで確認します。

## 8. Additive SceneでManagerが重複する

### 症状

- Eventが2回発火する。
- Audioが二重再生される。
- Inputが2回処理される。
- EventSystem／Camera／Audio Listener警告が出る。
- Result遷移が二重実行される。

### 確認

- Boot、GameplayShell、StageのどこにManagerが置かれているか。
- `DontDestroyOnLoad` Objectが再生成されていないか。
- Additive SceneにEventSystem／Cameraが含まれていないか。
- Event購読解除があるか。

Owner Sceneを1つに決め、重複時に片方を無条件Destroyするだけの回避策を増やさないようにします。

## 9. `Resources.Load`がnullになる

1. Assetが`Assets/**/Resources/`配下にあるか。
2. `Resources/`より後ろの相対パスか。
3. 拡張子を含めていないか。
4. 大文字小文字、空白、全角文字が一致するか。
5. Asset Typeが`Resources.Load<T>`の`T`と一致するか。
6. Spriteの場合、Texture ImportがSpriteか。

現在の主要Root:

```text
Textures/Title/UI_Title_Logo/
Textures/GAMECLEAR/
```

## 10. UnityPackageを直接Importしてフォルダが崩れた

追加操作を止め、保存・Commit前に範囲を確認します。

1. `git status`で新規／変更／削除を一覧化する。
2. Import直前から存在したユーザー変更を分離する。
3. Package原本を保持する。
4. Imported assetのGUIDと既存GUID衝突を確認する。
5. Scene／PrefabがImported assetを参照していないか確認する。
6. 参照がない新規Importだけを対象に戻す。
7. Asset Intakeからやり直す。

既存の未コミット変更が混在する場合、`git clean`や`reset --hard`を使わないでください。

## 11. GUID collision

### 症状

- ConsoleにGUID conflictが出る。
- 参照先が別Assetへ切り替わる。
- Unityが片方のmetaを再生成する。

### 対応

1. 重複GUIDを持つ2つのmetaを特定する。
2. それぞれのAsset出所を確認する。
3. 現在の参照元をGUID検索する。
4. Canonical Assetを決める。
5. 片方へ新GUIDを割り当てる場合、利用箇所を明示的に更新する。
6. 全Scene／Prefab／ScriptableObjectを検査する。

Asset Intakeは既存GUIDをImport前に拒否します。

## 12. `.meta`がない／余っている

### Missing meta

- Unityを開いて生成させる前に、元のGUIDが必要か確認する。
- 移動元にmetaが残っていないか探す。
- Git履歴から元metaを復元する。
- Scene参照があるAssetで新規metaを作らない。

### Orphan meta

- 対応するAssetが意図して削除されたか確認する。
- Assetだけ移動してmetaを置き去りにしていないか確認する。
- Git上のRenameとして扱われるべき差分か確認する。

## 13. Git LFS assetが壊れて見える

ファイル内容が次のような数行の場合はLFS pointerです。

```text
version https://git-lfs.github.com/spec/v1
oid sha256:...
size ...
```

対応:

```bash
git lfs install
git lfs pull
git lfs status
```

Unityを閉じる必要がある場合は閉じ、取得後にReimportします。

## 14. Test Runnerが止まる

- EditorがCompile中でないか。
- Play Mode transition中でないか。
- Modal dialogが開いていないか。
- Testが無限待機していないか。
- Scene load／domain reload待ちのtimeoutが短すぎないか。
- 前のTestがTime ScaleやStatic stateを戻しているか。

PlayMode testは十分な初期化timeoutを設定し、現在のTest名と進捗を確認します。

## 15. MCPが接続できない

1. Unity Editorが起動しているか。
2. PackageがCompile済みか。
3. Console Errorが0件か。
4. MCP Server／Bridgeが起動しているか。
5. 複数Instanceがある場合、`CreatorKousien`を選択しているか。
6. Unity再起動後、Instance IDが変わっていないか。
7. Package lockが固定コミットを指しているか。

接続後も、EditorがCompile中、Play中、Modal dialog表示中の場合は操作できないことがあります。

## 16. Enter Play Mode設定が戻る

Project設定値は`ProjectSettings/EditorSettings.asset`に保存されます。

現在の基準:

```text
m_EnterPlayModeOptions: 0
```

Test RunnerやEditor操作後に意図せず変わった場合、UnityのProject Settingsとファイル差分の両方を確認します。設定変更だけを他のScene差分へ混ぜないでください。

## 17. `_Recovery`をどう扱うか

`Assets/_Recovery`はUnityの復旧Sceneです。

- 自動的に製品Sceneとして利用しない。
- 内容確認前に一括削除しない。
- 対応する正式Scene、保存日時、Hierarchy差分を比較する。
- 必要な変更だけ正式Sceneへ反映する。
- 不要と判断した削除は独立したレビュー対象にする。

## 18. 問題報告テンプレート

```markdown
## 環境
- OS:
- Unity: 6000.3.7f1
- Branch / commit:

## 症状

## 期待する結果

## 再現手順
1.
2.
3.

## 再現率

## 対象Scene / Prefab / Asset

## Consoleの最初のError

## 直前に行った操作

## Screenshot / Video

## git status
```
