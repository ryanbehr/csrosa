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
     * A response to a question requesting an Decimal Value.  Adapted from IntegerData
     * @author Acellam Guy ,  Brian DeRenzi
     *
     */
    public class DecimalData : IAnswerData, System.ICloneable
    {
        double d;

        /**
         * Empty Constructor, necessary for dynamic construction during deserialization.
         * Shouldn't be used otherwise.
         */
        public DecimalData()
        {

        }

        public DecimalData(double d)
        {
            this.d = d;
        }
        public DecimalData(ref Double d)
        {
            Value =d;
        }

        public IAnswerData clone()
        {
            return new DecimalData(d);
        }

        virtual public System.String DisplayText
        {
            get
            {
                return System.Convert.ToString(d);
            }

        }
        virtual public System.Object Value
        {
            get
            {
                return (double)d;
            }

            set
            {
                if (value == null)
                {
                    throw new System.NullReferenceException("Attempt to set an IAnswerData class to null.");
                }
                d = ((System.Double)value);
            }

        }


        /* (non-Javadoc)
         * @see org.javarosa.core.services.storage.utilities.Externalizable#readExternal(java.io.DataInputStream)
         */
        public void readExternal(BinaryReader in_, PrototypeFactory pf)
        {
            d = ExtUtil.readDecimal(in_);
        }

        /* (non-Javadoc)
         * @see org.javarosa.core.services.storage.utilities.Externalizable#writeExternal(java.io.DataOutputStream)
         */
        public void writeExternal(BinaryWriter out_)
        {
            ExtUtil.writeDecimal(out_, d);
        }

        public UncastData uncast()
        {
            return new UncastData(((Double)Value).ToString());
        }

        public object Clone()
        {
            throw new NotImplementedException();
        }

        IAnswerData IAnswerData.cast(UncastData data)
        {
            try
            {
                return new DecimalData(Double.Parse(data.value_Renamed));
            }
            catch (FormatException nfe)
            {
                throw new ArgumentException("Invalid cast of data [" + data.Value + "] to type Decimal");
            }
        }
    }
}