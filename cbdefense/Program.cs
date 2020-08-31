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
using System.Text.RegularExpressions;

namespace cbdefense
{

    public class CbRestApi
    {
        private WebClient wc;
        private string apiSecretKey;
        private string apiId;

        public CbRestApi()
        {
            Util.DateLog(@"Created REST API client");
            this.wc = new WebClient();
            this.apiSecretKey = Properties.Settings.Default.apiSecretKey;
            this.apiId = Properties.Settings.Default.apiId;
            this.wc.Headers.Add(@"X-Auth-Token", String.Format(@"{0}/{1}", this.apiSecretKey, this.apiId));
        }

        public string Pull()
        {
            Util.DateLog(@"Querying CB Defense REST API for events...");
            string jsonStr = this.wc.DownloadString(@"http://127.0.0.1");
            //Util.DateLog(jsonStr);

            return jsonStr;
        }

        public string Pull(string uri)
        {
            Util.DateLog(@"Querying CB Defense REST API for events...");
            string jsonStr = this.wc.DownloadString(uri);

            return jsonStr;
        }

        public string Pull2(int CycleTime)
        {
            string jsonStr = this.wc.DownloadString(@"http://127.0.0.1");

            return jsonStr;
        }

        public void PullMemStream(MemoryStream m)
        {
            using (StreamWriter sw = new StreamWriter(m))
            {
                //sw.Write(this.wc.DownloadString(@"http://127.0.0.1"));
                sw.Write((string)(this.wc.DownloadString(@"http://127.0.0.1")));
                //sw.Close();
                
            }

            return;
        }
    }

    public class CbWriter
    {
        private Queue<JsonEvent> jq;
        private string logFile;
        private string logFilePath;
        public string logFileFinal;
        public string logFileFinalPath;
        //public string logFileJson;
        //public string logFileJsonPath;
        private FileStream fs;
        private StreamWriter sw;

        public CbWriter(ref List<JsonEvent> jel, string LogFile)
        {
            Util.DateLog(@"Created CB log file writer");
            //this.logFile = String.Format(@"{0}_{1}", DateTime.Now.ToString(@"yyyyMMddhhmmss"), LogFile);
            DateTime d = DateTime.Now;
            //this.logFile = String.Format(@"{0}_{1}_tmp.log", LogFile, DateTime.Now.ToString(@"yyyyMMddhhmmss"));
            this.logFile = String.Format(@"tmp_{0}_{1}.log", LogFile, d.ToString(@"yyyyMMddHHmmss"));
            this.logFilePath = Path.Combine(Properties.Settings.Default.logOutputPath, this.logFile);
            this.logFileFinal = String.Format(@"{0}_{1}.log", LogFile, d.ToString(@"yyyyMMddHHmmss"));
            this.logFileFinalPath = Path.Combine(Properties.Settings.Default.logOutputPath, this.logFileFinal);
            //this.logFileJson = String.Format(@"raw_{0}_{1}.json", LogFile, d.ToString(@"yyyyMMddHHmmss"));
            //this.logFileJsonPath = Path.Combine(Properties.Settings.Default.logOutputPath, this.logFileJson);
            //this.fs = new FileStream(this.logFile, FileMode.CreateNew, FileAccess.ReadWrite);
            this.fs = new FileStream(this.logFilePath, FileMode.CreateNew, FileAccess.ReadWrite);
            this.sw = new StreamWriter(fs);
            this.jq = new Queue<JsonEvent>();
            for (int i = 0; i < jel.Count; i++)
            {
                jq.Enqueue(jel[i]);
            }
            Util.DateLog(@"Finished enqueing CB events");
        }

        public void WriteFinal()
        {
            File.Copy(this.logFilePath, this.logFileFinalPath);
            File.Delete(this.logFilePath);
            return;
        }

        /*~CbWriter()
        {
            this.sw.Close();
            this.fs.Close();
        }*/

        public void WriteEvents()
        {
            int sleepCount = 0;
            while (true)
            {
                Thread.BeginCriticalRegion();
                if (jq.Count > 0)
                {
                    while (jq.Count > 0)
                    {
                        //Method 1: Invisible CRLF
                        //sw.WriteLine(jq.Dequeue().LogOutput);

                        //Method 2: Fix
                        StringBuilder sb = new StringBuilder();
                        sb.Append(jq.Dequeue().LogOutput);
                        sb.Append("\r\n");
                        sw.Write(sb.ToString());
                    }
                }
                else
                {
                    if (sleepCount > 3)
                    {
                        break;
                    }
                    Util.DateLog(@"Queue empty, sleeping writer thread for 2 seconds");
                    Thread.Sleep(2000);
                    sleepCount++;
                }
                Thread.EndCriticalRegion();
            }

            sw.Close();
            fs.Close();
            Util.DateLog(@"Writer thread exiting");
            return;
        }
    }

    /*public class CbOverseer
    {
        Thread threadRestApi;
        Thread threadWriter;
    }*/

    class Program
    {
        const string dateFmtRfc3339 = @"yyyy-MM-ddTHH:mm:sszzz";

