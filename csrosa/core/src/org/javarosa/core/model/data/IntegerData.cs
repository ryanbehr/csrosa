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
     * A response to a question requesting an Integer Value
     * @author Clayton Sims
     *
     */
    public class IntegerData : IAnswerData, System.ICloneable
    {
        int n;

        /**
         * Empty Constructor, necessary for dynamic construction during deserialization.
         * Shouldn't be used otherwise.
         */
        public IntegerData()
        {

        }

        public IntegerData(int n)
        {
            this.n = n;
        }
        public IntegerData(ref int n)
        {
            Value = n;
        }

        public IAnswerData clone()
        {
            return new IntegerData(n);
        }

        /* (non-Javadoc)
         * @see org.javarosa.core.model.data.IAnswerData#getDisplayText()
         */
        public String DisplayText
        {
            get
            {
                return n.ToString();
            }
        }

        /* (non-Javadoc)
         * @see org.javarosa.core.model.data.IAnswerData#getValue()
         */
        public Object Value
        {
            get { return n; }

            set
            {
                if (Value == null)
                {
                    throw new NullReferenceException("Attempt to set an IAnswerData class to null.");
                }
                n = ((int)Value);
            }
        }

        /* (non-Javadoc)
         * @see org.javarosa.core.services.storage.utilities.Externalizable#readExternal(java.io.DataInputStream)
         */
        public void readExternal(BinaryReader in_, PrototypeFactory pf)
        {
            n = ExtUtil.readInt(in_);
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
            throw new NotImplementedException();
        }

        IAnswerData IAnswerData.cast(UncastData data)
        {
             try  {
                return new IntegerData(int.Parse(data.value_Renamed));
            }
            catch (FormatException nfe)
            {
                throw new InvalidOperationException("Invalid cast of data [" + data.value_Renamed + "] to type Decimal");
            }
        }
    }
}