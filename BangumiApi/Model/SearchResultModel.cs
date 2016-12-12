using System.Collections.Generic;
using Newtonsoft.Json;

namespace BangumiApi.Model
{
    public class SearchResultModel
    {
        /// <summary>
        /// 返回结果的实际数量
        /// </summary>
        [JsonProperty("results")]
        public int Count { get; set; }

        /// <summary>
        /// 搜索结果列表
        /// </summary>
        [JsonProperty("list")]
        public List<SubjectInfo> SubjectInfo { get; set; }
    }
}