        static void Main(string[] args)
        {
            Util.DateLog("████████ LR CB Defense Collector ████████");
            System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.Batch;

            try
            {

                // Set up global variables

                string finalUriPrefix = String.Format(@"{0}{1}", Properties.Settings.Default.apiUri, Properties.Settings.Default.defaultApiPrefix);
                string finalUri = @"";
                //string resultObjName = @"cbevents";
                string resultObjName = Properties.Settings.Default.apiResultObjName;

                // Handle CLI Args
                //
                // Current CLI args:
                // cbdefense.exe <CB_API_QUERY_STRING (optional)> <RESULT_ARRAY_OBJECT_NAME (optional)>

                if (args.Length == 0)
                {
                    if (Properties.Settings.Default.doAutoTimeWindow)
                    {
                        Util.DateLog(String.Format(@"Using automatic time window: {0} (minutes)", Properties.Settings.Default.autoTimeWindow));
                        double dAutoWindow = (double)Properties.Settings.Default.autoTimeWindow * -1d;
                        DateTime dNow = DateTime.Now;
                        DateTime dPrev = dNow.AddMinutes(dAutoWindow);
                        string dNowStr = dNow.ToString(dateFmtRfc3339);
                        string dPrevStr = dPrev.ToString(dateFmtRfc3339);
                        Util.DateLog(String.Format(@"Log collection window start: {0}", dPrevStr));
                        Util.DateLog(String.Format(@"Log collection window end: {0}", dNowStr));
                        finalUri = String.Format(@"{0}event?startTime={1}&endTime={2}&rows={3}", finalUriPrefix, dPrevStr, dNowStr, Properties.Settings.Default.maxResults);
                    }
                    else
                    {
                        finalUri = String.Format(@"{0}event?searchWindow={1}&rows={2}", finalUriPrefix, Properties.Settings.Default.searchWindow, Properties.Settings.Default.maxResults);
                    }
                }
                else
                {
                    if (args.Length == 1)
                    {
                        finalUri = String.Format(@"{0}{1}", finalUriPrefix, args[0]);
                    }
                    else
                    {
                        finalUri = String.Format(@"{0}{1}", finalUriPrefix, args[0]);
                        resultObjName = args[1];
                    }
                }

                CbRestApi cbrestapi = new CbRestApi();
                JsonTools jt = new JsonTools();

                Util.DateLog(String.Format(@"Using API URI: {0}", finalUri));

                // Method 1

                //List<JsonEvent> jel = jt.ProcJson5(cbrestapi.Pull(finalUri), resultObjName, true);

                // Method 2 (Include raw JSON dump)

                string strRawJson = cbrestapi.Pull(finalUri);
                if (Properties.Settings.Default.doDumpRawJson)
                {
                    DateTime d = DateTime.Now;
                    string logFileJson = String.Format(@"raw_{0}_{1}.json", @"events_cb_defense", d.ToString(@"yyyyMMddHHmmss"));
                    string logFileJsonPath = Path.Combine(Properties.Settings.Default.logOutputPath, logFileJson);
                    using (FileStream fsj = new FileStream(logFileJsonPath, FileMode.CreateNew, FileAccess.ReadWrite))
                    {
                        StreamWriter sw = new StreamWriter(fsj);
                        sw.WriteLine(strRawJson);
                        sw.Close();
                        fsj.Close();
                        Util.DateLog(@"Write raw JSON file");
                    }
                }
                List<JsonEvent> jel = jt.ProcJson5(strRawJson, resultObjName, true);

                CbWriter cbwriter = new CbWriter(ref jel, @"events_cb_defense");
                Thread threadWriter = new Thread(cbwriter.WriteEvents);
                threadWriter.Start();
                while (true)
                {
                    if (threadWriter.IsAlive)
                    {
                        Util.DateLog(@"Sleeping main thread 10 seconds");
                        Thread.Sleep(10000);
                    }
                    else
                    {
                        break;
                    }
                }
                Thread.Sleep(1000);
                cbwriter.WriteFinal();

                if (Properties.Settings.Default.doAutoLogCleanup)
                {
                    Util.DateLog(@"Performing automatic log file cleanup...");
                    Regex r = new Regex(@"^.*?(\d{4})(\d{2})(\d{2})(\d{2})(\d{2})(\d{2}).*?$", RegexOptions.None);

                    string[] cbLogFiles = Directory.GetFiles(Properties.Settings.Default.logOutputPath, @"events_cb_defense*.log");
                    int iClearedFiles = 0;
                    foreach (string s in cbLogFiles)
                    {
                        string tmpDateStr = r.Replace(s, @"$1-$2-$3 $4:$5:$6");
                        DateTime tmpDate = DateTime.Parse(tmpDateStr);
                        if (tmpDate < (DateTime.Now.AddDays(-1)))
                        {
                            Util.DateLog(String.Format(@"Clearing old log file: {0}", s));
                            File.Delete(s);
                            iClearedFiles++;
                        }
                    }
                    Util.DateLog(String.Format(@"Cleared {0} old log files", iClearedFiles));
                }

                Util.DateLog(@"Main thread exiting. Done!");
                return;
            }
            catch (Exception m)
            {
                Util.DateLog(String.Format(@"Error occurred: {0}", m.ToString()));
                return;
            }
        }
    }
}
