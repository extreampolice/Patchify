using Newtonsoft.Json.Linq;

namespace Patchify_Ext_JSON
{
    public static class Ext_JObject
    {
        public static bool Contains(this JObject pObject ,string key)
        {
            JToken? DummyToken = null;
            return pObject.TryGetValue(key, out DummyToken);
        }
        public static void Write(this JObject target, string path)
        {
            string Location = Path.GetDirectoryName(path);
            if (!Directory.Exists(Location)) Directory.CreateDirectory(Location);
            
            File.WriteAllText(path, target.ToString());
        }
        public static void PrintList(this JObject pObject)
        {
            foreach (var fPair in pObject)
            {
                Console.WriteLine($" -{fPair.Key}");
            }
            Console.WriteLine();
        }
        public static JObject FromFile(string pPath)
        {
            IEnumerable<string> Lines = File.ReadLines(pPath);
            string JObjectString = "";
            foreach (string Line in Lines) JObjectString += Line;
            JObject ReturnObject = JObject.Parse(JObjectString);

            return ReturnObject;
        }
        public static JObject Combine(JObject A, JObject B)
        {
            JObject ReturnObject = new JObject();
            foreach (var fPair in A) ReturnObject.Add(fPair.Key, fPair.Value);
            foreach (var fPair in B) ReturnObject.Add(fPair.Key, fPair.Value);
            return ReturnObject;
        }
        public static JObject Difference(JObject pOriginal, JObject pVariant)
        {
            JObject ReturnObject = new JObject();

            foreach (var fPair in pVariant)
            {
                if (!pOriginal.Contains(fPair.Key))
                {
                    ReturnObject.Add(fPair.Key, fPair.Value);
                }
                else
                {
                    JToken Original = pOriginal[fPair.Key];
                    JToken Variant = pVariant[fPair.Key];

                    if (Original.Type == JTokenType.Object)
                    {
                        JObject JDifference = Difference((JObject)Original, (JObject)Variant);
                        if (JDifference.Count != 0) ReturnObject.Add(fPair.Key, JDifference);
                    }
                    else
                    {
                        if (!JToken.DeepEquals(Original, Variant)) ReturnObject.Add(fPair.Key, Variant);

                    }
                }
            }

            return ReturnObject;
        }
        public static JObject Apply (JObject pOriginal, JObject pVariant)
        {
            JObject ReturnObject = (JObject)(pOriginal.DeepClone());

            foreach (var fPair in pVariant)
            {
                if (!pOriginal.Contains(fPair.Key))
                {
                    ReturnObject.Add(fPair.Key, fPair.Value.DeepClone());
                }
                else
                {
                    //??????
                    if (fPair.Value.Type == JTokenType.Object && pOriginal[fPair.Key].Type == JTokenType.Object)
                    {
                        ReturnObject[fPair.Key] = Apply((JObject)pOriginal[fPair.Key], (JObject)fPair.Value);
                    }
                    else
                    {
                        ReturnObject[fPair.Key] = fPair.Value.DeepClone();

                    }
                }
            }

            return ReturnObject;
        }
    }
}
