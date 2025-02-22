﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Xml;

namespace emulatorLauncher
{
    static class XmlExtensions
    {
        public static T FromXml<T>(this string xmlPathName) where T : class
        {
            if (string.IsNullOrEmpty(xmlPathName))
                return default(T);

            XmlSerializer serializer = new XmlSerializer(typeof(T));

            using (FileStream sr = new FileStream(xmlPathName, FileMode.Open, FileAccess.Read))
                return serializer.Deserialize(sr) as T;
        }

        public static T FromXmlString<T>(this string xml) where T : class
        {
            if (string.IsNullOrEmpty(xml))
                return default(T);

            var settings = new XmlReaderSettings
            {
                CheckCharacters = false,
                IgnoreWhitespace = true,
                ConformanceLevel = ConformanceLevel.Auto,
                ValidationType = ValidationType.None
            };

            XmlSerializer serializer = new XmlSerializer(typeof(T));

            // Fix attributes strings containing & caracters
            foreach (var toFix in xml.ExtractStrings("\"", "\"", true).Distinct().Where(s => s.Contains("& ")))
                xml = xml.Replace(toFix, toFix.Replace("& ", "&amp; "));

            using (var reader = new StringReader(xml))
            using (var xmlReader = XmlReader.Create(reader, settings))
            {
                var obj = serializer.Deserialize(xmlReader);
                return (T)obj;
            }
        }

        public static string ToXml<T>(this T obj, bool omitXmlDeclaration = false)
        {
            return obj.ToXml(new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
                OmitXmlDeclaration = omitXmlDeclaration
            });
        }

        public static string ToXml<T>(this T obj, XmlWriterSettings xmlWriterSettings)
        {
            if (Equals(obj, default(T)))
                return String.Empty;

            using (var memoryStream = new MemoryStream())
            {
                var xmlSerializer = new XmlSerializer(obj.GetType());

                var xmlnsEmpty = new XmlSerializerNamespaces();
                xmlnsEmpty.Add(String.Empty, String.Empty);

                using (var xmlTextWriter = XmlWriter.Create(memoryStream, xmlWriterSettings))
                {
                    xmlSerializer.Serialize(xmlTextWriter, obj, xmlnsEmpty);
                    memoryStream.Seek(0, SeekOrigin.Begin); //Rewind the Stream.
                }

                var xml = xmlWriterSettings.Encoding.GetString(memoryStream.ToArray());
                return xml;
            }
        }
    }
}
