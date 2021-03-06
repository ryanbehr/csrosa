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

/**
 * 
 */
using org.javarosa.core.services.locale;
using org.javarosa.core.util;
using org.javarosa.core.util.externalizable;
using System;
using System.IO;
namespace org.javarosa.core.reference
{

    /**
     * The ReferenceDataSource is a source of locale data which
     * is located at a location which is defined by a ReferenceURI.
     * 
     * @author Acellam Guy ,  Clayton Sims
     * @date Jun 1, 2009 
     *
     */
    public class ReferenceDataSource : LocaleDataSource
    {

        String referenceURI;

        /**
         * NOTE: FOR SERIALIZATION ONLY!
         */
        public ReferenceDataSource()
        {

        }

        /**
         * Creates a new Data Source for Locale data with the given resource URI.
         * 
         * @param resourceURI a URI to the resource file from which data should be loaded
         * @throws NullPointerException if resourceURI is null
         */
        public ReferenceDataSource(String referenceURI)
        {
            if (referenceURI == null)
            {
                throw new NullReferenceException("Reference URI cannot be null when creating a Resource File Data Source");
            }
            this.referenceURI = referenceURI;
        }

        /* (non-Javadoc)
         * @see org.javarosa.core.services.locale.LocaleDataSource#getLocalizedText()
         */
        public OrderedHashtable getLocalizedText()
        {
            try
            {
                Stream is_renamed = ReferenceManager._().DeriveReference(referenceURI).Stream;
                return LocalizationUtils.parseLocaleInput(is_renamed);
            }
            catch (IOException e)
            {
                Console.WriteLine(e.StackTrace);
                throw new SystemException("IOException while getting localized text at reference " + referenceURI);
            }
            catch (InvalidReferenceException e)
            {
                Console.WriteLine(e.StackTrace);
                throw new SystemException("Invalid Reference! " + referenceURI);
            }
        }


        /* (non-Javadoc)
        * @see org.javarosa.core.util.externalizable.Externalizable#readExternal(java.io.DataInputStream, org.javarosa.core.util.externalizable.PrototypeFactory)
        */
        //UPGRADE_TODO: Class 'java.io.DataInputStream' was converted to 'System.IO.BinaryReader' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataInputStream'"
        public virtual void readExternal(System.IO.BinaryReader in_Renamed, PrototypeFactory pf)
        {
            //UPGRADE_ISSUE: Method 'java.io.DataInputStream.readUTF' was not converted. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1000_javaioDataInputStreamreadUTF'"
            referenceURI = in_Renamed.ReadString();
        }

        /* (non-Javadoc)
            * @see org.javarosa.core.util.externalizable.Externalizable#writeExternal(java.io.DataOutputStream)
            */
        //UPGRADE_TODO: Class 'java.io.DataOutputStream' was converted to 'System.IO.BinaryWriter' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataOutputStream'"
        public virtual void writeExternal(System.IO.BinaryWriter out_Renamed)
        {
            //UPGRADE_ISSUE: Method 'java.io.DataOutputStream.writeUTF' was not converted. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1000_javaioDataOutputStreamwriteUTF_javalangString'"
            out_Renamed.Write(referenceURI);
        }
    }
}