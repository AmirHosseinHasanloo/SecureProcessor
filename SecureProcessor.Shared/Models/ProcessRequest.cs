using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Shared.Models
{
    public class ProcessRequest
    {
        public string RequestId { get; set; }
        public Message Message { get; set; }
        public string RequesterId { get; set; }
        public DateTime RequestTime { get; set; } = DateTime.UtcNow;
    }
}
