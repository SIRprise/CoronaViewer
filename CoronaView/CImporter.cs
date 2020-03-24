using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace CoronaView
{
    /// <summary>
    /// This importer is specialized for the github csv data
    /// </summary>
    public class CImporter
    {

        public Dictionary<string,CCountryDataset> Import()
        {
            Dictionary<string, CCountryDataset> resultDict = new Dictionary<string, CCountryDataset>();

            string sourceURL = "https://raw.githubusercontent.com/CSSEGISandData/COVID-19/master/csse_covid_19_data/csse_covid_19_time_series/time_series_covid19_confirmed_global.csv";

            string sourceData = GetStringDataFromURL(sourceURL);

            string[,] source2d = Get2dSourceArray(sourceData);

            for (int y = 1; y < source2d.GetLength(0); y++)
            {
                CCountryDataset countryData = new CCountryDataset();
                if(source2d[y,0].Trim().Equals(string.Empty))
                    countryData.countryName = source2d[y, 1];
                else
                    countryData.countryName = source2d[y, 0]+","+source2d[y, 1];
                int lastVal = 0;

                for (int x = 5; x < source2d.GetLength(1); x++)
                {
                    string datestring = source2d[0, x];
                    string val = source2d[y, x];

                    string[] allowedFormats = { "M/d/yy" };//, "m/dd/yy", "mm/d/yy", "mm/dd/yy" }; //maybe first format needed only
                    try
                    {
                        CDateValue data = new CDateValue(DateTime.ParseExact(datestring, allowedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None), Int32.Parse(val));
                        if (lastVal != 0)
                            data.increaseRate = Int32.Parse(val) / (double)lastVal;
                        lastVal = Int32.Parse(val);
                        countryData.container.Add(data);
                    }
                    catch (Exception)
                    {
                        lastVal = 0;
                    }
                }
                resultDict.Add(countryData.countryName, countryData);
            }
            //FileExport(source2d);
            return resultDict;
        }

        /// <summary>
        /// Output is: 
        /// (0,0)State | Country | foo | foo | date 1 | date 2...
        /// (1,0) foo  | Thailand| foo | foo | cases 1| cases 2
        /// </summary>
        /// <param name="sourceData"></param>
        /// <returns></returns>
        public string[,] Get2dSourceArray(string sourceData)
        {
            string[] lines = sourceData.Split('\n');
            lines = lines.Select(x => x.Trim()).Where(x => x.Length>0).ToArray();

            //make 2d array
            int numberRows = lines.Count();
            int numberCols = lines[0].Split(',').Count();
            string[,] source2d = new string[numberRows, numberCols];

            for (int i = 0; i < numberRows; i++)
            {
                string[] elementsPerLine = lines[i].Split(',');
                for (int j = 0; j < numberCols; j++)
                {
                    source2d[i, j] = elementsPerLine[j];
                }
            }
            return source2d;
        }

        //for use with excel
        public void FileExport(string[,] source2d)
        {
            int numberCols = source2d.GetLength(1);
            int numberRows = source2d.GetLength(0);

            //transform (+ change separator to semikolon
            List<string> resultLines = new List<string>();
            for (int i = 0; i < numberCols; i++)
            {
                string[] resultLineArr = new string[numberRows];
                for (int j = 0; j < numberRows; j++)
                {
                    resultLineArr[j] = source2d[j, i].Replace('\n', ' ');
                    //allow exponential trend line in excel
                    if (resultLineArr[j].Trim().Equals("0"))
                        resultLineArr[j] = " ";
                    //blank unnecessarry lines
                    if (i == 0 || i == 2 || i == 3)
                        resultLineArr[j] = " ";
                    if (i == 1 && j == 0)
                        resultLineArr[j] = " ";
                }

                //if(i>2)
                //    resultLines.Add(string.Join(";", resultLineArr).Replace("/","-")); //for easier date import to excel
                //else
                resultLines.Add(string.Join(";", resultLineArr));
            }

            string outFileStr = string.Join("\r\n", resultLines);
            File.WriteAllText("outfile.csv", outFileStr);
        }

        public string GetStringDataFromURL(string sourceURL)
        {
            string sourceData;
            /*
            System.Net.HttpWebRequest httpReq;
            System.Net.HttpWebResponse httpRes;
            httpReq = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(sourceURL);
            //httpReq.AddRange((int)existLen);
            System.IO.Stream resStream;
            httpRes = (System.Net.HttpWebResponse)httpReq.GetResponse();
            resStream = httpRes.GetResponseStream();
            */
            using (var client = new WebClient
            {
                Encoding = System.Text.Encoding.UTF8
            })
            {
                client.Headers.Add("accept", "*/*");
                sourceData = client.DownloadString(sourceURL);
            }
            return sourceData;
        }
    }
}
