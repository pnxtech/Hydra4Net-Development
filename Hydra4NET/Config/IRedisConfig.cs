namespace Hydra4NET
{
    public interface IRedisConfig
    {
        int Db { get; }
        string? Host { get; }
        string? Options { get; }
        int Port { get; }
    }
}