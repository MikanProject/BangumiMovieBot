using Newtonsoft.Json;

namespace BangumiApi.Model
{
    public class Cover
    {
        [JsonProperty("large")]
        public string LargeCover { get; set; }

        [JsonProperty("common")]
        public string CommonCover { get; set; }

        [JsonProperty("medium")]
        public string MediumCover { get; set; }

        [JsonProperty("small")]
        public string SmallCover { get; set; }

        [JsonProperty("grid")]
        public string GridCover { get; set; }
    }
}