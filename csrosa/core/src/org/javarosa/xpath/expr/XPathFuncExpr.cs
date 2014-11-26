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
using org.javarosa.core.model.utils;
using org.javarosa.core.util;
using org.javarosa.core.util.externalizable;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
namespace org.javarosa.xpath.expr
{

    /**
     * Representation of an xpath function expression.
     * 
     * All of the built-in xpath functions are included here, as well as the xpath type conversion logic
     * 
     * Evaluation of functions can delegate out to custom function handlers that must be registered at
     * runtime.
     * 
     * @author Acellam Guy ,  Drew Roos
     *
     */
    public class XPathFuncExpr : XPathExpression
    {
        public XPathQName id;			//name of the function
        public XPathExpression[] args;	//argument list

        public XPathFuncExpr() { } //for deserialization

        public XPathFuncExpr(XPathQName id, XPathExpression[] args)
        {
            this.id = id;
            this.args = args;
        }

        public String ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("{func-expr:");
            sb.Append(id.ToString());
            sb.Append(",{");
            for (int i = 0; i < args.Length; i++)
            {
                sb.Append(args[i].ToString());
                if (i < args.Length - 1)
                    sb.Append(",");
            }
            sb.Append("}}");

            return sb.ToString();
        }

        public Boolean Equals(Object o)
        {
            if (o is XPathFuncExpr)
            {
                XPathFuncExpr x = (XPathFuncExpr)o;

                //Shortcuts for very easily comprable values
                if (!id.Equals(x.id) || args.Length != x.args.Length)
                {
                    return false;
                }

                return ExtUtil.arrayEquals(args, x.args);
            }
            else
            {
                return false;
            }
        }

        public void readExternal(BinaryReader in_renamed, PrototypeFactory pf)
        {
            id = (XPathQName)ExtUtil.read(in_renamed, typeof(XPathQName));
            ArrayList v = (ArrayList)ExtUtil.read(in_renamed, new ExtWrapListPoly(), pf);

            args = new XPathExpression[v.Count];
            for (int i = 0; i < args.Length; i++)
                args[i] = (XPathExpression)v[i];
        }

        public void writeExternal(BinaryWriter out_renamed)
        {
            ArrayList v = new ArrayList();
            for (int i = 0; i < args.Length; i++)
                v.Add(args[i]);

            ExtUtil.write(out_renamed, id);
            ExtUtil.write(out_renamed, new ExtWrapListPoly(v));
        }

