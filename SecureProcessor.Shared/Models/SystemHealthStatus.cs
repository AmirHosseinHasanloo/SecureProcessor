using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Shared.Models
{
    public class SystemHealthStatus
    {
        public bool IsEnabled { get; set; }
        public int ActiveClients { get; set; }
        public DateTime ExpirationTime { get; set; }
    }
}
