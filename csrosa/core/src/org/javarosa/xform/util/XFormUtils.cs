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

using org.javarosa.core.model;
using org.javarosa.core.util.externalizable;
using org.javarosa.xform.parse;
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Xml;
namespace org.javarosa.xform.util
{

    /**
     * Static Utility methods pertaining to XForms.
     *
     * @author Acellam Guy ,  Clayton Sims
     *
     */
    public class XFormUtils
    {
        private static IXFormParserFactory _factory = new XFormParserFactory();

        public static IXFormParserFactory setXFormParserFactory(IXFormParserFactory factory)
        {
            IXFormParserFactory oldFactory = _factory;
            _factory = factory;
            return oldFactory;
        }

        public static FormDef getFormFromResource(String resource)
        {
            Stream is_ = typeof(Type).Assembly.GetManifestResourceStream(resource);
            if (is_ == null)
            {
                Console.WriteLine("Can't find form resource \"" + resource + "\". Is it in the DLL?");
                return null;
            }

            return getFormFromInputStream(is_);
        }

        /*
         * This method throws XFormParseException when the form has errors.
         */
        public static FormDef getFormFromInputStream(System.IO.Stream is_Renamed)
        {
            System.IO.StreamReader isr = null;
            try
            {
                try
                {
                    //UPGRADE_TODO: Constructor 'java.io.InputStreamReader.InputStreamReader' was converted to 'System.IO.StreamReader' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioInputStreamReaderInputStreamReader_javaioInputStream_javalangString'"
                    isr = new System.IO.StreamReader(is_Renamed, System.Text.Encoding.GetEncoding("UTF-8"));
                }
                catch (System.IO.IOException uee)
                {
                    System.Console.Out.WriteLine("UTF 8 encoding unavailable, trying default encoding");
                    isr = new System.IO.StreamReader(is_Renamed, System.Text.Encoding.Default);
                }

                return _factory.getXFormParser(isr).parse();
            }
            catch (System.IO.IOException e)
            {
                //UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Throwable.getMessage' may return a different value. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1043'"
                throw new XFormParseException("IO Exception during parse! " + e.Message);
            }
            finally
            {
                try
                {
                    if (isr != null)
                    {
                        isr.Close();
                    }
                }
                catch (System.IO.IOException e)
                {
                    System.Console.Error.WriteLine("IO Exception while closing stream.");

                }
            }
        }

        public static FormDef getFormFromSerializedResource(System.String resource)
        {
            FormDef returnForm = null;
            //UPGRADE_ISSUE: Method 'java.lang.Class.getResourceAsStream' was not converted. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1000_javalangClassgetResourceAsStream_javalangString'"
            //UPGRADE_ISSUE: Class 'java.lang.System' was not converted. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1000_javalangSystem'"
            System.IO.Stream is_Renamed = typeof(Type).Assembly.GetManifestResourceStream(resource);
            //UPGRADE_TODO: Class 'java.io.DataInputStream' was converted to 'System.IO.BinaryReader' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataInputStream'"
            System.IO.BinaryReader dis = null;
            try
            {
                if (is_Renamed != null)
                {
                    dis = new System.IO.BinaryReader(is_Renamed);
                    returnForm = (FormDef)ExtUtil.read(dis, typeof(FormDef));
                }
                else
                {
                    //#if debug.output==verbose
                    System.Console.Out.WriteLine("ResourceStream NULL");
                    //#endif
                }
            }
            catch (System.IO.IOException e)
            {
                Console.Error.WriteLine(e.StackTrace);
            }
            catch (DeserializationException e)
            {
                Console.Error.WriteLine(e.StackTrace);
            }
            finally
            {
                if (is_Renamed != null)
                {
                    try
                    {
                        is_Renamed.Close();
                    }
                    catch (System.IO.IOException e)
                    {
                        Console.Error.WriteLine(e.StackTrace);
                    }
                }
                if (dis != null)
                {
                    try
                    {
                        //UPGRADE_TODO: Method 'java.io.FilterInputStream.close' was converted to 'System.IO.BinaryReader.Close' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioFilterInputStreamclose'"
                        dis.Close();
                    }
                    catch (System.IO.IOException e)
                    {
                        Console.Error.WriteLine(e.StackTrace);
                    }
                }
            }
            return returnForm;
        }


        /////Parser Attribute warning stuff

        public static ArrayList getAttributeList(XmlElement e)
        {
            ArrayList atts = new ArrayList();
            for (int i = 0; i < e.Attributes.Count; i++)
            {
                atts.Add(e.Attributes[i]);
            }

            return atts;
        }

        public static ArrayList getUnusedAttributes(XmlElement e, ArrayList usedAtts)
        {
            ArrayList unusedAtts = getAttributeList(e);
            for (int i = 0; i < usedAtts.Count; i++)
            {
                if (unusedAtts.Contains(usedAtts[i]))
                {
                    unusedAtts.Remove(usedAtts[i]);
                }
            }

            return unusedAtts;
        }

        public static String unusedAttWarning(XmlElement e, ArrayList usedAtts)
        {
            String warning = "Warning: ";
            ArrayList ua = getUnusedAttributes(e, usedAtts);
            warning += ua.Count + " Unrecognized attributes found in Element [" + e.Name + "] and will be ignored: ";
            warning += "[";
            for (int i = 0; i < ua.Count; i++)
            {
                warning += ua[i];
                if (i != ua.Count - 1) warning += ",";
            }
            warning += "] ";
            warning += "Location:\n" + XFormParser.getVagueLocation(e);

            return warning;
        }

        public static Boolean showUnusedAttributeWarning(XmlElement e, ArrayList usedAtts)
        {
            return getUnusedAttributes(e, usedAtts).Count > 0;
        }

        /**
         * Is this element an Output tag?
         * @param e
         * @return
         */
        public static Boolean isOutput(XmlElement e)
        {
            if (e.Name.ToLower().Equals("output")) return true;
            else return false;
        }

    }

}