using org.javarosa.core.model.data.helper;
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
using org.javarosa.core.util.externalizable;
using System;
using System.IO;
namespace org.javarosa.core.model.data
{

    /**
     * A response to a question requesting a selection
     * of one and only one item from a list
     * 
     * @author Drew Roos
     *
     */
    public class SelectOneData : IAnswerData
    {
        Selection s;

        /**
         * Empty Constructor, necessary for dynamic construction during deserialization.
         * Shouldn't be used otherwise.
         */
        public SelectOneData()
        {

        }

        public SelectOneData(Selection s)
        {
            Value = s;
        }

        public IAnswerData clone()
        {
            return new SelectOneData(s);
        }

        public Object Value
        {
            set
        {
            if (value == null)
            {
                throw new NullReferenceException("Attempt to set an IAnswerData class to null.");
            }
            s = (Selection)value;
        }
        get 
        {
            return s;
        }
        }

        public String DisplayText
        {
            get { return s.Value; }
        }
        /* (non-Javadoc)
         * @see org.javarosa.core.services.storage.utilities.Externalizable#readExternal(java.io.DataInputStream)
         */
        public void readExternal(BinaryReader in_, PrototypeFactory pf)
        {
            s = (Selection)ExtUtil.read(in_, typeof(Selection), pf);
        }

        /* (non-Javadoc)
         * @see org.javarosa.core.services.storage.utilities.Externalizable#writeExternal(java.io.DataOutputStream)
         */
        public void writeExternal(BinaryWriter out_)
        {
            ExtUtil.write(out_, s);
        }

        public UncastData uncast()
        {
            return new UncastData(s.Value);
        }
        
        public object Clone()
        {
            throw new NotImplementedException();
        }

        IAnswerData IAnswerData.cast(UncastData data)
        {
            return new SelectOneData(new Selection(data.value_Renamed));
        }
    }
}