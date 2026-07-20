# Testing and quality assurance

[← README](../README.md) | [Development](DEVELOPMENT.md) | [Troubleshooting →](TROUBLESHOOTING.md)

## 1. 品質ゲート

変更のリスクに応じて、近い検証から広い検証へ進みます。

1. 対象コードのCompile
2. 対象EditMode test
3. 対象Development Scene
4. 対象Prefab／Gameplay Scene検査
5. 関連PlayMode test
6. Title起点の回帰確認
7. meta／GUID／Build Settings検査
8. 必要なPlatform build

実行していない検証を「問題なし」と報告しません。

## 2. Baseline

2026-07-20のアセット再構成完了時点:

| 検証 | 結果 |
| --- | --- |
| Unity version | `6000.3.7f1` |
| Console compile errors | 0 |
| EditMode | 9 / 9 passed |
| PlayMode | 6 / 6 passed |
| Build Settings | 10 scenes、GUID／順序／enabled維持 |
| Missing meta | 0 |
| Orphan meta | 0 |
| Duplicate GUID | 0 |
| 検査したmeta GUID | 1,422 |
| 通常Scene | 26 scenes検査 |
| Recovery Scene | 13 scenes検査 |
| Broken Prefab | 0 |

テスト数は増減します。成功数を固定値としてテストするのではなく、実行時のTotalとFailedを記録してください。

## 3. Unity Test Runner

Unity menu:

```text
Window > General > Test Runner
```

### EditMode

Editor API、Data変換、UnityPackage解析、パス検証、Asset移動など、Play Modeを必要としない処理を検証します。

現在のAsset Organizationテスト:

- Path traversal rejection
- Code／plugin extension blocking
- Domain／Entity path planning
- Boss placement
- UnityPackage archive parsing
- Asset moveとGUID維持

### PlayMode

Runtime assembly、GameObject lifecycle、Roguelike runtime stateなどを検証します。

PlayModeテストでは次に注意します。

- Scene／ScriptableObjectの状態をテスト間で残さない。
- 作成したGameObjectを破棄する。
- Static event購読を解除する。
- Time ScaleやEnter Play Mode設定を元に戻す。
- 非同期処理の完了条件を明示する。

## 4. Console

### Error

PR前は0件必須です。

- Compile error
- MissingReferenceException
- NullReferenceException
- Serialized field type mismatch
- Shader／Material error
- Package resolution error

### Warning

Warningは件数だけで無視せず、今回の変更で新規発生したかを確認します。

- Obsolete API
- Missing asset reference
- Multiple Audio Listener／EventSystem
- Import warning
- Animation／Avatar mismatch
- Test Framework内部の終了時Log

既存Warningと新規Warningを区別し、不要なSuppressを追加しません。

## 5. Scene validation

各Sceneで確認する項目:

- Missing Script
- Broken Prefab／Missing Prefab asset
- Missing Material／Mesh／Animator Controller
- Main Camera重複
- Audio Listener重複
- EventSystem重複
- Dirty state
- Additive load時のManager重複
- Unload後の参照残り

### 既知のMissing Script

| Scene | 件数 | 状態 |
| --- | ---: | --- |
| `Application/Legacy/Select.unity` | 3 | 既存。`SelectSceneView` 1、`GameManager` 2 |
| `Development/UI/Proto_G_UIDebug.unity` | 2 | 既存。`EnemyController_Enemy_01` |
| `_Recovery/0.unity` | 2 | Recovery |
| `_Recovery/0 (7).unity` | 1 | Recovery |

このBaselineより増えた場合は回帰です。既存分も、正しいScript型を特定せずRemoveしません。

## 6. Prefab validation

- Prefab Modeで開いてMissing Scriptがない。
- VariantのBase Prefabが存在する。
- Overrideが意図したものだけ。
- Scene Objectへの不正参照がない。
- ルートComponent契約が維持されている。
- Model PrefabのMaterial remapが壊れていない。
- Animator ControllerとAvatarが存在する。
- Collider／Layer／Tagが用途に合う。

## 7. Asset Database validation

### meta completeness

検査対象:

