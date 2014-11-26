/**
 * 
 */
using org.javarosa.core.model.instance;
namespace org.javarosa.core.model.condition.pivot
{


    /**
     * @author Acellam Guy ,  ctsims
     *
     */
    public interface ConstraintHint
    {

         void init(EvaluationContext c, IConditionExpr conditional, FormInstance instance);
    }
}