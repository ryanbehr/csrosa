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

using org.javarosa.core.util.externalizable;
/**
 * 
 */
using System;
using System.Collections.Generic;
namespace org.javarosa.core.services.transport.payload
{

    /**
     * A ByteArrayPayload is a simple payload consisting of a
     * byte array.
     * 
     * @author Acellam Guy ,  Clayton Sims
     * @date Dec 18, 2008 
     *
     */
    public class ByteArrayPayload : IDataPayload
    {
        byte[] payload;

        String id;

        int type;

        /**
         * Note: Only useful for serialization.
         */
        public ByteArrayPayload()
        {
        }

        /**
         * 
         * @param payload The byte array for this payload.
         * @param id An optional id identifying the payload
         * @param type The type of data for this byte array
         */
        public ByteArrayPayload(byte[] payload, String id, int type)
        {
            this.payload = payload;
            this.id = id;
            this.type = type;
        }

        /* (non-Javadoc)
         * @see org.javarosa.core.services.transport.IDataPayload#getPayloadStream()
         */
        virtual public System.IO.Stream PayloadStream
        {
            /* (non-Javadoc)
            * @see org.javarosa.core.services.transport.IDataPayload#getPayloadStream()
            */

            get
            {

                return new System.IO.MemoryStream((byte[])(Array)payload);
            }

        }

        /* (non-Javadoc)
          * @see org.javarosa.core.util.externalizable.Externalizable#readExternal(java.io.DataInputStream, org.javarosa.core.util.externalizable.PrototypeFactory)
          */
        //UPGRADE_TODO: Class 'java.io.DataInputStream' was converted to 'System.IO.BinaryReader' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataInputStream'"
        public virtual void readExternal(System.IO.BinaryReader in_Renamed, PrototypeFactory pf)
        {
            int length = in_Renamed.ReadInt32();
            if (length > 0)
            {
                this.payload = new byte[length];
                in_Renamed.Read(this.payload, 0, this.payload.Length);
            }
            id = ExtUtil.nullIfEmpty(ExtUtil.readString(in_Renamed));
        }


        /* (non-Javadoc)
                * @see org.javarosa.core.util.externalizable.Externalizable#writeExternal(java.io.DataOutputStream)
                */
        //UPGRADE_TODO: Class 'java.io.DataOutputStream' was converted to 'System.IO.BinaryWriter' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataOutputStream'"
        public virtual void writeExternal(System.IO.BinaryWriter out_Renamed)
        {
            out_Renamed.Write(payload.Length);
            if (payload.Length > 0)
            {
                out_Renamed.Write(payload);
            }
            ExtUtil.writeString(out_Renamed, ExtUtil.emptyIfNull(id));
        }

        /*
         * (non-Javadoc)
         * @see org.javarosa.core.services.transport.IDataPayload#accept(org.javarosa.core.services.transport.IDataPayloadVisitor)
         */
        public Object accept(IDataPayloadVisitor<Object> visitor)
        {
            return visitor.visit(this);
        }

        /*
         * (non-Javadoc)
         * @see org.javarosa.core.services.transport.IDataPayload#getPayloadId()
         */
        virtual public String PayloadId
        {
            get{
                return id;
            }
        }

        /*
         * (non-Javadoc)
         * @see org.javarosa.core.services.transport.IDataPayload#getPayloadType()
         */
        virtual public int PayloadType
        {
            get {
                return type;
            }
        }

        virtual public long Length
        {
            get
            {
               return payload.Length;
            }
        }

        virtual public int TransportId
        {
            get {
                //TODO: Most messages can include this data
                return -1;
            }
        }


        public System.Collections.Generic.List<object> accept<T>(IDataPayloadVisitor<T> visitor)
        {
            List<object> lst = new List<object>();
            lst.Add(visitor.visit(this));
            return lst;
        }
    }
}