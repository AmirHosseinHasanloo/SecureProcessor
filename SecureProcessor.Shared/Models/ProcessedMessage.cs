using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Shared.Models
{
    /// <summary>
    /// Represents a processed message with analysis results
    /// </summary>
    public class ProcessedMessage
    {
        public int Id { get; set; }
        public string Engine { get; set; }
        public int MessageLength { get; set; }
        public bool IsValid { get; set; }
        public Dictionary<string, bool> AdditionalFields { get; set; } = new();
    }
}
