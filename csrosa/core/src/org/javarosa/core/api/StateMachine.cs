using System;
/**
 * 
 */
using System.Collections;


namespace org.javarosa.core.api
{
    public class StateMachine
    {
        private static Stack statesToReturnTo = new Stack();

        public static void setStateToReturnTo(State st)
        {
            statesToReturnTo.Push(st);
        }

        public static State getStateToReturnTo()
        {
            try
            {
                return (State)statesToReturnTo.Pop();
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new SystemException("Tried to return to a saved state, but no state to return to had been set earlier in the workflow");
            }
        }
    }
}
