
/**
 * 
 */
using org.javarosa.core.model.data;
using org.javarosa.xpath.expr;
using System;
namespace org.javarosa.core.model.condition.pivot
{


    /**
     * @author ctsims
     *
     */
    public class DateRangeHint : RangeHint<DateData,IAnswerData>
    {

        protected DateData castToValue(double value)
        {
            DateTime dt = (DateTime)XPathFuncExpr.toDate((Double)(Math.Floor(value)));
            return new DateData(ref dt);
        }

        public override double unit()
        {
            return 1;
        }

    }
}
