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

    public class XPathStringLiteral : XPathExpression
    {
        public String s;

        public XPathStringLiteral() { } //for deserialization

        public XPathStringLiteral(String s)
        {
            this.s = s;
        }

        public override Object eval(FormInstance model, EvaluationContext evalContext)
        {
            return s;
        }

        public String ToString()
        {
            return "{str:\'" + s + "\'}"; //TODO: s needs to be escaped (' -> \'; \ -> \\)
        }

        public Boolean equals(Object o)
        {
            if (o is XPathStringLiteral)
            {
                XPathStringLiteral x = (XPathStringLiteral)o;
                return s.Equals(x.s);
            }
            else
            {
                return false;
            }
        }

        public void readExternal(BinaryReader in_, PrototypeFactory pf)
        {
            s = ExtUtil.readString(in_);
        }

        public void writeExternal(BinaryWriter out_)
        {
            ExtUtil.writeString(out_, s);
        }
    }
}