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

using org.javarosa.core.model.data;
using org.javarosa.core.model.instance;
using org.javarosa.xpath;
using System;
using System.Collections;
using System.Collections.Generic;
namespace org.javarosa.core.model.condition
{
    public class EvaluationContext
    {
        private TreeReference contextNode; //unambiguous ref used as the anchor for relative paths
        private Hashtable functionHandlers;
        private Hashtable variables;

        public Boolean isConstraint; //true if we are evaluating a constraint
        public IAnswerData candidateValue; //if isConstraint, this is the value being validated
        public Boolean isCheckAddChild; //if isConstraint, true if we are checking the constraint of a parent node on how
        //  many children it may have

        private String outputTextForm = null; //Responsible for informing itext what form is requested if relevant

        internal FormInstance instance;

        public EvaluationContext(EvaluationContext base_, TreeReference context)
        {
            this.functionHandlers = base_.functionHandlers;
            this.contextNode = context;
            this.variables = new Hashtable();
        }

        public EvaluationContext()
        {
            functionHandlers = new Hashtable();
            variables = new Hashtable();
        }

        virtual public TreeReference ContextRef
        {
            get
            {
                return contextNode;
            }

        }

        public virtual void addFunctionHandler(IFunctionHandler fh)
        {
            functionHandlers.Add(fh.Name, fh);
        }

        //UPGRADE_TODO: Class 'java.util.HashMap' was converted to 'System.Collections.Hashtable' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javautilHashMap'"
        virtual public System.Collections.Hashtable FunctionHandlers
        {
            get
            {
                return functionHandlers;
            }

        }
        virtual public System.String OutputTextForm
        {
            get
            {
                return outputTextForm;
            }

            set
            {
                this.outputTextForm = value;
            }

        }
        virtual public FormInstance MainInstance
        {
            get
            {
                return instance;
            }

        }

        public void setVariables(IDictionary<String, Object> variables)
        {
            for (IEnumerator e = variables.GetEnumerator(); e.MoveNext(); )
            {
                String var = (String)e.Current;
                setVariable(var, variables[var]);
            }
        }

        public void setVariable(String name, Object value)
        {
            //No such thing as a null xpath variable. Empty
            //values in XPath just get converted to ""
            if (value == null)
            {
                variables.Add(name, "");
                return;
            }
            //Otherwise check whether the value is one of the normal first
            //order datatypes used in xpath evaluation
            if (value is Boolean ||
                       value is Double ||
                       value is String ||
                       value is DateTime ||
                       value is IExprDataType)
            {
                variables.Add(name, value);
                return;
            }

            //Some datatypes can be trivially converted to a first order
            //xpath datatype
            if (value is int)
            {
                variables.Add(name, Convert.ToDouble(value));
                return;
            }
            if (value is float)
            {
                variables.Add(name, Convert.ToDouble(value));
                return;
            }

            //Otherwise we just hope for the best, I suppose? Should we log this?
            else
            {
                variables.Add(name, value);
            }
        }

        public Object getVariable(String name)
        {
            return variables[name];
        }
    }
}