using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Mygod.Xml.Linq;

namespace Mygod.Net
{
    public static class YouTube
    {
        public sealed class Video
        {
            private Video(string id, IWebProxy proxy = null)
            {
                var videoInfo = DownloadString("http://www.youtube.com/get_video_info?eurl=http://mygod.tk/&video_id=" +
                                                    (this.id = id), proxy);
                information = (from info in videoInfo.Split('&') let i = info.IndexOf('=') 
                               select new { Key = info.Substring(0, i), Value = info.Substring(i + 1).UrlDecode() })
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
                if (information["status"] != "ok")
                    throw new Exception("获取视频信息失败！原因：" + information["reason"]);
                var resolutionLookup = string.IsNullOrWhiteSpace(information["fmt_list"])
                                           ? new Dictionary<string, string>()
                                           : (from x in information["fmt_list"].Split(',') select x.Split('/'))
                                                 .ToDictionary(l => l[0], l => l[1]);
                IEnumerable<string> fmts = information["url_encoded_fmt_stream_map"].Split(',');
                if (information.ContainsKey("adaptive_fmts"))
                    fmts = fmts.Concat(information["adaptive_fmts"].Split(','));
                var downloads = fmts.SelectMany(s => FmtStream.Create(s, this, resolutionLookup).Cast<Downloadable>());
                if (information.ContainsKey("ttsurl"))
                    downloads = downloads.Concat(Subtitle.Create(information["ttsurl"], this, proxy));
                Downloads = downloads.ToList();
                information.Remove("url_encoded_fmt_stream_map");
            }

            private static readonly Regex
                R0 = new Regex("data-video-id=(\"|')([A-Za-z0-9_\\-]{11})\\1", RegexOptions.Compiled),
                R1 = new Regex("(\\?|&)v=([A-Za-z0-9_\\-]{11})", RegexOptions.Compiled),
                R2 = new Regex("youtube(|\\.googleapis)\\.com/(v|embed)/([A-Za-z0-9_\\-]{11})", RegexOptions.Compiled);
            private static IEnumerable<Video> GetVideoFromContent(ISet<string> exception, string link,
                                                                  IWebProxy proxy = null)
            {
                var match = R0.Match(link);
                while (match.Success)
                {
                    Video result = null;
                    try
                    {
                        var id = match.Groups[2].Value;
                        if (!exception.Contains(id))
                        {
                            exception.Add(id);
                            result = new Video(id, proxy);
                        }
                    }
                    catch { }
                    if (result != null) yield return result;
                    match = match.NextMatch();
                }
                match = R1.Match(link);
                while (match.Success)
                {
                    Video result = null;
                    try
                    {
                        var id = match.Groups[2].Value;
                        if (!exception.Contains(id))
                        {
                            exception.Add(id);
                            result = new Video(id, proxy);
                        }
                    }
                    catch { }
                    if (result != null) yield return result;
                    match = match.NextMatch();
                }
                match = R2.Match(link);
                while (match.Success)
                {
                    Video result = null;
                    try
                    {
                        var id = match.Groups[3].Value;
                        if (!exception.Contains(id))
                        {
                            exception.Add(id);
                            result = new Video(id, proxy);
                        }
                    }
                    catch { }
                    if (result != null) yield return result;
                    match = match.NextMatch();
                }
            }
            public static IEnumerable<Video> GetVideoFromLink(string link, IWebProxy proxy = null)
            {
                var result = new HashSet<string>();
                foreach (var video in GetVideoFromContent(result, link, proxy)) yield return video;
                foreach (var video in GetVideoFromContent(result, DownloadString(link, proxy), proxy))
                    yield return video;
            }
            public static string DownloadString(string address, IWebProxy proxy = null)
            {
                using (var client = new WebClient())
                {
                    if (proxy != null) client.Proxy = proxy;
                    return client.DownloadString(address);
                }
            }
            public static byte[] DownloadData(string address, IWebProxy proxy = null)
            {
                using (var client = new WebClient())
                {
                    if (proxy != null) client.Proxy = proxy;
                    return client.DownloadData(address);
                }
            }

