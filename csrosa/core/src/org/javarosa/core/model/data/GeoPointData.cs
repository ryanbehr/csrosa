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

using org.javarosa.core.model.utils;
using org.javarosa.core.util.externalizable;
using System;
using System.Collections.Generic;
using System.IO;
namespace org.javarosa.core.model.data
{



    /**
     * A response to a question requesting an GeoPoint Value.
     * 
     * @author Acellam Guy ,  Yaw Anokwa
     * 
     */
    public class GeoPointData : IAnswerData, System.ICloneable
    {

        private double[] gp = new double[4];
        private int len = 2;


        /**
         * Empty Constructor, necessary for dynamic construction during
         * deserialization. Shouldn't be used otherwise.
         */
        public GeoPointData()
        {

        }


        public GeoPointData(double[] gp)
        {
            this.fillArray(gp);
        }


        private void fillArray(double[] gp)
        {
            len = gp.Length;
            for (int i = 0; i < len; i++)
            {
                this.gp[i] = gp[i];
            }
        }


        public IAnswerData clone()
        {
            return new GeoPointData(gp);
        }


        /*
         * (non-Javadoc)
         * 
         * @see org.javarosa.core.model.data.IAnswerData#getDisplayText()
         */
        public String DisplayText
        {
            get
            {
                String s = "";
                for (int i = 0; i < len; i++)
                {
                    s += gp[i] + " ";
                }
                return s.Trim();
            }
        }


        /*
         * (non-Javadoc)
         * 
         * @see org.javarosa.core.model.data.IAnswerData#getValue()
         */
        public Object Value
        {
            get
            {
                return gp;
            }
            set
            {
                if (Value == null)
                {
                    throw new NullReferenceException("Attempt to set an IAnswerData class to null.");
                }
                this.fillArray((double[])Value);
            }
        }

        public void readExternal(BinaryReader in_, PrototypeFactory pf)
        {
            len = (int)ExtUtil.readNumeric(in_);
            for (int i = 0; i < len; i++)
            {
                gp[i] = ExtUtil.readDecimal(in_);
            }
        }


        public void writeExternal(BinaryWriter out_)
        {
            ExtUtil.writeNumeric(out_, len);
            for (int i = 0; i < len; i++)
            {
                ExtUtil.writeDecimal(out_, gp[i]);
            }
        }


        public UncastData uncast()
        {
            return new UncastData(DisplayText);
        }



        public virtual System.Object Clone()
        {
            return new GeoPointData(gp);
        }

        IAnswerData IAnswerData.cast(UncastData data)
        {
            double[] ret = new double[4];

            List<String> choices = DateUtils.split(data.Value.ToString(), " ", true);
            int i = 0;
            foreach (String s in choices)
            {
                double d = Double.Parse(s);
                ret[i] = d;
                ++i;
            }
            return new GeoPointData(ret);
        }
    }
}