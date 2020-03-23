using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CoronaView
{
    /// <summary>
    /// This importer is specialized for the github csv data
    /// </summary>
    public class CImporterGermany : ICImporter
    {

        public Dictionary<string, CCountryDataset> Import()
        {
            Dictionary<string, CCountryDataset> resultDict = new Dictionary<string, CCountryDataset>();

            //string sourceURL = @"https://services7.arcgis.com/mOBPykOjAyBO2ZKk/arcgis/rest/services/RKI_COVID19/FeatureServer/0/query?where=Meldedatum+%3E+%28CURRENT_TIMESTAMP+-+3%29&objectIds=&time=&resultType=none&outFields=*&returnIdsOnly=false&returnUniqueIdsOnly=false&returnCountOnly=false&returnDistinctValues=false&cacheHint=false&orderByFields=Meldedatum%2C+Bundesland%2C+Landkreis&groupByFieldsForStatistics=&outStatistics=&having=&resultOffset=&resultRecordCount=&sqlFormat=none&f=json&token=";
            string sourceURL = @"https://services7.arcgis.com/mOBPykOjAyBO2ZKk/arcgis/rest/services/RKI_COVID19/FeatureServer/0/query?where=Meldedatum+%3E+%28CURRENT_TIMESTAMP+-+3%29&objectIds=&time=&resultType=none&outFields=*&returnIdsOnly=false&returnUniqueIdsOnly=false&returnCountOnly=false&returnDistinctValues=false&cacheHint=false&orderByFields=Meldedatum&outStatistics=&having=&resultOffset=&resultRecordCount=&sqlFormat=none&f=json&token=";
            string sourceData = GetStringDataFromURL(sourceURL);

            JObject o = JObject.Parse(sourceData);
            JArray ja = o["features"] as JArray;

            //Dictionary<string, Dictionary<DateTime, int>> dic = new Dictionary<string, Dictionary<DateTime, int>>();
            List<CDateValue> list = new List<CDateValue>();

            for (int i = 0; i < ja.Count; i++)
            {
                JToken featureJson = ja[i];
                var resultObjects = AllChildren(featureJson as JObject);

                string bundesland = (string)featureJson.SelectToken("attributes.Bundesland");//(featureJson as JObject)["Bundesland"];
                string landkreis = (string)featureJson.SelectToken("attributes.Landkreis");
                string geschlecht = (string)featureJson.SelectToken("attributes.Geschlecht");
                int anzahlfall = (int)featureJson.SelectToken("attributes.AnzahlFall");
                var anzahltodesfall = (int)featureJson.SelectToken("attributes.AnzahlTodesfall");
                long meldedatumLong = (long)featureJson.SelectToken("attributes.Meldedatum");
                DateTime meldedatum = UnixTimeToDateTime(meldedatumLong);

                CDateValue temp = new CDateValue(meldedatum, anzahlfall);
                temp.bundesland = bundesland;
                temp.lk = landkreis;
                temp.sex = geschlecht;
                list.Add(temp);

                
            }



            //sum up everything by date and bundesland

            var listGroupedByBundesland = from e in list group e by e.bundesland;

            foreach(var elem in listGroupedByBundesland)
            {
                List<CDateValue> tlist = new List<CDateValue>();
                var temp2 = from e in elem group e by e.date;
                foreach (var elem2 in temp2)
                {
                    int val = elem2.Sum(x => x.value);
                    var temp3 = new CDateValue(elem2.Key, val);
                    temp3.bundesland = elem.First().bundesland;
                    tlist.Add(temp3);
                }
                resultDict.Add(elem.Key, new CCountryDataset(elem.Key, tlist));
            }


       

            return resultDict;
        }

        // recursively yield all children of json
        private static IEnumerable<JToken> AllChildren(JToken json)
        {
            foreach (var c in json.Children())
            {
                yield return c;
                foreach (var cc in AllChildren(c))
                {
                    yield return cc;
                }
            }
        }

        private DateTime UnixTimeToDateTime(long? unixTicksMS)
        {
            DateTime UNIX_EPOCH = new DateTime(1970, 1, 1, 0, 0, 0);
            
            //if (unixTicksMS == null)
            //    return null;

            return new DateTime(UNIX_EPOCH.Ticks + (long)unixTicksMS * 10000);
        }

        public bool AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public string GetStringDataFromURL(string sourceURL)
        {
            string sourceData;

            ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);
            ServicePointManager.Expect100Continue = true;
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(sourceURL);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            using (Stream stream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                sourceData = reader.ReadToEnd();
            }

            return sourceData;
        }
    }
}
