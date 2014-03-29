using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Mygod.Net
{
    public static class YouTube
    {
        public sealed class Video
        {
            private Video(string id, IWebProxy proxy = null)
            {
                var videoInfo = DownloadString(string.Format(
                    "http://www.youtube.com/get_video_info?video_id={0}&eurl=http://mygod.tk/", this.id = id), proxy);
                information = (from info in videoInfo.Split('&') let i = info.IndexOf('=') 
                               select new { Key = info.Substring(0, i), Value = info.Substring(i + 1).UrlDecode() })
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
                if (information["status"] != "ok")
                    throw new Exception("获取视频信息失败！原因：" + information["reason"]);
                FmtStreamMap = information["url_encoded_fmt_stream_map"].Split(',')
                    .SelectMany(s => FmtStream.Create(s, this)).ToList();
                information.Remove("url_encoded_fmt_stream_map");
            }

            private static readonly Regex
                R0 = new Regex("data-video-id=(\"|')([A-Za-z0-9_\\-]{11})\\1", RegexOptions.Compiled),
                R1 = new Regex("(\\?|&)v=([A-Za-z0-9_\\-]{11})", RegexOptions.Compiled),
                R2 = new Regex("youtube(|.googleapis).com/(v|embed)/([A-Za-z0-9_\\-]{11})", RegexOptions.Compiled);
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
            private static string DownloadString(string address, IWebProxy proxy = null)
            {
                using (var client = new WebClient())
                {
                    if (proxy != null) client.Proxy = proxy;
                    return client.DownloadString(address);
                }
            }

            private readonly string id;
            private readonly Dictionary<string, string> information;
            public readonly List<FmtStream> FmtStreamMap;

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

        public class FmtStream : IComparable<FmtStream>
        {
            protected FmtStream(Video parent, string url)
            {
                this.parent = parent;
                this.url = url;
            }

            private FmtStream(VideoFormat videoFormat, VideoEncodings videoEncoding, int videoHeight,
                              double? videoMinBitrate, double? videoMaxBitrate, AudioEncodings audioEncoding, int audioMinChannels,
                              int audioMaxChannels, int audioSamplingRate, int? audioBitrate, string url, Video parent)
                : this(parent, url)
            {
                Format = videoFormat;
                VideoEncoding = videoEncoding;
                MaxVideoHeight = videoHeight;
                VideoMinBitrate = videoMinBitrate;
                VideoMaxBitrate = videoMaxBitrate;
                AudioEncoding = audioEncoding;
                MinChannels = audioMinChannels;
                MaxChannels = audioMaxChannels;
                SamplingRate = audioSamplingRate;
                AudioBitrate = audioBitrate;
            }

            public static IEnumerable<FmtStream> Create(string data, Video parent)
            {
                var dic = (from info in data.Split('&') let i = info.IndexOf('=')
                           select new { Key = info.Substring(0, i), Value = info.Substring(i + 1) })
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
                var result = Create(int.Parse(dic["itag"]), dic["url"].UrlDecode(), dic["fallback_host"], parent).ToList();
                foreach (var u in result.OfType<UnknownFmtStream>())
                {
                    u.Quality = dic["quality"];
                    u.Type = dic["type"].UrlDecode();
                }
                return result;
            }

            private static IEnumerable<FmtStream> Create(int itag, string url, string fallbackHost, Video parent)
            {
                var fallbackUrl = url.Substring(7);
                fallbackUrl = "http://" + fallbackHost + fallbackUrl.Remove(0, fallbackUrl.IndexOf('/'));
                var urls = string.IsNullOrEmpty(fallbackHost) ? new[] { url } : new[] {url, fallbackUrl};
                switch (itag)
                {
                    case 0:     //OUTDATED, 4 Unknown
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatFLV, VideoEncodings.Undefined, 240,
                                                       null, null, AudioEncodings.MP3, 1, 1, 22050, null, u, parent);
                        yield break;
                    case 5:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatFLV, VideoEncodings.SorensonH263, 240,
                                                       0.25, 0.25, AudioEncodings.MP3, 1, 2, 22050, 64, u, parent);
                        yield break;
                    case 6:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatFLV, VideoEncodings.SorensonH263, 270,
                                                       0.8, 0.8, AudioEncodings.MP3, 1, 2, 44100, 64, u, parent);
                        yield break;
                    case 13:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.Format3GP, VideoEncodings.MPEG4Visual, 144,
                                                       0.5, 0.5, AudioEncodings.AAC, 1, 1, 22050, 75, u, parent);
                        yield break;
                    case 17:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.Format3GP, VideoEncodings.MPEG4VisualSimple, 144,
                                                       2, 2, AudioEncodings.AAC, 2, 2, 44100, 75, u, parent);
                        yield break;
                    case 18:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatMP4, VideoEncodings.H264Baseline, 360,
                                                       0.5, 0.5, AudioEncodings.AAC, 2, 2, 44100, 96, u, parent);
                        yield break;
                    case 22:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatMP4, VideoEncodings.H264High, 720,
                                                       2.0, 2.9, AudioEncodings.AAC, 2, 2, 44100, 152, u, parent);
                        yield break;
                    case 34:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatFLV, VideoEncodings.H264Main, 360,
                                                       0.5, 0.5, AudioEncodings.AAC, 2, 2, 44100, 128, u, parent);
                        yield break;
                    case 35:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatFLV, VideoEncodings.H264Main, 480,
                                                       0.8, 1, AudioEncodings.AAC, 2, 2, 44100, 128, u, parent);
                        yield break;
                    case 36:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.Format3GP, VideoEncodings.MPEG4VisualSimple, 240,
                                                       0.8, 0.8, AudioEncodings.AAC, 1, 1, 22050, 75, u, parent);
                        yield break;
                    case 37:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatMP4, VideoEncodings.H264High, 1080,
                                                       3.5, 5, AudioEncodings.AAC, 2, 2, 44100, 152, u, parent);
                        yield break;
                    case 38:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatMP4, VideoEncodings.H264High, 3072,
                                                       0, 0, AudioEncodings.AAC, 2, 2, 44100, 152, u, parent);
                        yield break;
                    case 43:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatWebM, VideoEncodings.VP8, 360,
                                                       0.5, 0.5, AudioEncodings.Vorbis, 2, 2, 44100, 128, u, parent);
                        yield break;
                    case 44:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatWebM, VideoEncodings.VP8, 480,
                                                       1, 1, AudioEncodings.Vorbis, 2, 2, 44100, 128, u, parent);
                        yield break;
                    case 45:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatWebM, VideoEncodings.VP8, 720,
                                                       2, 2, AudioEncodings.Vorbis, 2, 2, 44100, 192, u, parent);
                        yield break;
                    case 46:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatWebM, VideoEncodings.VP8, 1080,
                                                       2, 2, AudioEncodings.Vorbis, 2, 2, 44100, 192, u, parent);
                        yield break;
                    case 82:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatMP4, VideoEncodings.H2643D, 360,
                                                       0.5, 0.5, AudioEncodings.AAC, 2, 2, 44100, 96, u, parent);
                        yield break;
                    case 83:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatMP4, VideoEncodings.H2643D, 240,
                                                       0.5, 0.5, AudioEncodings.AAC, 2, 2, 44100, 152, u, parent);
                        yield break;
                    case 84:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatMP4, VideoEncodings.H2643D, 720,
                                                       2, 2.9, AudioEncodings.AAC, 2, 2, 44100, 152, u, parent);
                        yield break;
                    case 85:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatMP4, VideoEncodings.H2643D, 520,
                                                       2, 2.9, AudioEncodings.AAC, 2, 2, 44100, 152, u, parent);
                        yield break;
                    case 100:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatWebM, VideoEncodings.VP83D, 360,
                                                       null, null, AudioEncodings.Vorbis, 2, 2, 44100, 128, u, parent);
                        yield break;
                    case 101:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatWebM, VideoEncodings.VP83D, 480,
                                                       null, null, AudioEncodings.Vorbis, 2, 2, 44100, 192, u, parent);
                        yield break;
                    case 102:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatWebM, VideoEncodings.VP83D, 720,
                                                       null, null, AudioEncodings.Vorbis, 2, 2, 44100, 192, u, parent);
                        yield break;
                    case 103:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatWebM, VideoEncodings.VP83D, 540,
                                                       null, null, AudioEncodings.Vorbis, 2, 2, 44100, 192, u, parent);
                        yield break;
                    case 120:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatFLV, VideoEncodings.H264MainL31, 720,
                                                       2, 2, AudioEncodings.AAC, 0, 0, 0, 128, u, parent);
                        yield break;
                    case 133:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatMP4, VideoEncodings.H264, 240,
                                                       0.2, 0.3, AudioEncodings.None, 0, 0, 0, null, u, parent);
                        yield break;
                    case 134:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatMP4, VideoEncodings.H264, 360,
                                                       0.3, 0.4, AudioEncodings.None, 0, 0, 0, null, u, parent);
                        yield break;
                    case 135:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatMP4, VideoEncodings.H264, 480,
                                                       0.5, 1, AudioEncodings.None, 0, 0, 0, null, u, parent);
                        yield break;
                    case 136:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatMP4, VideoEncodings.H264, 720,
                                                       1, 1.5, AudioEncodings.None, 0, 0, 0, null, u, parent);
                        yield break;
                    case 137:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatMP4, VideoEncodings.H264, 1080,
                                                       2, 2.9, AudioEncodings.None, 0, 0, 0, null, u, parent);
                        yield break;
                    case 139:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatMP4, VideoEncodings.None, 0,
                                                       null, null, AudioEncodings.AAC, 0, 0, 0, 48, u, parent);
                        yield break;
                    case 140:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatMP4, VideoEncodings.None, 0,
                                                       null, null, AudioEncodings.AAC, 0, 0, 0, 128, u, parent);
                        yield break;
                    case 141:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatMP4, VideoEncodings.None, 0,
                                                       null, null, AudioEncodings.AAC, 0, 0, 0, 256, u, parent);
                        yield break;
                    case 160:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatMP4, VideoEncodings.H264, 144,
                                                       0.1, 0.1, AudioEncodings.None, 0, 0, 0, null, u, parent);
                        yield break;
                    case 171:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatWebM, VideoEncodings.None, 0,
                                                       null, null, AudioEncodings.Vorbis, 0, 0, 0, 128, u, parent);
                        yield break;
                    case 172:
                        foreach (var u in urls)
                            yield return new FmtStream(VideoFormat.FormatWebM, VideoEncodings.None, 0,
                                                       null, null, AudioEncodings.Vorbis, 0, 0, 0, 192, u, parent);
                        yield break;
                    default:
                        foreach (var u in urls) yield return new UnknownFmtStream(itag, u, parent);
                        yield break;
                }
            }

            // ReSharper disable MemberCanBePrivate.Global
            public readonly VideoFormat Format = VideoFormat.Undefined;
            public readonly VideoEncodings VideoEncoding = VideoEncodings.Undefined;
            public readonly int MaxVideoHeight;
            public readonly double? VideoMinBitrate;
            public readonly double? VideoMaxBitrate;
            public readonly int MinChannels;
            public readonly int MaxChannels;
            public readonly AudioEncodings AudioEncoding = AudioEncodings.Undefined;
            public readonly int SamplingRate;
            public readonly int? AudioBitrate;
            // ReSharper restore MemberCanBePrivate.Global

            private readonly Video parent;
            public string Properties
            {
                get
                {
                    return string.Format("视频格式：{0}{6}视频编码：{1}{6}{2}音频编码：{3}{6}{4}视频下载地址：{5}{6}", VideoFormatToString(), 
                                         VideoEncodingsToString(), VideoEncoding == VideoEncodings.None ? string.Empty
                                            : string.Format("视频大小：{0}p{2}视频比特率：{1}MBps{2}", MaxVideoHeight, VideoBitrateToString(),
                                                            Environment.NewLine), 
                                         AudioEncodingsToString(), AudioEncoding == AudioEncodings.None ? string.Empty
                                            : string.Format("音频声道数：{0}{3}音频采样速率：{1}{3}音频比特率：{2}KBps{3}", ChannelsToString(),
                                                            SamplingRate, AudioBitrate, Environment.NewLine), url, Environment.NewLine);
                }
            }

            public Video Parent { get { return parent; } }
            public string Extension { get { return GetVideoFormatExtension(Format); } }

            private readonly string url = "about:blank;";

            public string GetUrl(string fileName = null)
            {
                if (string.IsNullOrEmpty(fileName)) return url;
                return url + "&title=" + fileName.ToValidPath().UrlEncode().UrlEncode();
                // double encode or non-ascii characters will go wrong in C# apps (HOLY SH*T THIS GODDAMN THING IS REALLY ANNOYING)
            }

            public int CompareTo(FmtStream other)
            {
                if (other == null) throw new NotSupportedException();
                if (this is UnknownFmtStream) if (other is UnknownFmtStream) return 0; else return -1;
                if (other is UnknownFmtStream) return 1;
                if (MaxVideoHeight > other.MaxVideoHeight) return 1;
                if (MaxVideoHeight < other.MaxVideoHeight) return -1;
                if (VideoMaxBitrate > other.VideoMaxBitrate) return 1;
                if (VideoMaxBitrate < other.VideoMaxBitrate) return -1;
                if (MinChannels > other.MinChannels) return 1;
                if (MinChannels < other.MinChannels) return -1;
                if (SamplingRate > other.SamplingRate) return 1;
                if (SamplingRate < other.SamplingRate) return -1;
                if (AudioBitrate > other.AudioBitrate) return 1;
                if (AudioBitrate < other.AudioBitrate) return -1;
                if (Format != VideoFormat.FormatWebM && other.Format == VideoFormat.FormatWebM) return 1; // 尽量不使用webm因为兼容性不好
                return 0;
            }

            public override string ToString()
            {
                return VideoFormatToString() + ' ' + VideoEncodingsToString()
                    + (VideoEncoding == VideoEncodings.None ? string.Empty : " " + MaxVideoHeight + "p") + ' ' + AudioEncodingsToString()
                    + (AudioEncoding == AudioEncodings.None ? string.Empty : ' ' + ChannelsToString() + ' ' + SamplingRate + "Hz"
                        + (AudioEncoding == AudioEncodings.Undefined ? string.Empty : (" " + AudioEncodingsToString())));
            }

            // ReSharper disable MemberCanBePrivate.Global

            public string VideoFormatToString()
            {
                return YouTube.VideoFormatToString(Format);
            }

            public string VideoEncodingsToString()
            {
                return YouTube.VideoEncodingsToString(VideoEncoding);
            }

            public string AudioEncodingsToString()
            {
                return YouTube.AudioEncodingsToString(AudioEncoding);
            }

            public string ChannelsToString()
            {
                return YouTube.ChannelsToString(MinChannels, MaxChannels);
            }

            public string VideoBitrateToString()
            {
                if (VideoMinBitrate == null || VideoMaxBitrate == null) return null;
                return Math.Abs(VideoMinBitrate.Value - VideoMaxBitrate.Value) < 1e-4
                    ? VideoMaxBitrate.Value.ToString(CultureInfo.InvariantCulture) : (VideoMinBitrate + "-" + VideoMaxBitrate);
            }

            // ReSharper restore MemberCanBePrivate.Global
        }

        private sealed class UnknownFmtStream : FmtStream
        {
            public UnknownFmtStream(int code, string url, Video parent) : base(parent, url)
            {
                videoTypeCode = code;
            }

            private readonly int videoTypeCode;
            public string Quality, Type;

            public override string ToString()
            {
                return string.Format("未知的 FMT #{0} 类型：{1} 质量：{2} 请联系 Mygod 工作室™ 以解决此问题",
                                     videoTypeCode, Type, Quality);
            }
        }

        public enum VideoFormat
        {
            Undefined,
            FormatFLV,
            FormatMP4,
            Format3GP,
            FormatWebM
        }

        private static string VideoFormatToString(VideoFormat format)
        {
            switch (format)
            {
                case VideoFormat.Format3GP:
                    return "3GP";
                case VideoFormat.FormatFLV:
                    return "FLV";
                case VideoFormat.FormatMP4:
                    return "MP4";
                case VideoFormat.FormatWebM:
                    return "WebM";
                default:
                    return "未知格式";
            }
        }

        private static string GetVideoFormatExtension(VideoFormat format)
        {
            switch (format)
            {
                case VideoFormat.Format3GP:
                    return ".3gp";
                case VideoFormat.FormatFLV:
                    return ".flv";
                case VideoFormat.FormatMP4:
                    return ".mp4";
                case VideoFormat.FormatWebM:
                    return ".webm";
                default:
                    return string.Empty;
            }
        }

        public enum VideoEncodings
        {
            Undefined, 
            None,
            SorensonH263, 
            H264,
            H264Main,
            H264Baseline, 
            H264High,
            H2643D, 
            H264MainL31,
            VP8, 
            VP83D, 
            MPEG4Visual,
            MPEG4VisualSimple
        }

        private static string VideoEncodingsToString(VideoEncodings encoding)
        {
            switch (encoding)
            {
                case VideoEncodings.None:
                    return "无视频";
                case VideoEncodings.SorensonH263:
                    return "Sorenson H.263";
                case VideoEncodings.H264:
                    return "MPEG-4 AVC (H.264)";
                case VideoEncodings.H264Main:
                    return "MPEG-4 AVC (H.264) Main";
                case VideoEncodings.H264Baseline:
                    return "MPEG-4 AVC (H.264) Baseline";
                case VideoEncodings.H264High:
                    return "MPEG-4 AVC (H.264) High";
                case VideoEncodings.H2643D:
                    return "MPEG-4 AVC (H.264) 3D";
                case VideoEncodings.H264MainL31:
                    return "MPEG-4 AVC (H.264) Main@L3.1";
                case VideoEncodings.VP8:
                    return "VP8";
                case VideoEncodings.VP83D:
                    return "VP8 3D";
                case VideoEncodings.MPEG4Visual:
                    return "MPEG-4 Visual";
                case VideoEncodings.MPEG4VisualSimple:
                    return "MPEG-4 Visual Simple";
                default:
                    return "未知视频解码";
            }
        }

        public enum AudioEncodings
        {
            Undefined,
            None,
            AAC,
            MP3,
            Vorbis
        }

        private static string AudioEncodingsToString(AudioEncodings encoding)
        {
            switch (encoding)
            {
                case AudioEncodings.None:
                    return "无音频";
                case AudioEncodings.AAC:
                    return "AAC";
                case AudioEncodings.MP3:
                    return "MP3";
                case AudioEncodings.Vorbis:
                    return "Vorbis";
                default:
                    return "未知音频编码";
            }
        }

        private static string ChannelsToString(int minChannels, int maxChannels)
        {
            if (minChannels != maxChannels && minChannels != 1 && maxChannels != 2) return string.Format("{0}至{1}声道", minChannels, maxChannels);
            switch (minChannels)
            {
                case 1:
                    return maxChannels == 1 ? "单声道" : "单声道或双声道";
                case 2:
                    return "双声道";
                case 6:
                    return "5.1声道";
                case 8:
                    return "7.1声道";
                default:
                    return minChannels + "声道";
            }
        }
    }
}
