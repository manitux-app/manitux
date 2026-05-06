using System;
using System.Collections.Generic;
using System.Text;

namespace TlsClient.Core.Models.Entities
{
    public class TransportOptions
    {
        public TimeSpan? IdleConnTimeout { get; set; }
        public int MaxIdleConns { get; set; }
        public int MaxIdleConnsPerHost { get; set; }
        public int MaxConnsPerHost { get; set; }
        public long MaxResponseHeaderBytes { get; set; }
        public int WriteBufferSize { get; set; }
        public int ReadBufferSize { get; set; }
        public bool DisableKeepAlives { get; set; }
        public bool DisableCompression { get; set; }
    }
}
