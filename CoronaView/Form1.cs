using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace CoronaView
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CImporter importer = new CImporter();
            Dictionary<string, CCountryDataset> countryContainer = importer.Import();

            chart1.SuppressExceptions = true;
            chart1.Series.Clear();
            chart2.SuppressExceptions = true;
            chart2.Series.Clear();

            
            StripLine stripline = new StripLine();
            stripline.Interval = 0;
            stripline.IntervalOffset = 1-0.01;
            stripline.StripWidth = 0.02;
            stripline.Text = "Faktor 1.0";
            stripline.BackColor = Color.Red;
            chart2.ChartAreas["ChartArea1"].AxisY.StripLines.Add(stripline);

            List<CCountryDataset> sortedList = (from elem in countryContainer.Select(x => x.Value) orderby elem.container.Last().value descending select elem).ToList();

            //foreach (var elem in countryContainer.Where(x => x.Value.container.Last().value > 10000))
            //foreach (var elem in countryContainer.Where(x => x.Key.Contains("Germany") || x.Key.Contains("Spain") || x.Key.Contains("Italy") || x.Key.Contains("France,France") || x.Key.Contains("Switzerland") || x.Key.Contains("Austria")))
            foreach (var elem in sortedList.Take(5))
            {
                //var elem = el.Value;
                var series = new Series(elem.countryName);

                series.ChartType = SeriesChartType.Line;
                series.BorderWidth = 3;
                //series.XValueType = ChartValueType.DateTime;

                var dateFilteredList = elem.container.Where(x => ((x.date >= dateTimePicker1.Value) && (x.date <= dateTimePicker2.Value)));

                series.Points.DataBindXY(dateFilteredList.Select(x => x.date).ToArray(), dateFilteredList.Select(y => y.value).ToArray());
                series.Points[dateFilteredList.Count()-1].IsValueShownAsLabel = true;
                chart1.Series.Add(series);

                /*
                if (dateFilteredList.Count() > 1)
                {
                    var dateFilteredListArr = dateFilteredList.ToArray();
                    for (int i = 1; i < dateFilteredList.Count(); i++)
                    {
                        var currentIncreaseRate = dateFilteredListArr[i].value / dateFilteredListArr[i - 1].value;

                    }
                    //dateFilteredListArr[0].value = 0;
                }
                */

                var series2 = new Series(elem.countryName);
                series2.ChartType = SeriesChartType.Line;
                series2.BorderWidth = 3;
                //series.XValueType = ChartValueType.DateTime;

                series2.Points.DataBindXY(dateFilteredList.Select(x => x.date).ToArray(), dateFilteredList.Select(y => Math.Round(y.increaseRate,4)).ToArray());
                series2.Points[dateFilteredList.Count() - 1].IsValueShownAsLabel = true;
                chart2.Series.Add(series2);
            }
            chart1.ChartAreas[0].AxisY.IsLogarithmic = true;

            //FileExport(source2d);
        }
        
        Point? prevPosition = null;
        ToolTip tooltip = new ToolTip();

        double pointXPixel;
        double pointYPixel;
        Point pos;
        DataPoint prop;

        private void chart1_MouseMove(object sender, MouseEventArgs e)
        {
            
            pos = e.Location;
            if (prevPosition.HasValue && pos == prevPosition.Value)
                return;
            tooltip.RemoveAll();
            prevPosition = pos;
            var results = chart1.HitTest(pos.X, pos.Y, false,
                                            ChartElementType.DataPoint);
            foreach (var result in results)
            {
                if (result.ChartElementType == ChartElementType.DataPoint)
                {
                    prop = result.Object as DataPoint;
                    if (prop != null)
                    {
                        pointXPixel = result.ChartArea.AxisX.ValueToPixelPosition(prop.XValue);
                        pointYPixel = result.ChartArea.AxisY.ValueToPixelPosition(prop.YValues[0]);

                        // check if the cursor is really close to the point (5 pixels around the point)
                        if (Math.Abs(pos.X - pointXPixel) < 5 &&
                            Math.Abs(pos.Y - pointYPixel) < 5)
                        {
                            tooltip.Show("Date=" + DateTime.FromOADate(prop.XValue).ToShortDateString() + ", Cases=" + prop.YValues[0], this.chart1,
                                            pos.X, pos.Y - 15);
                        }
                    }
                }
            }
            
        }
        
        DateTime prevPot;
        int prevVal = -1;

        private void chart1_MouseClick(object sender, MouseEventArgs e)
        {
            
        }

        private void chart1_MouseDown(object sender, MouseEventArgs e)
        {
            /*
            var pos = e.Location;
            var results = chart1.HitTest(pos.X, pos.Y, false,
                                            ChartElementType.DataPoint);
            foreach (var result in results)
            {
                if (result.ChartElementType == ChartElementType.DataPoint)
                {
                    var prop = result.Object as DataPoint;
                    if (prop != null)
                    {
                        var pointXPixel = result.ChartArea.AxisX.ValueToPixelPosition(prop.XValue);
                        var pointYPixel = result.ChartArea.AxisY.ValueToPixelPosition(prop.YValues[0]);
                        */
            // check if the cursor is really close to the point (5 pixels around the point)
            if (Math.Abs(pos.X - pointXPixel) < 5 &&
                Math.Abs(pos.Y - pointYPixel) < 5)
            {
                try
                {
                    if (prevVal < 0)
                    {
                        prevPot = DateTime.FromOADate(prop.XValue);
                        prevVal = (int)prop.YValues[0];
                        Debug.WriteLine("Got Point 1: " + prevVal + " cases");
                        textBox1.Text = prevVal.ToString();
                    }
                    else
                    {
                        var d = Math.Abs((prevPot - DateTime.FromOADate(prop.XValue)).TotalDays);
                        var val = (int)prop.YValues[0] / (double)prevVal;
                        textBox2.Text = ((int)prop.YValues[0]).ToString();

                        var increaseFactor = Math.Pow(val, 1.0 / d);
                        var doublingInDays = Math.Log(2, increaseFactor);
                        Debug.WriteLine("Got Point 2: " + (int)prop.YValues[0] + " cases");
                        Debug.WriteLine("Result: " + increaseFactor + " -> doubling every " + doublingInDays + "days");
                        textBox3.Text = String.Format("{0:0.0}" + "%", (increaseFactor - 1)*100);
                        textBox4.Text = String.Format("{0:0.00}", doublingInDays);

                        prevVal = -1;
                    }
                }
                catch (Exception) { };
            }
            //   }
            // }
            // }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            dateTimePicker2.Value = DateTime.Now;
            chart1.Series.Clear();
            chart2.Series.Clear();
        }

        Point? prevPosition2 = null;
        ToolTip tooltip2 = new ToolTip();

        private void chart2_MouseMove(object sender, MouseEventArgs e)
        {
            var pos2 = e.Location;
            if (prevPosition2.HasValue && pos2 == prevPosition2.Value)
                return;
            tooltip.RemoveAll();
            prevPosition2 = pos2;
            var results = chart2.HitTest(pos2.X, pos2.Y, false,
                                            ChartElementType.DataPoint);
            foreach (var result in results)
            {
                if (result.ChartElementType == ChartElementType.DataPoint)
                {
                    var prop2 = result.Object as DataPoint;
                    if (prop2 != null)
                    {
                        var pointXPixel2 = result.ChartArea.AxisX.ValueToPixelPosition(prop2.XValue);
                        var pointYPixel2 = result.ChartArea.AxisY.ValueToPixelPosition(prop2.YValues[0]);

                        // check if the cursor is really close to the point (5 pixels around the point)
                        if (Math.Abs(pos2.X - pointXPixel2) < 5 &&
                            Math.Abs(pos2.Y - pointYPixel2) < 5)
                        {
                            var doublingInDays = Math.Log(2, prop2.YValues[0]);

                            tooltip2.Show("Date=" + DateTime.FromOADate(prop2.XValue).ToShortDateString() + ", IncreaseFactor=" + prop2.YValues[0] + " -> doubling in days: " + String.Format("{0:0.00}", doublingInDays), this.chart2,
                                            pos2.X, pos2.Y - 15);
                        }
                    }
                }
            }
        }
    }
}
