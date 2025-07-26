using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Shared.Models
{
    public class HealthCheckResponseModel
    {
        public bool IsEnabled { get; set; }
        public int NumberOfActiveClients { get; set; }
        public DateTime ExpirationTime { get; set; }
    }
}
