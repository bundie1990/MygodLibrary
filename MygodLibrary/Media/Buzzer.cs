namespace Mygod.Media
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;

    /// <summary>
    /// 提供演奏蜂鸣器音乐的类。
    /// </summary>
    public class Buzzer
    {
        private const double Do = 261.624;     //可以演奏的最低的Do
        private const double Semitone = 1.0594630943592952645618252949463;      //半音上调幅度
        //public const int SndFilename = 0x00020000;
        //public const int SndAsync = 0x0001;

        /// <summary>
        /// 最基本的蜂鸣器音。
        /// </summary>
        /// <param name="freq">音高。（单位Hz）</param>
        /// <param name="dur">演奏时长。</param>
        /// <returns>返回演奏是否成功。</returns>
        [DllImport("kernel32.dll")] public static extern bool Beep(int freq, int dur);
        //[DllImport("winmm.dll")] public static extern bool PlaySound(string pszSound, int hmod, int fdwSound);

        /// <summary>
        /// 演奏速度（不精确）。
        /// </summary>
        public int Speed;
        /// <summary>
        /// 每拍演奏时长。（单位ms）
        /// </summary>
        public double Hit
        {
            get { return 60000.0 / Speed; }
            set { Speed = (int)(60000 / value); }
        }

        /// <summary>
        /// Buzzer构造函数。
        /// </summary>
        /// <param name="speed">演奏速度。（默认120）</param>
        public Buzzer(int speed = 120)
        {
            Speed = speed;
        }

        private static readonly TaskFactory Factory = new TaskFactory();

        /// <summary>
        /// 播放dur毫秒的note。（输入无论大小写）
        /// </summary>
        /// <param name="sNote">表示音符。第一个字符必须在'A'至'G'之间， 接下来可以输入一个或几个'#'或'b'表示升降号，最后输入音阶。
        /// 如果为0则延时。</param>
        /// <param name="iDur">播放时长。（单位毫秒）</param>
        public static void Play(string sNote, int iDur)
        {
            var note = new string(sNote.ToCharArray()); //避免操作原字符串
            var scales = new double[12];
            scales[0] = 1;
            for (var i = 1; i < 12; i++) scales[i] = scales[i - 1] * Semitone;
            double nowSemitone = Do;
            int playNote;
            switch (note.ToUpper()[0])
            {
                case 'C': playNote = 0; break;
                case 'D': playNote = 2; break;
                case 'E': playNote = 4; break;
                case 'F': playNote = 5; break;
                case 'G': playNote = 7; break;
                case 'A': playNote = 9; break;
                case 'B': playNote = 11; break;
                case '0': Beep(0, iDur); return;
                default: throw new FormatException("输入格式错误！第一个字符必须在'A'至'G'之间，表示音符。");
            }
            note = note.Remove(0, 1);
            while (note[0] == '#') { playNote++; note = note.Remove(0, 1); }
            while (note.ToLower()[0] == 'b') { playNote--; note = note.Remove(0, 1); }
            var scale = Convert.ToInt32(note);
            while (scale > 0) { nowSemitone *= 2; scale--; }
            while (playNote < 0) { playNote += 12; nowSemitone /= 2; }
            while (playNote > 12) { playNote -= 12; nowSemitone *= 2; }
            nowSemitone *= scales[playNote];
            Console.WriteLine(sNote + '\t' + iDur);
            Beep((int)nowSemitone, iDur);
        }
        /// <summary>
        /// 播放dur拍的note。（输入无论大小写）
        /// </summary>
        /// <param name="sNote">表示音符。第一个字符必须在'A'至'G'之间， 接下来可以输入一个或几个'#'或'b'表示升降号，最后输入音阶。
        /// 如果为空则不播放。</param>
        /// <param name="dDur">播放时长。（单位拍）</param>
        public void Play(string sNote, double dDur)
        {
            Play(sNote, (int) (dDur*Hit));
        }

        /// <summary>
        /// 播放notes。
        /// </summary>
        /// <param name="notes">格式：sNote、iDur、sNote、iDur……用空格分隔。</param>
        public static void Play(string notes)
        {
            var s = notes.Split(new[] { ' ', '\r', '\n', '\t', '\0', '\f', '\v' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < s.Length; i += 2) Play(s[i], (int) Convert.ToDouble(s[i + 1]));
        }

        /// <summary>
        /// 播放notes。
        /// </summary>
        /// <param name="notes">格式：sNote、dDur、sNote、dDur……用空格、回车、制表符、空字符分隔。</param>
        public void PlayNotesWithEnglishNoteNames(string notes)
        {
            var s = notes.Split(new[] { ' ', '\r', '\n', '\t', '\0', '\f', '\v' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < s.Length; i += 2) Play(s[i], Convert.ToDouble(s[i + 1]));
        }

        /// <summary>
        /// 播放notes。
        /// </summary>
        /// <param name="notes">格式：sNote、dDur、sNote、dDur……用空格、回车、制表符、空字符分隔。</param>
        public void PlayNotesWithFixedDoNoteNames(string notes)
        {
            var s = notes.Split(new[] { ' ', '\r', '\n', '\t', '\0', '\f', '\v' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < s.Length; i += 2) Play(GetEnglishNoteNameFromFixedDoNoteNames(s[i]), Convert.ToDouble(s[i + 1]));
        }

        private static string GetEnglishNoteNameFromFixedDoNoteNames(string s)
        {
            return s.Replace("Do", "C").Replace("Di", "C#").Replace("Re", "D").Replace("Ri", "D#").Replace("Mi", "E").
                    Replace("Fa", "F").Replace("Fi", "F#").Replace("So", "G").Replace("Si", "G#").Replace("La", "A").
                    Replace("Li", "A#").Replace("Ti", "B").Replace('-', '0');
        }
    }

    /// <summary>
    /// 提供演奏Buzzer的样例。
    /// </summary>
    public static class BuzzerExample
    {
        /// <summary>
        /// 使用Buzzer.Play实现的《欢乐颂》。（理论速度：120，实际速度：90～100）
        /// 播放字符串如下：E1 500 E1 500 F1 500 G1 500 G1 500 F1 500 E1 500 D1 500 C1 500 C1 500 D1 500 E1 500 E1 750 D1 250 D1 1000 
        /// E1 500 E1 500 F1 500 G1 500 G1 500 F1 500 E1 500 D1 500 C1 500 C1 500 D1 500 E1 500 D1 750 C1 250 C1 1000 
        /// D1 500 D1 500 E1 500 C1 500 D1 500 E1 250 F1 250 E1 500 C1 500 D1 500 E1 250 F1 250 E1 500 D1 500 C1 500 D1 500 G0 500 
        /// E1 1000 E1 500 F1 500 G1 500 G1 500 F1 500 E1 500 D1 500 C1 500 C1 500 D1 500 E1 500 D1 750 C1 250 C1 1000
        /// </summary>
        public static void OdeToJoy()
        {
            Buzzer.Play("E1 500 E1 500 F1 500 G1 500 G1 500 F1 500 E1 500 D1 500 C1 500 C1 500 D1 500 E1 500 E1 750 D1 250 D1 1000 " +
                        "E1 500 E1 500 F1 500 G1 500 G1 500 F1 500 E1 500 D1 500 C1 500 C1 500 D1 500 E1 500 D1 750 C1 250 C1 1000 " +
                        "D1 500 D1 500 E1 500 C1 500 D1 500 E1 250 F1 250 E1 500 C1 500 D1 500 E1 250 F1 250 E1 500 D1 500 C1 500 D1 500 G0 500 " +
                        "E1 1000 E1 500 F1 500 G1 500 G1 500 F1 500 E1 500 D1 500 C1 500 C1 500 D1 500 E1 500 D1 750 C1 250 C1 1000");
        }
        /// <summary>
        /// 播放一次300Hz至10000Hz的来回警报。（由于API等的原因不能连续）
        /// </summary>
        public static void Alarm()
        {
            for (var i = 300; i <= 10000; i = Convert.ToInt32(i * 1.2)) Buzzer.Beep(i, 100);
            for (var i = 10000; i >= 300; i = Convert.ToInt32(i / 1.2)) Buzzer.Beep(i, 100);
        }
    }
}
