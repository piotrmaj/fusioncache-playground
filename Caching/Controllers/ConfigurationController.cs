using Caching.Config;
using Microsoft.AspNetCore.Mvc;

namespace Caching.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConfigurationController : ControllerBase
    {
        private readonly ILogger<ConfigurationController> _logger;
        private readonly IConfigurationRepository configurationRepository;

        public ConfigurationController(
            ILogger<ConfigurationController> logger, 
            IConfigurationRepository configurationRepository)
        {
            _logger = logger;
            this.configurationRepository = configurationRepository;
        }

        [HttpGet()]
        public ActionResult Get(string key)
        {
            return Ok(configurationRepository.Get<object>(key));
        }

        [HttpPost, Route("refresh")]
        public ActionResult Refresh()
        {
            configurationRepository.Refresh();
            return Ok();
        }

        [HttpPost, Route("regenerate")]
        public ActionResult Regenerate()
        {
            configurationRepository.Regenerate();
            return Ok();
        }
    }
}
