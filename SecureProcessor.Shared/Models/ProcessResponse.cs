using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Shared.Models
{
    public class ProcessResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public int? AssignedProcessorId { get; set; }
        public DateTime ResponseTime { get; set; } = DateTime.UtcNow;
    }
}
