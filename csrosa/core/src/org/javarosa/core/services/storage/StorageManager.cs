
using System;
using System.Collections;
using System.Collections.Generic;

namespace org.javarosa.core.services.storage
{

    /**
     * Manages StorageProviders for JavaRosa, which maintain persistent
     * data on a device.
     * 
     * Largely derived from Cell Life's RMSManager
     * 
     * @author Acellam Guy ,  Clayton Sims
     *
     */
    public class StorageManager
    {
        private static IDictionary<String, IStorageUtility> storageRegistry = new Dictionary<String, IStorageUtility>();
        private static IStorageFactory storageFactory;

        /**
         * Attempts to set the storage factory for the current environment. Will fail silently
         * if a storage factory has already been set. Should be used by default environment.
         * 
         * @param fact An available storage factory.
         */
        public static void setStorageFactory(IStorageFactory fact)
        {
            StorageManager.setStorageFactory(fact, false);
        }

        /**
         * Attempts to set the storage factory for the current environment and fails and dies if there
         * is already a storage factory set if specified. Should be used by actual applications who need to use
         * a specific storage factory and shouldn't tolerate being pre-empted. 
         * 
         * @param fact An available storage factory.
         * @param mustWork true if it is intolerable for another storage factory to have been set. False otherwise
         */
        public static void setStorageFactory(IStorageFactory fact, Boolean mustWork)
        {
            if (storageFactory == null)
            {
                storageFactory = fact;
            }
            else
            {
                if (mustWork)
                {
                    Logger.die("A Storage Factory had already been set when storage factory " + fact.GetType().FullName + " attempted to become the only storage factory", new System.SystemException("Duplicate Storage Factory set"));
                }
                else
                {
                    //Not an issue
                }
            }
        }

        public static void registerStorage(System.String key, System.Type type)
        {
            registerStorage(key, key, type);
        }

        public static void registerStorage(System.String storageKey, System.String storageName, System.Type type)
        {
            if (storageFactory == null)
            {
                throw new System.SystemException("No storage factory has been set; I don't know what kind of storage utility to create. Either set a storage factory, or register your StorageUtilitys directly.");
            }

            registerStorage(storageKey, storageFactory.newStorage(storageName, type));
        }

        /// <summary> It is strongly, strongly advised that you do not register storage in this way.
        /// 
        /// </summary>
        /// <param name="key">
        /// </param>
        /// <param name="storage">
        /// </param>
        public static void registerStorage(System.String key, IStorageUtility storage)
        {
            storageRegistry.Add(key, storage);
        }

        public static void registerWrappedStorage(System.String key, System.String storeName, WrappingStorageUtility.SerializationWrapper wrapper)
        {
            StorageManager.registerStorage(key, new WrappingStorageUtility(storeName, wrapper, storageFactory));
        }

        public static IStorageUtility getStorage(System.String key)
        {
            if (storageRegistry.ContainsKey(key))
            {
                return (IStorageUtility)storageRegistry[key];
            }
            else
            {
                throw new System.SystemException("No storage utility has been registered to handle \"" + key + "\"; you must register one first with StorageManager.registerStorage()");
            }
        }

        public static void repairAll()
        {
            for (IEnumerator e = storageRegistry.GetEnumerator(); e.MoveNext(); )
            {
                ((IStorageUtility)e.Current).repair();
            }
        }

        public static String[] listRegisteredUtilities()
        {
            String[] returnVal = new String[storageRegistry.Count];
            int i = 0;
            for (IEnumerator e = storageRegistry.GetEnumerator(); e.MoveNext(); )
            {
                returnVal[i] = (String)e.Current;
                i++;
            }
            return returnVal;
        }

        public static void halt()
        {
            for (IEnumerator e = storageRegistry.GetEnumerator(); e.MoveNext(); )
            {
                ((IStorageUtility)e.Current).close();
            }
        }
    }
}