        /**
         * Evaluate the function call.
         * 
         * First check if the function is a member of the built-in function suite. If not, then check
         * for any custom handlers registered to handler the function. If not, throw and exception.
         * 
         * Both function name and appropriate arguments are taken into account when finding a suitable
         * handler. For built-in functions, the number of arguments must match; for custom functions,
         * the supplied arguments must match one of the function prototypes defined by the handler.
         * 
         */
        public override Object eval(FormInstance model, EvaluationContext evalContext)
        {
            String name = id.ToString();
            Object[] argVals = new Object[args.Length];

            Hashtable funcHandlers = evalContext.FunctionHandlers;

            for (int i = 0; i < args.Length; i++)
            {
                argVals[i] = args[i].eval(model, evalContext);
            }

            //check built-in functions
            if (name.Equals("true") && args.Length == 0)
            {
                return Boolean.TrueString;
            }
            else if (name.Equals("false") && args.Length == 0)
            {
                return Boolean.FalseString;
            }
            else if (name.Equals("Boolean") && args.Length == 1)
            {
                return toBoolean(argVals[0]);
            }
            else if (name.Equals("number") && args.Length == 1)
            {
                return toNumeric(argVals[0]);
            }
            else if (name.Equals("int") && args.Length == 1)
            { //non-standard
                return toInt(argVals[0]);
            }
            else if (name.Equals("string") && args.Length == 1)
            {
                return ToString(argVals[0]);
            }
            else if (name.Equals("date") && args.Length == 1)
            { //non-standard
                return toDate(argVals[0]);
            }
            else if (name.Equals("not") && args.Length == 1)
            {
                return boolNot(argVals[0]);
            }
            else if (name.Equals("Boolean-from-string") && args.Length == 1)
            {
                return boolStr(argVals[0]);
            }
            else if (name.Equals("format-date") && args.Length == 2)
            {
                return dateStr(argVals[0], argVals[1]);
            }
            else if (name.Equals("if") && args.Length == 3)
            { //non-standard
                return ifThenElse(argVals[0], argVals[1], argVals[2]);
            }
            else if ((name.Equals("selected") || name.Equals("is-selected")) && args.Length == 2)
            { //non-standard
                return multiSelected(argVals[0], argVals[1]);
            }
            else if (name.Equals("count-selected") && args.Length == 1)
            { //non-standard
                return countSelected(argVals[0]);
            }
            else if (name.Equals("coalesce") && args.Length == 2)
            {
                return (!isNull(argVals[0]) ? argVals[0] : argVals[1]);
            }
            else if (name.Equals("count") && args.Length == 1)
            {
                return count(argVals[0]);
            }
            else if (name.Equals("sum") && args.Length == 1)
            {
                if (argVals[0] is XPathNodeset)
                {
                    return sum(((XPathNodeset)argVals[0]).toArgList());
                }
                else
                {
                    throw new XPathTypeMismatchException("not a nodeset");
                }
            }
            else if (name.Equals("today") && args.Length == 0)
            {
                DateTime dt = new DateTime();
                return DateUtils.roundDate(ref dt);
            }
            else if (name.Equals("now") && args.Length == 0)
            {
                return new DateTime();
            }
            else if (name.Equals("concat"))
            {
                if (args.Length == 1 && argVals[0] is XPathNodeset)
                {
                    return join("", ((XPathNodeset)argVals[0]).toArgList());
                }
                else
                {
                    return join("", argVals);
                }
            }
            else if (name.Equals("join") && args.Length >= 1)
            {
                if (args.Length == 2 && argVals[1] is XPathNodeset)
                {
                    return join(argVals[0], ((XPathNodeset)argVals[1]).toArgList());
                }
                else
                {
                    return join(argVals[0], subsetArgList(argVals, 1));
                }
            }
            else if (name.Equals("substr") && (args.Length == 2 || args.Length == 3))
            {
                return Substring(argVals[0], argVals[1], args.Length == 3 ? argVals[2] : null);
            }
            else if (name.Equals("string-length") && args.Length == 1)
            {
                return stringLength(argVals[0]);
            }
            else if (name.Equals("checklist") && args.Length >= 2)
            { //non-standard
                if (args.Length == 3 && argVals[2] is XPathNodeset)
                {
                    return checklist(argVals[0], argVals[1], ((XPathNodeset)argVals[2]).toArgList());
                }
                else
                {
                    return checklist(argVals[0], argVals[1], subsetArgList(argVals, 2));
                }
            }
            else if (name.Equals("weighted-checklist") && args.Length >= 2 && args.Length % 2 == 0)
            { //non-standard
                if (args.Length == 4 && argVals[2] is XPathNodeset && argVals[3] is XPathNodeset)
                {
                    Object[] factors = ((XPathNodeset)argVals[2]).toArgList();
                    Object[] weights = ((XPathNodeset)argVals[3]).toArgList();
                    if (factors.Length != weights.Length)
                    {
                        throw new XPathTypeMismatchException("weighted-checklist: nodesets not same length");
                    }
                    return checklistWeighted(argVals[0], argVals[1], factors, weights);
                }
                else
                {
                    return checklistWeighted(argVals[0], argVals[1], subsetArgList(argVals, 2, 2), subsetArgList(argVals, 3, 2));
                }
            }
            else if (name.Equals("regex") && args.Length == 2)
            { //non-standard
                return regex(argVals[0], argVals[1]);
            }
            else if (name.Equals("depend") && args.Length >= 1)
            { //non-standard
                return argVals[0];
            }
            else if (name.Equals("random") && args.Length == 0)
            { //non-standard
                //calculated expressions may be recomputed w/o warning! use with caution!!
                return MathUtils.getRand().NextDouble();
            }
            else if (name.Equals("uuid") && (args.Length == 0 || args.Length == 1))
            { //non-standard
                //calculated expressions may be recomputed w/o warning! use with caution!!
                if (args.Length == 0)
                {
                    return PropertyUtils.genUUID();
                }

                int len = (int)toInt(argVals[0]);
                return PropertyUtils.genGUID(len);
            }
            else
            {
                //check for custom handler
                IFunctionHandler handler = (IFunctionHandler)funcHandlers[name];
                if (handler != null)
                {
                    return evalCustomFunction(handler, argVals);
                }
                else
                {
                    throw new XPathUnhandledException("function \'" + name + "\'");
                }
            }
        }

