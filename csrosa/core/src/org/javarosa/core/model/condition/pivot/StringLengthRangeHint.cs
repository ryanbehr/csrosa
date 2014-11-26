/**
 * 
 */
using org.javarosa.core.model.data;
using System;
namespace org.javarosa.core.model.condition.pivot
{

    /**
     * @author ctsims
     *
     */
    public class StringLengthRangeHint : RangeHint<StringData, IAnswerData>
    {

        protected StringData castToValue(double value)
        {
            String placeholder = "";
            for (int i = 0; i < ((int)value); ++i)
            {
                placeholder += "X";
            }
            return new StringData(placeholder);
        }

        public override double unit()
        {
            return 1;
        }

    }
}