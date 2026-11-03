using Patchify_Ext_JSON;
using Patchify_Ext_UAssetAPI;
using Newtonsoft.Json.Linq;
using UnrealFiles;
using System.Diagnostics;

namespace Program
{
    class Patchify
    {
        //--------------------------------Public Fields------------------------------------
        //--------------------------------Private Fields-----------------------------------
        string _Modsfolder;
        string _PatchesFolder;
        string _OutputFolder;
        string _PWFolder;
        //--------------------------------Properties--------------------------------------
        //--------------------------------Methods----------------------------------------
        //-----------------------Constructor, Deconstructor-----------------------------
        Patchify(string _Modsfolder, string _PatchesFolder, string _OutputFolder, string _PWFolder)
        {

        }
        //-------------Temporary Methods that are only used in this class-----------
    }
    class Program
    {
        static void Main(string[] args)
        {
            //pak 파일을 전부 수합
            //모든 pak 파일에 들어있는 UDataTable을 수합하고 오리지널 파일이랑 비교해서 json 생성
            //생성한 json + 유저 input을 오리지널 파일에 priority 순으로 적용
            //적용된 UDataTable을 원래 위치에 배치하고 .pak 생성
            //그 뒤 모드들에 대한 정보와 프로그램이 뭘 했는지에 대한 디테일한 보고서 작성

            string _ModsFolder = "~mods";
            //Output
            string _PatchesFolder = "~patches";

            string _Outputfolder = "~RESULT";

            string _ProjectWingmanPak = args[0];//"D:\\SteamLibrary\\steamapps\\common\\Project Wingman\\ProjectWingman\\Content\\Paks\\pakchunk0-WindowsNoEditor.pak";

            PakFile PW = new PakFile(_ProjectWingmanPak);
            
            PakStruct CombinedPak = new PakStruct();
            JObject CombinedPatch = new JObject();

            Stopwatch t = Stopwatch.StartNew();
            foreach (string PakPath in Directory.GetFiles(_ModsFolder, "*.pak", SearchOption.AllDirectories))
            {
                using (PakFile Pak = new PakFile(PakPath))
                {
                    //Pak.PrintTree();
                    Pak.Unpak();

                    foreach (UAssetFile UAF in Pak.DataTableList)
                    {
                        //UAF.Write(Path.Combine(_Outputfolder, UAF.ParentPak.Name));
                        bool exists = false;
                        foreach (UAssetFile fUAF in CombinedPak.DataTableList)
                        {
                            if (UAF.CorrectPath.Name == fUAF.CorrectPath.Name) exists = true;
                        }

                        if (!exists) CombinedPak.DataTableList.Add(PW.GetSpecifiedUAssetFile(UAF.CorrectPath.Value));

                    }
                    foreach (UAssetStruct UAF in Pak.AssetList)
                    {
                        CombinedPak.AssetList.Add(UAF);
                    }
                    //Pak.ShavePlanes(PW);
                    JObject PakPatch = Pak.GeneratePatch(PW);
                    PakPatch.Write(Path.Combine(_PatchesFolder, Pak.Name + ".json"));

                    CombinedPatch.Write(Path.Combine(_PatchesFolder, "Combined.json"));
                    CombinedPatch = Ext_JObject.Apply(CombinedPatch, PakPatch);
                } 
            }

            CombinedPatch.Write(Path.Combine(_PatchesFolder,"Combined.json"));
            CombinedPak.ApplyPatch(CombinedPatch);
            
            Console.WriteLine("DataTable List: ");
            foreach (UAssetFile UAF in CombinedPak.DataTableList)
            {
                Console.WriteLine($"Writing... {UAF.CorrectPath.Name} {UAF.Asset.DataTable().Count}");
                UAF.Write(Path.Combine(_Outputfolder));
            }
            
            CombinedPak.Repak("~RESULT\\Combined.pak");
            t.Stop();
            long PakSize = new FileInfo("~RESULT\\Combined.pak").Length;
            Console.WriteLine($"Total FileSize: {(double)PakSize / 1024.0 / 1024.0:0.00} MB");
            Console.WriteLine($"Total Runtime: {(double)t.ElapsedMilliseconds / 1000}s");
            Console.Write("Press Any Key to End...");
            Console.ReadKey(true);
        }
    }
}
