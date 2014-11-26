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
using org.javarosa.core.model.condition.pivot;
using org.javarosa.core.model.instance;
using org.javarosa.core.util.externalizable;
using System;
using System.Collections.Generic;
using System.IO;
namespace org.javarosa.xpath.expr
{


    public abstract class XPathBinaryOpExpr : XPathOpExpr
    {
        public XPathExpression a, b;

        public XPathBinaryOpExpr() { } //for deserialization of children

        public XPathBinaryOpExpr(XPathExpression a, XPathExpression b)
        {
            this.a = a;
            this.b = b;
        }

        public String ToString(String op)
        {
            return "{binop-expr:" + op + "," + a.ToString() + "," + b.ToString() + "}";
        }

        public Boolean Equals(Object o)
        {
            if (o is XPathBinaryOpExpr)
            {
                XPathBinaryOpExpr x = (XPathBinaryOpExpr)o;
                return a.Equals(x.a) && b.Equals(x.b);
            }
            else
            {
                return false;
            }
        }

        public void readExternal(BinaryReader in_, PrototypeFactory pf)
        {
            a = (XPathExpression)ExtUtil.read(in_, new ExtWrapTagged(), pf);
            b = (XPathExpression)ExtUtil.read(in_, new ExtWrapTagged(), pf);
        }

        public void writeExternal(BinaryWriter out_)
        {
            ExtUtil.write(out_, new ExtWrapTagged(a));
            ExtUtil.write(out_, new ExtWrapTagged(b));
        }

        public Object pivot(FormInstance model, EvaluationContext evalContext, List<Object> pivots, Object sentinal)
        {
            //Pivot both args
            Object aval = a.pivot(model, evalContext, pivots, sentinal);
            Object bval = b.pivot(model, evalContext, pivots, sentinal);

            //If either is the sentinal, we don't have a good way to represent the resulting expression, so fail
            if (aval == sentinal || bval == sentinal)
            {
                throw new UnpivotableExpressionException();
            }

            //If either has added a pivot, this expression can't produce any more pivots, so signal that
            if (aval == null || bval == null)
            {
                return null;
            }

            return null;

        }

        public override object eval(FormInstance model, EvaluationContext evalContext)
        {

            //Otherwise, return the value
            return this.eval(model, evalContext);
        }
    }
}