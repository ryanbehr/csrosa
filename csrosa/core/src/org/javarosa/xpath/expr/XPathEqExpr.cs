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

    public class XPathEqExpr : XPathBinaryOpExpr
    {
        public Boolean equal;

        public XPathEqExpr() { } //for deserialization

        public XPathEqExpr(Boolean equal, XPathExpression a, XPathExpression b)
            : base(a, b)
        {
            this.equal = equal;
        }

        public override Object eval(FormInstance model, EvaluationContext evalContext)
        {
            Object aval = XPathFuncExpr.unpack(a.eval(model, evalContext));
            Object bval = XPathFuncExpr.unpack(b.eval(model, evalContext));
            Boolean eq = false;

            if (aval is Boolean || bval is Boolean)
            {
                if (!(aval is Boolean))
                {
                    aval = XPathFuncExpr.toBoolean(aval);
                }
                else if (!(bval is Boolean))
                {
                    bval = XPathFuncExpr.toBoolean(bval);
                }

                Boolean ba = ((Boolean)aval);
                Boolean bb = ((Boolean)bval);
                eq = (ba == bb);
            }
            else if (aval is Double || bval is Double)
            {
                if (!(aval is Double))
                {
                    aval = XPathFuncExpr.toNumeric(aval);
                }
                else if (!(bval is Double))
                {
                    bval = XPathFuncExpr.toNumeric(bval);
                }

                double fa = ((Double)aval);
                double fb = ((Double)bval);
                eq = Math.Abs(fa - fb) < 1.0e-12;
            }
            else
            {
                aval = XPathFuncExpr.ToString(aval);
                bval = XPathFuncExpr.ToString(bval);
                eq = (aval.Equals(bval));
            }

            return (Boolean)(equal ? eq : !eq);
        }

        public String toString()
        {
            return base.ToString(equal ? "==" : "!=");
        }

        public Boolean equals(Object o)
        {
            if (o is XPathEqExpr)
            {
                XPathEqExpr x = (XPathEqExpr)o;
                return base.Equals(o) && equal == x.equal;
            }
            else
            {
                return false;
            }
        }

        public void readExternal(BinaryReader in_, PrototypeFactory pf)
        {
            equal = ExtUtil.readBool(in_);
            base.readExternal(in_, pf);
        }

        public void writeExternal(BinaryWriter out_)
        {
            ExtUtil.writeBool(out_, equal);
            base.writeExternal(out_);
        }
    }
}