using System;
using System.Collections.Generic;
using System.Text;

namespace TlsClient.Core.Models.Requests
{
    public class DestroyRequest
    {
        public Guid SessionID { get; set; }
    }
}
