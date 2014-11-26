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
    public class IntegerRangeHint : RangeHint<IntegerData,IAnswerData>
    {

        protected IntegerData castToValue(double value)
        {
            return new IntegerData((int)Math.Floor(value));
        }

        public override double unit()
        {
            return 1;
        }

    }
}