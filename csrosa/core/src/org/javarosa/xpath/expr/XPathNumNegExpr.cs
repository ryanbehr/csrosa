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


    public class XPathNumNegExpr : XPathUnaryOpExpr
    {
        public XPathNumNegExpr() { } //for deserialization

        public XPathNumNegExpr(XPathExpression a)
            : base(a)
        {

        }

        public override Object eval(FormInstance model, EvaluationContext evalContext)
        {
            double aval = XPathFuncExpr.toNumeric(a.eval(model, evalContext));
            return -aval;
        }

        public String toString()
        {
            return "{unop-expr:num-neg," + a.ToString() + "}";
        }

        public Boolean equals(Object o)
        {
            if (o is XPathNumNegExpr)
            {
                return base.Equals(o);
            }
            else
            {
                return false;
            }
        }

        public void readExternal(BinaryReader in_, PrototypeFactory pf)
        {
            base.readExternal(in_, pf);
        }

        public void writeExternal(BinaryWriter out_)
        {
            base.writeExternal(out_);
        }
    }
}