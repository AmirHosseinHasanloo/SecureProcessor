using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Shared.Models
{
    public class HealthCheckRequestModel
    {
        public string Id { get; set; }
        public DateTime SystemTime { get; set; }
        public int NumberOfConnectedClients { get; set; }
    }
}
