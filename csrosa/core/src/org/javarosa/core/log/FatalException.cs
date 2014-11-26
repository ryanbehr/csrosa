using System;
namespace org.javarosa.core.log
{

    [Serializable]
    public class FatalException : WrappedException
    {

        public FatalException()
            : this("")
        {
        }

        public FatalException(String message)
            : base(message)
        {
        }

        public FatalException(Exception child)
            : base(child)
        {
        }

        public FatalException(String message, Exception child)
            : base(message, child)
        {
        }
    }
}