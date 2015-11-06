using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace DhandhoTracker
{
    public class SharesInfo
    {
        [XmlElement("sshPrnamt")]
        public uint Amount;
    }

    
    public class Position
    {
        [XmlElement("nameOfIssuer")]
        public string Company;

        [XmlElement("titleOfClass")]
        public string Class;

        [XmlElement("value")]
        public uint Value;

        [XmlElement("shrsOrPrnAmt")]
        public SharesInfo Shares;

    }

    [XmlRoot("informationTable")]
    public class Form13F
    {
        const string XML_NS = @"http://www.sec.gov/edgar/document/thirteenf/informationtable";

        [XmlElement("infoTable")]
        public Position[] Positions;

        public DateTime Timestamp { get; set; }

        public bool IsAmendment { get; set; }

        internal static Form13F Parse(DateTime timeStamp, string xml, bool isAmendment)
        {
            XmlSerializer ser = new XmlSerializer(typeof(Form13F), XML_NS);
            Form13F form = ser.Deserialize(new StringReader(xml)) as Form13F;
            form.Timestamp = timeStamp;
            form.IsAmendment = isAmendment;
            return form;
        }
    }
}
