using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Shared.Models
{
        /// <summary>
        /// Represents a raw message from the queue
        /// </summary>
        public class Message
        {
            public int Id { get; set; }
            public string Sender { get; set; }
            public string Content { get; set; }
        }
    }