- `Assets`配下の全ファイルに`<file>.meta`がある。
- `Assets`配下の全フォルダに`<folder>.meta`がある。
- 対象本体がないorphan `.meta`がない。

GitHub Actionsのmeta-checkは、空白や日本語を含むパスを扱うためNUL区切りで検査します。

### GUID uniqueness

全`.meta`の`guid:`を収集し、重複が0件であることを確認します。

GUID重複がある場合:

1. どちらが元Assetかを確認する。
2. 参照元を調査する。
3. `.meta`を無条件に再生成しない。
4. 正しいCanonical GUIDへ参照を統合する。
5. Scene／Prefabを再検査する。

### Dependency stability

アセット移動前後で`AssetDatabase.GetDependencies`から得られる依存AssetのGUID集合を比較します。

- Folder移動でも中のAsset GUIDが維持されること。
- Scene／PrefabのSerialized referenceが変わっていないこと。
- Destination collisionがないこと。
- 検証失敗時にRollbackできること。

## 8. Build Settings validation

Build Settingsで確認する値:

- Scene path
- Scene GUID
- enabled
- order／build index

Scene移動ではpathだけが変わり、GUID、enabled、orderは維持されることが基本です。

Title、Loading、Boot、GameplayShell、Stage、UI_HUD、DebugOverlay、Roguelike、Result、WavePlaytestBootを照合します。

## 9. Resources validation

現在の`Resources.Load`利用はLoading UIです。

- `Textures/Title/UI_Title_Logo/`
- `Textures/GAMECLEAR/`

Resources配下を移動した場合:

1. コード内のロード文字列を検索する。
2. 拡張子なしの相対パスを照合する。
3. Sprite Importが維持されているか。
4. Title／Loading Sceneで実表示を確認する。

## 10. Asset Intake tests

最低ケース:

- 正常なasset／meta pairを読める。
- `Assets/../` traversalを拒否する。
- `Assets`外のpathを拒否する。
- `.cs`、`.asmdef`、`.dll`等を拒否する。
- 既存GUIDを拒否する。
- Destination collisionを拒否する。
- 同一Destinationへの複数Planを拒否する。
- Move後にGUIDが維持される。
- Dependency mismatch時にRollbackする。

実Package受け入れでは、テストに加えてImport Settingsと見た目を確認します。

## 11. Gameplay smoke test

Title起点で次を確認します。

1. Titleが表示される。
2. Start操作が1回だけ受理される。
3. Loadingが表示される。
4. Boot／GameplayShell／Stage／UIがロードされる。
5. Playerが入力に反応する。
6. Collectibleを収集できる。
7. UIの保持状態が更新される。
8. Enemyへ結果を与えられる。
9. GameClearまたはGameOverへ遷移する。
10. ResultからTitleへ戻れる。
11. 2回目のRunで前回状態が残らない。

## 12. Performance checks

Profilerで確認する項目:

- Main Thread frame time
- Physics step
- GC Alloc per frame
- Instantiate／Destroy count
- Active Rigidbody／Collider count
- UI rebuild
- VFX／Particle count
- Draw calls／SetPass
- Memory増加

物量テストでは少量時だけでなく、想定上限と上限超過時を確認します。

## 13. PR test report template

```markdown
## 検証

- Unity: 6000.3.7f1
- Console: Error 0 / Warning X
- EditMode: X / X passed
- PlayMode: X / X passed
- Scenes:
  - <scene>: Missing Script 0 / Broken Prefab 0
- Build Settings: 変更なし / 変更内容
- meta: missing 0 / orphan 0 / duplicate GUID 0
- Manual smoke test: 実施 / 未実施
- Build: 対象Platformと結果 / 未実施理由
```

## 14. Release／merge前チェックリスト

- [ ] Console Error 0
- [ ] 新規Warningを説明できる
- [ ] EditMode成功
- [ ] 必要なPlayMode成功
- [ ] 対象SceneのMissing Script増加なし
- [ ] Broken Prefab 0
- [ ] Missing／orphan meta 0
- [ ] Duplicate GUID 0
- [ ] Build Settings整合
- [ ] Resources文字列ロード確認
- [ ] Title起点Smoke Test
- [ ] 必要なPlatform build
- [ ] 未検証事項をPRへ記載
