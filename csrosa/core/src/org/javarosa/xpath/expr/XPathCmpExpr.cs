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

    public class XPathCmpExpr : XPathBinaryOpExpr
    {
        public  const int LT = 0;
        public  const int GT = 1;
        public  const int LTE = 2;
        public  const int GTE = 3;

        public int op;

        public XPathCmpExpr() { } //for deserialization

        public XPathCmpExpr(int op, XPathExpression a, XPathExpression b)
            : base(a, b)
        {

            this.op = op;
        }

        public Object eval(FormInstance model, EvaluationContext evalContext)
        {
            Object aval = a.eval(model, evalContext);
            Object bval = b.eval(model, evalContext);
            Boolean result = false;

            //xpath spec says comparisons only defined for numbers (not defined for strings)
            aval = XPathFuncExpr.toNumeric(aval);
            bval = XPathFuncExpr.toNumeric(bval);

            double fa = ((Double)aval);
            double fb = ((Double)bval);

            switch (op)
            {
                case LT: result = fa < fb; break;
                case GT: result = fa > fb; break;
                case LTE: result = fa <= fb; break;
                case GTE: result = fa >= fb; break;
            }

            return result;
        }

        public String ToString()
        {
            String sOp = null;

            switch (op)
            {
                case LT: sOp = "<"; break;
                case GT: sOp = ">"; break;
                case LTE: sOp = "<="; break;
                case GTE: sOp = ">="; break;
            }

            return base.ToString(sOp);
        }

        public Boolean Equals(Object o)
        {
            if (o is XPathCmpExpr)
            {
                XPathCmpExpr x = (XPathCmpExpr)o;
                return base.Equals(o) && op == x.op;
            }
            else
            {
                return false;
            }
        }

        public void readExternal(BinaryReader in_, PrototypeFactory pf)
        {
            op = ExtUtil.readInt(in_);
            base.readExternal(in_, pf);
        }

        public void writeExternal(BinaryWriter out_)
        {
            ExtUtil.writeNumeric(out_, op);
            base.writeExternal(out_);
        }


        public Object pivot(FormInstance model, EvaluationContext evalContext, List<Object> pivots, Object sentinal)
        {
            Object aval = a.pivot(model, evalContext, pivots, sentinal);
            Object bval = b.pivot(model, evalContext, pivots, sentinal);

            if (handled(aval, bval, sentinal, pivots) || handled(bval, aval, sentinal, pivots)) { return null; }

            return this.eval(model, evalContext);
        }

        private Boolean handled(Object a, Object b, Object sentinal, List<Object> pivots)
        {
            if (sentinal == a)
            {
                if (b == null)
                {
                    //Can't pivot on an expression which is derived from pivoted expressions
                    throw new UnpivotableExpressionException();
                }
                else if (sentinal == b)
                {
                    //WTF?
                    throw new UnpivotableExpressionException();
                }
                else
                {
                    Double val = 0.0;
                    //either of
                    if (b is Double)
                    {
                        val = (Double)b;
                    }
                    else
                    {
                        //These are probably the 
                        if (b is int)
                        {
                            val = ((int)b);
                        }
                        if (b is long)
                        {
                            val = ((long)b);
                        }
                        if (b is float)
                        {
                            val = ((float)b);
                        }
                        if (b is short)
                        {
                            val = ((short)b);
                        }
                        if (b is Byte)
                        {
                            val = ((Byte)b);
                        }
                        else
                        {
                            throw new UnpivotableExpressionException("Unrecognized numeric data in cmp expression: " + b);
                        }
                    }


                    pivots.Add(new CmpPivot(val, op));
                    return true;
                }
            }
            return false;
        }
    }
}