using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using HtmlAgilityPack;

namespace DhandhoTracker
{
    internal static class Edgar
    {
        static Dictionary<string, string> s_funds = new Dictionary<string, string>()
        {
            {"DALAL STREET LLC (Monish Pabrai :Author of \"The Dhandho Investor\")", "0001549575" },
            {"PERSHING SQUARE CAPITAL MANAGEMENT, L.P. (Bill Ackman)","0001336528" },
            {"ICAHN CAPITAL LP(Carl Icahn)", "0000921669" },
            {"GOTHAM ASSET MANAGEMENT, LLC (Joel Greenblatt: Author of \"Little book that beat the street\"):", "0001510387" },
            {"BILL & MELINDA GATES FOUNDATION TRUST (Bill Gates)" , "0001166559"},
            {"GREENLIGHT CAPITAL INC (David Einhorn)", "0001079114" },
            {"AQUAMARINE CAPITAL LLC (Guy Spier : Author of \"The Education of a Value Investor\"", "0001404599"},
            { "BAUPOST GROUP LLC (Seth Klarman}", "0001061768" }
        };        

        internal static IList<string> FundNames { get { return new List<string>(s_funds.Keys); } }

        static string GetWebPage(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Http.Get;
            request.UserAgent = "karthik karthiksk@msn.com";
            WebResponse response = request.GetResponse();
            string body = null;
            using (StreamReader rdr = new StreamReader(response.GetResponseStream()))
            {
                body = rdr.ReadToEnd();
            }

            return body;
        }

        internal static IEnumerable<Form13F> FetchForm13Fs(string fundName, int count)
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
            for (int i = 1; (i < rows.Count) && (count != 0); ++i)
            {
                HtmlNode row = rows[i];
                HtmlNodeCollection cols = row.SelectNodes("td");

                bool isAmendment = !cols[0].InnerText.Trim().Equals("13f-hr", StringComparison.OrdinalIgnoreCase);

                if (!isAmendment)
                {
                    --count;
                }

                string indexPageLink = null;
                foreach (HtmlNode a in cols[1].SelectNodes("a[@href]"))
                {
                    indexPageLink = a.Attributes["href"].Value;

                    break;
                }

                string date = cols[3].InnerText;

                HtmlAgilityPack.HtmlDocument indexHtml = new HtmlAgilityPack.HtmlDocument();
                indexHtml.LoadHtml(GetWebPage(GetLinkUrl(indexPageLink)));
                string infoTableLink = null;
                foreach (HtmlNode a in indexHtml.DocumentNode.SelectNodes("//a[@href]"))
                {
                    if (a.InnerText.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    {
                        infoTableLink = a.Attributes["href"].Value;
                    }
                }

                yield return Form13F.Parse( DateTime.Parse(date), GetWebPage(GetLinkUrl(infoTableLink)), isAmendment);
            }

            yield break;
        }

        static string GetQueryUrl(string fundName)
        {
            return string.Format(QUERY_PATTERN, s_funds[fundName]);
        }

        internal static string GetLinkUrl(string link)
        {
            return string.Format("{0}{1}", SEC_URI, link);
        }

        internal const string SEC_URI = @"http://www.sec.gov";
        internal const string QUERY_PATTERN = @"http://www.sec.gov/cgi-bin/browse-edgar?action=getcompany&CIK={0}&type=13f&dateb=&owner=exclude&count=10";
    }
}

