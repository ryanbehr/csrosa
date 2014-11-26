/**
 * 
 */


/**
 * A state represents a particular state of the application. Each state has an
 * associated view and controller and a set of transitions to new states.
 * 
 * @see TrivialTransitions
 * @author Acellam Guy ,  ctsims
 * 
 */
namespace org.javarosa.core.api
{
    public interface State
    {

         void start();

    }
}