        /**
         * Given a handler registered to handle the function, try to coerce the function arguments into
         * one of the prototypes defined by the handler. If no suitable prototype found, throw an eval
         * exception. Otherwise, evaluate.
         * 
         * Note that if the handler supports 'raw args', it will receive the full, unaltered argument
         * list if no prototype matches. (this lets functions support variable-length argument lists)
         * 
         * @param handler
         * @param args
         * @return
         */
        private static Object evalCustomFunction(IFunctionHandler handler, Object[] args)
        {
            ArrayList prototypes = handler.Prototypes;
            IEnumerator e = prototypes.GetEnumerator();
            Object[] typedArgs = null;

            while (typedArgs == null && e.MoveNext())
            {
                typedArgs = matchPrototype(args, (Type[])e.Current);
            }

            if (typedArgs != null)
            {
                return handler.eval(typedArgs, new EvaluationContext());
            }
            else if (handler.rawArgs())
            {
                return handler.eval(args, new EvaluationContext());  //should we have support for expanding nodesets here?
            }
            else
            {
                throw new XPathTypeMismatchException("for function \'" + handler.Name + "\'");
            }
        }

        /**
         * Given a prototype defined by the function handler, attempt to coerce the function arguments
         * to match that prototype (checking # args, type conversion, etc.). If it is coercible, return
         * the type-converted argument list -- these will be the arguments used to evaluate the function.
         * If not coercible, return null.
         * 
         * @param args
         * @param prototype
         * @return
         */
        private static Object[] matchPrototype(Object[] args, Type[] prototype)
        {
            Object[] typed = null;

            if (prototype.Length == args.Length)
            {
                typed = new Object[args.Length];

                for (int i = 0; i < prototype.Length; i++)
                {
                    typed[i] = null;

                    //how to handle type conversions of custom types?
                    if (prototype[i].IsAssignableFrom(args[i].GetType()))
                    {
                        typed[i] = args[i];
                    }
                    else
                    {
                        try
                        {
                            if (prototype[i] == typeof(Boolean))
                            {
                                typed[i] = toBoolean(args[i]);
                            }
                            else if (prototype[i] == typeof(Double))
                            {
                                typed[i] = toNumeric(args[i]);
                            }
                            else if (prototype[i] == typeof(String))
                            {
                                typed[i] = ToString(args[i]);
                            }
                            else if (prototype[i] == typeof(DateTime))
                            {
                                typed[i] = toDate(args[i]);
                            }
                        }
                        catch (XPathTypeMismatchException xptme) { /* swallow type mismatch exception */ }
                    }

                    if (typed[i] == null)
                        return null;
                }
            }

            return typed;
        }

        /******** HANDLERS FOR BUILT-IN FUNCTIONS ********
         * 
         * the functions below are the handlers for the built-in xpath function suite
         * 
         * if you add a function to the suite, it should adhere to the following pattern:
         * 
         *   * the function takes in its arguments as objects (DO NOT cast the arguments when calling
         *     the handler up in eval() (i.e., return stringLength((String)argVals[0])  <--- NO!)
         *     
         *   * the function converts the generic argument(s) to the desired type using the built-in
         *     xpath type conversion functions (toBoolean(), toNumeric(), toString(), toDate())
         *     
         *   * the function MUST return an object of type Boolean, Double, String, or Date; it may
         *     never return null (instead return the empty string or NaN)
         *   
         *   * the function may throw exceptions, but should try as hard as possible not to, and if
         *     it must, strive to make it an XPathException
         * 
         */

