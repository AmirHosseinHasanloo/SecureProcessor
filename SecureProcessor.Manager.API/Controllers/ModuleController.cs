using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SecureProcessor.Shared.Models;

namespace SecureProcessor.Manager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModuleController : ControllerBase
    {
        private readonly ILogger<ModuleController> _logger;

        public ModuleController(ILogger<ModuleController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Health check endpoint for system monitoring
        /// </summary>
        [HttpPost("health")]
        public IActionResult HealthCheck([FromBody] HealthCheckRequest request)
        {
            _logger.LogInformation($"Health check received from client {request.Id}");

            var response = new HealthCheckResponse
            {
                IsEnabled = true,
                NumberOfActiveClients = new Random().Next(0, 6),
                ExpirationTime = DateTime.UtcNow.AddMinutes(10)
            };

            _logger.LogInformation($"Health check response sent: IsEnabled={response.IsEnabled}, ActiveClients={response.NumberOfActiveClients}");
            return Ok(response);
        }
    }
}
