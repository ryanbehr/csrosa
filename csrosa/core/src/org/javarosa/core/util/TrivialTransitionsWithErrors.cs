using System;
namespace org.javarosa.core.util
{

    /**
     * Convenience interface for states that do not have any transitions. 
     */
    public interface TrivialTransitionsWithErrors
    {

        void done(Boolean errors);

    }
}