        public static Boolean isNull(Object o)
        {
            if (o == null)
            {
                return true; //true 'null' values aren't allowed in the xpath engine, but whatever
            }
            else if (o is String && ((String)o).Length == 0)
            {
                return true;
            }
            else if (o is Double && Double.IsNaN((Double)o))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static Double stringLength(Object o)
        {
            String s = ToString(o);
            if (s == null)
            {
                return 0.0D;
            }
            return s.Length;
        }

        /**
         * convert a value to a Boolean using xpath's type conversion rules
         *
         * @param o
         * @return
         */
        public static Boolean toBoolean(Object o)
        {
            Boolean val = false;

            o = unpack(o);

            if (o is Boolean)
            {
                val = (Boolean)o;
            }
            else if (o is Double)
            {
                double d = ((Double)o);
                val = (Boolean)(Math.Abs(d) > 1.0e-12 && !Double.IsNaN(d));
            }
            else if (o is String)
            {
                String s = (String)o;
                val = (Boolean)(s.Length > 0);
            }
            else if (o is DateTime)
            {
                val = true;
            }
            else if (o is IExprDataType)
            {
                val = ((IExprDataType)o).toBoolean();
            }

            if (val != null)
            {
                return val;
            }
            else
            {
                throw new XPathTypeMismatchException("converting to Boolean");
            }
        }

        /**
         * convert a value to a number using xpath's type conversion rules (note that xpath itself makes
         * no distinction between integer and floating point numbers)
         * 
         * @param o
         * @return
         */
        public static Double toNumeric(Object o)
        {
            Double val = 0.0;

            o = unpack(o);

            if (o is Boolean)
            {
                val = (Double)(((Boolean)o) ? 1 : 0);
            }
            else if (o is Double)
            {
                val = (Double)o;
            }
            else if (o is String)
            {
                /* annoying, but the xpath spec doesn't recognize scientific notation, or +/-Infinity
                 * when converting a string to a number
                 */

                String s = (String)o;
                double d;
                try
                {
                    s = s.Trim();
                    for (int i = 0; i < s.Length; i++)
                    {
                        char c = s[i];
                        if (c != '-' && c != '.' && (c < '0' || c > '9'))
                            throw new FormatException();
                    }

                    d = Double.Parse(s);
                    val = (Double)(d);
                }
                catch (FormatException nfe)
                {
                    val = (Double)(Double.NaN);
                }
            }
            else if (o is DateTime)
            {
                DateTime dt = (DateTime)o;
                val = (Double)(DateUtils.daysSinceEpoch(ref dt));
            }
            else if (o is IExprDataType)
            {
                val = ((IExprDataType)o).toNumeric();
            }

            if (val != null)
            {
                return val;
            }
            else
            {
                throw new XPathTypeMismatchException("converting to numeric");
            }
        }

        /**
         * convert a number to an integer by truncating the fractional part. if non-numeric, coerce the
         * value to a number first. note that the resulting return value is still a Double, as required
         * by the xpath engine
         * 
         * @param o
         * @return
         */
        public static Double toInt(Object o)
        {
            Double val = toNumeric(o);

            if (Double.IsInfinity(val) || Double.IsNaN(val))
            {
                return val;
            }
            else if (val >= long.MaxValue || val <= long.MinValue)
            {
                return val;
            }
            else
            {
                long l = (long)val;
                Double dbl = (Double)(l);
                if (l == 0 && (val < 0.0 || val.Equals((Double)(-0.0))))
                {
                    dbl = (Double)(-0.0);
                }
                return dbl;
            }
        }

        /**
         * convert a value to a string using xpath's type conversion rules
         * 
         * @param o
         * @return
         */
        public static String ToString(Object o)
        {
            String val = null;

            o = unpack(o);

            if (o is Boolean)
            {
                val = (((Boolean)o) ? "true" : "false");
            }
            else if (o is Double)
            {
                double d = ((Double)o);
                if (Double.IsNaN(d))
                {
                    val = "NaN";
                }
                else if (Math.Abs(d) < 1.0e-12)
                {
                    val = "0";
                }
                else if (Double.IsInfinity(d))
                {
                    val = (d < 0 ? "-" : "") + "Infinity";
                }
                else if (Math.Abs(d - (int)d) < 1.0e-12)
                {
                    val = ((int)d).ToString();
                }
                else
                {
                    val = d.ToString();
                }
            }
            else if (o is String)
            {
                val = (String)o;
            }
            else if (o is DateTime)
            {

                DateTime dt = (DateTime)o;
                val = DateUtils.formatDate(ref dt, DateUtils.FORMAT_ISO8601);
            }
            else if (o is IExprDataType)
            {
                val = ((IExprDataType)o).ToString();
            }

            if (val != null)
            {
                return val;
            }
            else
            {
                throw new XPathTypeMismatchException("converting to string");
            }
        }

        /**
         * convert a value to a date. note that xpath has no intrinsic representation of dates, so this
         * is off-spec. dates convert to strings as 'yyyy-mm-dd', convert to numbers as # of days since
         * the unix epoch, and convert to Booleans always as 'true'
         * 
         * string and int conversions are reversable, however:
         *   * cannot convert bool to date
         *   * empty string and NaN (xpath's 'null values') go unchanged, instead of being converted
         *     into a date (which would cause an error, since Date has no null value (other than java
         *     null, which the xpath engine can't handle))
         *   * note, however, than non-empty strings that aren't valid dates _will_ cause an error
         *     during conversion
         * 
         * @param o
         * @return
         */
        public static Object toDate(Object o)
        {
            o = unpack(o);

            if (o is Double)
            {
                Double n = toInt(o);

                if (Double.IsNaN(n))
                {
                    return n;
                }

                if (Double.IsInfinity(n) || n > int.MaxValue || n < int.MinValue)
                {
                    throw new XPathTypeMismatchException("converting out-of-range value to date");
                }

                DateTime dt = DateUtils.getDate(1970, 1, 1);

                return DateUtils.dateAdd(ref dt, (int)n);
            }
            else if (o is String)
            {
                String s = (String)o;

                if (s.Length == 0)
                {
                    return s;
                }

                DateTime d = DateUtils.parseDateTime(s);
                if (d == null)
                {
                    throw new XPathTypeMismatchException("converting to date");
                }
                else
                {
                    return d;
                }
            }
            else if (o is DateTime)
            {
                DateTime dt = (DateTime)o;
                return DateUtils.roundDate(ref dt);
            }
            else
            {
                throw new XPathTypeMismatchException("converting to date");
            }
        }

        public static Boolean boolNot(Object o)
        {
            Boolean b = toBoolean(o);
            return !b;
        }

        public static Boolean boolStr(Object o)
        {
            String s = ToString(o);
            if (s.Equals("true", StringComparison.CurrentCultureIgnoreCase) || s.Equals("1"))
                return true;
            else
                return false;
        }

        public static String dateStr(Object od, Object of)
        {
            od = toDate(od);
            if (od is DateTime)
            {
                DateTime dt = (DateTime)od;
                return DateUtils.format(ref dt, ToString(of));
            }
            else
            {
                return "";
            }
        }

        public static Object ifThenElse(Object o1, Object o2, Object o3)
        {
            Boolean b = toBoolean(o1);
            return (b ? o2 : o3);
        }

        /**
         * return whether a particular choice of a multi-select is selected
         * 
         * @param o1 XML-serialized answer to multi-select question (i.e, space-delimited choice values)
         * @param o2 choice to look for
         * @return
         */
        public static Boolean multiSelected(Object o1, Object o2)
        {
            String s1 = (String)unpack(o1);
            String s2 = ((String)unpack(o2)).Trim();

            return (" " + s1 + " ").IndexOf(" " + s2 + " ") != -1;
        }

        /**
         * return the number of choices in a multi-select answer
         * 
         * @param o XML-serialized answer to multi-select question (i.e, space-delimited choice values)
         * @return
         */
        public static Double countSelected(Object o)
        {
            String s = (String)unpack(o);

            return DateUtils.split(s, " ", true).Count;
        }

        /**
         * count the number of nodes in a nodeset
         * 
         * @param o
         * @return
         */
        public static Double count(Object o)
        {
            if (o is XPathNodeset)
            {
                return ((XPathNodeset)o).size();
            }
            else
            {
                throw new XPathTypeMismatchException("not a nodeset");
            }
        }

        /**
         * sum the values in a nodeset; each element is coerced to a numeric value
         * 
         * @param model
         * @param o
         * @return
         */
        public static Double sum(Object[] argVals)
        {
            double sum = 0.0;
            for (int i = 0; i < argVals.Length; i++)
            {
                sum += toNumeric(argVals[i]);
            }
            return sum;
        }

        /**
         * concatenate an abritrary-length argument list of string values together
         * 
         * @param argVals
         * @return
         */
        public static String join(Object oSep, Object[] argVals)
        {
            String sep = ToString(oSep);
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < argVals.Length; i++)
            {
                sb.Append(ToString(argVals[i]));
                if (i < argVals.Length - 1)
                    sb.Append(sep);
            }

            return sb.ToString();
        }

