namespace Game.Core
{
    /// <summary>
    /// 全てのドメインエンティティの基底インターフェース
    /// </summary>
    public interface IEntity { }

    /// <summary>
    /// システムの状態を保持するデータの基底インターフェース
    /// </summary>
    public interface IRuntimeData { }

    /// <summary>
    /// システムの振る舞いを定義するインターフェース
    /// </summary>
    public interface ISystem
    {
        void Initialize();
    }
}