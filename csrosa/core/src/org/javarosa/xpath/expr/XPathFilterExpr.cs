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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace org.javarosa.xpath.expr
{


    public class XPathFilterExpr : XPathExpression
    {
        public XPathExpression x;
        public XPathExpression[] predicates;

        public XPathFilterExpr() { } //for deserialization

        public XPathFilterExpr(XPathExpression x, XPathExpression[] predicates)
        {
            this.x = x;
            this.predicates = predicates;
        }

        public override Object eval(FormInstance model, EvaluationContext evalContext)
        {
            throw new XPathUnsupportedException("filter expression");
        }

        public String toString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("{filt-expr:");
            sb.Append(x.ToString());
            sb.Append(",{");
            for (int i = 0; i < predicates.Length; i++)
            {
                sb.Append(predicates[i].ToString());
                if (i < predicates.Length - 1)
                    sb.Append(",");
            }
            sb.Append("}}");

            return sb.ToString();
        }

        public Boolean equals(Object o)
        {
            if (o is XPathFilterExpr)
            {
                XPathFilterExpr fe = (XPathFilterExpr)o;

                ArrayList a = new ArrayList();
                for (int i = 0; i < predicates.Length; i++)
                    a.Add(predicates[i]);
                ArrayList b = new ArrayList();
                for (int i = 0; i < fe.predicates.Length; i++)
                    b.Add(fe.predicates[i]);

                return x.Equals(fe.x) && ExtUtil.vectorEquals(a, b);
            }
            else
            {
                return false;
            }
        }

        public void readExternal(BinaryReader in_, PrototypeFactory pf)
        {
            x = (XPathExpression)ExtUtil.read(in_, new ExtWrapTagged(), pf);
            ArrayList v = (ArrayList)ExtUtil.read(in_, new ExtWrapListPoly(), pf);

            predicates = new XPathExpression[v.Count];
            for (int i = 0; i < predicates.Length; i++)
                predicates[i] = (XPathExpression)v[i];
        }

        public void writeExternal(BinaryWriter out_)
        {
            ArrayList v = new ArrayList();
            for (int i = 0; i < predicates.Length; i++)
                v.Add(predicates[i]);

            ExtUtil.write(out_, new ExtWrapTagged(x));
            ExtUtil.write(out_, new ExtWrapListPoly(v));
        }

        public Object pivot(FormInstance model, EvaluationContext evalContext, List<Object> pivots, Object sentinal)
        {
            throw new UnpivotableExpressionException();
        }
    }
}