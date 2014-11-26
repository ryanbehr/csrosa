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

    public class XPathBoolExpr : XPathBinaryOpExpr
    {
        public  const int AND = 0;
        public  const int OR = 1;

        public int op;

        public XPathBoolExpr() { } //for deserialization

        public XPathBoolExpr(int op, XPathExpression a, XPathExpression b) :
            base(a, b)
        {
            this.op = op;
        }

        public Object eval(FormInstance model, EvaluationContext evalContext)
        {
            Boolean aval = XPathFuncExpr.toBoolean(a.eval(model, evalContext));

            //short-circuiting
            if ((!aval && op == AND) || (aval && op == OR))
            {
                return aval;
            }

            Boolean bval = XPathFuncExpr.toBoolean(b.eval(model, evalContext));

            Boolean result = false;
            switch (op)
            {
                case AND: result = aval && bval; break;
                case OR: result = aval || bval; break;
            }
            return result;
        }

        public String ToString()
        {
            String sOp = null;

            switch (op)
            {
                case AND: sOp = "and"; break;
                case OR: sOp = "or"; break;
            }

            return base.ToString(sOp);
        }

        public Boolean Equals(Object o)
        {
            if (o is XPathBoolExpr)
            {
                XPathBoolExpr x = (XPathBoolExpr)o;
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