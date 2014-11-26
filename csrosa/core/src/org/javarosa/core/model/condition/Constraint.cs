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
using org.javarosa.core.services;
using org.javarosa.core.util.externalizable;
using org.javarosa.xpath;
using org.javarosa.xpath.expr;
using System;
using System.IO;
namespace org.javarosa.core.model.condition
{


    public class Constraint : Externalizable
    {
        public IConditionExpr constraint;
        private String constraintMsg;
        private XPathExpression xPathConstraintMsg;

        public Constraint() { }

        public Constraint(IConditionExpr constraint, String constraintMsg)
        {
            this.constraint = constraint;
            this.constraintMsg = constraintMsg;
            attemptConstraintCompile();
        }

        public String getConstraintMessage(EvaluationContext ec, FormInstance instance)
        {
            if (xPathConstraintMsg == null)
            {
                return constraintMsg;
            }
            else
            {
                try
                {
                    Object value = xPathConstraintMsg.eval(instance, ec);
                    if (value != null)
                    {
                        return (String)value;
                    }
                    return null;
                }
                catch (Exception e)
                {
                    Logger.exception("Error evaluating a valid-looking constraint xpath ", e);
                    return constraintMsg;
                }
            }
        }

        private void attemptConstraintCompile()
        {
            xPathConstraintMsg = null;
            try
            {
                if (constraintMsg != null)
                {
                    xPathConstraintMsg = XPathParseTool.parseXPath("string(" + constraintMsg + ")");
                }
            }
            catch (Exception e)
            {
                //Expected in probably most cases.
            }
        }

        public void readExternal(BinaryReader in_, PrototypeFactory pf)
        {
            constraint = (IConditionExpr)ExtUtil.read(in_, new ExtWrapTagged(), pf);
            constraintMsg = ExtUtil.nullIfEmpty(ExtUtil.readString(in_));
            attemptConstraintCompile();
        }

        public void writeExternal(BinaryWriter out_)
        {
            ExtUtil.write(out_, new ExtWrapTagged(constraint));
            ExtUtil.writeString(out_, ExtUtil.emptyIfNull(constraintMsg));
        }
    }
}