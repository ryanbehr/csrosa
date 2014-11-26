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
using org.javarosa.core.util.externalizable;
using System;
using System.IO;
namespace org.javarosa.core.model.data
{


    /**
     * An answer data storing multiple pointers
     * @author Acellam Guy ,  Cory Zue
     *
     */
    public class MultiPointerAnswerData : IAnswerData,System.ICloneable
    {

        private IDataPointer[] data;

        /**
         * NOTE: Only for serialization/deserialization
         */
        public MultiPointerAnswerData()
        {
            //Only for serialization/deserialization
        }

        public MultiPointerAnswerData(IDataPointer[] values)
        {
            data = values;
        }

        public String getDisplayText()
        {
            String toReturn = "";
            for (int i = 0; i < data.Length; i++)
            {
                if (i != 0)
                {
                    toReturn += ", ";
                }
                toReturn += data[i].DisplayText;
            }
            return toReturn;
        }

        public Object Value
        {
            get { return data; }
            set
            {
                if (value == null)
                {
                    throw new NullReferenceException("Attempt to set an IAnswerData class to null.");
                }
                data = (IDataPointer[])value;
            }
        }
      
        public void readExternal(BinaryReader in_, PrototypeFactory pf)
        {
            int length = in_.ReadInt32();
            data = new IDataPointer[length];
            for (int i = 0; i < data.Length; ++i)
            {
                data[i] = (IDataPointer)ExtUtil.read(in_, new ExtWrapTagged());
            }
        }

        public void writeExternal(BinaryWriter out_)
        {
            out_.Write(data.Length);
            for (int i = 0; i < data.Length; ++i)
            {
                ExtUtil.write(out_, new ExtWrapTagged(data[i]));
            }
        }

        public UncastData uncast()
        {
            String ret = "";
            foreach (IDataPointer datum in data)
            {
                ret += datum.DisplayText + " ";
            }
            if (ret.Length > 0)
            {
                ret = ret.Substring(0, ret.Length - 1);
            }
            return new UncastData(ret);
        }

       

        public object Clone()
        {
            return null; //not cloneable
        }


        public string DisplayText
        {
            get { throw new NotImplementedException(); }
        }

        IAnswerData IAnswerData.cast(UncastData data)
        {
            return null;
        }
    }
}