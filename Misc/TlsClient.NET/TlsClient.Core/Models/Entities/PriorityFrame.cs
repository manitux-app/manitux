using System;
using System.Collections.Generic;
using System.Text;

namespace TlsClient.Core.Models.Entities
{
    // Reference: https://github.com/bogdanfinn/tls-client/blob/master/cffi_src/types.go#L110
    public class PriorityFrame
    {
        public PriorityParam PriorityParam { get; set; } = new PriorityParam();
        public uint StreamID { get; set; }
    }
    public class PriorityParam
    {
        public uint StreamDep { get; set; }
        public bool Exclusive { get; set; }
        public byte Weight { get; set; }
    }
}
