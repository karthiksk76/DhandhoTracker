using System;
using System.Collections.Generic;

namespace DhandhoTracker
{
    using CIKInfo = KeyValuePair<string, string>;

    internal static class Edgar
    {
        static Dictionary<string, CIKInfo> s_funds = new Dictionary<string, CIKInfo>()
        {
            {"DALAL STREET LLC (Monish Pabrai :Author of \"The Dhandho Investor\")", new CIKInfo("0001549575", "infotable.xml")},
            {"PERSHING SQUARE CAPITAL MANAGEMENT, L.P. (Bill Ackman)",new CIKInfo("0001336528", "infotable.xml") },
            {"ICAHN CAPITAL LP(Carl Icahn)", new CIKInfo("0000921669", "form13fInfoTable.xml")},
            {"Gotham Funds LLC (Joel Greenblatt: Author of \"Little book that beat the street\"):", new CIKInfo("0001585183", "infotable.xml")}
        };        

        internal static IList<string> FundNames { get { return new List<string>(s_funds.Keys); } }

        internal static string GetQueryUrl(string fundName)
        {
            return string.Format(QUERY_PATTERN, s_funds[fundName].Key);
        }

        internal static string GetInfoTableUrl(string fundName, string link)
        {
            link = link.Trim();
            link = link.Substring(0, link.LastIndexOf("/"));

            return string.Format("{0}{1}/{2}", SEC_URI, link, s_funds[fundName].Value);
        }

        internal const string SEC_URI = @"http://www.sec.gov";
        internal const string QUERY_PATTERN = @"http://www.sec.gov/cgi-bin/browse-edgar?action=getcompany&CIK={0}&type=13f&dateb=&owner=exclude&count=10";
    }
}

