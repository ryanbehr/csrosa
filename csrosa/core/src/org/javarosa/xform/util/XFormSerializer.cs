/*
 * Copyright (C) 2009 JavaRosa ,Copyright (C) 2014 Simbacode
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a copy of
 * the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 */

using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using org.javarosa.xml;
namespace org.javarosa.xform.util
{


    /* this is just a big dump of serialization-related code */

    /* basically, anything that didn't belong in XFormParser */

    public class XFormSerializer
    {

        public static MemoryStream getStream(XmlDocument doc)
        {
            try
            {
                return XmlParseHelpers.ToStream(doc);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                return null;
            }
        }

        public static String elementToString(XmlElement e)
        {

            try
            {
                return XmlParseHelpers.ToStream(e);

            }
            catch (Exception uce)
            {
                Console.WriteLine(uce.StackTrace);
                return "";
            }
        }

        public static String getString(XmlDocument doc)
        {
            Stream bos = getStream(doc);

           /* byte[] byteArr = (byte[])bos;
            char[] charArray = new char[byteArr.Length];
            for (int i = 0; i < byteArr.Length; i++)
                charArray[i] = (char)byteArr[i];*/

            return System.Convert.ToString(doc);
        }
    }
}