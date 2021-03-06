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

/// <summary> </summary>
using System;
using MultiInputStream = org.javarosa.core.util.MultiStream;
using DeserializationException = org.javarosa.core.util.externalizable.DeserializationException;
using ExtUtil = org.javarosa.core.util.externalizable.ExtUtil;
using ExtWrapListPoly = org.javarosa.core.util.externalizable.ExtWrapListPoly;
using PrototypeFactory = org.javarosa.core.util.externalizable.PrototypeFactory;
using System.Collections.Generic;
namespace org.javarosa.core.services.transport.payload
{

    /// <author>  Clayton Sims
    /// </author>
    /// <date>  Dec 18, 2008 </date>
    /// <summary> 
    /// </summary>
    public class MultiMessagePayload : IDataPayload
    {
        /// <returns> A vector object containing each IDataPayload in this payload.
        /// </returns>
        virtual public System.Collections.ArrayList Payloads
        {
            get
            {
                return payloads;
            }

        }
        virtual public System.IO.Stream PayloadStream
        {
            /*
            * (non-Javadoc)
            * @see org.javarosa.core.services.transport.IDataPayload#getPayloadStream()
            */

            get
            {
                MultiInputStream bigStream = new MultiInputStream();
                System.Collections.IEnumerator en = payloads.GetEnumerator();
                //UPGRADE_TODO: Method 'java.util.Enumeration.hasMoreElements' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javautilEnumerationhasMoreElements'"
                while (en.MoveNext())
                {
                    //UPGRADE_TODO: Method 'java.util.Enumeration.nextElement' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javautilEnumerationnextElement'"
                    IDataPayload payload = (IDataPayload)en.Current;
                    bigStream.addStream(payload.PayloadStream);
                }
                bigStream.prepare();
                return bigStream;
            }

        }
        virtual public System.String PayloadId
        {
            get
            {
                return null;
            }

        }
        virtual public int PayloadType
        {
            get
            {
                return org.javarosa.core.services.transport.payload.IDataPayload_Fields.PAYLOAD_TYPE_MULTI;
            }

        }
        virtual public int TransportId
        {
            get
            {
                return -1;
            }

        }
        virtual public long Length
        {
            get
            {
                int len = 0;
                System.Collections.IEnumerator en = payloads.GetEnumerator();
                //UPGRADE_TODO: Method 'java.util.Enumeration.hasMoreElements' was converted to 'System.Collections.IEnumerator.MoveNext' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javautilEnumerationhasMoreElements'"
                while (en.MoveNext())
                {
                    //UPGRADE_TODO: Method 'java.util.Enumeration.nextElement' was converted to 'System.Collections.IEnumerator.Current' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javautilEnumerationnextElement'"
                    IDataPayload payload = (IDataPayload)en.Current;
                    len += (int) payload.Length;
                }
                return len;
            }

        }
        /// <summary>IDataPayload *</summary>
        internal System.Collections.ArrayList payloads = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));

        /// <summary> Note: Only useful for serialization.</summary>
        public MultiMessagePayload()
        {
            //ONLY FOR SERIALIZATION
        }

        /// <summary> Adds a payload that should be sent as part of this
        /// payload.
        /// </summary>
        /// <param name="payload">A payload that will be transmitted
        /// after all previously added payloads.
        /// </param>
        public virtual void addPayload(IDataPayload payload)
        {
            payloads.Add(payload);
        }

        /*
        * (non-Javadoc)
        * @see org.javarosa.core.util.externalizable.Externalizable#readExternal(java.io.DataInputStream, org.javarosa.core.util.externalizable.PrototypeFactory)
        */
        //UPGRADE_TODO: Class 'java.io.DataInputStream' was converted to 'System.IO.BinaryReader' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataInputStream'"
        public virtual void readExternal(System.IO.BinaryReader in_Renamed, PrototypeFactory pf)
        {
            payloads = (System.Collections.ArrayList)ExtUtil.read(in_Renamed, new ExtWrapListPoly(), pf);
        }

        /*
        * (non-Javadoc)
        * @see org.javarosa.core.util.externalizable.Externalizable#writeExternal(java.io.DataOutputStream)
        */
        //UPGRADE_TODO: Class 'java.io.DataOutputStream' was converted to 'System.IO.BinaryWriter' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataOutputStream'"
        public virtual void writeExternal(System.IO.BinaryWriter out_Renamed)
        {
            ExtUtil.write(out_Renamed, new ExtWrapListPoly(payloads));
        }

        public System.Collections.Generic.List<object> accept<T>(IDataPayloadVisitor<T> visitor)
        {
            List<object> lst = new List<object>();
            lst.Add(visitor.visit(this));
            return lst;
        }
    }
}