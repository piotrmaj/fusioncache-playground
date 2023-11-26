namespace Caching.Config
{
    public interface IConfigurationRepository
    {
        T Get<T>(string key, T defaultValue = default);
        void Refresh();
        void Regenerate();
    }
}