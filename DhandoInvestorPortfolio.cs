using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;
using System.Net;
using System.Xml;
using System.Xml.Serialization;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace DhandhoTracker
{
    public partial class DhandoInvestorPortfolio : Form
    {
        bool isInitialized;
        Portfolio portfolio;

        delegate void EnableDelegate();
        public DhandoInvestorPortfolio()
        {
            isInitialized = false;
            InitializeComponent();
            this.investorListBox.DataSource = Edgar.FundNames;
            this.investorListBox.SelectedIndex = -1;

            isInitialized = true;
        }

        void investorListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!isInitialized) return;

            string fundName = investorListBox.SelectedValue.ToString();

            Disable();

            Task task = Task.Run(() => { Download13F(fundName); });
        }

        void Disable()
        {
            this.portfolioGrid.Enabled = false;
            this.investorListBox.Enabled = false;
            this.progressBar.Visible = true;
            this.weightingChart.Visible = false;
            this.historyChart.Visible = false;
        }

        void Enable()
        {
            this.portfolioGrid.DataSource = this.portfolio.ToTable();
            this.portfolioGrid.Enabled = true;
            this.progressBar.Visible = false;
            this.investorListBox.Enabled = true;
            this.portfolio.AddWeightingDataPoints(this.weightingChart.Series[0].Points);
            this.weightingChart.Visible = true;
            this.historyChart.Visible = true;
        }

        string GetWebPage(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Http.Get;
            WebResponse response = request.GetResponse();
            string body = null;
            using (StreamReader rdr = new StreamReader(response.GetResponseStream()))
            {
                body = rdr.ReadToEnd();
            }

            return body;
        }

        void Download13F(string fundName)
        {
            HtmlAgilityPack.HtmlDocument html = new HtmlAgilityPack.HtmlDocument();
            html.LoadHtml(GetWebPage(Edgar.GetQueryUrl(fundName)));

            HtmlNode table = null;
            HtmlNodeCollection tables = html.DocumentNode.SelectNodes("//table[@summary]");
            foreach (HtmlNode t in tables)
            {
                if (t.Attributes["summary"].Value == "Results")
                {
                    table = t;
                }
            }

            HtmlNodeCollection rows = table.SelectNodes("tr");
            this.portfolio = new Portfolio();
            for (int i = 1; (i < rows.Count) && (i <= 6); ++i)
            {
                HtmlNode row = rows[i];
                HtmlNodeCollection cols = row.SelectNodes("td");

                string link = null;
                foreach (HtmlNode a in cols[1].SelectNodes("a[@href]"))
                {
                    link = a.Attributes["href"].Value;

                    break;
                }

                string date = cols[3].InnerText;
                string form = GetWebPage(Edgar.GetInfoTableUrl(fundName, link));
                portfolio.Add(DateTime.Parse(date),Form13F.Parse(form));
            }

            this.Invoke(new EnableDelegate(this.Enable));
            
        }

        private void portfolioGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            Portfolio.FormatCell(e);
        }

        private void portfolioGrid_SelectionChanged(object sender, EventArgs e)
        {
            if ((this.portfolioGrid.SelectedRows != null) && (this.portfolioGrid.SelectedRows.Count > 0))
            {
                DataTable table = this.portfolioGrid.DataSource as DataTable;
                this.historyChart.Series[0].Points.Clear();
                this.historyChart.Series[1].Points.Clear();
                this.portfolio.AddHistoryDataPoints(
                    table.Rows[this.portfolioGrid.SelectedRows[0].Index],
                    this.historyChart.Series[0].Points,
                    this.historyChart.Series[1].Points);
            }
        }
    }
}
