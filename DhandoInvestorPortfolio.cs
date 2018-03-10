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
using System.Threading;
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
            this.portfolioGrid.Visible = false;
            this.historyChart.Visible = false;
            this.weightingChart.Visible = false;
            this.investorListBox.DataSource = Edgar.FundNames;
            this.investorListBox.SelectedIndex = -1;
            this.WindowState = FormWindowState.Maximized;
            isInitialized = true;
        }

        void investorListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!isInitialized) return;

            string fundName = investorListBox.SelectedValue.ToString();

            Disable();

            ThreadPool.QueueUserWorkItem(this.Download13F, fundName);
        }

        void Disable()
        {
            this.portfolioGrid.Visible = false;
            this.investorListBox.Enabled = false;
            this.progressBar.Visible = true;
            this.weightingChart.Visible = false;
            this.historyChart.Visible = false;
            foreach (var series in this.historyChart.Series)
            {
                series.Points.Clear();
                series.IsVisibleInLegend = false;
            }
        }

        void Enable()
        {
            this.portfolioGrid.DataSource = this.portfolio.ToTable();
            this.portfolioGrid.Visible = true;
            this.portfolioGrid.Enabled = true;
            this.progressBar.Visible = false;
            this.investorListBox.Enabled = true;
        }

        void Download13F(object args)
        {
            this.portfolio = new Portfolio();
            foreach (Form13F form in Edgar.FetchForm13Fs(args as string, 10))
            {
                portfolio.Add(form);
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
                foreach (var s in historyChart.Series)
                {
                    s.Points.Clear();
                }

                this.weightingChart.Series[0].Points.Clear();

                this.portfolio.AddChartDataPoints(
                    (this.portfolioGrid.SelectedRows[0].DataBoundItem as DataRowView).Row,
                    this.weightingChart.Series[0].Points,
                    this.historyChart.Series[0].Points,
                    this.historyChart.Series[1].Points,
                    this.historyChart.Series[2].Points);

                this.historyChart.Visible = false;
                foreach (var s in historyChart.Series)
                {
                    this.historyChart.Visible |= (s.Points.Count > 0);
                    s.IsVisibleInLegend = (s.Points.Count > 0);
                }

                this.weightingChart.Visible = (this.weightingChart.Series[0].Points.Count > 0);
            }
        }

        private void DhandoInvestorPortfolio_Load(object sender, EventArgs e)
        {

        }
    }
}
