using org.javarosa.core.model.data;
using org.javarosa.core.model.instance;
using org.javarosa.xpath.expr;
using System;
using System.Collections.Generic;
namespace org.javarosa.core.model.condition.pivot
{

    /**
     * @author ctsims
     *
     */
    public abstract class RangeHint<T, IAD> : ConstraintHint where IAD : IAnswerData
    {

        Double min;
        Double max;
        Boolean minInclusive;
        Boolean maxInclusive;

        public void init(EvaluationContext c, IConditionExpr conditional, FormInstance instance)
        {

            List<Object> pivots = conditional.pivot(instance, c);

            List<CmpPivot> internalPivots = new List<CmpPivot>();
            foreach (Object p in pivots)
            {
                if (!(p is CmpPivot))
                {
                    throw new UnpivotableExpressionException();
                }
                internalPivots.Add((CmpPivot)p);
            }

            if (internalPivots.Count > 2)
            {
                //For now.
                throw new UnpivotableExpressionException();
            }

            foreach (CmpPivot pivot in internalPivots)
            {
                evaluatePivot(pivot, conditional, c, instance);
            }
        }

        public T getMin()
        {
            return castToValue(min);
        }

        public Boolean isMinInclusive()
        {
            return minInclusive;
        }

        public T getMax()
        {
            return castToValue(max);
        }

        public Boolean isMaxInclusive()
        {
            return maxInclusive;
        }

        private void evaluatePivot(CmpPivot pivot, IConditionExpr conditional, EvaluationContext c, FormInstance instance)
        {
            double unit = this.unit();
            double val = pivot.Val;
            double lt = val - unit;
            double gt = val + unit;

            c.isConstraint = true;


            c.candidateValue = (IAnswerData)(Object)castToValue(val);
            Boolean eq = XPathFuncExpr.toBoolean(conditional.eval(instance, c));

            c.candidateValue = (IAnswerData)(Object)castToValue(lt);
            Boolean ltr = XPathFuncExpr.toBoolean(conditional.eval(instance, c));

            c.candidateValue = (IAnswerData)(Object)castToValue(gt);
            Boolean gtr = XPathFuncExpr.toBoolean(conditional.eval(instance, c));

            if (ltr && !gtr)
            {
                max = (Double)(val);
                maxInclusive = eq;
            }

            if (!ltr && gtr)
            {
                min = (Double)(val);
                minInclusive = eq;
            }
        }

        public  T castToValue(double value)
        {

            return (T)(Object)value;

        }

         public abstract double unit();
    }

}