# Scenes

[← README](../README.md) | [Architecture](ARCHITECTURE.md) | [Testing →](TESTING.md)

## 1. Scene分類

```text
Assets/CreatorKousien/Scenes/
├── Application/
│   ├── Title.unity
│   ├── Loading.unity
│   ├── Boot.unity
│   ├── GameplayShell.unity
│   ├── Result.unity
│   └── Legacy/
├── Gameplay/
│   ├── Stage.unity
│   ├── UI_HUD.unity
│   ├── DebugOverlay.unity
│   └── Roguelike.unity
└── Development/
    ├── Collectibles/
    ├── Combo/
    ├── Enemies/
    ├── Player/
    ├── PrefabAuthoring/
    ├── Stage/
    ├── StageEditor/
    └── UI/
```

## 2. Build Settings

2026-07-20時点の登録順です。Scene名ロードを使うコードがあるため、名前と登録状態を変更するときは呼び出し元も確認します。

| Index | Scene | GUID | Enabled | 責務 |
| ---: | --- | --- | :---: | --- |
| 0 | `Application/Title` | `1450679543521814a87f43f45bd6c1cb` | Yes | エントリ画面 |
| 1 | `Application/Loading` | `c3a9f5d6e1724b4e9238d7a1f6c0b5e4` | Yes | 遷移中表示と初期ロード |
| 2 | `Application/Boot` | `686ca123f808c3946ab11552a54e63a3` | Yes | サービス／Scene flow起動 |
| 3 | `Application/GameplayShell` | `415e201eeb64fd247a7984009a6c5d57` | Yes | Additive Sceneの接続点 |
| 4 | `Gameplay/Stage` | `9f0232278e1390c43931d080e2cb854c` | Yes | メインStage |
| 5 | `Gameplay/UI_HUD` | `c78b4ef46c3b50e41ba33bbeefd38003` | Yes | Gameplay HUD |
| 6 | `Gameplay/DebugOverlay` | `16e59e1f2b1985243b6877079711dbc4` | Yes | Debug表示・操作 |
| 7 | `Gameplay/Roguelike` | `01bd6769c4caa744ab4468f11752ac15` | Yes | Upgrade選択／Roguelike UI |
| 8 | `Application/Result` | `35e170c8581fee74eba0ef47281fcbc5` | Yes | Result／GameClear／GameOver後 |
| 9 | `Development/StageEditor/WavePlaytestBoot` | `881adaf056568d0449f97aceba5892c7` | Yes | Wave検証起動 |

## 3. Scene flow

```mermaid
sequenceDiagram
    participant Title
    participant Loading
    participant Boot
    participant Shell as GameplayShell
    participant Stage
    participant HUD as UI_HUD
    participant Debug as DebugOverlay
    participant Result

    Title->>Loading: Start
    Loading->>Boot: Load additive
    Boot->>Shell: Compose gameplay
    Shell->>Stage: Load additive
    Shell->>HUD: Load additive
    Shell->>Debug: Load additive
    Stage->>Result: Clear / Game over
    Result->>Title: Return
```

実際の遷移コードにはScene名文字列が使われています。

- `PrototypeSceneFlowController`
- `LoadingFlowController`
- `GameProgressionManager`
- `GameResetManager`
- `TitleMenuController`
- `ResultFlowController`
- `GameOverCinematicController`

Scene名を変更する場合は、Build SettingsだけでなくSerializeField初期値と文字列リテラルを検索してください。

## 4. Sceneごとの責務

### Title

- ゲーム開始入力を受ける。
- Loading Sceneへ遷移する。
- GameplayのManagerやStage Objectを置かない。

### Loading

- Bootや必要Sceneのロードを開始する。
- ロード中の表示を担当する。
- `Resources.Load`されるTitle／GAMECLEAR Textureを利用する。

### Boot

- 起動時に必要なServiceとScene Flowを準備する。
- Stage固有Objectを持たない。
- 重複生成を防ぐ。

### GameplayShell

- Player、Stage、UI、DebugなどAdditive Scene間を接続する。
- Game全体の進行を構成する。
- 個別Stageのアートを直接持たない。

### Stage

- Field、Enemy、Collectible、Wave、Stage Dataを構成する。
- UI CanvasやTitle遷移を直接所有しない。
- Stage固有Objectと共通Prefabを区別する。

