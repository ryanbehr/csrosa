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


using org.javarosa.core.data;
using org.javarosa.core.model;
using org.javarosa.core.model.data;
using org.javarosa.core.model.data.helper;
using org.javarosa.core.model.utils;
using System;
using System.Collections;
using System.Text;
using System.Xml;
namespace org.javarosa.xform.util
{

    /**
     * The XFormAnswerDataSerializer takes in AnswerData objects, and provides
     * an XForms compliant (String or Element) representation of that AnswerData.
     * 
     * By default, this serializer can properly operate on StringData, DateData
     * SelectMultiData, and SelectOneData AnswerData objects. This list can be
     * extended by registering appropriate XForm serializing AnswerDataSerializers
     * with this class.
     * 
     * @author Acellam Guy ,  Clayton Sims
     *
     */
    public class XFormAnswerDataSerializer : IAnswerDataSerializer
    {

        public const String DELIMITER = " ";

        ArrayList additionalSerializers = new ArrayList();

        public void registerAnswerSerializer(IAnswerDataSerializer ads)
        {
            additionalSerializers.Add(ads);
        }

        public Boolean canSerialize(IAnswerData data)
        {
            if (data is StringData || data is DateData || data is TimeData ||
                data is SelectMultiData || data is SelectOneData ||
                data is IntegerData || data is DecimalData || data is PointerAnswerData ||
                data is MultiPointerAnswerData || data is GeoPointData || data is LongData || data is DateTimeData || data is UncastData)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /**
         * @param data The AnswerDataObject to be serialized
         * @return A String which contains the given answer
         */
        public Object serializeAnswerData(UncastData data)
        {
            return data.String;
        }


        /**
         * @param data The AnswerDataObject to be serialized
         * @return A String which contains the given answer
         */
        public Object serializeAnswerData(StringData data)
        {
            return (String)data.Value;
        }

        /**
         * @param data The AnswerDataObject to be serialized
         * @return A String which contains a date in xsd:date
         * formatting
         */
        public Object serializeAnswerData(DateData data)
        {
            DateTime dt = ((DateTime)data.Value);
            return DateUtils.formatDate(ref dt, DateUtils.FORMAT_ISO8601);
        }

        /**
         * @param data The AnswerDataObject to be serialized
         * @return A String which contains a date in xsd:date
         * formatting
         */
        public Object serializeAnswerData(DateTimeData data)
        {

            DateTime dt = (DateTime)data.Value;
            return DateUtils.formatDateTime(ref dt, DateUtils.FORMAT_ISO8601);
        }

        /**
         * @param data The AnswerDataObject to be serialized
         * @return A String which contains a date in xsd:time
         * formatting
         */
        public Object serializeAnswerData(TimeData data)
        {
            DateTime dt = (DateTime)data.Value;
            return DateUtils.formatTime(ref dt, DateUtils.FORMAT_ISO8601);
        }

        /**
         * @param data The AnswerDataObject to be serialized
         * @return A String which contains a reference to the 
         * data
         */
        public Object serializeAnswerData(PointerAnswerData data)
        {
            //Note: In order to override this default behavior, a
            //new serializer should be used, and then registered
            //with this serializer
            IDataPointer pointer = (IDataPointer)data.getValue();
            return pointer.DisplayText;
        }

        /**
         * @param data The AnswerDataObject to be serialized
         * @return A String which contains a reference to the 
         * data
         */
        public Object serializeAnswerData(MultiPointerAnswerData data)
        {
            //Note: In order to override this default behavior, a
            //new serializer should be used, and then registered
            //with this serializer
            IDataPointer[] pointers = (IDataPointer[])data.Value;
            if (pointers.Length == 1)
            {
                return pointers[0].DisplayText;
            }

            XmlDocument doc = new XmlDocument();
            XmlElement parent = doc.CreateElement("serializeAnswerData");
            for (int i = 0; i < pointers.Length; ++i)
            {
                XmlElement datael = doc.CreateElement("data");

                XmlText xtext = doc.CreateTextNode(pointers[i].DisplayText);

                datael.AppendChild(xtext);
                parent.AppendChild(datael);
            }
            return parent;
        }

        /**
         * @param data The AnswerDataObject to be serialized
         * @return A string containing the xforms compliant format
         * for a <select> tag, a string containing a list of answers
         * separated by space characters.
         */
        public Object serializeAnswerData(SelectMultiData data)
        {
            ArrayList selections = (ArrayList)data.Value;
            IEnumerator en = selections.GetEnumerator();
            StringBuilder selectString = new StringBuilder();

            while (en.MoveNext())
            {
                Selection selection = (Selection)en.Current;
                if (selectString.Length > 0)
                    selectString.Append(DELIMITER);
                selectString.Append(selection.Value);
            }
            //As Crazy, and stupid, as it sounds, this is the XForms specification
            //for storing multiple selections.	
            return selectString.ToString();
        }

        /**
         * @param data The AnswerDataObject to be serialized
         * @return A String which contains the value of a selection
         */
        public Object serializeAnswerData(SelectOneData data)
        {
            return ((Selection)data.Value).Value;
        }

        public Object serializeAnswerData(IntegerData data)
        {
            return ((int)data.Value).ToString();
        }

        public Object serializeAnswerData(LongData data)
        {
            return ((long)data.Value).ToString();
        }

        public Object serializeAnswerData(DecimalData data)
        {
            return ((Double)data.Value).ToString();
        }

        public Object serializeAnswerData(GeoPointData data)
        {
            return data.DisplayText;
        }

        public Object serializeAnswerData(BooleanData data)
        {
            if (((Boolean)data.Value))
            {
                return "1";
            }
            else
            {
                return "0";
            }
        }

        public Object serializeAnswerData(IAnswerData data, int dataType)
        {
            // First, we want to go through the additional serializers, as they should
            // take priority to the default serializations
            IEnumerator en = additionalSerializers.GetEnumerator();
            while (en.MoveNext())
            {
                IAnswerDataSerializer serializer = (IAnswerDataSerializer)en.Current;
                if (serializer.canSerialize(data))
                {
                    return serializer.serializeAnswerData(data, dataType);
                }
            }
            //Defaults
            Object result = serializeAnswerData(data);
            return result;
        }

        public Object serializeAnswerData(IAnswerData data)
        {
            if (data is StringData)
            {
                return serializeAnswerData((StringData)data);
            }
            else if (data is SelectMultiData)
            {
                return serializeAnswerData((SelectMultiData)data);
            }
            else if (data is SelectOneData)
            {
                return serializeAnswerData((SelectOneData)data);
            }
            else if (data is IntegerData)
            {
                return serializeAnswerData((IntegerData)data);
            }
            else if (data is LongData)
            {
                return serializeAnswerData((LongData)data);
            }
            else if (data is DecimalData)
            {
                return serializeAnswerData((DecimalData)data);
            }
            else if (data is DateData)
            {
                return serializeAnswerData((DateData)data);
            }
            else if (data is TimeData)
            {
                return serializeAnswerData((TimeData)data);
            }
            else if (data is PointerAnswerData)
            {
                return serializeAnswerData((PointerAnswerData)data);
            }
            else if (data is MultiPointerAnswerData)
            {
                return serializeAnswerData((MultiPointerAnswerData)data);
            }
            else if (data is GeoPointData)
            {
                return serializeAnswerData((GeoPointData)data);
            }
            else if (data is DateTimeData)
            {
                return serializeAnswerData((DateTimeData)data);
            }
            else if (data is BooleanData)
            {
                return serializeAnswerData((BooleanData)data);
            }
            else if (data is UncastData)
            {
                return serializeAnswerData((UncastData)data);
            }

            return null;
        }

        /*
         * (non-Javadoc)
         * @see org.javarosa.core.model.IAnswerDataSerializer#containsExternalData(org.javarosa.core.model.data.IAnswerData)
         */
        public Boolean containsExternalData(IAnswerData data)
        {
            //First check for registered serializers to identify whether
            //they override this one.
            IEnumerator en = additionalSerializers.GetEnumerator();
            while (en.MoveNext())
            {
                IAnswerDataSerializer serializer = (IAnswerDataSerializer)en.Current;
                Boolean contains = serializer.containsExternalData(data);
                if (contains != null)
                {
                    return contains;
                }
            }
            if (data is PointerAnswerData ||
            data is MultiPointerAnswerData)
            {
                return true;
            }
            return false;
        }

        public IDataPointer[] retrieveExternalDataPointer(IAnswerData data)
        {
            IEnumerator en = additionalSerializers.GetEnumerator();
            while (en.MoveNext())
            {
                IAnswerDataSerializer serializer = (IAnswerDataSerializer)en.Current;
                Boolean contains = serializer.containsExternalData(data);
                if (contains != null)
                {
                    return serializer.retrieveExternalDataPointer(data);
                }
            }
            if (data is PointerAnswerData)
            {
                IDataPointer[] pointer = new IDataPointer[1];
                pointer[0] = (IDataPointer)((PointerAnswerData)data).getValue();
                return pointer;
            }
            else if (data is MultiPointerAnswerData)
            {
                return (IDataPointer[])((MultiPointerAnswerData)data).Value;
            }
            //This shouldn't have been called.
            return null;
        }
    }
}