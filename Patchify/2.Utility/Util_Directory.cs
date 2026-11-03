using Newtonsoft.Json.Linq;
using Patchify_Ext_String;

namespace Patchify_Util_Directory
{
    public class Util_Directory
    {
        public static void Clear(string FolderPath)
        {
            DirectoryInfo di = new DirectoryInfo(FolderPath);

            foreach (FileInfo fFile in di.GetFiles())
            {
                fFile.Delete();
            }
            foreach (DirectoryInfo fDI in di.GetDirectories())
            {
                fDI.Delete(true);
            }
            Console.WriteLine($"{FolderPath.Quote()} Cleared!");
        }
        public static JObject FolderToJSON(string RelativePath)
        {
            JObject ReturnObject = new JObject();

            string MainPath = Path.GetDirectoryName(System.Environment.ProcessPath) + "\\";

            DirectoryInfo di = new DirectoryInfo(RelativePath);

            foreach (DirectoryInfo sub_Dir in di.GetDirectories())
            {
                ReturnObject.Add(Path.GetFileName(sub_Dir.FullName), FolderToJSON(RelativePath + "\\" +  sub_Dir.Name));
            }

            foreach (FileInfo File in di.GetFiles())
            {
                ReturnObject.Add(File.Name, "File");
            }

            return ReturnObject;
        }
        public static void DisplayContent(string RelativePath)
        {
            PrintDFS(RelativePath, "", true);
        }
        internal static void PrintDFS(string RelativePath, string Prefix, bool LastFolder)
        {
            if (Prefix == "")   Console.WriteLine(Path.GetFileName(RelativePath));
            else                Console.WriteLine(Prefix + "└ " + Path.GetFileName(RelativePath));

            if (!LastFolder)    Prefix += "| ";
            else Prefix += "  ";

            DirectoryInfo di = new DirectoryInfo(RelativePath);

            foreach (FileInfo File in di.GetFiles())
            {
                Console.WriteLine(Prefix + "└ " + File.Name);
            }

            int FolderCount = di.GetDirectories().Length;

            for (int i = 0; i < FolderCount - 1; i++)
            {
                PrintDFS(di.GetDirectories()[i].FullName, Prefix, false);
            }
            if (di.GetDirectories().Length > 0)
            {
                PrintDFS(di.GetDirectories()[FolderCount - 1].FullName, Prefix, true);
            }
        }
        public static List<string> SearchFor(string RelativePath, string TargetFile)
        {
            List<string> ReturnList = new List<string>();

            DirectoryInfo di = new DirectoryInfo(RelativePath);

            foreach(FileInfo File in di.GetFiles())
            {
                if (File.Name == TargetFile) ReturnList.Add(File.FullName);
            }

            foreach(DirectoryInfo Dir in di.GetDirectories())
            {
                ReturnList.AddRange(SearchFor(Dir.FullName, TargetFile));
            }

            return ReturnList;
        }
    }
}
