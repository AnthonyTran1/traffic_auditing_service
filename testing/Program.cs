using System;
using System.Xml;

namespace testing
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(@"C:\Users\Antho\Desktop\project3_folder\DMVDatabase.xml");
            XmlElement root = xmlDoc.DocumentElement;
            XmlNode queryID = root.SelectSingleNode("vehicle[@plate=\"" + "6TRJ244" + "\"]");
            if (queryID != null)
            {
                string plate = queryID.Attributes[0].Value;
                Console.WriteLine(plate);
                XmlNode makeNode = queryID.SelectSingleNode("make");
                string make = makeNode.InnerText;
                Console.WriteLine(make);
                XmlNode modelNode = queryID.SelectSingleNode("model");
                string model = modelNode.InnerText;
                Console.WriteLine(model);
                XmlNode colorNode = queryID.SelectSingleNode("color");
                string color = colorNode.InnerText;
                Console.WriteLine(color);
                XmlNode ownerNode = queryID.SelectSingleNode("owner");
                string language = ownerNode.Attributes[0].Value;
                Console.WriteLine(language);
                XmlNode nameNode = ownerNode.SelectSingleNode("name");
                string name = nameNode.InnerText;
                Console.WriteLine(name);
                XmlNode contactNode = ownerNode.SelectSingleNode("contact");
                string contact = contactNode.InnerText;
                Console.WriteLine(contact);




            }
        }
    }
}
