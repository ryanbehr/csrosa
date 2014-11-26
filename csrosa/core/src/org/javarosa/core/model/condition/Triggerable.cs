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
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace org.javarosa.core.model.condition
{

    public abstract class Triggerable : Externalizable
    {
        public IConditionExpr expr;
        public ArrayList targets;
        public TreeReference contextRef;  //generic ref used to turn triggers into absolute references

        public Triggerable()
        {

        }

        public Triggerable(IConditionExpr expr, TreeReference contextRef)
        {
            this.expr = expr;
            this.contextRef = contextRef;
            this.targets = new ArrayList();
        }

        //UPGRADE_NOTE: Access modifiers of method 'eval' were changed to 'public'. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1204'"
        public abstract System.Object eval(FormInstance instance, EvaluationContext ec);

        //UPGRADE_NOTE: Access modifiers of method 'apply' were changed to 'public'. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1204'"
        public abstract void apply(TreeReference ref_Renamed, System.Object result, FormInstance instance, FormDef f);
		

        public abstract Boolean canCascade();

        public void apply(FormInstance instance, EvaluationContext evalContext, FormDef f)
        {
            Object result = eval(instance, evalContext);

            for (int i = 0; i < targets.Count; i++)
            {
                TreeReference targetRef = ((TreeReference)targets[i]).contextualize(evalContext.ContextRef);
                List<TreeReference> v = instance.expandReference(targetRef);
                for (int j = 0; j < v.Count; j++)
                {
                    TreeReference affectedRef = (TreeReference)v[j];
                    apply(affectedRef, result, instance, f);
                }
            }
        }

        public void addTarget(TreeReference target)
        {
            if (targets.IndexOf(target) == -1)
                targets.Add(target);
        }

        public ArrayList getTargets()
        {
            return targets;
        }

        public List<TreeReference> getTriggers()
        {
            List<TreeReference> relTriggers = expr.getTriggers();
            List<TreeReference> absTriggers = new List<TreeReference>();
            for (int i = 0; i < relTriggers.Count; i++)
            {
                absTriggers.Add(((TreeReference)relTriggers[i]).anchor(contextRef));
            }
            return absTriggers;
        }

        public Boolean Equals(Object o)
        {
            if (o is Triggerable)
            {
                Triggerable t = (Triggerable)o;
                if (this == t)
                    return true;

                if (this.expr.Equals(t.expr))
                {
                    //check triggers
                    List<TreeReference> Atriggers = this.getTriggers();
                    List<TreeReference> Btriggers = t.getTriggers();

                    //order and quantity don't matter; all that matters is every trigger in A exists in B and vice versa
                    for (int k = 0; k < 2; k++)
                    {
                        List<TreeReference> v1 = (k == 0 ? Atriggers : Btriggers);
                        List<TreeReference> v2 = (k == 0 ? Btriggers : Atriggers);

                        for (int i = 0; i < v1.Count; i++)
                        {
                            if (v2.IndexOf(v1[i]) == -1)
                            {
                                return false;
                            }
                        }
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public void readExternal(BinaryReader in_renamed, PrototypeFactory pf)
        {
            expr = (IConditionExpr)ExtUtil.read(in_renamed, new ExtWrapTagged(), pf);
            contextRef = (TreeReference)ExtUtil.read(in_renamed, typeof(TreeReference), pf);
            targets = (ArrayList)ExtUtil.read(in_renamed, new ExtWrapList(typeof(TreeReference)), pf);
        }

        public void writeExternal(BinaryWriter out_renamed)
        {
            ExtUtil.write(out_renamed, new ExtWrapTagged(expr));
            ExtUtil.write(out_renamed, contextRef);
            ExtUtil.write(out_renamed, new ExtWrapList(targets));
        }

        public String toString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < targets.Count; i++)
            {
                sb.Append(((TreeReference)targets[i]).toString());
                if (i < targets.Count - 1)
                    sb.Append(",");
            }
            return "trig[expr:" + expr.ToString() + ";targets[" + sb.ToString() + "]]";
        }
    }

}