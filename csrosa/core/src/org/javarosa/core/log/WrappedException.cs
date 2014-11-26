using System;
namespace org.javarosa.core.log
{

    public class WrappedException : SystemException
    {

        String message;
        Exception child;

        public WrappedException(String message)
            : this(message, null)
        {
        }

        public WrappedException(Exception child)
            : this(null, child)
        {

        }

        public WrappedException(String message, Exception child)
            : base(constructMessage(message, child))
        {

            this.message = message;
            this.child = child;
        }

        public static String constructMessage(String message, Exception child)
        {
            String str = "";
            if (message != null)
            {
                str += message;
            }
            if (child != null)
            {
                str += (message != null ? " => " : "") + printException(child);
            }

            if (str.Equals(""))
                str = "[exception]";
            return str;
        }

        public static String printException(Exception e)
        {
            if (e is WrappedException)
            {
                return (e is FatalException ? "FATAL: " : "") + e.Message;
            }
            else
            {
                return e.GetType().FullName + "[" + e.Message + "]";
            }
        }

    }
}
