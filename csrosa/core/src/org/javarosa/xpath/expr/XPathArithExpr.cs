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

    public class XPathArithExpr : XPathBinaryOpExpr
    {
        public  const int ADD = 0;
        public  const int SUBTRACT = 1;
        public  const int MULTIPLY = 2;
        public  const int DIVIDE = 3;
        public  const int MODULO = 4;

        public int op;

        public XPathArithExpr() { } //for deserialization

        public XPathArithExpr(int op, XPathExpression a, XPathExpression b) :
            base(a, b)
        {
            this.op = op;
        }

        public Object eval(FormInstance model, EvaluationContext evalContext)
        {
            double aval = XPathFuncExpr.toNumeric(a.eval(model, evalContext));
            double bval = XPathFuncExpr.toNumeric(b.eval(model, evalContext));

            double result = 0;
            switch (op)
            {
                case ADD: result = aval + bval; break;
                case SUBTRACT: result = aval - bval; break;
                case MULTIPLY: result = aval * bval; break;
                case DIVIDE: result = aval / bval; break;
                case MODULO: result = aval % bval; break;
            }
            return result;
        }

        public String ToString()
        {
            String sOp = null;

            switch (op)
            {
                case ADD: sOp = "+"; break;
                case SUBTRACT: sOp = "-"; break;
                case MULTIPLY: sOp = "*"; break;
                case DIVIDE: sOp = "/"; break;
                case MODULO: sOp = "%"; break;
            }

            return base.ToString(sOp);
        }

        public Boolean Equals(Object o)
        {
            if (o is XPathArithExpr)
            {
                XPathArithExpr x = (XPathArithExpr)o;
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
    }
}