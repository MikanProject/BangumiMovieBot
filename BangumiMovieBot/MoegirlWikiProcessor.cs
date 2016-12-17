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
using Newtonsoft.Json.Serialization;
using Formatting = Newtonsoft.Json.Formatting;

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

        public async Task GenerateBangumiDataAsync()
        {
            await GenerateBangumiDataAsync(await ReadFilesAsync());
        }

        public async Task GenerateBangumiDataAsync(List<MoegirlWikiInfo> list)
        {
            var finalList = new List<BangumiDataInfo>();
            foreach (var moegirlWikiInfo in list.Where(t => t.BangumiId == 0 && t.ReleaseDate < DateTime.UtcNow))
            {
                Console.WriteLine($"Warn: {moegirlWikiInfo.BangumiInfo.ChsName} has released, " +
                                  "but there is no BangumiId");
            }
            foreach (var moegirlWikiInfo in list.Where(t => t.BDReleaseDate != null && t.BangumiId != 0 && t.BangumiId != -1))
            {
                if (moegirlWikiInfo.BangumiInfo == null)
                {
                    moegirlWikiInfo.BangumiInfo =
                        await BangumiApiClient.GetSubjectAsync(moegirlWikiInfo.BangumiId.ToString());
                }
                finalList.Add(new BangumiDataInfo
                {
                    Title = moegirlWikiInfo.BangumiInfo.JpnName,
                    TitleTranslate = new Dictionary<string, List<string>>
                    {
                        {"zh-Hans", new List<string> {moegirlWikiInfo.BangumiInfo.ChsName}}
                    },
                    Lang = "ja",
                    OfficialSite = moegirlWikiInfo.BangumiInfo.OfficalHomePage,
                    // ReSharper disable once PossibleInvalidOperationException
                    Begin = moegirlWikiInfo.BDReleaseDate.Value,
                    End = (moegirlWikiInfo.BDReleaseDate + new TimeSpan(30*3, 0, 0, 0)).Value,
                    Comment = "",
                    Sites = new List<BangumiDataInfo.SiteInfo>
                    {
                        new BangumiDataInfo.SiteInfo
                        {
                            Site = "bangumi",
                            Id = moegirlWikiInfo.BangumiId.ToString()
                        }
                    },
                    ReleaseDate = moegirlWikiInfo.ReleaseDate,
                    BDReleaseDate = moegirlWikiInfo.BDReleaseDate.Value,
                    AnimeType = moegirlWikiInfo.BangumiInfo.AnimeType
                });
            }

            if (!Directory.Exists("data")) Directory.CreateDirectory("data");
            if (!Directory.Exists(Path.Combine("data", "items")))
                Directory.CreateDirectory(Path.Combine("data", "items"));
            var firstDate = finalList.OrderBy(t => t.BDReleaseDate).First();
            var lastDate = finalList.OrderByDescending(t => t.BDReleaseDate).First();
            foreach (var year in Enumerable.Range(firstDate.BDReleaseDate.Year,
                lastDate.BDReleaseDate.Year - firstDate.BDReleaseDate.Year + 1))
            {
                if (!Directory.Exists(Path.Combine("data", "items", year.ToString())))
                    Directory.CreateDirectory(Path.Combine("data", "items", year.ToString()));
                await WriteFilesAsync(finalList
                        .Where(t => t.BDReleaseDate.Year == year && t.AnimeType == "剧场版")
                        .OrderBy(t => t.BDReleaseDate),
                    Path.Combine("data", "items", year.ToString(), "movie.json"));
                await WriteFilesAsync(finalList
                        .Where(t => t.BDReleaseDate.Year == year && t.AnimeType == "OVA")
                        .OrderBy(t => t.BDReleaseDate),
                    Path.Combine("data", "items", year.ToString(), "ova.json"));
            }
        }

        public async Task<List<MoegirlWikiInfo>> AddReleaseDateAsync()
        {
            return await AddReleaseDateAsync(await ReadFilesAsync());
        }

        public async Task<List<MoegirlWikiInfo>> AddReleaseDateAsync(List<MoegirlWikiInfo> list)
        {
            foreach (var moegirlWikiInfo in list.Where(t => t.BDReleaseDate == null))
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
                if (nodes == null) continue;
                foreach (XmlNode node in nodes)
                {
                    try
                    {
                        moegirlWikiInfo.BDReleaseDate = DateTime.Parse(node.InnerText);
                        if (moegirlWikiInfo.BDReleaseDate < moegirlWikiInfo.ReleaseDate)
                        {
                            moegirlWikiInfo.BDReleaseDate = null;
                            continue;
                        }
                        break;
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
            return list.OrderBy(t=>t.ReleaseDate).ToList();
        }

        public async Task WriteFilesAsync<T>(T list)
        {
            await WriteFilesAsync(list, FilePath);
        }

        public async Task WriteFilesAsync<T>(T list, string savePath)
        {
            if (File.Exists(savePath)) File.Delete(savePath);
            using (var stream = File.CreateText(savePath))
            {
                await stream.WriteAsync(JsonConvert.SerializeObject(list, Formatting.Indented,
                    new JsonSerializerSettings {ContractResolver = new CamelCasePropertyNamesContractResolver()}));
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
                        t.ReleaseDate == DateTime.Parse(match.Groups["showDate"].Value) &&
                        (match.Groups["releaseDate"].Value == "" ||
                         t.ReleaseDate == DateTime.Parse(match.Groups["releaseDate"].Value)));
                    if (info != null) continue;
                    info = list.FirstOrDefault(t =>
                        t.Name == match.Groups["name"].Value &&
                        t.ReleaseDate == DateTime.Parse(match.Groups["showDate"].Value));
                    if (info == null)
                    {
                        info = new MoegirlWikiInfo
                        {
                            Name = match.Groups["name"].Value,
                            ReleaseDate = DateTime.Parse(match.Groups["showDate"].Value)
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
                            else throw;
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
                        info.BDReleaseDate = DateTime.Parse(match.Groups["releaseDate"].Value);
                    }
                    // ReSharper disable once EmptyGeneralCatchClause
                    catch
                    {
                    }
                }
            }
            return list.OrderBy(t => t.ReleaseDate).ToList();
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
        public DateTime ReleaseDate { get; set; }

        /// <summary>
        /// BD发售日期
        /// </summary>
        public DateTime? BDReleaseDate { get; set; }

        /// <summary>
        /// Bangumi Id，0为尚无数据，-1为无效数据
        /// </summary>
        public int BangumiId { get; set; }

        /// <summary>
        /// Bangumi信息
        /// </summary>
        public SubjectInfo BangumiInfo { get; set; }
    }

    public class BangumiDataInfo
    {
        public class SiteInfo
        {
            public string Site { get; set; }
            public string Id { get; set; }
        }

        /// <summary>
        /// 番组原始标题 [required]
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 番组标题翻译 [required]
        /// </summary>
        public Dictionary<string,List<string>> TitleTranslate { get; set; }

        /// <summary>
        /// 番组语言 [required]
        /// </summary>
        public string Lang { get; set; }

        /// <summary>
        /// 官网 [required]
        /// </summary>
        public string OfficialSite { get; set; }

        /// <summary>
        /// 番组开始时间，还未确定则置空 [required]
        /// </summary>
        public DateTime Begin { get; set; }

        /// <summary>
        /// 番组开始时间，还未确定则置空 [required]
        /// </summary>
        public DateTime End { get; set; }

        /// <summary>
        /// 上映日期
        /// </summary>
        public DateTime ReleaseDate { get; set; }

        /// <summary>
        /// BD发售日期
        /// </summary>
        public DateTime BDReleaseDate { get; set; }

        /// <summary>
        /// 类型（剧场版或OVA）
        /// </summary>
        public string AnimeType { get; set; }

        /// <summary>
        /// 备注 [required]
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// 站点 [required]
        /// </summary>
        public List<SiteInfo> Sites { get; set; }
    }
}
