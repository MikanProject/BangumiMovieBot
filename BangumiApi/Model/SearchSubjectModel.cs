namespace BangumiApi.Model
{
    public class SearchSubjectModel
    {
        /// <summary>
        /// 关键字
        /// </summary>
        public string Keyword { get; set; }

        /// <summary>
        /// 第一条返回结果的编号
        /// </summary>
        public int IndexStart { get; set; } = 0;

        /// <summary>
        /// 返回结果数目
        /// </summary>
        public int MaxCount { get; set; } = 20;
    }
}