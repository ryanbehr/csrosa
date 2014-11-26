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
using System;
using IDataPointer = org.javarosa.core.data.IDataPointer;
using DeserializationException = org.javarosa.core.util.externalizable.DeserializationException;
using ExtUtil = org.javarosa.core.util.externalizable.ExtUtil;
using ExtWrapTagged = org.javarosa.core.util.externalizable.ExtWrapTagged;
using PrototypeFactory = org.javarosa.core.util.externalizable.PrototypeFactory;
using System.Collections.Generic;
namespace org.javarosa.core.services.transport.payload
{

    /**
     * A payload for a Pointer to some data.
     *  
     * @author Clayton Sims
     * @date Dec 29, 2008 
     *
     */
    public class DataPointerPayload : IDataPayload
    {
        IDataPointer pointer;

        /**
         * Note: Only useful for serialization.
         */
        public DataPointerPayload()
        {
        }

        public DataPointerPayload(IDataPointer pointer)
        {
            this.pointer = pointer;
        }

        /*
         * (non-Javadoc)
         * @see org.javarosa.core.services.transport.IDataPayload#accept(org.javarosa.core.services.transport.IDataPayloadVisitor)
         */
        public Object accept(IDataPayloadVisitor<Object> visitor)
        {
            return visitor.visit(this);
        }

        virtual public long Length
        {
            get
            {
                return pointer.Length;
            }

        }
        /*
         * (non-Javadoc)
         * @see org.javarosa.core.services.transport.IDataPayload#getPayloadId()
         */
        virtual public String PayloadId
        {
            get { return pointer.DisplayText; }
        }

        /*
         * (non-Javadoc)
         * @see org.javarosa.core.services.transport.IDataPayload#getPayloadStream()
         */
        virtual public System.IO.Stream PayloadStream
        {
            /*
            * (non-Javadoc)
            * @see org.javarosa.core.services.transport.IDataPayload#getPayloadStream()
            */

            get
            {
                return pointer.DataStream;
            }

        }

        /*
         * (non-Javadoc)
         * @see org.javarosa.core.services.transport.IDataPayload#getPayloadType()
         */
        virtual public int PayloadType
        {
            //TODO: FIX so this isn't always the case
            get { return IDataPayload_Fields.PAYLOAD_TYPE_JPG; }
        }

        /*
            * (non-Javadoc)
            * @see org.javarosa.core.util.externalizable.Externalizable#readExternal(java.io.DataInputStream, org.javarosa.core.util.externalizable.PrototypeFactory)
            */
        //UPGRADE_TODO: Class 'java.io.DataInputStream' was converted to 'System.IO.BinaryReader' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataInputStream'"
        public virtual void readExternal(System.IO.BinaryReader in_Renamed, PrototypeFactory pf)
        {
            pointer = (IDataPointer)ExtUtil.read(in_Renamed, new ExtWrapTagged());
        }
        /*
            * (non-Javadoc)
            * @see org.javarosa.core.util.externalizable.Externalizable#writeExternal(java.io.DataOutputStream)
            */
        //UPGRADE_TODO: Class 'java.io.DataOutputStream' was converted to 'System.IO.BinaryWriter' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataOutputStream'"
        public virtual void writeExternal(System.IO.BinaryWriter out_Renamed)
        {
            ExtUtil.write(out_Renamed, new ExtWrapTagged(pointer));
        }

        virtual public int TransportId
        {
            get
            {
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