            private readonly string id;
            private readonly Dictionary<string, string> information;
            public readonly List<Downloadable> Downloads = new List<Downloadable>();

            public string Title { get { return information["title"]; } }
            public string Author { get { return information["author"]; } }
            public string[] Keywords
            {
                get { return information["keywords"] == null ? null : information["keywords"].Split(','); }
            }
            public double AverageRating { get { return double.Parse(information["avg_rating"]); } }
            public long ViewCount { get { return long.Parse(information["view_count"]); } }
            public DateTime UploadTime
            {
                get
                {
                    return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        .AddSeconds(double.Parse(information["timestamp"]));
                }
            }
            public TimeSpan Length
            {
                get { return TimeSpan.FromSeconds(double.Parse(information["length_seconds"])); }
            }
            public string Url { get { return "http://www.youtube.com/watch?v=" + id; } }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj.GetType() == GetType() && Equals((Video) obj);
            }
            private bool Equals(Video other)
            {
                return string.Equals(id, other.id);
            }
            public override int GetHashCode()
            {
                return (id != null ? id.GetHashCode() : 0);
            }
            public override string ToString()
            {
                return Title;
            }
        }

        public abstract class Downloadable
        {
            protected Downloadable(Video parent)
            {
                this.parent = parent;
            }

            private readonly Video parent;
            public Video Parent { get { return parent; } }
            public abstract string Properties { get; }
            public abstract string GetUrl(string fileName = null);
            public abstract string Extension { get; }
        }

        public class FmtStream : Downloadable
        {
            private FmtStream(int itag, string url, string type, string size, long bitrate, Video parent) : base(parent)
            {
                ITag = itag;
                this.url = url;
                Type = type;
                Size = size;
                Bitrate = bitrate;
            }

            public static IEnumerable<FmtStream> Create(string data, Video parent,
                                                        Dictionary<string, string> resolutionLookup)
            {
                var dic = (from info in data.Split('&') let i = info.IndexOf('=')
                           select new { Key = info.Substring(0, i), Value = info.Substring(i + 1) })
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
                string signature = dic.ContainsKey("sig") ? dic["sig"] : null, url = dic["url"].UrlDecode();
                if (dic.ContainsKey("s"))
                    throw new NotSupportedException("Signature ciphered, copyright protected.");
                if (!string.IsNullOrWhiteSpace(signature)) url += "&signature=" + signature;
                var fallbackHost = dic.ContainsKey("fallback_host") ? dic["fallback_host"] : null;
                var fmt = dic["itag"];
                string[] urls;
                if (string.IsNullOrWhiteSpace(fallbackHost)) urls = new[] { url };
                else
                {
                    var fallbackUrl = url.Substring(7);
                    fallbackUrl = "http://" + fallbackHost + fallbackUrl.Remove(0, fallbackUrl.IndexOf('/'));
                    urls = new[] { url, fallbackUrl };
                }
                return urls.Select(u => new FmtStream(int.Parse(fmt), u, dic["type"].UrlDecode(),
                    resolutionLookup.ContainsKey(fmt) ? resolutionLookup[fmt]
                                                      : dic.ContainsKey("size") ? dic["size"] : null,
                    dic.ContainsKey("bitrate") ? long.Parse(dic["bitrate"]) : 0, parent));
            }

            // ReSharper disable MemberCanBePrivate.Global
            public readonly int ITag;
            public readonly string Type, Size;
            public readonly long Bitrate;
            // ReSharper restore MemberCanBePrivate.Global

            public override string Properties
            {
                get
                {
                    return string.Format("#{2} {0}{1}下载地址：{3}", this, Environment.NewLine, ITag, GetUrl());
                }
            }

