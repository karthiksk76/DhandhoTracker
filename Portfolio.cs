using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace DhandhoTracker
{
    internal struct ShareDataPoint
    {
        public DateTime Timestamp { get; set; }

        public uint Count { get; set; }

        public uint Value { get; set; }

        public ShareDataPoint(DateTime date, uint count, uint value)
        {
            Timestamp = date;
            Count = count;
            Value = value;
        }
    }

    internal enum IndicatorType
    {
        None,
        New,
        Add,
        Sell,
        SellOut
    }

    internal class PortfolioEntry
    {
        static Dictionary<string, string> s_SynonymousHoldings = new Dictionary<string, string>()
        { 
            { "WELLS FARGO & CO NEW", "WELLS FARGO CO"},

            { "WELLS FARGO CO NEW", "WELLS FARGO CO"}
        };

        internal static string[] s_ColumnNames = new string[] { "Company", "Class", "Shares", "Indicator", "Value(1000)" };

        public string Company { get; set; }

        public string Class { get; set; }

        public List<ShareDataPoint> DataPoints { get; set; }

        internal PortfolioEntry(string company, string cls)
        {
            Class = cls;
            Company = company;
            DataPoints = new List<ShareDataPoint>();
        }

        internal static string GetKey(DataRow row)
        {
            return GetKey(row["Company"] as string, row["Class"] as string);
        }

        internal static string GetKey(string company, string cls)
        {
            if (s_SynonymousHoldings.ContainsKey(company))
            {
                company = s_SynonymousHoldings[company];
            }

            return string.Format("{0}({1})", company.ToLower(), cls.ToLower());
        }

        internal void AddTo(DateTime latestTimestamp, DateTime previousTimestamp, DataTable table)
        {
            DataRow row = table.NewRow();
            row["Company"] = Company;
            row["Class"] = Class;
            row["Shares"] = 0;
            row["Value(1000)"] = 0;
            row["Debug"] = "";

            bool isStaleRow = false;
            IndicatorType indicator = IndicatorType.None;
            if (DataPoints[0].Timestamp == latestTimestamp)
            {
                row["Shares"] = DataPoints[0].Count;
                row["Value(1000)"] = DataPoints[0].Value;

                if (DataPoints.Count > 1)
                {
                    if (DataPoints[1].Timestamp < previousTimestamp)
                    {
                        indicator = IndicatorType.New;
                    }
                    else if (DataPoints[0].Count > DataPoints[1].Count)
                    {
                        indicator = IndicatorType.Add;
                    }
                    else if (DataPoints[0].Count < DataPoints[1].Count)
                    {
                        indicator = IndicatorType.Sell;
                    }
                }
                else
                {
                    indicator = IndicatorType.New;
                }
            }
            else if (DataPoints[0].Timestamp == previousTimestamp)
            {
                indicator = IndicatorType.SellOut;
            }
            else
            {
                isStaleRow = true;
            }

            row["Indicator"] = indicator;

            if (!isStaleRow)
            {
                table.Rows.Add(row);
            }
        }

        internal void AddPoint(ShareDataPoint point)
        {
            if (DataPoints.Count == 0)
            {
                DataPoints.Add(point);
            }
            else
            {
                if (DataPoints[DataPoints.Count - 1].Timestamp == point.Timestamp)
                {
                    point.Count += DataPoints[DataPoints.Count - 1].Count;
                    point.Value += DataPoints[DataPoints.Count - 1].Value;
                }
                else
                {
                    DataPoints.Add(point);
                }
            }
        }
    }

    internal class Portfolio
    {
        Dictionary<string, PortfolioEntry> entries;

        List<Form13F> ammendments;

        DateTime latestTimestamp;
        DateTime previousTimestamp;
        ulong totalValue;

        internal Portfolio()
        {
            entries = new Dictionary<string, PortfolioEntry>();
            ammendments = new List<Form13F>();
            latestTimestamp = previousTimestamp = DateTime.MinValue;
        }

        internal void Add(Form13F form)
        {
            if (form.IsAmendment)
            {
                ammendments.Add(form);
                return;
            }

            ulong formValue = 0;
            if (ammendments.Count > 0)
            {
                foreach (var ammendment in ammendments)
                {
                    ammendment.IsAmendment = false;
                    ammendment.Timestamp = form.Timestamp;
                    formValue += ProcessForm(ammendment);
                }

                ammendments.Clear();
            }

            formValue += ProcessForm(form);

            if (form.Timestamp > latestTimestamp)
            {
                latestTimestamp = form.Timestamp;
                totalValue = formValue;
            }
            else if (form.Timestamp > previousTimestamp)
            {
                previousTimestamp = form.Timestamp;
            }
        }

        ulong ProcessForm(Form13F form)
        {
            ulong formValue = 0;
            foreach (var position in form.Positions)
            {
                string key = PortfolioEntry.GetKey(position.Company, position.Class);
                PortfolioEntry pe;
                if (!entries.TryGetValue(key, out pe))
                {
                    pe = new PortfolioEntry(position.Company, position.Class);
                    entries[key] = pe;
                }


                pe.AddPoint(new ShareDataPoint(
                    form.Timestamp,
                    position.Shares.Amount,
                    position.Value));
                formValue += position.Value;
            }

            return formValue;
        }

        internal DataTable ToTable()
        {
            DataTable table = new DataTable();
            foreach (string colName in PortfolioEntry.s_ColumnNames)
            {
                table.Columns.Add(colName);
            }
            table.Columns.Add("Debug");
            foreach (PortfolioEntry pe in entries.Values)
            {
                pe.AddTo(latestTimestamp, previousTimestamp, table);
            }

            return table;
        }

        internal void AddChartDataPoints(
            DataRow dataRow, 
            DataPointCollection weightingSeries,
            DataPointCollection sharePriceSeries,
            DataPointCollection valueSeries,
            DataPointCollection countSeries)
        {
            countSeries.Clear();
            valueSeries.Clear();
            string key = PortfolioEntry.GetKey(dataRow);
            PortfolioEntry pe = entries[key];
            ulong value = 0;
            foreach (var p in pe.DataPoints)
            {
                if (p.Timestamp == latestTimestamp)
                {
                    value = p.Value;
                }
                countSeries.AddXY(p.Timestamp, ((double)p.Count)/1000);
                valueSeries.AddXY(p.Timestamp, ((double)p.Value) / 1000);
                sharePriceSeries.AddXY(p.Timestamp, (((double)p.Value) * 1000) / p.Count);
            }
            weightingSeries.AddXY(pe.Company, value);
            weightingSeries.AddXY("Other", totalValue);
        }

        internal static void FormatCell(DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == 0)
            {
                e.CellStyle.WrapMode = DataGridViewTriState.NotSet;
            }
            else if ((e.ColumnIndex == 3) && (e.Value != null))
            {
                IndicatorType indicator = (IndicatorType)Enum.Parse(typeof(IndicatorType), e.Value.ToString());
                switch (indicator)
                {
                    case IndicatorType.Add:
                        e.CellStyle.BackColor = Color.Green;
                        break;
                    case IndicatorType.New:
                        e.CellStyle.BackColor = Color.Green;
                        break;
                    case IndicatorType.Sell:
                        e.CellStyle.BackColor = Color.Red;
                        break;
                    case IndicatorType.SellOut:
                        e.CellStyle.BackColor = Color.Red;
                        break;
                    default:
                        break;

                }
            }
        }
    }
}
