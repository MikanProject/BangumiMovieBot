using System;
using Newtonsoft.Json;

namespace BangumiApi.Model
{
    public class SubjectInfo
    {
        /// <summary>
        /// BangumiId
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Bangumi链接
        /// </summary>
        [JsonProperty("url")]
        public string BangumiUrl { get; set; }

        /// <summary>
        /// 日文名
        /// </summary>
        [JsonProperty("name")]
        public string JpnName { get; set; }

        /// <summary>
        /// 中文名
        /// </summary>
        [JsonProperty("name_cn")]
        public string ChsName { get; set; }

        /// <summary>
        /// 封面
        /// </summary>
        [JsonProperty("images")]
        public Cover Cover { get; set; }

        /// <summary>
        /// 类型
        /// </summary>
        [JsonProperty("type")]
        public int Type { get; set; }

        /// <summary>
        /// 简介
        /// </summary>
        public string Desctiption { get; set; }

        /// <summary>
        /// 话数
        /// </summary>
        public string TotalEpisode { get; set; }

        /// <summary>
        /// 放送开始
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// 放送开始
        /// </summary>
        [JsonProperty("air_date")]
        private string StartDateString
        {
            set
            {
                DateTime temp;
                DateTime.TryParse(value, out temp);
                StartDate = temp;
            }
        }

        /// <summary>
        /// 官方网站
        /// </summary>
        public string OfficalHomePage { get; set; }

        /// <summary>
        /// 星期几
        /// </summary>
        public DayOfWeek DayOfWeek { get; set; }

        /// <summary>
        /// 放送类型
        /// </summary>
        public string AnimeType { get; set; }
    }
}