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

using org.javarosa.core.model.condition;
using org.javarosa.core.model.instance;
using org.javarosa.core.util.externalizable;
using System;
using System.IO;
namespace org.javarosa.xpath.expr
{


    public class XPathNumericLiteral : XPathExpression
    {
        public double d;

        public XPathNumericLiteral() { } //for deserialization

        public XPathNumericLiteral(Double d)
        {
            this.d = d;
        }

        public override Object eval(FormInstance model, EvaluationContext evalContext)
        {
            return d;
        }

        public String toString()
        {
            return "{num:" + d.ToString() + "}";
        }

        public Boolean equals(Object o)
        {
            if (o is XPathNumericLiteral)
            {
                XPathNumericLiteral x = (XPathNumericLiteral)o;
                return (Double.IsNaN(d) ? Double.IsNaN(x.d) : d == x.d);
            }
            else
            {
                return false;
            }
        }

        public void readExternal(BinaryReader in_, PrototypeFactory pf)
        {
            if (in_.ReadByte() == (byte)0x00)
            {
                d = ExtUtil.readNumeric(in_);
            }
            else
            {
                d = ExtUtil.readDecimal(in_);
            }
        }

        public void writeExternal(BinaryWriter out_)  {
		if (d == (int)d) {
			out_.Write(0x00);
			ExtUtil.writeNumeric(out_, (int)d);
		} else {
            out_.Write(0x01);
			ExtUtil.writeDecimal(out_, d);
		}
	}
    }
}