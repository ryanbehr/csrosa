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
using System;
namespace org.javarosa.core.model.condition
{


    public class Recalculate : Triggerable
    {
        public Recalculate()
        {

        }

        public Recalculate(IConditionExpr expr, TreeReference contextRef)
            : base(expr, contextRef)
        {

        }

        public Recalculate(IConditionExpr expr, TreeReference target, TreeReference contextRef) :
            base(expr, contextRef)
        {
            addTarget(target);
        }

        public override Object eval(FormInstance model, EvaluationContext ec)
        {
            return expr.evalRaw(model, ec);
        }

        public override void apply(TreeReference ref_, Object result, FormInstance model, FormDef f)
        {
            int dataType = f.Instance.resolveReference(ref_).dataType;
            f.setAnswer(wrapData(result, dataType), ref_);
        }

        public override Boolean canCascade()
        {
            return true;
        }

        public Boolean Equals(Object o)
        {
            if (o is Recalculate)
            {
                Recalculate r = (Recalculate)o;
                if (this == r)
                    return true;

                return base.Equals(r);
            }
            else
            {
                return false;
            }
        }

        //droos 1/29/10: we need to come up with a consistent rule for whether the resulting data is determined
        //by the type of the instance node, or the type of the expression result. right now it's a mix and a mess
        //note a caveat with going solely by instance node type is that untyped nodes default to string!

        //for now, these are the rules:
        // if node type == bool, convert to Boolean (for numbers, zero = f, non-zero = t; empty string = f, all other datatypes -> error)
        // if numeric data, convert to int if node type is int OR data is an integer; else convert to double
        // if string data or date data, keep as is
        // if NaN or empty string, null
        /**
         * convert the data object returned by the xpath expression into an IAnswerData suitable for
         * storage in the FormInstance
         * 
         */
        private static IAnswerData wrapData(Object val, int dataType)
        {
            if ((val is String && ((String)val).Length == 0) ||
                (val is Double && (Double.IsNaN((Double)val))))
            {
                return null;
            }

            if (Constants.DATATYPE_BOOLEAN == dataType || val is Boolean)
            {
                //ctsims: We should really be using the Boolean datatype for real, it's 
                //necessary for backend calculations and XSD compliance

                Boolean b;

                if (val is Boolean)
                {
                    b = ((Boolean)val);
                }
                else if (val is Double)
                {
                    Double d = (Double)val;
                    b = Math.Abs(d) > (0.000000000001) && !Double.IsNaN(d);
                }
                else if (val is String)
                {
                    String s = (String)val;
                    b = s.Length > 0;
                }
                else
                {
                    throw new SystemException("unrecognized data representation while trying to convert to BOOLEAN");
                }

                return new BooleanData(b);
            }
            else if (val is Double)
            {
                double d = ((Double)val);
                long l = (long)d;
                Boolean isIntegral = Math.Abs(d - l) < 1.0e-9;
                if (Constants.DATATYPE_INTEGER == dataType ||
                           (isIntegral && (int.MaxValue >= l) && (int.MinValue <= l)))
                {
                    return new IntegerData((int)d);
                }
                else if (Constants.DATATYPE_LONG == dataType || isIntegral)
                {
                    return new LongData((long)d);
                }
                else
                {
                    return new DecimalData(d);
                }
            }
            else if (val is String)
            {
                return new StringData((String)val);
            }
            else if (val is DateTime)
            {
                if (dataType == Constants.DATATYPE_DATE_TIME)
                {
                    DateTime dt = (DateTime)val;
                    return new DateTimeData(ref dt);
                }
                else if (dataType == Constants.DATATYPE_TIME)
                {
                    DateTime dt = (DateTime)val;
                    return new TimeData(ref dt);
                }
                else
                {
                    DateTime dt = (DateTime)val;
                    return new DateData(ref dt);
                }
            }
            else
            {
                throw new SystemException("unrecognized data type in 'calculate' expression: " + val.GetType().Name);
            }
        }
    }

}