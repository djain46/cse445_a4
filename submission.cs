using System;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConsoleApp1
{
    public class Program
    {
        // Q1 urls used by the autograder
        public static string xmlURL = "https://raw.githubusercontent.com/djain46/cse445_a4/refs/heads/main/Hotels.xml";
        public static string xmlErrorURL = "https://raw.githubusercontent.com/djain46/cse445_a4/refs/heads/main/HotelsErrors.xml";
        public static string xsdURL = "https://raw.githubusercontent.com/djain46/cse445_a4/refs/heads/main/Hotels.xsd";

        public static void Main(string[] args)
        {
            string result;

            // Q3 part 1 verify valid xml against xsd
            result = Verification(xmlURL, xsdURL);
            Console.WriteLine(result);

            // Q3 part 2 verify faulty xml to show errors
            result = Verification(xmlErrorURL, xsdURL);
            Console.WriteLine(result);

            // Q3 part 3 convert valid xml to json
            result = Xml2Json(xmlURL);
            Console.WriteLine(result);
        }

        // Q2.1 schema validation helper
        public static string Verification(string xmlUrl, string xsdUrl)
        {
            var errors = new StringBuilder();

            try
            {
                var schemaSet = new XmlSchemaSet();
                schemaSet.Add(null, xsdUrl);

                var settings = new XmlReaderSettings();
                settings.ValidationType = ValidationType.Schema;
                settings.Schemas = schemaSet;
                settings.DtdProcessing = DtdProcessing.Ignore;

                // be chatty so we see all warnings
                settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
                settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessIdentityConstraints;
                settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation;

                settings.ValidationEventHandler += delegate(object sender, ValidationEventArgs e)
                {
                    errors.AppendLine(e.Severity + ": " + e.Message);
                };

                // reading triggers validation
                using (XmlReader reader = XmlReader.Create(xmlUrl, settings))
                {
                    while (reader.Read()) { }
                }
            }
            catch (XmlException ex)
            {
                errors.AppendLine("XmlException: " + ex.Message);
            }
            catch (Exception ex)
            {
                errors.AppendLine("Exception: " + ex.Message);
            }

            if (errors.Length == 0) return "No Error";
            return errors.ToString().TrimEnd();
        }

        // Q2.2 xml to json that matches the required shape
        public static string Xml2Json(string xmlUrl)
        {
            var doc = new XmlDocument();
            doc.Load(xmlUrl);

            XmlElement hotelsRoot = doc.DocumentElement;
            if (hotelsRoot == null || hotelsRoot.Name != "Hotels")
                throw new InvalidOperationException("Root element Hotels not found.");

            JArray hotelsArray = new JArray();

            // walk each Hotel and extract fields
            XmlNodeList hotelNodes = hotelsRoot.SelectNodes("Hotel");
            foreach (XmlNode hotelNode in hotelNodes)
            {
                if (hotelNode.NodeType != XmlNodeType.Element) continue;

                JObject hotelObj = new JObject();

                // name
                XmlNode nameNode = hotelNode.SelectSingleNode("Name");
                if (nameNode != null) hotelObj["Name"] = nameNode.InnerText;

                // phone list
                JArray phones = new JArray();
                XmlNodeList phoneNodes = hotelNode.SelectNodes("Phone");
                foreach (XmlNode phoneNode in phoneNodes)
                {
                    if (phoneNode != null)
                    {
                        string t = phoneNode.InnerText == null ? null : phoneNode.InnerText.Trim();
                        if (!string.IsNullOrWhiteSpace(t)) phones.Add(t);
                    }
                }
                if (phones.Count > 0) hotelObj["Phone"] = phones;

                // address object with optional attribute
                XmlNode addressNode = hotelNode.SelectSingleNode("Address");
                if (addressNode != null)
                {
                    JObject addr = new JObject();

                    XmlNode n;
                    n = addressNode.SelectSingleNode("Number");
                    if (n != null) addr["Number"] = n.InnerText;

                    n = addressNode.SelectSingleNode("Street");
                    if (n != null) addr["Street"] = n.InnerText;

                    n = addressNode.SelectSingleNode("City");
                    if (n != null) addr["City"] = n.InnerText;

                    n = addressNode.SelectSingleNode("State");
                    if (n != null) addr["State"] = n.InnerText;

                    n = addressNode.SelectSingleNode("Zip");
                    if (n != null) addr["Zip"] = n.InnerText;

                    if (addressNode.Attributes != null)
                    {
                        XmlAttribute a = addressNode.Attributes["NearestAirport"];
                        if (a != null) addr["_NearestAirport"] = a.Value;
                    }

                    hotelObj["Address"] = addr;
                }

                // optional Rating attribute to underscored field
                XmlElement hotelElem = hotelNode as XmlElement;
                if (hotelElem != null)
                {
                    string ratingAttr = hotelElem.GetAttribute("Rating");
                    if (!string.IsNullOrWhiteSpace(ratingAttr)) hotelObj["_Rating"] = ratingAttr;
                }

                hotelsArray.Add(hotelObj);
            }

            // final root object Hotels -> Hotel array
            JObject hotelsWrapper = new JObject();
            hotelsWrapper["Hotel"] = hotelsArray;

            JObject root = new JObject();
            root["Hotels"] = hotelsWrapper;

            return root.ToString(Newtonsoft.Json.Formatting.Indented);
        }
    }
}