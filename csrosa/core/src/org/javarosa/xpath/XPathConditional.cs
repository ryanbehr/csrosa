using org.javarosa.core.log;
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
using org.javarosa.xpath.expr;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
namespace org.javarosa.xpath
{

    public class XPathConditional : IConditionExpr
    {
        private XPathExpression expr;
        public String xpath; //not serialized!
        public Boolean hasNow; //indicates whether this XpathConditional contains the now() function (used for timestamping)

        public XPathConditional(String xpath)
        {
            hasNow = false;
            if (xpath.IndexOf("now()") > -1)
            {
                hasNow = true;
            }
            this.expr = XPathParseTool.parseXPath(xpath);
            this.xpath = xpath;
        }

        public XPathConditional(XPathExpression expr)
        {
            this.expr = expr;
        }

        public XPathConditional()
        {

        }

        public XPathExpression getExpr()
        {
            return expr;
        }

        public Object evalRaw(FormInstance model, EvaluationContext evalContext)
        {
            return XPathFuncExpr.unpack(expr.eval(model, evalContext));
        }

        public Boolean eval(FormInstance model, EvaluationContext evalContext)
        {
            return XPathFuncExpr.toBoolean(evalRaw(model, evalContext));
        }

        public String evalReadable(FormInstance model, EvaluationContext evalContext)
        {
            return XPathFuncExpr.ToString(evalRaw(model, evalContext));
        }

        public List<TreeReference> evalNodeset(FormInstance model, EvaluationContext evalContext)
        {
            if (expr is XPathPathExpr)
            {
                return (List<TreeReference>) (((XPathPathExpr)expr).eval(model, evalContext));
            }
            else
            {
                throw new FatalException("evalNodeset: must be path expression");
            }
        }


        private static void getTriggers(XPathExpression x, List<TreeReference> v)
        {
            if (x is XPathPathExpr)
            {
                TreeReference ref_ = ((XPathPathExpr)x).getReference();
                if (!v.Contains(ref_))
                    v.Add(ref_);
            }
            else if (x is XPathBinaryOpExpr)
            {
                getTriggers(((XPathBinaryOpExpr)x).a, v);
                getTriggers(((XPathBinaryOpExpr)x).b, v);
            }
            else if (x is XPathUnaryOpExpr)
            {
                getTriggers(((XPathUnaryOpExpr)x).a, v);
            }
            else if (x is XPathFuncExpr)
            {
                XPathFuncExpr fx = (XPathFuncExpr)x;
                for (int i = 0; i < fx.args.Length; i++)
                    getTriggers(fx.args[i], v);
            }
        }

        public Boolean equals(Object o)
        {
            if (o is XPathConditional)
            {
                XPathConditional cond = (XPathConditional)o;
                return expr.Equals(cond.expr);
            }
            else
            {
                return false;
            }
        }

        public void readExternal(BinaryReader in_, PrototypeFactory pf)
        {
            expr = (XPathExpression)ExtUtil.read(in_, new ExtWrapTagged(), pf);
            hasNow = (Boolean)ExtUtil.readBool(in_);
        }

        public void writeExternal(BinaryWriter out_)
        {
            ExtUtil.write(out_, new ExtWrapTagged(expr));
            ExtUtil.writeBool(out_, hasNow);
        }

        public String ToString()
        {
            return "xpath[" + expr.ToString() + "]";
        }

        public List<Object> pivot(FormInstance model, EvaluationContext evalContext)
        {
            return expr.pivot(model, evalContext);
        }


        List<TreeReference> IConditionExpr.getTriggers()
        {
            List<TreeReference> triggers = new List<TreeReference>();
            getTriggers(expr, triggers);
            return triggers;
        }
    }

}