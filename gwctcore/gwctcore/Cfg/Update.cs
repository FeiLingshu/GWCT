using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace GWCT.Cfg
{
    /// <summary>
    /// 提供更新检查功能
    /// </summary>
    public static class Update
    {
        /// <summary>
        /// Gitee更新数据地址
        /// </summary>
        private const string GiteeLink = "https://gitee.com/FeiLingshu/GWCT_mirror/raw/master/version_id=";
        /// <summary>
        /// GitHub更新数据地址
        /// </summary>
        private const string GitHubLink = "https://github.com/FeiLingshu/GWCT/raw/refs/heads/main/version_id=";

        /// <summary>
        /// 指示 <see href="Http"/> 响应状态
        /// </summary>
        public enum HttpStatus
        {
            /// <summary>
            /// 成功
            /// </summary>
            OK,
            /// <summary>
            /// 文件未找到
            /// </summary>
            NotFound,
            /// <summary>
            /// 超时
            /// </summary>
            Timeout,
            /// <summary>
            /// <see href="Http"/> 错误
            /// </summary>
            HttpError,
            /// <summary>
            /// 未知错误
            /// </summary>
            Unknown
        }
        /// <summary>
        /// 指示 <see href="Http"/> 响应结果
        /// </summary>
        public struct HttpReport
        {
            /// <summary>
            /// <see href="Http"/> 响应状态
            /// </summary>
            public HttpStatus Status;
            /// <summary>
            /// <see href="Http"/> 响应代码
            /// </summary>
            public int Code;
            /// <summary>
            /// 初始化 <see cref="HttpReport"/> 新实例
            /// </summary>
            /// <param name="Status"><see href="Http"/> 响应状态</param>
            /// <param name="Code"><see href="Http"/> 响应代码</param>
            public HttpReport(HttpStatus Status, int Code)
            {
                this.Status = Status;
                this.Code = Code;
            }
        }

        /// <summary>
        /// 获取更新信息
        /// </summary>
        /// <param name="version">版本标志</param>
        /// <param name="timeoutMilliseconds">超时时间</param>
        /// <returns>返回包含执行结果的 <see cref="Task"/> 实例</returns>
        public static async Task<HttpReport> CheckUpdateAsync(Version version, int timeoutMilliseconds = 3000)
        {
            bool CN = false;
            try
            {
                TimeZoneInfo localZone = TimeZoneInfo.Local;
                DateTimeOffset now = DateTimeOffset.Now;
                TimeSpan offset = localZone.GetUtcOffset(now);
                CN = localZone.Id == "China Standard Time" && offset.TotalHours == 8 && !localZone.IsDaylightSavingTime(now.DateTime);
            }
            catch (Exception) { }
            string url = $"{(CN ? GiteeLink : GitHubLink)}{version}";
            try
            {
                using (var handler = new HttpClientHandler())
                {
                    handler.AllowAutoRedirect = true;
                    using (var client = new HttpClient(handler))
                    {
                        client.Timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
                        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                        client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9,zh-CN;q=0.8,zh;q=0.7");
                        var request = new HttpRequestMessage(HttpMethod.Head, url);
                        var response = await client.SendAsync(request);
                        switch (response.StatusCode)
                        {
                            case HttpStatusCode.OK:
                                return new HttpReport(HttpStatus.OK, 0);
                            case HttpStatusCode.NotFound:
                                return new HttpReport(HttpStatus.NotFound, 0);
                            case HttpStatusCode.Gone:
                                goto case HttpStatusCode.NotFound;
                            default:
                                return new HttpReport(HttpStatus.HttpError, (int)response.StatusCode);
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                return new HttpReport(HttpStatus.Timeout, -1);
            }
            catch (HttpRequestException)
            {
                return new HttpReport(HttpStatus.HttpError, -1);
            }
            catch (Exception)
            {
                return new HttpReport(HttpStatus.Unknown, -1);
            }
        }
    }
}
