namespace Mygod.Text
{
    /// <summary>
    /// Based on: http://stackoverflow.com/a/4685745/2245107
    /// </summary>
    public static class Csv
    {
        private static readonly char[] StupidChars = { ',', '\n' };

        public static string Escape(string s)
        {
            return s.IndexOf('"') >= 0 ? '"' + s.Replace("\"", "\"\"") + '"'
                : (s.IndexOfAny(StupidChars) >= 0 ? '"' + s + '"' : s);
        }
    }
}