            public override string Extension
            {
                get
                {
                    if (Type.StartsWith("audio/mp4;")) return " [A].m4a";
                    if (Type.StartsWith("audio/webm;")) return " [A].webm";
                    var prefix = Bitrate == 0 ? string.Empty : " [V]";
                    if (Type.StartsWith("video/mp4;")) return prefix + ".mp4";
                    if (Type.StartsWith("video/webm;")) return prefix + ".webm";
                    if (Type.StartsWith("video/3gpp;")) return prefix + ".3gp";
                    if (Type.StartsWith("video/x-flv;")) return prefix + ".flv";
                    if (Type.StartsWith("video/mp2t;")) return prefix + ".ts";
                    return string.Empty;
                }
            }

            private readonly string url = "about:blank;";
            public override string GetUrl(string fileName = null)
            {
                if (string.IsNullOrEmpty(fileName)) return url;
                return url + "&title=" + fileName.ToValidPath().UrlEncode().UrlEncode();
                // double encode or non-ascii characters will go wrong in C# apps (HOLY SH*T THIS GODDAMN THING IS REALLY ANNOYING)
            }

            public override string ToString()
            {
                return Type + (Size == null ? string.Empty : "; " + Size) +
                              (Bitrate == 0 ? string.Empty : "; " + Helper.GetSize(Bitrate, "字节") + " 每秒");
            }
        }

        public class Subtitle : Downloadable
        {
            private Subtitle(long id, string name, string lang, string code, string vss, string extension,
                             string url, Video parent) : base(parent)
            {
                this.id = id;
                Name = name;
                Language = lang;
                langCode = code;
                vssID = vss;
                this.extension = extension;
                this.url = url;
            }

            private static readonly Regex LanguageReplacer = new Regex(@"(?<=hl\=)[^&]*", RegexOptions.Compiled);
            public static IEnumerable<Subtitle> Create(string ttsurl, Video parent, IWebProxy proxy = null)
            {
                var root = XDocument.Parse(Encoding.UTF8.GetString(Video.DownloadData(
                    (ttsurl = LanguageReplacer.Replace(ttsurl, "zh-CN")) +
                    "&type=list&tlangs=1&fmts=1&vssids=1&asrs=1", proxy))).Root;
                var extensions =
                    root.ElementsCaseInsensitive("format").Select(e => e.GetAttributeValue("fmt_code")).ToList();
                foreach (var e in root.ElementsCaseInsensitive("track"))
                {
                    var id = e.GetAttributeValue<long>("id");
                    string name = e.GetAttributeValue("name"), lang = e.GetAttributeValue("lang_translated"),
                           code = e.GetAttributeValue("lang_code"), vss = e.GetAttributeValue("vss_id");
                    foreach (var extension in extensions)
                        yield return new Subtitle(id, name, lang, code, vss, extension, ttsurl, parent);
                }
            }

            private readonly long id;
            private readonly string langCode, vssID, extension, url;

            public string Name { get; private set; }
            public string Language { get; private set; }
            public override string Properties
            {
                get { return string.Format("{0}{1}下载地址：{2}", this, Environment.NewLine, GetUrl()); }
            }
            public override string Extension
            {
                get
                {
                    var result = vssID;
                    if (!result.StartsWith(".", StringComparison.InvariantCulture)) result = '.' + result;
                    if (!string.IsNullOrWhiteSpace(Name)) result += '.' + Name;
                    return result + '.' + extension;
                }
            }

            public override string GetUrl(string fileName = null)
            {
                return string.Format("{0}&type=track&lang={1}&name={2}&kind&fmt={3}", url, langCode, Name, extension);
            }

            public override string ToString()
            {
                return string.Format("{0}字幕{2} ({1} 格式)", Language, extension,
                                     string.IsNullOrWhiteSpace(Name) ? string.Empty : " (" + Name + ')');
            }
        }
    }
}
