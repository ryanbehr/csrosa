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
     * Answer data representing a pointer object.  The pointer is a reference to some 
     * other object that it knows how to get out of memory.
     * 
     * @author Acellam Guy ,  Cory Zue
     *
     */
    public class PointerAnswerData : IAnswerData,System.ICloneable
    {

        private IDataPointer data;


        /**
         * NOTE: Only for serialization/deserialization
         */
        public PointerAnswerData()
        {
            //Only for serialization/deserialization
        }

        public PointerAnswerData(IDataPointer data)
        {
            this.data = data;
        }
 
        public String getDisplayText()
        {
            return data.DisplayText;
        }

        public Object getValue()
        {
            return data;
        }

        public void setValue(Object o)
        {
            if (o == null)
            {
                throw new NullReferenceException("Attempt to set an IAnswerData class to null.");
            }
            data = ((IDataPointer)o);
        }

        public void readExternal(BinaryReader in_, PrototypeFactory pf)
        {
            data = (IDataPointer)ExtUtil.read(in_, new ExtWrapTagged());
        }

        public void writeExternal(BinaryWriter out_)
        {
            ExtUtil.write(out_, new ExtWrapTagged(data));
        }

        public UncastData uncast()
        {
            return new UncastData(data.DisplayText);
        }

        public object Value
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public string DisplayText
        {
            get { throw new NotImplementedException(); }
        }

        public object Clone()
        {
            
            return null; //not cloneable
        }

        IAnswerData IAnswerData.cast(UncastData data)
        {
            return null;
        }
    }
}