namespace Hydra4NET
{
    public interface IRedisConfig
    {
        int Db { get; }
        string? Host { get; }
        string? Options { get; }
        int? Port { get; }

        /// <summary>
        /// Gets the hostname and port (if configured) for the redis instance
        /// </summary>
        /// <returns></returns>
        string GetRedisHost();
    }
}