using System;
using InvalidIndexException = org.javarosa.core.util.InvalidIndexException;
using DeserializationException = org.javarosa.core.util.externalizable.DeserializationException;
using Externalizable = org.javarosa.core.util.externalizable.Externalizable;
using PrototypeFactory = org.javarosa.core.util.externalizable.PrototypeFactory;
using System.Collections.Generic;
using System.Collections;
namespace org.javarosa.core.services.storage
{

    /**
     * A wrapper implementation of IStorageUtility that lets you serialize an object with a serialization
     * scheme other than the default scheme provided by the object's readExternal/writeExternal methods.
     * 
     * For example, FormInstance contains lots of redundant information about the structure of the instance
     * which doesn't change among saved instances. The extra space used for this redundant info can seriously
     * limit the number of saved forms we can store on a device. We can use this utility to serialize
     * FormInstances in a different way that excludes this redundant info (meaning we have to take the more
     * complicated step of restoring it from elsewhere during deserialization), with the benefit of much
     * smaller record sizes.
     * 
     * The alternate scheme is provided via a wrapper object, which accepts the base object and whose
     * readExternal/writeExternal methods implement the new scheme.
     * 
     * All methods pass through to an underlying StorageUtility; you may get warnings about type mismatches
     * 
     * @author Drew Roos
     *
     */
    public class WrappingStorageUtility : IStorageUtilityIndexed
    {
        IStorageUtility storage;		/* underlying StorageUtility */
        SerializationWrapper wrapper;   /* wrapper that defines the alternate serialization scheme; the wrapper is set once for
	                                 * the life of the StorageUtility and is re-used all read and write calls
	                                 */
        //UPGRADE_NOTE: Field 'EnclosingInstance' was added to class 'AnonymousClassIStorageIterator' to access its enclosing instance. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1019'"
        private class AnonymousClassIStorageIterator : IStorageIterator
        {
            public AnonymousClassIStorageIterator(WrappingStorageUtility enclosingInstance)
            {
                InitBlock(enclosingInstance);
            }
            private void InitBlock(WrappingStorageUtility enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
                baseIterator = Enclosing_Instance.storage.iterate();
            }
            private WrappingStorageUtility enclosingInstance;
            public WrappingStorageUtility Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }

            }
            //UPGRADE_NOTE: The initialization of  'baseIterator' was moved to method 'InitBlock'. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1005'"
            internal IStorageIterator baseIterator;
            public virtual bool hasMore()
            {
                return baseIterator.hasMore();
            }

            public virtual int nextID()
            {
                return baseIterator.nextID();
            }

            public virtual Externalizable nextRecord()
            {
                return ((WrappingStorageUtility.SerializationWrapper)baseIterator.nextRecord()).Data;
            }

            public virtual int numRecords()
            {
                return baseIterator.numRecords();
            }

            public virtual int peekID()
            {
                return baseIterator.peekID();
            }
        }
        /**
         * Defines an alternate serialization scheme. The alternate scheme is implemented in this class's
         * readExternal and writeExternal methods.
         * 
         * (kind of like ExternalizableWrapper -- but not quite a drop-in replacement)
         */
        public interface SerializationWrapper : Externalizable
        {
            /**
             * set the underlying object (to be followed by a call to writeExternal)
             * @param e
             */
            Externalizable Data
            {
                get;

                set;

            }

            /**
             * return type of underlying object
             * @return
             */
            Type baseType();
        }

        /**
         * Create a new wrapping StorageUtility

         * @param name unique name for underlying StorageUtility
         * @param wrapper serialization wrapper
         * @param storageFactory factory to create underlying StorageUtility
         */
        public WrappingStorageUtility(String name, SerializationWrapper wrapper, IStorageFactory storageFactory)
        {
            this.storage = storageFactory.newStorage(name, wrapper.GetType());
            this.wrapper = wrapper;
        }

        public Externalizable read(int id)
        {
            return ((SerializationWrapper)storage.read(id)).Data;
        }

        public virtual void write(Persistable p)
        {
            lock (wrapper)
            {
                wrapper.Data = p;
                if (wrapper is IMetaData)
                {
                    storage.write(new FauxIndexedPersistable(p, wrapper, (IMetaData)wrapper));
                }
                else
                {
                    storage.write(new FauxIndexedPersistable(p, wrapper));
                }
            }
        }


        public virtual int add(Externalizable e)
        {
            lock (wrapper)
            {
                wrapper.Data = e;
                int result = storage.add(wrapper);
                return result;
            }
        }

        public virtual void update(int id, Externalizable e)
        {
            lock (wrapper)
            {
                wrapper.Data = e;
                storage.update(id, wrapper);
            }
        }

        public virtual IStorageIterator iterate()
        {
            return new AnonymousClassIStorageIterator(this);
        }

        /* pass-through methods */

        public byte[] readBytes(int id)
        {
            return storage.readBytes(id);
        }

        public void remove(int id)
        {
            storage.remove(id);
        }

        public void remove(Persistable p)
        {
            storage.remove(p);
        }

        public void removeAll()
        {
            storage.removeAll();
        }

        public List<int> removeAll(EntityFilter ef)
        {
            return storage.removeAll(ef);
        }

        public Boolean exists(int id)
        {
            return storage.exists(id);
        }

        public Boolean isEmpty()
        {
            return storage.isEmpty();
        }

        public int getNumRecords()
        {
            return storage.getNumRecords();
        }

        public int getRecordSize(int id)
        {
            return storage.getRecordSize(id);
        }

        public int getTotalSize()
        {
            return storage.getTotalSize();
        }

        public void close()
        {
            storage.close();
        }

        public void destroy()
        {
            storage.destroy();
        }

        public void repack()
        {
            storage.repack();
        }

        public void repair()
        {
            storage.repair();
        }

        public Object getAccessLock()
        {
            return storage.getAccessLock();
        }

        public virtual System.Collections.ArrayList getIDsForValue(System.String fieldName, System.Object value_Renamed)
        {
            return indexedStorage().getIDsForValue(fieldName, value_Renamed);
        }

        public virtual Externalizable getRecordForValue(System.String fieldName, System.Object value_Renamed)
        {
            return ((WrappingStorageUtility.SerializationWrapper)indexedStorage().getRecordForValue(fieldName, value_Renamed)).Data;
        }

        private IStorageUtilityIndexed indexedStorage()
        {
            if (!(storage is IStorageUtilityIndexed))
            {
                throw new System.SystemException("WrappingStorageUtility's factory is not of an indexed type, but had indexed operations requested. Please implement storage " + storage.GetType().FullName + " as indexed storage");
            }
            return (IStorageUtilityIndexed)storage;
        }

    }
}
