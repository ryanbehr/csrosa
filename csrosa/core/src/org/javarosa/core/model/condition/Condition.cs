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

using org.javarosa.core.model.instance;
using org.javarosa.core.util.externalizable;
using org.javarosa.xpath;
using System;
using System.Collections;
using System.IO;
namespace org.javarosa.core.model.condition
{

    public class Condition : Triggerable
    {
        public  const int ACTION_NULL = 0;
        public  const int ACTION_SHOW = 1;
        public  const int ACTION_HIDE = 2;
        public  const int ACTION_ENABLE = 3;
        public  const int ACTION_DISABLE = 4;
        public  const int ACTION_LOCK = 5;
        public  const int ACTION_UNLOCK = 6;
        public  const int ACTION_REQUIRE = 7;
        public  const int ACTION_DONT_REQUIRE = 8;

        public int trueAction;
        public int falseAction;

        public Condition()
        {

        }

        public Condition(IConditionExpr expr, int trueAction, int falseAction, TreeReference contextRef)
            : this(expr, trueAction, falseAction, contextRef, new ArrayList())
        {

        }

        public Condition(IConditionExpr expr, int trueAction, int falseAction, TreeReference contextRef, ArrayList targets)
            : base(expr, contextRef)
        {

            this.trueAction = trueAction;
            this.falseAction = falseAction;
            this.targets = targets;
        }

        public override System.Object eval(FormInstance model, EvaluationContext evalContext)
        {
            try
            {
                return expr.eval(model, evalContext);
            }
            catch (XPathException e)
            {
               Console.WriteLine("Relevant expression for " + contextRef.toString(true));
                throw e;
            }
        }

        public Boolean evalBool(FormInstance model, EvaluationContext evalContext)
        {
            return ((Boolean)eval(model, evalContext));
        }

        public override void apply(TreeReference ref_Renamed, System.Object rawResult, FormInstance model, FormDef f)
        {
            bool result = ((System.Boolean)rawResult);
            performAction(model.resolveReference(ref_Renamed), result ? trueAction : falseAction);
        }
		
        
        public override Boolean canCascade()
        {
            return (trueAction == ACTION_SHOW || trueAction == ACTION_HIDE);
        }

        private void performAction(TreeElement node, int action)
        {
            switch (action)
            {
                case ACTION_NULL: break;
                case ACTION_SHOW: node.setRelevant(true); break;
                case ACTION_HIDE: node.setRelevant(false); break;
                case ACTION_ENABLE: node.setEnabled(true); break;
                case ACTION_DISABLE: node.setEnabled(false); break;
                case ACTION_LOCK:         /* not supported */; break;
                case ACTION_UNLOCK:       /* not supported */; break;
                case ACTION_REQUIRE: node.setRequired(true); break;
                case ACTION_DONT_REQUIRE: node.setRequired(false); break;
            }
        }

        //conditions are equal if they have the same actions, expression, and triggers, but NOT targets or context ref
        public Boolean equals(Object o)
        {
            if (o is Condition)
            {
                Condition c = (Condition)o;
                if (this == c)
                    return true;

                return (this.trueAction == c.trueAction && this.falseAction == c.falseAction && base.Equals(c));
            }
            else
            {
                return false;
            }
        }

        public void readExternal(BinaryReader in_, PrototypeFactory pf)
        {
            base.readExternal(in_, pf);
            trueAction = ExtUtil.readInt(in_);
            falseAction = ExtUtil.readInt(in_);
        }

        public void writeExternal(BinaryWriter out_)
        {
            base.writeExternal(out_);
            ExtUtil.writeNumeric(out_, trueAction);
            ExtUtil.writeNumeric(out_, falseAction);
        }
    }
}