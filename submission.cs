using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Newtonsoft.Json.Linq;

namespace ConsoleApp1
{
    public class Program
    {
        // *** Update these three URLs after you deploy the XML/XSD files to GitHub Pages (or other public host) ***
        public static string xmlURL       = "https://jrrahma1.github.io/cse445_a4/Hotels.xml";          // Q1.2
        public static string xmlErrorURL  = "https://jrrahma1.github.io/cse445_a4/HotelsErrors.xml";    // Q1.3
        public static string xsdURL       = "https://jrrahma1.github.io/cse445_a4/Hotels.xsd";          // Q1.1

        // Buffer for collecting validation errors
        private static readonly List<string> _validationErrors = new List<string>();

        public static void Main(string[] args)
        {
            // (1) Verify correct XML
            string result = Verification(xmlURL, xsdURL);
            Console.WriteLine("Valid XML → " + result);

            // (2) Verify faulty XML
            result = Verification(xmlErrorURL, xsdURL);
            Console.WriteLine("Faulty XML → " + result);

            // (3) Convert valid XML to JSON
            result = Xml2Json(xmlURL);
            Console.WriteLine("\nJSON output:\n" + result);
        }

        // ------------------------------------------------------------
        //  Q2.1  – Validate XML against XSD
        // ------------------------------------------------------------
        public static string Verification(string xmlUrl, string xsdUrl)
        {
            _validationErrors.Clear();

            // Download the XSD file
            XmlSchemaSet schemas = new XmlSchemaSet();
            using (WebClient wc = new WebClient())
            {
                using (Stream xsdStream = wc.OpenRead(xsdUrl))
                {
                    schemas.Add(null, XmlReader.Create(xsdStream));
                }
            }

            XmlReaderSettings settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = schemas
            };
            settings.ValidationEventHandler += (s, e) =>
            {
                _validationErrors.Add($"Line {e.Exception.LineNumber}, Pos {e.Exception.LinePosition}: {e.Message}");
            };

            try
            {
                using (WebClient wc = new WebClient())
                {
                    using (Stream xmlStream = wc.OpenRead(xmlUrl))
                    {
                        using (XmlReader reader = XmlReader.Create(xmlStream, settings))
                        {
                            while (reader.Read()) { /* read whole doc to trigger validation */ }
                        }
                    }
                }
            }
            catch (XmlException xe)
            {
                _validationErrors.Add($"XML well‑formedness error: {xe.Message}");
            }
            catch (Exception ex)
            {
                _validationErrors.Add($"Unhandled exception: {ex.Message}");
            }

            return _validationErrors.Count == 0 ? "No Error" : string.Join(" | ", _validationErrors);
        }

        // ------------------------------------------------------------
        //  Q2.2  – Convert XML to JSON with required attribute naming
        // ------------------------------------------------------------
        public static string Xml2Json(string xmlUrl)
        {
            // Load XML from URL
            XDocument doc;
            using (WebClient wc = new WebClient())
            {
                using (Stream xmlStream = wc.OpenRead(xmlUrl))
                {
                    doc = XDocument.Load(xmlStream);
                }
            }

            JObject json = new JObject();
            JArray hotelsArray = new JArray();

            foreach (var hotelElem in doc.Root.Elements("Hotel"))
            {
                JObject hotelObj = new JObject
                {
                    ["Name"] = hotelElem.Element("Name")?.Value
                };

                // Phone list
                JArray phoneArr = new JArray();
                foreach (var phone in hotelElem.Elements("Phone"))
                    phoneArr.Add(phone.Value);
                hotelObj["Phone"] = phoneArr;

                // Address object (attributes)
                var addr = hotelElem.Element("Address");
                if (addr != null)
                {
                    JObject addrObj = new JObject
                    {
                        ["Number"] = addr.Attribute("Number")?.Value,
                        ["Street"] = addr.Attribute("Street")?.Value,
                        ["City"] = addr.Attribute("City")?.Value,
                        ["State"] = addr.Attribute("State")?.Value,
                        ["Zip"] = addr.Attribute("Zip")?.Value
                    };
                    string nearest = addr.Attribute("NearestAirport")?.Value;
                    if (!string.IsNullOrEmpty(nearest))
                        addrObj["_NearestAirport"] = nearest;

                    hotelObj["Address"] = addrObj;
                }

                // Optional Rating attribute
                string rating = hotelElem.Attribute("Rating")?.Value;
                if (!string.IsNullOrEmpty(rating))
                    hotelObj["_Rating"] = rating;

                hotelsArray.Add(hotelObj);
            }

            json["Hotels"] = new JObject { ["Hotel"] = hotelsArray };

            return json.ToString();
        }
    }
}
