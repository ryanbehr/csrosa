/**
 * 
 */
using System;
namespace org.javarosa.core.util
{

    /**
     * Thrown when an index used contains an invalid value
     * 
     * @author ctsims
     *
     */
    public class InvalidIndexException : SystemException
    {
        String index;
        public InvalidIndexException(String message, String index):base(message)
        {
            this.index = index;
        }

        public String getIndex()
        {
            return index;
        }
    }
}
