using org.javarosa.core.util.externalizable;
namespace org.javarosa.core.services.storage
{

    /**
     * A modest extension to Externalizable which identifies objects that have the concept of an internal 'record ID'
     */
    public interface Persistable : Externalizable
    {
        int ID { get;set;}
    }
}