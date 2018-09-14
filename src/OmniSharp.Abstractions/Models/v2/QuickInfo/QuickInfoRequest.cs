using Newtonsoft.Json;
using OmniSharp.Mef;

namespace OmniSharp.Models.V2.QuickInfo
{
    [OmniSharpEndpoint(OmniSharpEndpoints.V2.QuickInfo, typeof(QuickInfoRequest), typeof(QuickInfoResponse))]
    public class QuickInfoRequest : SimpleFileRequest
    {
        [JsonConverter(typeof(ZeroBasedIndexConverter))]
        public int Line { get; set; }
        [JsonConverter(typeof(ZeroBasedIndexConverter))]
        public int Column { get; set; }
    }
}