### UI_HUD

- Gameplay状態を表示する。
- ゲームルールを直接書き換えない。
- Event／Snapshot／Presenterを介して更新する。

### DebugOverlay

- 検証値、強制操作、状態可視化を提供する。
- 製品ルールの成立に必須の処理を置かない。
- Debug無効化でGameplayが壊れないようにする。

### Roguelike

- Upgrade候補、選択、所持金、結果遷移を表示・制御する。
- StageのGameObject階層へ直接依存しない。

### Result

- GameClear／GameOver後の表示とTitleへの復帰を担当する。
- 前回Runの一時状態を適切にResetする。

## 5. Development Scenes

Development Sceneは機能単体の検証用です。

| 分類 | 代表Scene | 用途 |
| --- | --- | --- |
| Player | `Proto_PlayerBase`, `Proto_PlayerCollectTest` | 移動、収集、Player構成 |
| Collectibles | `Proto_Collectable_Compress`, `Proto_D_PayloadMutation` | 圧縮、変化、収集物 |
| Enemies | `EnemyTestBoot`, `Stage_EnemyTest` | Enemy、Spawn、Stage接続 |
| Stage | `Test_Stage`, `Stage_Prototype_01`, `Proto_FALU_Field` | Field、Stage Art、Stage Data |
| Combo | `Falu_combo_bonus`など | Combo UIと演出 |
| UI | `Proto_G_UIDebug` | UI検証。既存Missing Scriptあり |
| PrefabAuthoring | `Falu_Title_Prefab` | Title Prefab作成 |
| StageEditor | `WavePlaytestBoot` | Wave Data検証 |

Development Sceneのルール:

- 単体で開いたときの必要MockをScene内で完結させる。
- 本番PrefabへDevelopment Scene Objectを参照させない。
- 検証結果はPrefab、ScriptableObject、Runtimeコードとして本番へ渡す。
- 個人用CameraやDebug UIを本番Prefabへ含めない。
- Missing Scriptを含むSceneを複製して新しい検証Sceneを作らない。

## 6. Scene ownership

Scene YAMLは競合解消が難しいため、同じ共有Sceneを複数人で同時編集しません。

| Scene分類 | 編集方針 |
| --- | --- |
| Application | Scene flow担当または統合担当が編集 |
| GameplayShell | 統合担当が最終編集 |
| Stage | Stage担当と統合担当で編集タイミングを調整 |
| UI_HUD | UI担当が編集し、Gameplay側はPrefab／Event契約で接続 |
| DebugOverlay | Debug担当が編集 |
| Development | 機能Ownerが自由に編集可能 |

共有Scene変更を含むPRでは、変更したHierarchyと目的をPR本文へ記載します。

## 7. Sceneを追加する

1. Application、Gameplay、Developmentのどれかを決める。
2. 既存Sceneを複製する場合、不要なManager／Camera／EventSystemを削除する。
3. Sceneを正しいフォルダへ保存する。
4. `.meta`を確認する。
5. 必要な場合だけBuild Settingsへ追加する。
6. Scene名文字列、SceneCatalog、Build Index依存を確認する。
7. Missing Script／Broken Prefabを検査する。
8. 単体再生とTitle起点の両方を確認する。

## 8. Sceneを移動・改名する

- Unity Editorから移動する。
- 移動前のScene GUIDを記録する。
- Build Settingsの順序、enabled、GUIDを前後比較する。
- `SceneManager.LoadScene*`の文字列を検索する。
- SerializeFieldで設定されたScene名もInspectorで確認する。
- AddressablesやBuild ProfileにScene参照がないか確認する。
- 全利用SceneのMissing Script／Broken Prefabを再検査する。

## 9. Scene検証チェックリスト

- [ ] Sceneが保存済みでDirtyでない
- [ ] Missing Scriptが新規発生していない
- [ ] Broken Prefabがない
- [ ] Main Cameraが重複していない
- [ ] EventSystemが重複していない
- [ ] Global Managerが重複していない
- [ ] Build Settingsの順序・enabled・GUIDが正しい
- [ ] Additive Load／Unloadが成立する
- [ ] Title起点で到達できる
- [ ] ResultからTitleへ戻れる
- [ ] Play停止後にScriptableObjectへ実行時状態が残らない
