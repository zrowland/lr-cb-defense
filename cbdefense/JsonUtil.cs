using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace cbdefense
{
    public static class Util
    {
        public static void DateLog(string InputString)
        {
            Console.WriteLine(String.Format(@"{0}: {1}", DateTime.Now.ToString("s"), InputString));
            return;
        }

        public static string GetEpochToDate(string EpochTime)
        {
            string outDate = @"";
            try
            {

                DateTime dOrigin = DateTime.Parse(@"1/1/1970");
                double ddEpoch = Double.Parse(EpochTime);
                dOrigin = dOrigin.AddMilliseconds(ddEpoch);
                //outDate = dOrigin.ToString(@"yyyy-MM-dd hh:mm:ss.fff");
                DateTime dFinal = dOrigin.ToLocalTime();
                outDate = dFinal.ToString(@"yyyy-MM-dd hh:mm:ss.fff");
                return outDate;
            }
            catch
            {
                outDate = DateTime.Now.ToString(@"yyyy-MM-dd hh:mm:ss.fff");
                return outDate;
            }
        }
    }

    public class JsonField
    {
        public int Order;
        public string Name;
        public string TypeName;
        public object Value;

        public JsonField()
        {
            this.Order = 0;
            this.Name = "none";
            this.TypeName = "none";
            this.Value = null;
        }

        public JsonField(int Order, string Name, string TypeName, object Value)
        {
            this.Order = Order;
            this.Name = Name;
            this.TypeName = TypeName;
            this.Value = Value;
        }
    }

    public class JsonEvent
    {
        public SortedList<int, string> FieldOrder;
        //public JsonField[] aFields;
        public SortedList<int, JsonField> Fields;
        public string LogOutput;

        public JsonEvent()
        {
            this.FieldOrder = new SortedList<int, string>();
            this.Fields = new SortedList<int, JsonField>();
            //this.aFields = null;
            this.LogOutput = @"";
        }

        public void ImportFields(SortedList<int, JsonField> sl)
        {
            this.Fields = sl;
        }

        public void PrintFields()
        {
            foreach (JsonField jf in this.Fields.Values)
            {
                Console.WriteLine(jf.Name);
            }
        }

        public void SetLogOutput(string LogVal)
        {
            this.LogOutput = LogVal;
            return;
        }

        // ===== Known Good/Original method

        public void GetLogOutput(bool DoBreaks)
        {
            foreach (JsonField jf in this.Fields.Values)
            {
                if (jf.TypeName != "Object" && jf.TypeName != "Array")
                {
                    string tmpStr = String.Format(@"{0}={1}|", jf.Name, jf.Value);
                    this.LogOutput += tmpStr;
                }
            }

            this.LogOutput = this.LogOutput.TrimEnd("|".ToCharArray());

            if (DoBreaks)
            {
                this.LogOutput = "\n\n" + this.LogOutput;
                this.LogOutput = this.LogOutput.Replace('|', (char)0x0A);
            }

            return;
        }

        public void GetLogOutput2(bool DoBreaks)
        {
            Regex r = new Regex(@"createTime=(\d{13})", RegexOptions.None);
            foreach (JsonField jf in this.Fields.Values)
            {
                if (jf.TypeName != "Object" && jf.TypeName != "Array")
                {
                    string tmpStr = String.Format(@"{0}={1}|", jf.Name, jf.Value);
                    this.LogOutput += tmpStr;
                }
            }

            this.LogOutput = this.LogOutput.TrimEnd("|".ToCharArray());

            if (DoBreaks)
            {
                this.LogOutput = "\n\n" + this.LogOutput;
                this.LogOutput = this.LogOutput.Replace('|', (char)0x0A);
            }

            if (r.IsMatch(this.LogOutput))
            {
                Match m = r.Match(this.LogOutput);
                string matchStr = m.Groups[0].Value;
                string repStr = m.Groups[1].Value;
                string newDate = String.Format(@"eventTime={0}",Util.GetEpochToDate(repStr));
                this.LogOutput = this.LogOutput.Replace(matchStr, newDate);
            }

            return;
        }

        public void TestPrint()
        {
            string outputString = @"";

            foreach (JsonField jf in this.Fields.Values)
            {
                if (jf.TypeName != "Object" && jf.TypeName != "Array")
                {
                    string tmpStr = String.Format(@"{0}={1}|", jf.Name, jf.Value);
                    outputString += tmpStr;
                }
            }

            outputString = outputString.TrimEnd("|".ToCharArray());

            //Console.WriteLine(outputString);
            Util.DateLog(outputString);

            return;
        }

        public void TestPrint(bool DoBreaks)
        {
            string outputString = @"";

            foreach (JsonField jf in this.Fields.Values)
            {
                if (jf.TypeName != "Object" && jf.TypeName != "Array")
                {
                    string tmpStr = String.Format(@"{0}={1}|", jf.Name, jf.Value);
                    outputString += tmpStr;
                }
            }

            outputString = outputString.TrimEnd("|".ToCharArray());

            if (DoBreaks)
            {
                outputString = "\n\n" + outputString;
                outputString = outputString.Replace('|', (char)0x0A);
            }

            //Console.WriteLine(outputString);
            Util.DateLog(outputString);

            return;
        }
    }

    public class JsonTools
    {
        public JsonEvent ProcJson(string InputFile)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            JsonEvent je = new JsonEvent();
            SortedList<int, string> tmpFieldOrder = new SortedList<int, string>();
            SortedList<int, JsonField> tmpFields = new SortedList<int, JsonField>();

            string jsonStr = "";
            
            using (FileStream fs = new FileStream(InputFile, FileMode.Open, FileAccess.ReadWrite))
            {
                StreamReader sr = new StreamReader(fs);
                jsonStr = sr.ReadToEnd();
                sr.Close();
                fs.Close();
            }
            
            JObject jobj = (JObject)JsonConvert.DeserializeObject(jsonStr);

            int tmpFieldCounter = 1;
            
            // ========== Property Enumeration/Processing, rev 1: DEPRECATED, using rev 2 below

            /*foreach (JProperty jp in jobj.Properties())
            {
                //string tmpPropOut = @"";
                string tmpPropName = jp.Name;
                string tmpPropType = jp.Value.Type.ToString();
                //string tmpPropVal = @"";
                object tmpPropVal = null;
                Type tmpTypeType = Type.GetType(tmpPropType.ToLower());

                if (tmpPropType == "Object" || tmpPropType == "Array")
                {
                    tmpPropVal = new object();
                    tmpPropVal = jp.Value;
                    
                    if (jp.Value != null)
                    {
                        string propPrefix = tmpPropName;
                        JObject jo = (JObject)tmpPropVal;
                        foreach (JProperty jpp in jo.Properties())
                        {

                        }
                    }
                }
                else if (tmpPropType == "Null")
                {
                    tmpPropVal = @"none";
                }
                else
                {
                    tmpPropVal = jp.Value.ToString();
                }

                //tmpPropVal = jp.Value;

                JsonField jf = new JsonField(tmpFieldCounter, tmpPropName, tmpPropType, null);
                switch (tmpPropType)
                {
                    case "String":
                        //jf.Value = (string)tmpPropVal;
                        jf.Value = (string)((JValue)tmpPropVal.ToString());
                        break;
                    case "Null":
                        //jf.Value = (string)((JValue)tmpPropVal.ToString());
                        jf.Value = "none";
                        break;
                    case "Object":
                        jf.Value = new object();
                        jf.Value = (object)tmpPropVal;
                        break;
                    case "Int":
                        jf.Value = new int();
                        jf.Value = (int)tmpPropVal;
                        break;
                    default:
                        jf.Value = new object();
                        jf.Value = (object)tmpPropVal;
                        break;
                }
                tmpFields.Add(tmpFieldCounter, jf);

                tmpFieldOrder.Add(tmpFieldCounter, jp.Name);
                tmpFieldCounter++;
            }*/

            // ========== Property Enumeration/Processing, rev 2: Current 8-31-20

            foreach (JProperty jp in jobj.Properties())
            {
                JsonField jf = new JsonField(tmpFieldCounter, jp.Name, jp.Value.Type.ToString(), null);

                switch (jp.Value.Type.ToString())
                {
                    case "String":
                        jf.Value = (string)((JValue)jp.Value.ToString());
                        break;
                    case "Null":
                        jf.Value = "null";
                        break;
                    case "Object":
                        jf.Value = new object();
                        jf.Value = (object)jp.Value;
                        break;
                    case "Int":
                        jf.Value = new int();
                        jf.Value = (int)jp.Value;
                        break;
                    default:
                        jf.Value = new object();
                        jf.Value = (object)jp.Value;
                        break;
                }

                tmpFields.Add(tmpFieldCounter, jf);
                tmpFieldOrder.Add(tmpFieldCounter, jp.Name);
                tmpFieldCounter++;
            }

            je.ImportFields(tmpFields);
            sw.Stop();
            Util.DateLog(String.Format(@"Conversion time: {0}", sw.Elapsed));

            return je;
         }

        public List<JsonEvent> ProcJson(string InputObject, string ParentObjectKey, bool DoInputString)
        {
            Stopwatch sw = new Stopwatch();
            //Stopwatch sw2 = new Stopwatch();
            sw.Start();
            List<JsonEvent> jsonEvents = new List<JsonEvent>();
            //JsonEvent je = new JsonEvent();
            //SortedList<int, string> tmpFieldOrder = new SortedList<int, string>();
            //SortedList<int, JsonField> tmpFields = new SortedList<int, JsonField>();

            string jsonStr = "";

            if (DoInputString)
            {
                jsonStr = InputObject;
            }
            else
            {
                using (FileStream fs = new FileStream(InputObject, FileMode.Open, FileAccess.ReadWrite))
                {
                    StreamReader sr = new StreamReader(fs);
                    jsonStr = sr.ReadToEnd();
                    sr.Close();
                    fs.Close();
                }
            }

            JObject jpobj = (JObject)JsonConvert.DeserializeObject(jsonStr);

            JToken jptk = jpobj[ParentObjectKey];

            if (jptk.Type.ToString() != "Array")
            {
                Util.DateLog(@"ERROR: Parent object is not an array!");
                throw new Exception(@"Parent object is not an array!");
            }

            int totChilds = jptk.Count();
            Util.DateLog(String.Format(@"Utilizing parent object: {0}", ParentObjectKey));
            Util.DateLog(String.Format(@"Found {0} child objects to process", totChilds));

            //foreach (JToken jtk in jptk.Values())
            //foreach (JToken jtk in jptk) // Good
            foreach (JObject jobj in jptk)
            {
                //sw2.Start();
                JsonEvent je = new JsonEvent();
                //SortedList<int, string> tmpFieldOrder = new SortedList<int, string>();
                SortedList<int, JsonField> tmpFields = new SortedList<int, JsonField>();

                int tmpFieldCounter = 1;

                foreach (JProperty jp in jobj.Properties())
                {
                    JsonField jf = new JsonField(tmpFieldCounter, jp.Name, jp.Value.Type.ToString(), null);

                    switch (jp.Value.Type.ToString())
                    {
                        case "String":
                            jf.Value = (string)((JValue)jp.Value.ToString());
                            break;
                        case "Null":
                            jf.Value = "null";
                            break;
                        case "Object":
                            // Method 1 - leave as string
                            //jf.Value = new object();
                            //jf.Value = (object)jp.Value;

                            // Method 2 - Second order conversion
                            jf.Value = (string)@"object";
                            JObject tmpChildJson = (JObject)JsonConvert.DeserializeObject(jp.Value.ToString());
                            this.ProcJsonChild2(tmpChildJson, jp.Name, ref tmpFieldCounter, ref tmpFields);
                            break;
                        case "Int":
                            jf.Value = new int();
                            jf.Value = (int)jp.Value;
                            break;
                        default:
                            jf.Value = new object();
                            jf.Value = (object)jp.Value;
                            break;
                    }

                    tmpFields.Add(tmpFieldCounter, jf);
                    //tmpFieldOrder.Add(tmpFieldCounter, jp.Name);
                    tmpFieldCounter++;
                }

                je.ImportFields(tmpFields);
                je.GetLogOutput2(false);
                jsonEvents.Add(je);
                //sw2.Stop();
                //Util.DateLog(String.Format(@"Conversion time: {0}", sw2.Elapsed));
                //sw2.Reset();
            }

            sw.Stop();
            Util.DateLog(String.Format(@"Total conversion time: {0}", sw.Elapsed));
            double dEventRate = (double)(totChilds / sw.Elapsed.TotalSeconds);
            //Util.DateLog(String.Format(@"Conversion rate: {1}{0:F2} logs/sec{2}", dEventRate, "\x1B[92m", "\x1B[0m"));
            Util.DateLog(String.Format(@"Conversion rate: {0:F2} logs/sec", dEventRate));

            return jsonEvents;
        }

        public List<JsonEvent> ProcJson3(string InputObject, string ParentObjectKey, bool DoInputString)
        {
            Stopwatch sw = new Stopwatch();
            //Stopwatch sw2 = new Stopwatch();
            sw.Start();
            List<JsonEvent> jsonEvents = new List<JsonEvent>();
            //JsonEvent je = new JsonEvent();
            //SortedList<int, string> tmpFieldOrder = new SortedList<int, string>();
            //SortedList<int, JsonField> tmpFields = new SortedList<int, JsonField>();

            // Set up temporary hash table to test field ordering
            Hashtable hPrefFieldOrder = new Hashtable();
            hPrefFieldOrder.Add(@"eventTime", 1);
            hPrefFieldOrder.Add(@"eventType", 2);
            hPrefFieldOrder.Add(@"threatScore", 3);
            hPrefFieldOrder.Add(@"longDescription", -1);

            string jsonStr = "";

            if (DoInputString)
            {
                jsonStr = InputObject;
            }
            else
            {
                using (FileStream fs = new FileStream(InputObject, FileMode.Open, FileAccess.ReadWrite))
                {
                    StreamReader sr = new StreamReader(fs);
                    jsonStr = sr.ReadToEnd();
                    sr.Close();
                    fs.Close();
                }
            }

            JObject jpobj = (JObject)JsonConvert.DeserializeObject(jsonStr);

            JToken jptk = jpobj[ParentObjectKey];

            if (jptk.Type.ToString() != "Array")
            {
                Util.DateLog(@"ERROR: Parent object is not an array!");
                throw new Exception(@"Parent object is not an array!");
            }

            int totChilds = jptk.Count();
            Util.DateLog(String.Format(@"Utilizing parent object: {0}", ParentObjectKey));
            Util.DateLog(String.Format(@"Found {0} child objects to process", totChilds));

            //foreach (JToken jtk in jptk.Values())
            //foreach (JToken jtk in jptk) // Good
            foreach (JObject jobj in jptk)
            {
                //sw2.Start();
                JsonEvent je = new JsonEvent();
                //SortedList<int, string> tmpFieldOrder = new SortedList<int, string>();
                SortedList<int, JsonField> tmpFields = new SortedList<int, JsonField>();

                int tmpFieldCounter = 1;

                foreach (JProperty jp in jobj.Properties())
                {
                    JsonField jf = new JsonField(tmpFieldCounter, jp.Name, jp.Value.Type.ToString(), null);

                    switch (jp.Value.Type.ToString())
                    {
                        case "String":
                            jf.Value = (string)((JValue)jp.Value.ToString());
                            break;
                        case "Null":
                            jf.Value = "null";
                            break;
                        case "Object":
                            // Method 1 - leave as string
                            //jf.Value = new object();
                            //jf.Value = (object)jp.Value;

                            // Method 2 - Second order conversion
                            jf.Value = (string)@"object";
                            JObject tmpChildJson = (JObject)JsonConvert.DeserializeObject(jp.Value.ToString());
                            this.ProcJsonChild2(tmpChildJson, jp.Name, ref tmpFieldCounter, ref tmpFields);
                            break;
                        case "Int":
                            jf.Value = new int();
                            jf.Value = (int)jp.Value;
                            break;
                        default:
                            jf.Value = new object();
                            jf.Value = (object)jp.Value;
                            break;
                    }

                    tmpFields.Add(tmpFieldCounter, jf);
                    //tmpFieldOrder.Add(tmpFieldCounter, jp.Name);
                    tmpFieldCounter++;
                }

                // Perform field ordering here

                // ============ METHOD 1

                /*//SortedList<int, JsonField> tmpFields2 = tmpFields; // Method 2
                //SortedList<int, JsonField> tmpFields2 = new SortedList<int, JsonField>(); // Method 2
                //tmpFields2 = tmpFields; // Method 2
                foreach (KeyValuePair<int,JsonField> kvpjf in tmpFields)
                {
                    if (hPrefFieldOrder.ContainsKey(kvpjf.Value.Name))
                    {
                        //tmpFields.Remove(kvpjf.Key); // Method 1
                        int oldFieldOrder = kvpjf.Key;
                        int newFieldOrder = (int)hPrefFieldOrder[kvpjf.Value.Name];
                        JsonField tjf = kvpjf.Value;
                        tjf.Order = newFieldOrder;
                        //KeyValuePair<int, JsonField> tjf2 = new KeyValuePair<int, JsonField>(newFieldOrder, tjf);

                        //tmpFields.Add(newFieldOrder, tjf); // Method 1
                        //tmpFields2[newFieldOrder] = tjf; // Method 2
                    }
                }*/

                // ============ METHOD 2

                //List<string> tmpFNames = new List<string>();
                Hashtable hTmpFieldNames = new Hashtable();
                Hashtable hTmpFieldValues = new Hashtable();
                Hashtable hTmpFieldTypes = new Hashtable();
                Dictionary<string, int> dTmpFieldNames = new Dictionary<string, int>();
                Dictionary<string, string> dTmpFieldValues = new Dictionary<string, string>();
                Dictionary<string, string> dTmpFieldTypes = new Dictionary<string, string>();
                int ctrTotalFields = 0;

                foreach (KeyValuePair<int,JsonField> kvpjf in tmpFields)
                {
                    //tmpFNames.Add(kvpjf.Value.Name);
                    hTmpFieldNames.Add(kvpjf.Value.Name, kvpjf.Key);
                    hTmpFieldValues.Add(kvpjf.Value.Name, kvpjf.Value.Value);
                    hTmpFieldTypes.Add(kvpjf.Value.Name, kvpjf.Value.TypeName);
                    dTmpFieldNames.Add(kvpjf.Value.Name, kvpjf.Key);
                    dTmpFieldValues.Add(kvpjf.Value.Name, kvpjf.Value.Value.ToString());
                    dTmpFieldTypes.Add(kvpjf.Value.Name, kvpjf.Value.TypeName);
                    ctrTotalFields++;
                }



                //int ctNonZeroes = dTmpFieldNames.Count(f => f.Value > 0);
                //int ctSubZeroes = dTmpFieldNames.Count(f => f.Value < 0);

                //var testGT50 = dTmpFieldNames.Select(f => f.Value > 20);
                //var testGT50v2 = dTmpFieldNames.Where(f => f.Value > 20);
                //var testGT50v3 = dTmpFieldNames.Where(f => f.Value > 0);
                //var testGT50v4 = dTmpFieldNames.Where(f => f.Value > 0).OrderBy(f2 => f2.Key);
                //var testGT50v5 = dTmpFieldNames.Where(f => f.Value > 0).OrderByDescending(f2 => f2.Value);
                /*foreach (var iv5 in testGT50v5)
                {
                    Console.WriteLine(String.Format(@"{0}: {1}", iv5.Key, iv5.Value));
                }*/
                //var testGT50v6 = dTmpFieldNames.Where(f => hPrefFieldOrder.ContainsKey(f.Key)); // Getting closer

                string tmpLogOut = @"";
                //var orderedFields = dTmpFieldNames.Where(f => hPrefFieldOrder.ContainsKey(f.Key) && ((int)hPrefFieldOrder[f.Key] != -1));
                var orderedFields = dTmpFieldNames.Where(f => hPrefFieldOrder.ContainsKey(f.Key) && ((int)hPrefFieldOrder[f.Key] != -1)).OrderBy(f2 => (int)hPrefFieldOrder[f2.Key]);
                foreach (var of in orderedFields)
                {
                    tmpLogOut += String.Format(@"{0}={1}|", of.Key, dTmpFieldValues[of.Key]);
                }
                var unorderedFields = dTmpFieldNames.Where(f => hPrefFieldOrder.ContainsKey(f.Key) == false && dTmpFieldTypes[f.Key] != "Array" && dTmpFieldTypes[f.Key] != "Object");
                foreach (var uf in unorderedFields)
                {
                    tmpLogOut += String.Format(@"{0}={1}|", uf.Key, dTmpFieldValues[uf.Key]);
                }
                Regex r = new Regex(@"eventTime=(\d{13})", RegexOptions.None);
                if (r.IsMatch(tmpLogOut))
                {
                    Match m = r.Match(tmpLogOut);
                    string matchStr = m.Groups[0].Value;
                    string repStr = m.Groups[1].Value;
                    string newDate = String.Format(@"eventTime={0}", Util.GetEpochToDate(repStr));
                    tmpLogOut = tmpLogOut.Replace(matchStr, newDate);
                }
                tmpLogOut = tmpLogOut.TrimEnd("|".ToCharArray());
                je.SetLogOutput(tmpLogOut);
                //Console.WriteLine(tmpLogOut);
                jsonEvents.Add(je);
                //sw2.Stop();
                //Util.DateLog(String.Format(@"Conversion time: {0}", sw2.Elapsed));
                //sw2.Reset();
            }

            sw.Stop();
            Util.DateLog(String.Format(@"Total conversion time: {0}", sw.Elapsed));
            double dEventRate = (double)(totChilds / sw.Elapsed.TotalSeconds);
            //Util.DateLog(String.Format(@"Conversion rate: {1}{0:F2} logs/sec{2}", dEventRate, "\x1B[92m", "\x1B[0m"));
            Util.DateLog(String.Format(@"Conversion rate: {0:F2} logs/sec", dEventRate));

            return jsonEvents;
        }

        public List<JsonEvent> ProcJson4(string InputObject, string ParentObjectKey, bool DoInputString)
        {
            Stopwatch sw = new Stopwatch();
            //Stopwatch sw2 = new Stopwatch();
            sw.Start();
            List<JsonEvent> jsonEvents = new List<JsonEvent>();
            //JsonEvent je = new JsonEvent();
            //SortedList<int, string> tmpFieldOrder = new SortedList<int, string>();
            //SortedList<int, JsonField> tmpFields = new SortedList<int, JsonField>();

            // Set up temporary hash table to test field ordering
            Hashtable hPrefFieldOrder = new Hashtable();
            /*hPrefFieldOrder.Add(@"eventTime", 1);
            hPrefFieldOrder.Add(@"eventType", 2);
            hPrefFieldOrder.Add(@"threatScore", 3);
            hPrefFieldOrder.Add(@"longDescription", -1);*/

            string jsFieldOrder = Properties.Settings.Default.fieldSettings;
            JObject joFieldOrder = (JObject)JsonConvert.DeserializeObject(jsFieldOrder);
            foreach (var jf in joFieldOrder)
            {
                hPrefFieldOrder.Add(jf.Key, (int)jf.Value);
            }

            string jsonStr = "";

            if (DoInputString)
            {
                jsonStr = InputObject;
            }
            else
            {
                using (FileStream fs = new FileStream(InputObject, FileMode.Open, FileAccess.ReadWrite))
                {
                    StreamReader sr = new StreamReader(fs);
                    jsonStr = sr.ReadToEnd();
                    sr.Close();
                    fs.Close();
                }
            }

            JObject jpobj = (JObject)JsonConvert.DeserializeObject(jsonStr);

            JToken jptk = jpobj[ParentObjectKey];

            if (jptk.Type.ToString() != "Array")
            {
                Util.DateLog(@"ERROR: Parent object is not an array!");
                throw new Exception(@"Parent object is not an array!");
            }

            int totChilds = jptk.Count();
            Util.DateLog(String.Format(@"Utilizing parent object: {0}", ParentObjectKey));
            Util.DateLog(String.Format(@"Found {0} child objects to process", totChilds));

            //foreach (JToken jtk in jptk.Values())
            //foreach (JToken jtk in jptk) // Good
            foreach (JObject jobj in jptk)
            {
                //sw2.Start();
                JsonEvent je = new JsonEvent();
                //SortedList<int, string> tmpFieldOrder = new SortedList<int, string>();
                SortedList<int, JsonField> tmpFields = new SortedList<int, JsonField>();

                int tmpFieldCounter = 1;

                foreach (JProperty jp in jobj.Properties())
                {
                    JsonField jf = new JsonField(tmpFieldCounter, jp.Name, jp.Value.Type.ToString(), null);

                    switch (jp.Value.Type.ToString())
                    {
                        case "String":
                            jf.Value = (string)((JValue)jp.Value.ToString());
                            break;
                        case "Null":
                            jf.Value = "null";
                            break;
                        case "Object":
                            // Method 1 - leave as string
                            //jf.Value = new object();
                            //jf.Value = (object)jp.Value;

                            // Method 2 - Second order conversion
                            jf.Value = (string)@"object";
                            JObject tmpChildJson = (JObject)JsonConvert.DeserializeObject(jp.Value.ToString());
                            this.ProcJsonChild2(tmpChildJson, jp.Name, ref tmpFieldCounter, ref tmpFields);
                            break;
                        case "Int":
                            jf.Value = new int();
                            jf.Value = (int)jp.Value;
                            break;
                        default:
                            jf.Value = new object();
                            jf.Value = (object)jp.Value;
                            break;
                    }

                    tmpFields.Add(tmpFieldCounter, jf);
                    //tmpFieldOrder.Add(tmpFieldCounter, jp.Name);
                    tmpFieldCounter++;
                }

                // Perform field ordering here

                // ============ METHOD 2

                Dictionary<string, int> dTmpFieldNames = new Dictionary<string, int>();
                Dictionary<string, string> dTmpFieldValues = new Dictionary<string, string>();
                Dictionary<string, string> dTmpFieldTypes = new Dictionary<string, string>();

                foreach (KeyValuePair<int, JsonField> kvpjf in tmpFields)
                {
                    dTmpFieldNames.Add(kvpjf.Value.Name, kvpjf.Key);
                    dTmpFieldValues.Add(kvpjf.Value.Name, kvpjf.Value.Value.ToString());
                    dTmpFieldTypes.Add(kvpjf.Value.Name, kvpjf.Value.TypeName);
                }

                string tmpLogOut = @"";
                //var orderedFields = dTmpFieldNames.Where(f => hPrefFieldOrder.ContainsKey(f.Key) && ((int)hPrefFieldOrder[f.Key] != -1));
                var orderedFields = dTmpFieldNames.Where(f => hPrefFieldOrder.ContainsKey(f.Key) && ((int)hPrefFieldOrder[f.Key] != -1)).OrderBy(f2 => (int)hPrefFieldOrder[f2.Key]);
                foreach (var of in orderedFields)
                {
                    tmpLogOut += String.Format(@"{0}={1}|", of.Key, dTmpFieldValues[of.Key]);
                }
                var unorderedFields = dTmpFieldNames.Where(f => hPrefFieldOrder.ContainsKey(f.Key) == false && dTmpFieldTypes[f.Key] != "Array" && dTmpFieldTypes[f.Key] != "Object");
                foreach (var uf in unorderedFields)
                {
                    tmpLogOut += String.Format(@"{0}={1}|", uf.Key, dTmpFieldValues[uf.Key]);
                }
                Regex r = new Regex(@"eventTime=(\d{13})", RegexOptions.None);
                if (r.IsMatch(tmpLogOut))
                {
                    Match m = r.Match(tmpLogOut);
                    string matchStr = m.Groups[0].Value;
                    string repStr = m.Groups[1].Value;
                    string newDate = String.Format(@"eventTime={0}", Util.GetEpochToDate(repStr));
                    tmpLogOut = tmpLogOut.Replace(matchStr, newDate);
                }
                tmpLogOut = tmpLogOut.TrimEnd("|".ToCharArray());
                je.SetLogOutput(tmpLogOut);
                //Console.WriteLine(tmpLogOut);
                jsonEvents.Add(je);
                //sw2.Stop();
                //Util.DateLog(String.Format(@"Conversion time: {0}", sw2.Elapsed));
                //sw2.Reset();
            }

            sw.Stop();
            Util.DateLog(String.Format(@"Total conversion time: {0}", sw.Elapsed));
            double dEventRate = (double)(totChilds / sw.Elapsed.TotalSeconds);
            //Util.DateLog(String.Format(@"Conversion rate: {1}{0:F2} logs/sec{2}", dEventRate, "\x1B[92m", "\x1B[0m"));
            Util.DateLog(String.Format(@"Conversion rate: {0:F2} logs/sec", dEventRate));

            return jsonEvents;
        }

        public List<JsonEvent> ProcJson5(string InputObject, string ParentObjectKey, bool DoInputString)
        {
            Stopwatch sw = new Stopwatch();
            //Stopwatch sw2 = new Stopwatch();
            sw.Start();
            List<JsonEvent> jsonEvents = new List<JsonEvent>();
            //JsonEvent je = new JsonEvent();
            //SortedList<int, string> tmpFieldOrder = new SortedList<int, string>();
            //SortedList<int, JsonField> tmpFields = new SortedList<int, JsonField>();

            // Set up temporary hash table to test field ordering
            Hashtable hPrefFieldOrder = new Hashtable();
            /*hPrefFieldOrder.Add(@"eventTime", 1);
            hPrefFieldOrder.Add(@"eventType", 2);
            hPrefFieldOrder.Add(@"threatScore", 3);
            hPrefFieldOrder.Add(@"longDescription", -1);*/

            string jsFieldOrder = Properties.Settings.Default.fieldSettings;
            JObject joFieldOrder = (JObject)JsonConvert.DeserializeObject(jsFieldOrder);
            foreach (var jf in joFieldOrder)
            {
                hPrefFieldOrder.Add(jf.Key, (int)jf.Value);
            }

            string jsonStr = "";

            if (DoInputString)
            {
                jsonStr = InputObject;
            }
            else
            {
                using (FileStream fs = new FileStream(InputObject, FileMode.Open, FileAccess.ReadWrite))
                {
                    StreamReader sr = new StreamReader(fs);
                    jsonStr = sr.ReadToEnd();
                    sr.Close();
                    fs.Close();
                }
            }

            JObject jpobj = (JObject)JsonConvert.DeserializeObject(jsonStr);

            JToken jptk = jpobj[ParentObjectKey];

            if (jptk.Type.ToString() != "Array")
            {
                Util.DateLog(@"ERROR: Parent object is not an array!");
                throw new Exception(@"Parent object is not an array!");
            }

            int totChilds = jptk.Count();
            Util.DateLog(String.Format(@"Utilizing parent object: {0}", ParentObjectKey));
            Util.DateLog(String.Format(@"Found {0} child objects to process", totChilds));

            //foreach (JToken jtk in jptk.Values())
            //foreach (JToken jtk in jptk) // Good
            foreach (JObject jobj in jptk)
            {
                //sw2.Start();
                JsonEvent je = new JsonEvent();
                //SortedList<int, string> tmpFieldOrder = new SortedList<int, string>();
                SortedList<int, JsonField> tmpFields = new SortedList<int, JsonField>();

                int tmpFieldCounter = 1;

                foreach (JProperty jp in jobj.Properties())
                {
                    JsonField jf = new JsonField(tmpFieldCounter, jp.Name, jp.Value.Type.ToString(), null);

                    switch (jp.Value.Type.ToString())
                    {
                        case "String":
                            jf.Value = (string)((JValue)jp.Value.ToString());
                            break;
                        case "Null":
                            jf.Value = "null";
                            break;
                        case "Object":
                            // Method 1 - leave as string
                            //jf.Value = new object();
                            //jf.Value = (object)jp.Value;

                            // Method 2 - Second order conversion
                            jf.Value = (string)@"object";
                            JObject tmpChildJson = (JObject)JsonConvert.DeserializeObject(jp.Value.ToString());
                            this.ProcJsonChild2(tmpChildJson, jp.Name, ref tmpFieldCounter, ref tmpFields);
                            break;
                        case "Int":
                            jf.Value = new int();
                            jf.Value = (int)jp.Value;
                            break;
                        case "Integer":
                            jf.Value = new Int64();
                            jf.Value = (Int64)jp.Value;
                            break;
                        case "Array":
                            string aTmpStr = @"";
                            foreach (JToken jt in jp.Children())
                            {
                                //aTmpStr += jt.ToString();
                                //aTmpStr += jt.Value<string>().ToString();
                                string[] aTmp = jt.ToObject<string[]>();
                                foreach (string s in aTmp)
                                {
                                    aTmpStr += s + ",";
                                }
                            }
                            aTmpStr = aTmpStr.TrimEnd(",".ToCharArray());
                            jf.TypeName = "String";
                            jf.Value = (string)aTmpStr;
                            break;
                        default:
                            jf.Value = new object();
                            jf.Value = (object)jp.Value;
                            break;
                    }

                    tmpFields.Add(tmpFieldCounter, jf);
                    //tmpFieldOrder.Add(tmpFieldCounter, jp.Name);
                    tmpFieldCounter++;
                }

                // Perform field ordering here

                // ============ METHOD 2

                Dictionary<string, int> dTmpFieldNames = new Dictionary<string, int>();
                Dictionary<string, string> dTmpFieldValues = new Dictionary<string, string>();
                Dictionary<string, string> dTmpFieldTypes = new Dictionary<string, string>();

                foreach (KeyValuePair<int, JsonField> kvpjf in tmpFields)
                {
                    dTmpFieldNames.Add(kvpjf.Value.Name, kvpjf.Key);
                    dTmpFieldValues.Add(kvpjf.Value.Name, kvpjf.Value.Value.ToString());
                    dTmpFieldTypes.Add(kvpjf.Value.Name, kvpjf.Value.TypeName);
                }

                string tmpLogOut = @"";
                //var orderedFields = dTmpFieldNames.Where(f => hPrefFieldOrder.ContainsKey(f.Key) && ((int)hPrefFieldOrder[f.Key] != -1));
                var orderedFields = dTmpFieldNames.Where(f => hPrefFieldOrder.ContainsKey(f.Key) && ((int)hPrefFieldOrder[f.Key] != -1)).OrderBy(f2 => (int)hPrefFieldOrder[f2.Key]);
                foreach (var of in orderedFields)
                {
                    tmpLogOut += String.Format(@"{0}={1}|", of.Key, dTmpFieldValues[of.Key]);
                }
                var unorderedFields = dTmpFieldNames.Where(f => hPrefFieldOrder.ContainsKey(f.Key) == false && dTmpFieldTypes[f.Key] != "Array" && dTmpFieldTypes[f.Key] != "Object");
                foreach (var uf in unorderedFields)
                {
                    tmpLogOut += String.Format(@"{0}={1}|", uf.Key, dTmpFieldValues[uf.Key]);
                }
                Regex r = new Regex(@"eventTime=(\d{13})", RegexOptions.None);
                if (r.IsMatch(tmpLogOut))
                {
                    Match m = r.Match(tmpLogOut);
                    string matchStr = m.Groups[0].Value;
                    string repStr = m.Groups[1].Value;
                    string newDate = String.Format(@"eventTime={0}", Util.GetEpochToDate(repStr));
                    tmpLogOut = tmpLogOut.Replace(matchStr, newDate);
                }
                tmpLogOut = tmpLogOut.TrimEnd("|".ToCharArray());
                je.SetLogOutput(tmpLogOut);
                //Console.WriteLine(tmpLogOut);
                jsonEvents.Add(je);
                //sw2.Stop();
                //Util.DateLog(String.Format(@"Conversion time: {0}", sw2.Elapsed));
                //sw2.Reset();
            }

            sw.Stop();
            Util.DateLog(String.Format(@"Total conversion time: {0}", sw.Elapsed));
            double dEventRate = (double)(totChilds / sw.Elapsed.TotalSeconds);
            //Util.DateLog(String.Format(@"Conversion rate: {1}{0:F2} logs/sec{2}", dEventRate, "\x1B[92m", "\x1B[0m"));
            Util.DateLog(String.Format(@"Conversion rate: {0:F2} logs/sec", dEventRate));

            return jsonEvents;
        }

        /*public List<JsonEvent> ProcJson2(string jsonStr, string ParentObjectKey, bool DoInputString)
        {
            Stopwatch sw = new Stopwatch();
            //Stopwatch sw2 = new Stopwatch();
            sw.Start();
            List<JsonEvent> jsonEvents = new List<JsonEvent>();
            //JsonEvent je = new JsonEvent();
            //SortedList<int, string> tmpFieldOrder = new SortedList<int, string>();
            //SortedList<int, JsonField> tmpFields = new SortedList<int, JsonField>();

            JObject jpobj = (JObject)JsonConvert.DeserializeObject(jsonStr);

            JToken jptk = jpobj[ParentObjectKey];

            if (jptk.Type.ToString() != "Array")
            {
                Util.DateLog(@"ERROR: Parent object is not an array!");
                throw new Exception(@"Parent object is not an array!");
            }

            int totChilds = jptk.Count();
            Util.DateLog(String.Format(@"Utilizing parent object: {0}", ParentObjectKey));
            Util.DateLog(String.Format(@"Found {0} child objects to process", totChilds));

            //foreach (JToken jtk in jptk.Values())
            //foreach (JToken jtk in jptk) // Good
            foreach (JObject jobj in jptk)
            {
                //sw2.Start();
                JsonEvent je = new JsonEvent();
                //SortedList<int, string> tmpFieldOrder = new SortedList<int, string>();
                SortedList<int, JsonField> tmpFields = new SortedList<int, JsonField>();

                int tmpFieldCounter = 1;

                foreach (JProperty jp in jobj.Properties())
                {
                    JsonField jf = new JsonField(tmpFieldCounter, jp.Name, jp.Value.Type.ToString(), null);

                    switch (jp.Value.Type.ToString())
                    {
                        case "String":
                            jf.Value = (string)((JValue)jp.Value.ToString());
                            break;
                        case "Null":
                            jf.Value = "null";
                            break;
                        case "Object":
                            // Method 1 - leave as string
                            //jf.Value = new object();
                            //jf.Value = (object)jp.Value;

                            // Method 2 - Second order conversion
                            jf.Value = (string)@"object";
                            JObject tmpChildJson = (JObject)JsonConvert.DeserializeObject(jp.Value.ToString());
                            this.ProcJsonChild2(tmpChildJson, jp.Name, ref tmpFieldCounter, ref tmpFields);
                            break;
                        case "Int":
                            jf.Value = new int();
                            jf.Value = (int)jp.Value;
                            break;
                        default:
                            jf.Value = new object();
                            jf.Value = (object)jp.Value;
                            break;
                    }

                    tmpFields.Add(tmpFieldCounter, jf);
                    //tmpFieldOrder.Add(tmpFieldCounter, jp.Name);
                    tmpFieldCounter++;
                }

                je.ImportFields(tmpFields);
                je.GetLogOutput(false);
                jsonEvents.Add(je);
                //sw2.Stop();
                //Util.DateLog(String.Format(@"Conversion time: {0}", sw2.Elapsed));
                //sw2.Reset();
            }

            sw.Stop();
            Util.DateLog(String.Format(@"Total conversion time: {0}", sw.Elapsed));
            double dEventRate = (double)(totChilds / sw.Elapsed.TotalSeconds);
            //Util.DateLog(String.Format(@"Conversion rate: {1}{0:F2} logs/sec{2}", dEventRate, "\x1B[92m", "\x1B[0m"));
            Util.DateLog(String.Format(@"Conversion rate: {0:F2} logs/sec", dEventRate));
            if (Properties.Settings.Default.PerfLogging)
            {
                Console.WriteLine(String.Format("{0}\t{1:F2}", sw.Elapsed, dEventRate));
            }

            return jsonEvents;
        }*/

        public void ProcJsonChild(JObject jobj, string ParentFieldName, ref int rtmpFieldCounter, ref SortedList<int, JsonField> rtmpFields)
        {
            foreach (JProperty jp in jobj.Properties())
            {
                string tmpFieldName = String.Format(@"{0}.{1}", ParentFieldName, jp.Name);
                JsonField jf = new JsonField(rtmpFieldCounter, tmpFieldName, jp.Value.Type.ToString(), null);

                switch (jp.Value.Type.ToString())
                {
                    case "String":
                        jf.Value = (string)((JValue)jp.Value.ToString());
                        break;
                    case "Null":
                        jf.Value = "null";
                        break;
                    case "Object":
                        jf.Value = new object();
                        jf.Value = (object)jp.Value;
                        break;
                    case "Int":
                        jf.Value = new int();
                        jf.Value = (int)jp.Value;
                        break;
                    default:
                        jf.Value = new object();
                        jf.Value = (object)jp.Value;
                        break;
                }

                rtmpFields.Add(rtmpFieldCounter, jf);
                //tmpFieldOrder.Add(tmpFieldCounter, jp.Name);
                rtmpFieldCounter++;
            }

            return;
        }

        public void ProcJsonChild2(JObject jobj, string ParentFieldName, ref int rtmpFieldCounter, ref SortedList<int, JsonField> rtmpFields)
        {
            foreach (JProperty jp in jobj.Properties())
            {
                string tmpFieldName = String.Format(@"{0}.{1}", ParentFieldName, jp.Name);
                JsonField jf = new JsonField(rtmpFieldCounter, tmpFieldName, jp.Value.Type.ToString(), null);

                switch (jp.Value.Type.ToString())
                {
                    case "String":
                        jf.Value = (string)((JValue)jp.Value.ToString());
                        break;
                    case "Null":
                        jf.Value = "null";
                        break;
                    case "Object":
                        //jf.Value = new object();
                        //jf.Value = (object)jp.Value;
                        jf.Value = (string)@"object";
                        JObject tmpChildJson = (JObject)JsonConvert.DeserializeObject(jp.Value.ToString());
                        this.ProcJsonChild2(tmpChildJson, jp.Name, ref rtmpFieldCounter, ref rtmpFields);
                        break;
                    case "Int":
                        jf.Value = new int();
                        jf.Value = (int)jp.Value;
                        break;
                    default:
                        jf.Value = new object();
                        jf.Value = (object)jp.Value;
                        break;
                }

                rtmpFields.Add(rtmpFieldCounter, jf);
                //tmpFieldOrder.Add(tmpFieldCounter, jp.Name);
                rtmpFieldCounter++;
            }

            return;
        }
    }

}