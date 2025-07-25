using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Shared.Models
{
    public class SystemConfiguration
    {
        public int MaxActiveClients { get; set; } = 5;
        public int HealthCheckInterval { get; set; } = 30;
        public bool IsEnabled { get; set; } = true;
    }
}
