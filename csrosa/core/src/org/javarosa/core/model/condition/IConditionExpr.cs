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
using System;
using UnpivotableExpressionException = org.javarosa.core.model.condition.pivot.UnpivotableExpressionException;
using FormInstance = org.javarosa.core.model.instance.FormInstance;
using TreeReference = org.javarosa.core.model.instance.TreeReference;
using Externalizable = org.javarosa.core.util.externalizable.Externalizable;
using System.Collections.Generic;
using System.Collections;
namespace org.javarosa.core.model.condition
{

    /// <summary> A condition expression is an expression which is evaluated against the current
    /// model and produces a value. These objects should keep track of the expression that
    /// they evaluate, as well as being able to identify what references will require the
    /// condition to be triggered.
    /// 
    /// As a metaphor into an XForm, a Condition expression represents an XPath expression
    /// which you can query for a value (in a calculate or relevancy condition, for instance),
    /// can tell you what nodes its value will depend on, and optionally what values it "Pivots"
    /// around. (IE: if an expression's value is true if a node is > 25, and false otherwise, it
    /// has a "Comparison Pivot" around 25).
    /// 
    /// </summary>
    /// <author>  ctsims
    /// 
    /// </author>
    public interface IConditionExpr : Externalizable
    {

        Boolean eval(FormInstance model, EvaluationContext evalContext);
        Object evalRaw(FormInstance model, EvaluationContext evalContext);
        String evalReadable(FormInstance model, EvaluationContext evalContext);
        List<TreeReference> evalNodeset(FormInstance model, EvaluationContext evalContext);
        List<TreeReference> getTriggers(); /* vector of TreeReference */

        List<Object> pivot(FormInstance model, EvaluationContext evalContext);
    }
}