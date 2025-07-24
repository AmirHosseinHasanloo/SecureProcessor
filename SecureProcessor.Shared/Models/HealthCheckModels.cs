using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Shared.Models
{
    /// <summary>
    /// Request model for health check
    /// </summary>
    public class HealthCheckRequest
    {
        public string Id { get; set; }
        public DateTime SystemTime { get; set; }
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
