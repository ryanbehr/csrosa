/**
 * 
 */
using org.javarosa.core.util;
namespace org.javarosa.core.log
{
    /**
     * @author Acellam Guy ,  ctsims
     *
     */
    public abstract class StreamLogSerializer
    {

        SortedIntSet logIDs;
        Purger purger = null;

        public interface Purger
        {
            void purge(SortedIntSet IDs);
        }

        public StreamLogSerializer()
        {
            logIDs = new SortedIntSet();
        }

        public void serializeLog(int id, LogEntry entry)
        {
            logIDs.add(id);
            serializeLog(entry);
        }

        protected abstract void serializeLog(LogEntry entry);

        public void setPurger(Purger purger)
        {
            this.purger = purger;
        }

        public void purge()
        {
            this.purger.purge(logIDs);
        }
    }
}