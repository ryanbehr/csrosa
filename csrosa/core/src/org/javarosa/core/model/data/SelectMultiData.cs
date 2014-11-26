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

using org.javarosa.core.model.data.helper;
using org.javarosa.core.model.utils;
using org.javarosa.core.util.externalizable;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace org.javarosa.core.model.data
{

    /**
     * A response to a question requesting a selection of
     * any number of items from a list.
     * 
     * @author Acellam Guy ,  Drew Roos
     *
     */
    public class SelectMultiData : IAnswerData
    {
        List<Selection> vs; //vector of Selection

        /**
         * Empty Constructor, necessary for dynamic construction during deserialization.
         * Shouldn't be used otherwise.
         */
        public SelectMultiData()
        {

        }

        public SelectMultiData(List<Selection> vs)
        {
            Value =(vs);
        }

        public SelectMultiData(ArrayList vs)
        {
            Value = (vs);
        }

        public IAnswerData clone()
        {
            List<Selection> v = new List<Selection>();
            foreach (Selection s in vs)
            {
                v.Add((Selection)s.Clone());
            }
            return new SelectMultiData(v);
        }


            //UPGRADE_ISSUE: The equivalent in .NET for method 'java.lang.Object.clone' returns a different type. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1224'"
        public virtual System.Object Clone()
        {

            List<Selection> v = new List<Selection>();
            foreach (Selection s in vs)
            {
                v.Add((Selection)s.Clone());
            }
            return new SelectMultiData(v);
        }

        /*
         * (non-Javadoc)
         * @see org.javarosa.core.model.data.IAnswerData#getValue()
         */
        public Object Value
        {
            get { return vectorCopy(vs); }
            set {
                if (value == null)
                {
                    throw new NullReferenceException("Attempt to set an IAnswerData class to null.");
                }

                vs = vectorCopy((List<Selection>)value);
            }
        }

        /**
         * @return A type checked vector containing all of the elements
         * contained in the vector input
         * TODO: move to utility class
         */
        private List<Selection> vectorCopy(List<Selection> input)
        {
            List<Selection> output = new List<Selection>();
            //validate type
            for (int i = 0; i < input.Count; i++)
            {
                Selection s = (Selection)input[i];
                output.Add(s);
            }
            return output;
        }
        /**
         * @return THE XMLVALUE!!
         */
        /*
         * (non-Javadoc)
         * @see org.javarosa.core.model.data.IAnswerData#getDisplayText()
         */
        public String DisplayText
        {
            get
            {
                String str = "";

                for (int i = 0; i < vs.Count; i++)
                {
                    Selection s = (Selection)vs[i];
                    str += s.Value;
                    if (i < vs.Count - 1)
                        str += ", ";
                }

                return str;
            }
        }
        /* (non-Javadoc)
         * @see org.javarosa.core.services.storage.utilities.Externalizable#readExternal(java.io.DataInputStream)
         */
        public void readExternal(BinaryReader in_, PrototypeFactory pf)
        {
            vs = (List<Selection>)ExtUtil.read(in_, new ExtWrapList(typeof(Selection)), pf);
        }

        /* (non-Javadoc)
         * @see org.javarosa.core.services.storage.utilities.Externalizable#writeExternal(java.io.DataOutputStream)
         */
        public void writeExternal(BinaryWriter out_)
        {
            ExtUtil.write(out_, new ExtWrapList(vs));
        }

        public UncastData uncast()
        {
            IEnumerator en = vs.GetEnumerator();
            StringBuilder selectString = new StringBuilder();

            while (en.MoveNext())
            {
                Selection selection = (Selection)en.Current;
                if (selectString.Length > 0)
                    selectString.Append(" ");
                selectString.Append(selection.Value);
            }
            //As Crazy, and stupid, as it sounds, this is the XForms specification
            //for storing multiple selections.	
            return new UncastData(selectString.ToString());
        }


        IAnswerData IAnswerData.cast(UncastData data)
        {
            List<Selection> v = new List<Selection>();

            List<String> choices = DateUtils.split(data.Value.ToString(), " ", true);
            foreach (String s in choices)
            {
                v.Add(new Selection(s));
            }
            return new SelectMultiData(v);
        }
    }
}