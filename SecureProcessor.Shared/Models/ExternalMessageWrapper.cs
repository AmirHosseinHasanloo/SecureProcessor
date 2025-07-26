using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Shared.Models
{
    public class ExternalMessageWrapper
    {
        public Message Message { get; set; }
        public string RequestId { get; set; }
        public string RequesterId { get; set; }
        public DateTime ReceivedTime { get; set; }
    }
}
