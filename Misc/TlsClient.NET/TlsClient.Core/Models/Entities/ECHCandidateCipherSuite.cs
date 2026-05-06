using System;
using System.Collections.Generic;
using System.Text;

namespace TlsClient.Core.Models.Entities
{
    // Reference: https://github.com/bogdanfinn/tls-client/blob/master/cffi_src/types.go#L124
    public class ECHCandidateCipherSuite
    {
        public string KdfID { get; set; } = string.Empty;
        public string AeadId { get; set; } = string.Empty;
    }
}
