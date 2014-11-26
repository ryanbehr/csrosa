/*
 * Copyright (C) 2009 JavaRosa ,Copyright (C) 2014 Simbacode
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a copy of
 * the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 */

using org.javarosa.core.model.utils;
/**
 * 
 */
using org.javarosa.core.services.transport.payload;
using System;
using csrosa.core.src.org.javarosa.core.model.instance.utils;
using System.IO;
using org.javarosa.core.util.externalizable;
using org.javarosa.core.services.storage;
using System.Collections.Generic;

namespace org.javarosa.core.model.instance.utils
{



    /**
     * The ModelReferencePayload essentially provides a wrapper functionality
     * around a ModelTree to allow it to be used as a payload, but only to
     * actually perform the various computationally expensive functions
     * of serialization when required.
     * 
     * @author Clayton Sims
     * @date Apr 27, 2009 
     *
     */
    public class ModelReferencePayload : IDataPayload
    {

        int recordId;
        IDataPayload payload;
        String destination = null;

        IInstanceSerializingVisitor serializer;

        //NOTE: Should only be used for serializaiton.
        public ModelReferencePayload()
        {

        }

        public ModelReferencePayload(int modelRecordId)
        {
            this.recordId = modelRecordId;
        }

        /**
         * @param serializer the serializer to set
         */
        public void setSerializer(IInstanceSerializingVisitor serializer)
        {
            this.serializer = serializer;
        }

        public System.Collections.Generic.List<object> accept<T>(IDataPayloadVisitor<T> visitor)
        {
            memoize();
            return payload.accept(visitor);
        }

        /* (non-Javadoc)
         * @see org.javarosa.core.services.transport.IDataPayload#getLength()
         */
        public long Length
        {
            get
            {
                memoize();
                return payload.Length;
            }
        }

        /* (non-Javadoc)
         * @see org.javarosa.core.services.transport.IDataPayload#getPayloadId()
         */
        public String PayloadId
        {
            get
            {
                memoize();
                return payload.PayloadId;
            }
        }

        /* (non-Javadoc)
         * @see org.javarosa.core.services.transport.IDataPayload#getPayloadStream()
         */
        public Stream PayloadStream
        {
            get
            {
                memoize();
                return payload.PayloadStream;
            }
        }

        /* (non-Javadoc)
         * @see org.javarosa.core.services.transport.IDataPayload#getPayloadType()
         */
        public int PayloadType
        {
            get
            {
                memoize();
                return payload.PayloadType;
            }
        }

        /* (non-Javadoc)
         * @see org.javarosa.core.util.externalizable.Externalizable#readExternal(java.io.DataInputStream, org.javarosa.core.util.externalizable.PrototypeFactory)
         */
        public void readExternal(BinaryReader in_, PrototypeFactory pf)
        {
            recordId = in_.Read();
        }

        /* (non-Javadoc)
         * @see org.javarosa.core.util.externalizable.Externalizable#writeExternal(java.io.DataOutputStream)
         */
        public void writeExternal(BinaryWriter out_)
        {
            out_.Write(recordId);
        }

        private void memoize()
        {
            if (payload == null)
            {
                IStorageUtility instances = StorageManager.getStorage(FormInstance.STORAGE_KEY);
                try
                {
                    FormInstance tree = (FormInstance)instances.read(recordId);
                    payload = serializer.createSerializedPayload(tree);
                }
                catch (IOException e)
                {
                    //Assertion, do not catch!
                    Console.WriteLine(e.StackTrace);
                    throw new SystemException("ModelReferencePayload failed to retrieve its model from rms [" + e.Message + "]");
                }
            }
        }

        public int TransportId
        {
            get
            {
                return recordId;
            }
        }

        public void setDestination(String destination)
        {
            this.destination = destination;
        }

        public String getDestination()
        {
            return destination;
        }


    }
}