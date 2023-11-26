using Caching.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane;

namespace Caching.Config
{
    public class ConfigurationRepository : IConfigurationRepository
    {
        private readonly IFusionCache _cache;
        private readonly ILogger<ConfigurationRepository> _logger;
        //private readonly IFusionCacheBackplane _cacheBackplane;
        private readonly IOptions<FusionCacheOptions> _cacheOptions;
        private ConcurrentDictionary<string, string> db = new ConcurrentDictionary<string, string>();

        public ConfigurationRepository(
            IFusionCache cache,
            ILogger<ConfigurationRepository> logger,
            //IFusionCacheBackplane cacheBackplane,
            IOptions<FusionCacheOptions> cacheOptions)
        {

            _cache = cache;
            _logger = logger;
            //_cacheBackplane = cacheBackplane;

            //_cacheBackplane.Subscribe(new BackplaneSubscriptionOptions(
            //    channelName: GetBackplaneChannelName(cacheOptions.Value),
            //    connectHandler: conn =>
            //    {
            //        _logger.LogInformation("connectHandler {@isReconnection}", conn.IsReconnection);
            //    }
            //    ,
            //    incomingMessageHandler: message =>
            //    {
            //        _logger.LogInformation("incomingMessageHandler {@message}", message.CacheKey);
            //    }));

            _cache.Events.Backplane.MessageReceived += (s, e) =>
            {
                Refresh(skipBackplaneNotifications: true);
                _logger.LogInformation("MessageReceived @{action} {@cacheKey}", e.Message.Action, e.Message.CacheKey);
            };
            cache.Events.Backplane.MessagePublished += (s, e) =>
            {
                _logger.LogInformation("MessagePublished @{action} {@cacheKey}", e.Message.Action, e.Message.CacheKey);
            };

            Refresh();
            _cacheOptions = cacheOptions;
        }

        private static string GetBackplaneChannelName(FusionCacheOptions options)
        {
            var prefix = options.BackplaneChannelPrefix;
            if (string.IsNullOrWhiteSpace(prefix))
                prefix = options.CacheName;

            // SAFETY NET (BUT IT SHOULD NOT HAPPEN)
            if (string.IsNullOrWhiteSpace(prefix))
                prefix = "FusionCache";

            return $"{prefix}.Backplane";
        }

        public T Get<T>(string key, T defaultValue = default)
        {
            _logger.LogInformation("Get key {@key}", key);

            var result = _cache.TryGet<string>(key);
            if (!result.HasValue)
            {
                return defaultValue;
            }
            return JsonSerializer.Deserialize<T>(result.Value)!;
        }

        private void Set<T>(string key, T value, bool skipBackplaneNotifications)
        {
            string valueSerialized = JsonSerializer.Serialize(value);
            db[key] = valueSerialized;
            //_cache.Remove(key);
            _cache.Set(key, valueSerialized, opts => 
                opts
                    .SetDurationInfinite()
                    .SetSkipBackplaneNotifications(skipBackplaneNotifications));
        }

        public void Refresh()
        {
            Refresh(skipBackplaneNotifications: false);
        }

        private void Refresh(bool skipBackplaneNotifications)
        {
            Set("Feature1", GetValueFromSource("Feature1"), skipBackplaneNotifications);
            Set("Feature2", GetValueFromSource("Feature2"), skipBackplaneNotifications);
            Set("Feature3", GetValueFromSource("Feature3"), skipBackplaneNotifications);
        }

        public void Regenerate()
        {
            string[] keys = new string[] { "Feature1", "Feature2", "Feature3" };
            List<string> lines = new List<string>();
            foreach(string key in keys)
            {
                lines.Add($"{key}:{Guid.NewGuid()}");
            }
            File.WriteAllLines(FilePath, lines);
        }

        private const string FilePath = @"c:\temp\cache.txt";

        private string GetValueFromSource(string key)
        {
            var lines = File.ReadAllLines(FilePath);
            foreach (var line in lines)
            {
                var splitted = line.Split(":");
                var cacheKey = splitted[0];
                if(key == cacheKey)
                {
                    return splitted[1];
                }
            }
            return null;
        }
    }
}
