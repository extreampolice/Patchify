using Newtonsoft.Json.Linq;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;

namespace Patchify_Ext_UAssetAPI
{
    public static class Ext_UAssetAPI
    {
        public static bool IsDataTable(this UAsset pUAsset)
        {
            if (pUAsset.Exports[0].GetType() == typeof(DataTableExport))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static List<StructPropertyData> DataTable(this UAsset pUAsset)
        {
            return ((DataTableExport)pUAsset.Exports[0]).Table.Data;
        }
        private static JToken ValueOf(PropertyData property, UAsset asset)
        {
            JToken? ReturnJToken = null;

            switch (property.PropertyType.ToString())
            {
                case "Vector":
                    {
                        VectorPropertyData sProperty = (VectorPropertyData)property;
                        
                        JArray ReturnArray = new JArray();
                        ReturnArray.Add(sProperty.Value.X);
                        ReturnArray.Add(sProperty.Value.Y);
                        ReturnArray.Add(sProperty.Value.Z);

                        ReturnJToken = (JToken)ReturnArray;

                        break;
                    }
                case "ArrayProperty":
                    {
                        ArrayPropertyData sProperty = (ArrayPropertyData)property;

                        JArray ReturnArray = new JArray();
                        foreach (PropertyData fProperty in sProperty.Value)
                        {
                            ReturnArray.Add(ValueOf(fProperty, asset));
                        }

                        ReturnJToken = (JToken)ReturnArray;

                        break;
                    }
                case "StructProperty":
                    {
                        StructPropertyData sProperty = (StructPropertyData)property;

                        JObject ReturnObject = new JObject();
                        foreach(PropertyData fProperty in sProperty.Value)
                        {
                            ReturnObject.Add(fProperty.Name.ToString(), ValueOf(fProperty, asset));
                        }

                        ReturnJToken = (JToken)ReturnObject;

                        break;
                    }
                case "ObjectProperty":
                    {
                        try
                        {
                            List<Import> Imports = asset.Imports;

                            ObjectPropertyData ObjectProperty = (ObjectPropertyData)property;
                            int FirstIndex = -(ObjectProperty.Value.Index) - 1;
                            Import FirstImport = Imports[FirstIndex];

                            int SecondIndex = -(FirstImport.OuterIndex.Index) - 1;
                            Import SecondImport = Imports[SecondIndex];


                            JObject FirstImportJSON = new JObject();
                            JObject SecondImportJSON = new JObject();

                            FirstImportJSON.Add("ClassPackage", FirstImport.ClassPackage.ToString());
                            FirstImportJSON.Add("ClassName", FirstImport.ClassName.ToString());
                            FirstImportJSON.Add("OuterIndex", FirstImport.OuterIndex.Index);
                            FirstImportJSON.Add("ObjectName", FirstImport.ObjectName.ToString());

                            SecondImportJSON.Add("ClassPackage", SecondImport.ClassPackage.ToString());
                            SecondImportJSON.Add("ClassName", SecondImport.ClassName.ToString());
                            SecondImportJSON.Add("OuterIndex", SecondImport.OuterIndex.Index);
                            SecondImportJSON.Add("ObjectName", SecondImport.ObjectName.ToString());

                            JArray ImportArray = new JArray();
                            ImportArray.Add(FirstImportJSON);
                            ImportArray.Add(SecondImportJSON);

                            ReturnJToken = (JToken)ImportArray;

                            break;
                        }
                        catch (Exception e)
                        {
                            break;
                        }
                    }
                case "TextProperty":
                    {
                        TextPropertyData TextProperty = (TextPropertyData)property;

                        if (TextProperty.CultureInvariantString != null)
                        {
                            ReturnJToken = (JToken)(new JValue(TextProperty.CultureInvariantString.ToString()));
                        }
                        else
                        {
                            ReturnJToken = (JToken)(JValue.CreateNull());
                        }
                        break;
                    }
                case "ByteProperty":
                    {
                        BytePropertyData ByteProperty = (BytePropertyData)property;

                        JObject JByte = new JObject();
                        JByte.Add("EnumType", ByteProperty.EnumType.ToString());
                        JByte.Add("EnumValue", ByteProperty.EnumValue.ToString());

                        ReturnJToken = (JToken)JByte;

                        break;
                    }
                case "IntProperty":
                    {
                        IntPropertyData IntProperty = (IntPropertyData)property;

                        ReturnJToken = (JToken)(new JValue(IntProperty.Value));

                        break;
                    }
                case "FloatProperty":
                    {
                        FloatPropertyData FloatProperty = (FloatPropertyData)property;

                        ReturnJToken = (JToken)(new JValue(FloatProperty.Value));

                        break;
                    }
                case "BoolProperty":
                    {
                        BoolPropertyData BoolProperty = (BoolPropertyData)property;

                        ReturnJToken = (JToken)(new JValue(BoolProperty.Value));

                        break;
                    }
                case "StrProperty":
                    {
                        StrPropertyData StrProperty = (StrPropertyData)property;

                        if (StrProperty.Value == null)
                        {
                            ReturnJToken = (JToken)(JValue.CreateNull());
                        }
                        else
                        {
                            ReturnJToken = (JToken)(new JValue(StrProperty.Value.ToString()));
                        }

                        break;
                    }
                case "NameProperty":
                    {
                        NamePropertyData NameProperty = (NamePropertyData)property;

                        ReturnJToken = (JToken)(new JValue(NameProperty.Value.ToString()));

                        break;
                    }
                default:
                    break;
            }

            return ReturnJToken;
        }
        public static PropertyData CloneEmpty(this PropertyData pProperty, UAsset asset)
        {
            PropertyData ReturnProperty = null;

            switch (pProperty.PropertyType.ToString())
            {
                case "Vector":
                    {
                        ReturnProperty = new VectorPropertyData(pProperty.Name);
                        break;
                    }
                case "ArrayProperty":
                    {
                        ReturnProperty = new ArrayPropertyData(pProperty.Name);
                        ((ArrayPropertyData)ReturnProperty).ArrayType = ((ArrayPropertyData)pProperty).ArrayType;
                        break;
                    }
                case "StructProperty":
                    {
                        ReturnProperty = new StructPropertyData(pProperty.Name);
                        ((StructPropertyData)ReturnProperty).StructType = ((StructPropertyData)pProperty).StructType;

                        foreach (PropertyData fProperty in ((StructPropertyData)pProperty).Value)
                        {
                            ((StructPropertyData)ReturnProperty).Value.Add(fProperty.CloneEmpty(asset));
                        }

                        break;
                    }
                case "ObjectProperty":
                    {
                        ReturnProperty = new ObjectPropertyData(pProperty.Name);
                        break;
                    }
                case "TextProperty":
                    {
                        ReturnProperty = (PropertyData)((TextPropertyData)pProperty).Clone();
                        ((TextPropertyData)ReturnProperty).CultureInvariantString = new FString();
                        // If the "Value" of the Multiple TextPropertyDatas are the same, They all ignore their
                        //CultureInvariantString and use the CultureInvariantString of the last TextPropertyData
                        //that uses the same Value. I do not know why this happens. but leaving it at null
                        //seems to solve this issue. I have not faced any side effects of doing this thus far.
                        ((TextPropertyData)ReturnProperty).Value = new FString();
                        break;
                    }
                case "ByteProperty":
                    {
                        ReturnProperty = new BytePropertyData(pProperty.Name);
                        ((BytePropertyData)ReturnProperty).ByteType = ((BytePropertyData)pProperty).ByteType;
                        ((BytePropertyData)ReturnProperty).EnumType = ((BytePropertyData)pProperty).EnumType;
                        ((BytePropertyData)ReturnProperty).EnumValue = ((BytePropertyData)pProperty).EnumValue;
                        break;
                    }
                case "IntProperty":
                    {
                        ReturnProperty = new IntPropertyData(pProperty.Name);
                        break;
                    }
                case "FloatProperty":
                    {
                        ReturnProperty = new FloatPropertyData(pProperty.Name);
                        break;
                    }
                case "BoolProperty":
                    {
                        ReturnProperty = new BoolPropertyData(pProperty.Name);
                        break;
                    }
                case "StrProperty":
                    {
                        ReturnProperty = new StrPropertyData(pProperty.Name);
                        ((StrPropertyData)ReturnProperty).Value = new FString("");
                        break;
                    }
                case "NameProperty":
                    {
                        ReturnProperty = new NamePropertyData(pProperty.Name);
                        break;
                    }
                default:
                    break;
            }

            if (ReturnProperty == null)
            {
                throw new Exception($"{pProperty.PropertyType.ToString()} {pProperty.Name.ToString()}: Returning null!");
            }
            return ReturnProperty;
        }
        public static void NameIt(this PropertyData pProperty, UAsset asset, string Name)
        {
            pProperty.Name = new FName(asset, Name);
        }
        public static JObject SerializeJsonSimple(this UAsset pUAsset)
        {
            JObject ReturnObject = new JObject();

            string FileName = pUAsset.Exports[0].ObjectName.ToString();

            JObject FileContent = new JObject();
            foreach (StructPropertyData fStruct in pUAsset.DataTable())
            {
                string fStructName = fStruct.Name.ToString();
                JToken fStructValue = ValueOf((PropertyData)fStruct, pUAsset);
                FileContent.Add(fStructName, fStructValue);
            }

            ReturnObject.Add(FileName, FileContent);

            return ReturnObject;
        }
    }
}

