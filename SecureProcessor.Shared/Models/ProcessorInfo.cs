using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Shared.Models
{
    public class ProcessorInfo
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public bool IsActive { get; set; }
        public DateTime LastActivity { get; set; }
    }
}

