//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : UpgradeRuntimeEntry.cs
// brief  : 強化の分類
//
// auther : Takitani Shohei
// date   : 2026/07/01 - begin.
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/

public enum UpgradeCategory
{
    Player,     // プレイヤー強化 - 移動速度
    Drop,       // ドロップ強化 - 出現数
    Engine,     // コンボ・状態異常を起点にするビルドエンジン
    Relic,      // 生成や抽選のルール変更
}
