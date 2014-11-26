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
     * A response to a question requesting an Long Numeric Value
     * @author Acellam Guy ,  Clayton Sims
     *
     */
    public class LongData : IAnswerData, System.ICloneable
    {
        long n;

        /**
         * Empty Constructor, necessary for dynamic construction during deserialization.
         * Shouldn't be used otherwise.
         */
        public LongData()
        {

        }

        public LongData(long n)
        {
            this.n = n;
        }
        public LongData(ref long n)
        {
            Value = n;
        }

      

        /* (non-Javadoc)
         * @see org.javarosa.core.model.data.IAnswerData#getDisplayText()
         */
        public String DisplayText
        {
            get { return n.ToString(); }
        }

        /* (non-Javadoc)
         * @see org.javarosa.core.model.data.IAnswerData#getValue()
         */
        public Object Value
        {
            get { return n; }
            set
            {
                if (value == null)
                {
                    throw new NullReferenceException("Attempt to set an IAnswerData class to null.");
                }
                n = ((long)value);
            }
        }

        /* (non-Javadoc)
         * @see org.javarosa.core.services.storage.utilities.Externalizable#readExternal(java.io.DataInputStream)
         */
        public void readExternal(BinaryReader in_, PrototypeFactory pf)
        {
            n = ExtUtil.readNumeric(in_);
        }

        /* (non-Javadoc)
         * @see org.javarosa.core.services.storage.utilities.Externalizable#writeExternal(java.io.DataOutputStream)
         */
        public void writeExternal(BinaryWriter out_)
        {
            ExtUtil.writeNumeric(out_, n);
        }

        public UncastData uncast()
        {
            return new UncastData(n.ToString());
        }
        

        public object Clone()
        {
            return new LongData(n);
        }

        IAnswerData IAnswerData.cast(UncastData data)
        {
            try
            {
                return new LongData(long.Parse(data.value_Renamed));
            }
            catch (FormatException nfe)
            {
                throw new InvalidOperationException("Invalid cast of data [" + data.value_Renamed + "] to type Long");
            }
        }
    }
}