        public static String Substring(Object o1, Object o2, Object o3) {
		String s = ToString(o1);
		int start =(int) toInt(o2);
		
		int len = s.Length;

		int end = (o3 != null ? (int)toInt(o3) : len);		
		if (start < 0) {
			start = len + start;
		}
		if (end < 0) {
			end = len + end;
		}
		start = Math.Min(Math.Max(0, start), end);
		end = Math.Min(Math.Max(0, end), end);
		
		return (start <= end ? s.Substring(start, end) : "");
	}

        /**
         * perform a 'checklist' computation, enabling expressions like 'if there are at least 3 risk
         * factors active'
         * 
         * @param argVals
         *   the first argument is a numeric value expressing the minimum number of factors required.
         *     if -1, no minimum is applicable
         *   the second argument is a numeric value expressing the maximum number of allowed factors.
         *     if -1, no maximum is applicalbe
         *   arguments 3 through the end are the individual factors, each coerced to a Boolean value
         * @return true if the count of 'true' factors is between the applicable minimum and maximum,
         *   inclusive
         */
        public static Boolean checklist(Object oMin, Object oMax, Object[] factors)
        {
            int min = (int)toNumeric(oMin);
            int max = (int)toNumeric(oMax);

            int count = 0;
            for (int i = 0; i < factors.Length; i++)
            {
                if (toBoolean(factors[i]))
                    count++;
            }

            return (min < 0 || count >= min) && (max < 0 || count <= max);
        }

