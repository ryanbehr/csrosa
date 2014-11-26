using org.javarosa.core.model.data;
/**
 * 
 */
using System;
namespace org.javarosa.core.model.condition.pivot
{

    /**
     * @author ctsims
     *
     */
    public class DecimalRangeHint : RangeHint<DecimalData,IAnswerData>
    {

        public DecimalData castToValue(double value)
        {
            return new DecimalData(value);
        }

        public override double unit()
        {
            return Double.MinValue;
        }

    }
}