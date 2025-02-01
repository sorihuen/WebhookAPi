using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaypalApi.Models
{
    public class TokenJson
    {
        public string scope { get; set; } = string.Empty;
        public string access_token { get; set; } = string.Empty;
        public string token_type { get; set; } = string.Empty;
        public string app_id { get; set; } = string.Empty;
        public int expires_in { get; set; } = 0;
        public string nonce { get; set; } = string.Empty;
}
}