        /**
         * very similar to checklist, only each factor is assigned a real-number 'weight'.
         * 
         * the first and second args are again the minimum and maximum, but -1 no longer means
         * 'not applicable'.
         * 
         * subsequent arguments come in pairs: first the Boolean value, then the floating-point
         * weight for that value
         * 
         * the weights of all the 'true' factors are summed, and the function returns whether
         * this sum is between the min and max
         * 
         * @param argVals
         * @return
         */
        public static Boolean checklistWeighted(Object oMin, Object oMax, Object[] flags, Object[] weights) {
		double min = toNumeric(oMin);
		double max = toNumeric(oMax);
		
		double sum = 0.0;
		for (int i = 0; i < flags.Length; i++) {
			Boolean flag = toBoolean(flags[i]);
			double weight = toNumeric(weights[i]);
			
			if (flag)
				sum += weight;
		}
		
		return sum >= min && sum <= max;
	}

        /**
         * determine if a string matches a regular expression. 
         * 
         * @param o1 string being matched
         * @param o2 regular expression
         * @return
         */
        public static Boolean regex(Object o1, Object o2)
        {
            String str = ToString(o1);
            String re = ToString(o2);

            Regex regexp = new Regex(re);

            Boolean result = (regexp.Match(str)!=null? true : false);
           
            return result;
        }

        private static Object[] subsetArgList(Object[] args, int start)
        {
            return subsetArgList(args, start, 1);
        }

        /**
         * return a subset of an argument list as a new arguments list
         * 
         * @param args
         * @param start index to start at
         * @param skip sub-list will contain every nth argument, where n == skip (default: 1)
         * @return
         */
        private static Object[] subsetArgList(Object[] args, int start, int skip)
        {
            if (start > args.Length || skip < 1)
            {
                throw new SystemException("error in subsetting arglist");
            }

            Object[] subargs = new Object[(int)MathUtils.divLongNotSuck(args.Length - start - 1, skip) + 1];
            for (int i = start, j = 0; i < args.Length; i += skip, j++)
            {
                subargs[j] = args[i];
            }

            return subargs;
        }

        public static Object unpack(Object o)
        {
            if (o is XPathNodeset)
            {
                return ((XPathNodeset)o).unpack();
            }
            else
            {
                return o;
            }
        }

        /**
         * 
         */
        public Object pivot(FormInstance model, EvaluationContext evalContext, List<Object> pivots, Object sentinal)
        {
            String name = this.id.ToString();

            //for now we'll assume that all that functions do is return the composition of their components
            Object[] argVals = new Object[args.Length];


            //Identify whether this function is an identity: IE: can reflect back the pivot sentinal with no modification
            String[] identities = new String[] { "string-length" };
            Boolean id = false;
            foreach (String identity in identities)
            {
                if (identity.Equals(name))
                {
                    id = true;
                }
            }

            //get each argument's pivot
            for (int i = 0; i < args.Length; i++)
            {
                argVals[i] = args[i].pivot(model, evalContext, pivots, sentinal);
            }

            Boolean pivoted = false;
            //evaluate the pivots
            for (int i = 0; i < argVals.Length; ++i)
            {
                if (argVals[i] == null)
                {
                    //one of our arguments contained pivots,  
                    pivoted = true;
                }
                else if (sentinal.Equals(argVals[i]))
                {
                    //one of our arguments is the sentinal, return the sentinal if possible
                    if (id)
                    {
                        return sentinal;
                    }
                    else
                    {
                        //This function modifies the sentinal in a way that makes it impossible to capture
                        //the pivot.
                        throw new UnpivotableExpressionException();
                    }
                }
            }

            if (pivoted)
            {
                if (id)
                {
                    return null;
                }
                else
                {
                    //This function modifies the sentinal in a way that makes it impossible to capture
                    //the pivot.
                    throw new UnpivotableExpressionException();
                }
            }

            //TODO: Inner eval here with eval'd args to improve speed
            return eval(model, evalContext);

        }

    }
}