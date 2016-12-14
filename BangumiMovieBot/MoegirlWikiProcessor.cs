using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using BangumiApi;
using BangumiApi.Model;
using Newtonsoft.Json;

namespace BangumiMovieBot
{
    public class MoegirlWikiProcessor
    {
        private string DataUrl { get; } = "https://zh.moegirl.org/index.php?title=%E6%97%A5%E6%9C%AC{0}%E5%B9%B4%E5%89%A7%E5%9C%BA%E7%89%88%E5%8A%A8%E7%94%BB&action=edit";

        private string ReleaseDateQueryUrl { get; } = "http://www.gameiroiro.com/search/ajax_search.php?type=amazon&num=4&keyword={0}&cat=&noimage=0";

        private string FilePath { get; } = "wiki_data.json";

        private Regex Regex { get; } =
            new Regex(@"^==(?<name>[^=]*)==[^=]*?上映日期：(?<showDate>.*?) .*?发售日期：(?<releaseDate>.*?)[(\n]",
                RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled);

        private BangumiApiClient BangumiApiClient { get; } = new BangumiApiClient();

        private HttpClient HttpClient { get; } = new HttpClient();

        public async Task<List<MoegirlWikiInfo>> ReadFilesAsync()
        {
            if (!File.Exists(FilePath)) return new List<MoegirlWikiInfo>();
            using (var stream = File.OpenText(FilePath))
            {
                return JsonConvert.DeserializeObject<List<MoegirlWikiInfo>>(await stream.ReadToEndAsync());
            }
        }

        public async Task<List<MoegirlWikiInfo>> AddReleaseDateAsync()
        {
            return await AddReleaseDateAsync(await ReadFilesAsync());
        }

        public async Task<List<MoegirlWikiInfo>> AddReleaseDateAsync(List<MoegirlWikiInfo> list)
        {
            foreach (var moegirlWikiInfo in list.Where(t => t.ReleaseDate == null))
            {
                if (moegirlWikiInfo.BangumiInfo == null)
                {
                    if (moegirlWikiInfo.BangumiId == 0 || moegirlWikiInfo.BangumiId == -1) continue;
                    moegirlWikiInfo.BangumiInfo = await BangumiApiClient.GetSubjectAsync(moegirlWikiInfo.BangumiId.ToString());
                }
                var resultXml = new XmlDocument();
                resultXml.Load(string.Format(ReleaseDateQueryUrl,
                    WebUtility.UrlEncode(moegirlWikiInfo.BangumiInfo.JpnName.Replace("＆", " "))));
                var nodes = resultXml.SelectNodes("/items/item/date[(../binding='Blu-ray') or (../binding='DVD')]");
                foreach (XmlNode node in nodes)
                {
                    try
                    {
                        moegirlWikiInfo.ReleaseDate = DateTime.Parse(node.InnerText);
                        break;
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
            return list;
        }

        public async Task WriteFilesAsync(List<MoegirlWikiInfo> list)
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
            using (var stream = File.CreateText(FilePath))
            {
                await stream.WriteAsync(JsonConvert.SerializeObject(list));
            }
        }


        public async Task<List<MoegirlWikiInfo>> GetMovieInfoAsync(int startYear, int endYear)
        {
            return await GetMovieInfoAsync(startYear, endYear, await ReadFilesAsync());
        }

        public async Task<List<MoegirlWikiInfo>> GetMovieInfoAsync(int startYear,int endYear, List<MoegirlWikiInfo> list)
        {
            for (var i = startYear; i <= endYear; i++)
            {
                var html = await HttpClient.GetStringAsync(string.Format(DataUrl, i));
                foreach (Match match in Regex.Matches(html))
                {
                    DateTime temp;
                    if (!DateTime.TryParse(match.Groups["showDate"].Value, out temp))
                    {
                        Console.WriteLine($"Error in {match.Groups["name"].Value}");
                        continue;
                    }
                    var info = list.FirstOrDefault(t =>
                        t.Name == match.Groups["name"].Value &&
                        t.ShowDate == DateTime.Parse(match.Groups["showDate"].Value) &&
                        (match.Groups["releaseDate"].Value == "" ||
                         t.ShowDate == DateTime.Parse(match.Groups["releaseDate"].Value)));
                    if (info != null) continue;
                    info = list.FirstOrDefault(t =>
                        t.Name == match.Groups["name"].Value &&
                        t.ShowDate == DateTime.Parse(match.Groups["showDate"].Value));
                    if (info == null)
                    {
                        info = new MoegirlWikiInfo
                        {
                            Name = match.Groups["name"].Value,
                            ShowDate = DateTime.Parse(match.Groups["showDate"].Value)
                        };
                        list.Add(info);
                    }
                    if (info.BangumiId == 0)
                    {
                        SearchResultModel searchResult;
                        try
                        {
                            searchResult = await BangumiApiClient.SearchSubjectAsync(
                                new SearchSubjectModel {Keyword = match.Groups["name"].Value});
                        }
                        catch (BangumiApiException exception)
                        {
                            if (exception.Message.ToLower() == "not found") searchResult = new SearchResultModel();
                            else throw exception;
                        }
                        if (searchResult.Count != 0)
                        {
                            foreach (var subjectInfo in searchResult.SubjectInfo)
                            {
                                if (subjectInfo.StartDate == DateTime.Parse(match.Groups["showDate"].Value))
                                {
                                    info.BangumiInfo = subjectInfo;
                                    info.BangumiId = subjectInfo.Id;
                                    Console.WriteLine($"I suppose movie {match.Groups["name"].Value} is {info.BangumiInfo.ChsName} in bgm.tv");
                                    break;
                                }
                            }
                        }
                    }
                    try
                    {
                        info.ReleaseDate = DateTime.Parse(match.Groups["releaseDate"].Value);
                    }
                    // ReSharper disable once EmptyGeneralCatchClause
                    catch
                    {
                    }
                }
            }
            return list;
        }
    }

    public class MoegirlWikiInfo
    {
        /// <summary>
        /// 萌娘百科上的名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 上映日期
        /// </summary>
        public DateTime ShowDate { get; set; }

        /// <summary>
        /// BD发售日期
        /// </summary>
        public DateTime? ReleaseDate { get; set; }

        /// <summary>
        /// Bangumi Id，0为尚无数据，-1为无效数据
        /// </summary>
        public int BangumiId { get; set; }

        /// <summary>
        /// Bangumi信息
        /// </summary>
        [JsonIgnore]
        public SubjectInfo BangumiInfo { get; set; }
    }
}
