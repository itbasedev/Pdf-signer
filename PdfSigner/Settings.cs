using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace DocumentSigner
{
    public static class Settings
    {
        public static readonly byte[] SHA256_HASH_PREFIX = {
                                                              0x30, 0x31, 0x30, 0x0d, 0x06, 0x09,
                                                              0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01,
                                                              0x05, 0x00, 0x04, 0x20
        };

        public static string TSA_CLIENT = "https://freetsa.org/tsr";
        public static string SIGNATURE_NAME = "ITBASE";
    }
}
