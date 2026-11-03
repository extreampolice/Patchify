namespace Patchify_Ext_String
{
    public static class Ext_String
    {
        static public string Readable(this string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '_' && Char.IsDigit(str[i + 1]))
                {
                    return str.Substring(0, i);
                }
            }
            return str;
        }
        static public string Quote(this string str)
        {
            return "\"" + str + "\"";
        }
        static public string UnQuote(this string str)
        {
            if (str.Length == 0) return str;

            int StartingIndex = 0;
            int SlicingLength = str.Length;
            if (str[0] == '"')
            {
                StartingIndex++;
                SlicingLength--;
            }
            if (str[str.Length - 1] == '"')
            {
                SlicingLength--;
            }
            return str.Substring(StartingIndex, SlicingLength);
        }
        static public string NoExtension(this string str)
        {
            return Path.GetFileNameWithoutExtension(str);
        }
        static public string Extension(this string str)
        {
            return Path.GetExtension(str);
        }
        static public string ToFullPath(this string str)
        {
            return Path.GetFullPath(str);
        }
        static public string ToRelativePath(this string str)
        {
            string MainPath = System.IO.Directory.GetCurrentDirectory();
            string ReturnPath = Path.GetRelativePath(MainPath, str);
            return ReturnPath;
        }
        static public string ToSlash(this string str)
        {
            return str.Replace('\\', '/');
        }
        static public string ToBackSlash(this string str)
        {
            return str.Replace('/', '\\');
        }
    }
}
