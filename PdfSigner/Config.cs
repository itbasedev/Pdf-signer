using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentSigner
{
    public class Config
    {
        [JsonProperty("TSAClient")]
        public string? TSAClient { get; set; } = default!;
    }
}
