using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SecureProcessor.Shared.Models
{
    /// <summary>
    /// Request model for health check
    /// </summary>
    public class HealthCheckRequest
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("systemTime")]
        public long SystemTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        [JsonPropertyName("numberOfConnectedClients")]
        public int NumberOfConnectedClients { get; set; }
    }

    /// <summary>
    /// Response model for health check
    /// </summary>
    public class HealthCheckResponse
    {
        public bool IsEnabled { get; set; }
        public int NumberOfActiveClients { get; set; }
        public DateTime ExpirationTime { get; set; }
    }
}
