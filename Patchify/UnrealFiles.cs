using Newtonsoft.Json.Linq;
using Patchify_Ext_JSON;
using Patchify_Ext_String;
using Patchify_Ext_UAssetAPI;
using System.Diagnostics;
using UAssetAPI;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;

namespace UnrealFiles
{
    internal class Node
    {
        public string Name;
        public List<Node> Leaves;
        public Node(string Name)
        {
            this.Name = Name;
            Leaves = new List<Node>();
        }
        public void AddLeaf(Node Leaf)
        {
            Leaves.Add(Leaf);
        }
        public void Print()
        {
            bool is_end = (Leaves.Count == 0);
            PrintFR("", is_end);
        }
        private void PrintFR(string Prefix, bool is_end)
        {
            if (Prefix == "") Console.WriteLine(this.Name);
            else Console.WriteLine(Prefix + "└ " + this.Name);

            if (is_end == false && Prefix != "") Prefix += "| ";
            else Prefix += "  ";

            List<Node> ChildNode_End = new List<Node>();
            List<Node> ChildNode_Continue = new List<Node>();
            foreach (Node ChildNode in Leaves)
            {
                if (ChildNode.Leaves.Count == 0) ChildNode_End.Add(ChildNode);
                else ChildNode_Continue.Add(ChildNode);
            }

            foreach (Node ChildNode in ChildNode_End)
            {
                Console.WriteLine(Prefix + "└ " + ChildNode.Name);
            }

            for (int i = 0; i < ChildNode_Continue.Count - 1; i++)
            {
                ChildNode_Continue[i].PrintFR(Prefix, false);
            }
            if (ChildNode_Continue.Count > 0)
            {
                ChildNode_Continue[ChildNode_Continue.Count - 1].PrintFR(Prefix, true);
            }
        }
    }
    interface IPathInfo
    {
        public string OriginalPath { get;  }
        public string? Location { get; }
        public string Name { get; }
        public string Extension { get; }
    }
    class PathInfo
    {
        private readonly string _originalpath;
        public string Value => _originalpath;
        public string? Location => Path.GetDirectoryName(_originalpath);
        public string Name => Path.GetFileNameWithoutExtension(_originalpath);
        public string Extension => Path.GetExtension(_originalpath);
        public PathInfo(string path)
        {
            this._originalpath = path;
        }
    }
    class PakStruct
    {
        public List<UAssetStruct> AssetList;
        public List<UAssetFile> DataTableList;
        public void Repak(string outputPath)
        {
            string PakName = Path.GetFileName(outputPath);
            Stopwatch t = Stopwatch.StartNew();
            using (FileStream stream = new FileStream(outputPath, FileMode.Create))
            {
                var builder = new PakBuilder();
                var pak_writer = builder.Writer(stream);

                foreach (UAssetStruct fUAsset in this.AssetList)
                {
                    /*
                     If the Path uses \ instead / as separators, UAssetAPI does not 
                    recognize them as separators and just names the file as the Path.
                    What a joke.
                    */
                    string uassetPath = fUAsset.CorrectPath.Value.ToSlash();
                    string uexpPath = Path.ChangeExtension(uassetPath, ".uexp").ToSlash();
                    string ubulkPath = Path.ChangeExtension(uassetPath, ".ubulk").ToSlash();

                    pak_writer.WriteFile(uassetPath, fUAsset.uassetByte);
                    if (fUAsset.UseUexp) pak_writer.WriteFile(uexpPath, fUAsset.uexpByte);
                    if (fUAsset.UseUbulk) pak_writer.WriteFile(ubulkPath, fUAsset.ubulkByte);
                }
                foreach (UAssetFile fUAsset in this.DataTableList)
                {
                    string uassetPath = fUAsset.CorrectPath.Value.ToSlash();
                    string uexpPath = Path.ChangeExtension(uassetPath, ".uexp").ToSlash();

                    pak_writer.WriteFile(uassetPath, fUAsset.uassetByte);
                    if (fUAsset.UseUexp) pak_writer.WriteFile(uexpPath, fUAsset.uexpByte);
                }
                pak_writer.WriteIndex();
            }
            t.Stop();
            Console.WriteLine($"Repaking {PakName} Successfully Done! {(double)t.ElapsedMilliseconds / 1000.0}s");
        }
        public void ApplyPatch(JObject Patch)
        {
            foreach (UAssetFile DataBaseFile in this.DataTableList)
            {
                if (Patch.TryGetValue(DataBaseFile.CorrectPath.Name, out JToken Out))
                {
                    JObject DataBasePatch = (JObject)Out;
                    UAsset asset = DataBaseFile.Asset;

                    List<StructPropertyData> DataTable = asset.DataTable();
                    foreach (var fPair in DataBasePatch)
                    {
                        string StructName = fPair.Key;
                        JObject StructPatch = (JObject)fPair.Value;

                        StructPropertyData Struct = null;
                        foreach (StructPropertyData fStruct in DataTable)
                        {
                            if (fStruct.Name.ToString() == StructName)
                            {
                                Struct = fStruct;
                                break;
                            }
                        }

                        if (Struct != null)
                        {
                            ApplyJSON(Struct, StructPatch, asset);
                        }
                        else
                        {
                            StructPropertyData Tmp = (StructPropertyData)DataTable[0].CloneEmpty(asset);
                            Tmp.NameIt(DataBaseFile.Asset, StructName);
                            DataTable.Add(Tmp);
                            ApplyJSON(Tmp, StructPatch, asset);
                        }

                        JObject PlaneInfo = (JObject)fPair.Value;
                    }
                }
            }
        }
        private static void ApplyJSON(PropertyData property, JToken jtoken, UAsset asset)
        {
            switch (property.PropertyType.ToString())
            {
                case "Vector":
                    {
                        if (jtoken.Type != JTokenType.Array)
                        {
                            Console.WriteLine($"Error! Cannot edit Vector {property.Name.ToString()} with non-JArray!");
                            break;
                        }

                        JArray JVector = (JArray)jtoken;
                        if (JVector.Count != 3)
                        {
                            Console.WriteLine($"Error! Cannot edit Vector {property.Name.ToString()} with JArray whose count exceeds 3!");
                            break;
                        }

                        if (JVector[0].Type != JTokenType.Float || JVector[1].Type != JTokenType.Float || JVector[2].Type != JTokenType.Float)
                        {
                            Console.WriteLine($"Error! Cannot edit Vector {property.Name.ToString()} with JArray that contains non-float type variable!");
                            break;
                        }

                        double X = (double)((JValue)JVector[0]).Value;
                        double Y = (double)((JValue)JVector[1]).Value;
                        double Z = (double)((JValue)JVector[2]).Value;

                        VectorPropertyData propertyCast = (VectorPropertyData)property;
                        FVector NewVector = new FVector(X, Y, Z);
                        propertyCast.Value = NewVector;
                        break;
                    }
                case "ArrayProperty":
                    {
                        if (jtoken.Type != JTokenType.Array)
                        {
                            Console.WriteLine($"Error! Cannot edit ArrayPropertyData {property.Name.ToString()} with non-array JArray!");
                            break;
                        }

                        ArrayPropertyData TmpProperty = (ArrayPropertyData)property;
                        List<PropertyData> TmpList = new List<PropertyData>();
                        switch (TmpProperty.ArrayType.ToString())
                        {
                            case "IntProperty":
                                {
                                    foreach (JToken token in ((JArray)jtoken))
                                    {
                                        IntPropertyData NewInt = new IntPropertyData();
                                        NewInt.Value = (int)(long)((JValue)token).Value;
                                        TmpList.Add(NewInt);
                                    }
                                    break;
                                }
                            case "StrProperty":
                                {
                                    foreach (JToken token in ((JArray)jtoken))
                                    {
                                        StrPropertyData NewStr = new StrPropertyData();
                                        NewStr.Value = new FString((string)((JValue)token).Value);
                                        TmpList.Add(NewStr);
                                    }
                                    break;
                                }
                            case "NameProperty":
                                {
                                    foreach (JToken token in ((JArray)jtoken))
                                    {
                                        string NewNameStr = (string)((JValue)token).Value;
                                        int NewNameIndex = asset.AddNameReference(new FString(NewNameStr), false, false);
                                        NamePropertyData NewName = new NamePropertyData();
                                        NewName.Value = new FName(asset, NewNameIndex, 0);
                                        TmpList.Add(NewName);
                                    }
                                    break;
                                }
                            case "ObjectProperty":
                                {
                                    foreach (JToken token in ((JArray)jtoken))
                                    {
                                        JObject FirstImportJSON = (JObject)((JArray)token)[0];
                                        string FirstClassPackage = (string)(JValue)FirstImportJSON["ClassPackage"];
                                        string FirstClassName = (string)(JValue)FirstImportJSON["ClassName"];
                                        string FirstObjectName = (string)(JValue)FirstImportJSON["ObjectName"];

                                        JObject SecondImportJSON = (JObject)((JArray)token)[1];
                                        string SecondClassPackage = (string)(JValue)SecondImportJSON["ClassPackage"];
                                        string SecondClassName = (string)(JValue)SecondImportJSON["ClassName"];
                                        int SecondOuterIndex = (int)(JValue)SecondImportJSON["OuterIndex"];
                                        string SecondObjectName = (string)(JValue)SecondImportJSON["ObjectName"];


                                        Import SecondImport = new Import(SecondClassPackage, SecondClassName, new FPackageIndex(SecondOuterIndex), SecondObjectName, false, asset);
                                        FPackageIndex FirstOuterIndex = asset.AddImport(SecondImport);
                                        Import FirstImport = new Import(FirstClassPackage, FirstClassName, FirstOuterIndex, FirstObjectName, false, asset);
                                        FPackageIndex FinalIndex = asset.AddImport(FirstImport);

                                        ObjectPropertyData NewObject = new ObjectPropertyData();
                                        NewObject.Value = FinalIndex;

                                        TmpList.Add(NewObject);
                                    }
                                    break;
                                }
                        }
                        TmpProperty.Value = TmpList.ToArray();

                        break;
                    }
                case "StructProperty":
                    {
                        if (jtoken.Type != JTokenType.Object)
                        {
                            Console.WriteLine($"Error! Cannot edit StructPropertyData {property.Name.ToString()} with non-JObject!");
                            break;
                        }

                        StructPropertyData TmpStruct = (StructPropertyData)property;
                        JObject TmpObject = (JObject)jtoken;

                        foreach (PropertyData fProperty in TmpStruct.Value)
                        {
                            if (TmpObject.Contains(fProperty.Name.ToString()))
                            {
                                ApplyJSON(fProperty, TmpObject[fProperty.Name.ToString()], asset);
                            }
                        }
                        break;
                    }
                case "ObjectProperty":
                    {
                        if (jtoken.Type != JTokenType.Array)
                        {
                            Console.WriteLine($"Error! Cannot edit ObjectPropertyData {property.Name} with non-JArray!");
                            break;
                        }

                        JObject FirstImportJSON = (JObject)((JArray)jtoken)[0];
                        string FirstClassPackage = (string)(JValue)FirstImportJSON["ClassPackage"];
                        string FirstClassName = (string)(JValue)FirstImportJSON["ClassName"];
                        string FirstObjectName = (string)(JValue)FirstImportJSON["ObjectName"];

                        JObject SecondImportJSON = (JObject)((JArray)jtoken)[1];
                        string SecondClassPackage = (string)(JValue)SecondImportJSON["ClassPackage"];
                        string SecondClassName = (string)(JValue)SecondImportJSON["ClassName"];
                        int SecondOuterIndex = (int)(JValue)SecondImportJSON["OuterIndex"];
                        string SecondObjectName = (string)(JValue)SecondImportJSON["ObjectName"];


                        Import SecondImport = new Import(SecondClassPackage, SecondClassName, new FPackageIndex(SecondOuterIndex), SecondObjectName, false, asset);
                        FPackageIndex FirstOuterIndex = asset.AddImport(SecondImport);
                        Import FirstImport = new Import(FirstClassPackage, FirstClassName, FirstOuterIndex, FirstObjectName, false, asset);
                        FPackageIndex FinalIndex = asset.AddImport(FirstImport);

                        ((ObjectPropertyData)property).Value = FinalIndex;

                        break;
                    }
                case "TextProperty":
                    {
                        if (jtoken.Type != JTokenType.String)
                        {
                            Console.WriteLine($"Error! Cannot edit TextPropertyData {property.Name.ToString()} with non-String!");
                            break;
                        }
                        ((TextPropertyData)property).CultureInvariantString = new FString((string)((JValue)jtoken).Value);
                        break;
                    }
                case "ByteProperty":
                    {
                        if (jtoken.Type != JTokenType.Object)
                        {
                            Console.WriteLine($"Error! Cannot edit BytePropertyData {property.Name.ToString()} with non-JObject!");
                            break;
                        }
                        JObject JByte = (JObject)jtoken;
                        if (!JByte.Contains("EnumType"))
                        {
                            Console.WriteLine($"Error! Cannot edit BytePropertyData {property.Name.ToString()} with JObject that doesn't contain \"EnumType\"!");
                            break;
                        }
                        if (!JByte.Contains("EnumType"))
                        {
                            Console.WriteLine($"Error! Cannot edit BytePropertyData {property.Name.ToString()} with JObject that doesn't contain \"EnumType\"!");
                            break;
                        }

                        string EnumType = (string)((JValue)JByte["EnumType"]).Value;
                        string EnumValue = (string)((JValue)JByte["EnumValue"]).Value;
                        //((BytePropertyData)property).EnumType = 12;
                        break;
                    }
                case "IntProperty":
                    {
                        if (jtoken.Type != JTokenType.Integer)
                        {
                            Console.WriteLine($"Error! Cannot edit IntPropertyData {property.Name} with non-Integer");
                            break;
                        }
                        ((IntPropertyData)property).Value = (int)(long)((JValue)jtoken).Value;
                        break;
                    }
                case "FloatProperty":
                    {
                        if (jtoken.Type != JTokenType.Float)
                        {
                            Console.WriteLine($"Error! Cannot edit FloatPropertyData {property.Name} with non-Integer");
                            break;
                        }
                        ((FloatPropertyData)property).Value = (float)((JValue)jtoken).Value;
                        break;
                    }
                case "BoolProperty":
                    {
                        if (jtoken.Type != JTokenType.Boolean)
                        {
                            Console.WriteLine($"Error! Cannot edit BoolPropertyData {property.Name} with non-Boolean");
                            break;
                        }
                        ((BoolPropertyData)property).Value = (bool)((JValue)jtoken).Value;
                        break;
                    }
                case "StrProperty":
                    {
                        if (jtoken.Type != JTokenType.String)
                        {
                            Console.WriteLine($"Error! Cannot edit StrPropertyData {property.Name} with non-String");
                            break;
                        }
                        ((StrPropertyData)property).Value = new FString((string)((JValue)jtoken).Value);
                        break;
                    }
                case "NameProperty":
                    {
                        if (jtoken.Type != JTokenType.String)
                        {
                            Console.WriteLine($"Error! Cannot edit NamePropertyData {property.Name} with non-String");
                            break;
                        }
                        string NewName = (string)((JValue)jtoken).Value;
                        int NewNameIndex = asset.AddNameReference(new FString(NewName), false, false);
                        FName NewFName = new FName(asset, NewNameIndex, 0);
                        ((NamePropertyData)property).Value = NewFName;
                        break;
                    }
                default:
                    break;
            }
        }
        public PakStruct()
        {
            this.AssetList = new List<UAssetStruct>();
            this.DataTableList = new List<UAssetFile>();
        }
    }
    class PakFile : PakStruct, IDisposable, IPathInfo
    {
        private PathInfo _originalpath;
        public string OriginalPath => _originalpath.Value;
        public string Location => _originalpath.Location;
        public string Name => _originalpath.Name;
        public string Extension => _originalpath.Extension;
        private Node Tree;
        public FileStream PakFileStream;
        public PakReader Reader;
        public int Priority
        {
            get
            {
                int priority = 0;
                foreach (char Chr in this.Name ?? "")
                {
                    if (Chr == '~') priority++;
                    else break;
                }
                return priority;
            }
        }
        public string[] Files => this.Reader.Files();
        public void PrintTree()
        {
            if (this.Tree.Leaves.Count == 0) MakeTree();
            this.Tree.Print();
        }
        public void PrintContent()
        {
            foreach (string path in this.Reader.Files())
            {
                Console.WriteLine($"File: {path}");
            }
        }
        public UAssetFile GetSpecifiedUAssetFile(string path)
        {
            path = path.ToSlash();

            if (Path.GetExtension(path) != ".uasset") throw new Exception("The Extention must only be .uasset!");

            if (!this.Files.Contains(path)) throw new Exception($"{this.Name} does not contain {path}!");

            return new UAssetFile(this, path);
        }
        public void Unpak()
        {
            Console.Write($"Unpacking {this.Name}...");

            string PakPath = Path.Combine(this.Location, this.Name) + ".pak";

            //Having a DataBase NameList like this improves speed but limits scalability.    
            string[] DBNameList = 
            [
                "DAirUnitNPC",
                "DB_Aircraft",
                "DB_MF_Aircraft",
                "DWeaponDB",
                "HangarTextTable",
            ];
            Stopwatch t = Stopwatch.StartNew();
            foreach (string FilePath in this.Files)
            {
                if (Path.GetExtension(FilePath) != ".uasset") continue;

                if (DBNameList.Contains(Path.GetFileNameWithoutExtension(FilePath)))
                {
                    this.DataTableList.Add(new UAssetFile(this, FilePath.ToSlash()));
                }
                else
                {
                    this.AssetList.Add(new UAssetStruct(this, FilePath.ToSlash()));
                }
                //UAssetFile Result = this.GetSpecifiedUAssetFile(FilePath);
                //
                //if (Result.is_DataTable) this.DataTableList.Add(Result);
                //else this.AssetList.Add(new UAssetStruct(Result));
            }
            t.Stop();

            Console.WriteLine($"  Done! {(double)t.ElapsedMilliseconds / 1000.0}s");
        }
        public JObject GeneratePatch(PakFile PW)
        {
            JObject Patch = new JObject();
            foreach (UAssetFile UAF in this.DataTableList)
            {
                string UAFPath = UAF.CorrectPath.Value;
                JObject OriginalJSON = PW.GetSpecifiedUAssetFile(UAFPath).Asset.SerializeJsonSimple();
                JObject UAFJSON = UAF.Asset.SerializeJsonSimple();

                JObject Difference = Ext_JObject.Difference(OriginalJSON, UAFJSON);

                Patch = Ext_JObject.Combine(Patch, Difference);
            }
            return Patch;
        }
        public void Dispose()
        {
            foreach (UAssetFile uaf in this.DataTableList) uaf.Dispose();
            foreach (UAssetStruct uaf in this.AssetList) uaf.Dispose();
            //this.PakFileStream.Close();
        }
        public void ShavePlanes(PakFile PW)
        {
            JObject Patch = this.GeneratePatch(PW);
            JObject DB_Aircraft = (JObject)Patch["DB_Aircraft"];
            if (DB_Aircraft == null)
            {
                Console.WriteLine($"{this.Name} does not contain DB_Aircraft!");
                return;
            }
            bool NoPlaneAddsAircraftModel = true;
            List<string> PlanesThatReferencesAircraftModel = new List<string>();
            foreach (var fPair in DB_Aircraft)
            {
                bool AddsAircraftModel = false;
                if (((JObject)fPair.Value).ContainsKey("AircraftModel_2_CA30FD8A4DA9128A11322188C2138F70")) AddsAircraftModel = true;
                if (((JObject)fPair.Value).ContainsKey("CockpitViewModel_116_5D7DD8424750B14898B20CB80160E059")) AddsAircraftModel = true;
                if (((JObject)fPair.Value).ContainsKey("CockpitViewExternalModel_118_835488C045436D49C351BA9DD25ECAAB")) AddsAircraftModel = true;
                if (((JObject)fPair.Value).ContainsKey("GearModel_36_6B3AF44846A4BECBCBD9E78DF61BF9DF")) AddsAircraftModel = true;
                if (AddsAircraftModel)
                {
                    NoPlaneAddsAircraftModel = false;
                    PlanesThatReferencesAircraftModel.Add(fPair.Key);
                }
            }

            UAssetFile? UDB_Aircraft = null;
            foreach (UAssetFile UAF in this.DataTableList) if (UAF.CorrectPath.Name == "DB_Aircraft") UDB_Aircraft = UAF;
            for (int i = UDB_Aircraft.Asset.DataTable().Count - 1; i >= 0; i--)
            {
                StructPropertyData Plane = UDB_Aircraft.Asset.DataTable()[i];
            }
        }
        public PakFile(string PakPath) : base()
        {
            if (!File.Exists(PakPath))
            {
                throw new Exception($"Could not find '{PakPath}'!");
            }

            this._originalpath = new PathInfo(PakPath);

            PakBuilder Builder = new PakBuilder();
            this.PakFileStream = new FileStream(PakPath, FileMode.Open, FileAccess.Read);
            this.Reader = Builder.Reader(PakFileStream);

            this.Tree = new Node("Root");
        }
        ~PakFile()
        {
            //this.PakFileStream.Close();
        }
        private void MakeTree()
        {
            foreach (string Path in this.Files)
            {
                string[] Names = Path.Split('\\', '/');

                Node TempNode = Tree;
                for (int i = 0; i < Names.Length; i++)
                {
                    bool already_exist = false;
                    int index = 0;
                    for (int j = 0; j < TempNode.Leaves.Count; j++)
                    {
                        if (TempNode.Leaves[j].Name == Names[i])
                        {
                            already_exist = true;
                            index = j;
                            break;
                        }
                    }
                    if (already_exist)
                    {
                        TempNode = TempNode.Leaves[index];
                    }
                    else
                    {
                        TempNode.Leaves.Add(new Node(Names[i]));
                        TempNode = TempNode.Leaves[TempNode.Leaves.Count - 1];
                    }
                }
            }
        }
    }
    class UAssetStruct: IDisposable
    {
        public PathInfo OriginalPath;
        public PathInfo CorrectPath;
        public readonly PakFile ParentPak;
        public readonly bool UseUexp;
        public readonly bool UseUbulk;
        public virtual byte[] uassetByte
        {
            get
            {
                FileStream PakStream = ParentPak.PakFileStream;
                PakReader Reader = ParentPak.Reader;
                string uassetPath = OriginalPath.Value;
                return Reader.Get(PakStream, uassetPath);
            }
        }
        public virtual byte[] uexpByte
        {
            get
            {
                FileStream PakStream = ParentPak.PakFileStream;
                PakReader Reader = ParentPak.Reader;
                string uexpPath = Path.ChangeExtension(OriginalPath.Value, ".uexp");
                return Reader.Get(PakStream, uexpPath);
            }
        }
        public virtual byte[] ubulkByte
        {
            get
            {
                FileStream PakStream = ParentPak.PakFileStream;
                PakReader Reader = ParentPak.Reader;
                string ubulkPath = Path.ChangeExtension(OriginalPath.Value, ".ubulk");
                return Reader.Get(PakStream, ubulkPath);
            }
        }
        public virtual MemoryStream uassetStream => new MemoryStream(this.uassetByte);
        public virtual MemoryStream uexpStream => new MemoryStream(this.uexpByte);
        public virtual MemoryStream ubulkStream => new MemoryStream(this.ubulkByte);
        public void Write(string location)
        {
            if (!Directory.Exists(location)) Directory.CreateDirectory(location);

            string uassetPath = Path.Combine(location, this.OriginalPath.Name + ".uasset");
            string uexpPath = Path.Combine(location, this.OriginalPath.Name + ".uexp");
            string ubulkPath = Path.Combine(location, this.OriginalPath.Name + ".ubulk");

            File.WriteAllBytes(uassetPath, this.uassetByte);
            if (this.UseUexp) File.WriteAllBytes(uexpPath, this.uexpByte);
            if (this.UseUbulk) File.WriteAllBytes(ubulkPath, this.ubulkByte);
        }
        public void Dispose() { }
        public UAssetStruct(PakFile parentpak, string originalpath)
        {
            if (Path.GetExtension(originalpath) != ".uasset")
            {
                throw new Exception($"Failed to construct UAssetStruct. {originalpath} does not have .uasset extension");
            }
            string[] FolderNameList = originalpath.Split('\\', '/');
            string correctpath = "";
            switch (FolderNameList[0])
            {
                case "ProjectWingman":
                    {
                        if (FolderNameList[1] == "Content" || FolderNameList[1] == "Plugins")
                        {
                            correctpath = originalpath;

                        }
                        else
                        {
                            correctpath = Path.Combine("ProjectWingman", "Content", originalpath);
                        }
                        break;
                    }
                case "Content":
                    {
                        correctpath = Path.Combine("ProjectWingman", originalpath);
                        break;
                    }
                case "Plugins":
                    {
                        correctpath = Path.Combine("ProjectWingman", originalpath);
                        break;
                    }
                default:
                    {
                        correctpath = Path.Combine("ProjectWingman", "Content", originalpath);
                        break;
                    }
            }
            
            this.OriginalPath = new PathInfo(originalpath);
            this.CorrectPath = new PathInfo(correctpath);

            string UexpPath = Path.ChangeExtension(originalpath, ".uexp");
            string UbulkPath = Path.ChangeExtension(originalpath, ".ubulk");
            this.UseUexp = parentpak.Files.Contains(UexpPath);
            this.UseUbulk = parentpak.Files.Contains(UbulkPath);

            this.ParentPak = parentpak;
        }
        public UAssetStruct(UAssetFile uassetfile) : this(uassetfile.ParentPak, uassetfile.OriginalPath.Value) { }
    }
    class UAssetFile : UAssetStruct
    {
        public UAsset Asset;
        public bool is_DataTable => Asset.IsDataTable();
        public override byte[] uassetByte => this.uassetStream.ToArray();
        public override byte[] uexpByte => this.uexpStream.ToArray();
        public override MemoryStream uassetStream
        {
            get
            {
                MemoryStream ReturnStream = new MemoryStream();
                this.Asset.Write(out ReturnStream, out _);
                return ReturnStream;
            }
        }
        public override MemoryStream uexpStream
        {
            get
            {
                MemoryStream ReturnStream = new MemoryStream();
                this.Asset.Write(out _, out ReturnStream);
                return ReturnStream;
            }
        }
        public UAssetFile(PakFile parentpak, string originalpath) : base(parentpak, originalpath)
        {
            using (UAssetStruct TmpStruct = new UAssetStruct(parentpak, originalpath))
            {
                MemoryStream MainStream = new MemoryStream();
                TmpStruct.uassetStream.CopyTo(MainStream);
                TmpStruct.uexpStream.CopyTo(MainStream);

                AssetBinaryReader ABR = new AssetBinaryReader(MainStream, TmpStruct.UseUexp);

                this.Asset = new UAsset(ABR, EngineVersion.VER_UE4_27, null, TmpStruct.UseUexp);
            }
        }
        public UAssetFile(UAssetStruct uassetstruct):this(uassetstruct.ParentPak, uassetstruct.OriginalPath.Value)
        {

        }
    }
}
