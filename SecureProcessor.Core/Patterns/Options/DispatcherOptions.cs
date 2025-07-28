using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Core.Patterns.Options
{
    public class DispatcherOptions
    {
        public int MaxActiveProcessors { get; set; } = 5;
        public int HealthCheckInterval { get; set; } = 30;
        public string ManagerServiceUrl { get; set; } = "https://localhost:7138/api/Module/health";
    }
}
