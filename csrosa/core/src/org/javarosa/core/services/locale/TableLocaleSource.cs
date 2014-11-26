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
using org.javarosa.core.util;
using org.javarosa.core.util.externalizable;
using System;
using System.IO;
namespace org.javarosa.core.services.locale
{

    /**
     * @author Acellam Guy ,  Clayton Sims
     * @date May 26, 2009 
     *
     */
    public class TableLocaleSource : LocaleDataSource
    {
        private OrderedHashtable localeData; /*{ String -> String } */

        public TableLocaleSource()
        {
            localeData = new OrderedHashtable();
        }

        public TableLocaleSource(OrderedHashtable localeData)
        {
            this.localeData = localeData;
        }


        /**
         * Set a text mapping for a single text handle for a given locale.
         * 
         * @param textID Text handle. Must not be null. Need not be previously defined for this locale.
         * @param text Localized text for this text handle and locale. Will overwrite any previous mapping, if one existed.
         * If null, will remove any previous mapping for this text handle, if one existed.
         * @throws UnregisteredLocaleException If locale is not defined or null.
         * @throws NullPointerException if textID is null
         */
        public void setLocaleMapping(String textID, String text)
        {
            if (textID == null)
            {
                throw new NullReferenceException("Null textID when attempting to register " + text + " in locale table");
            }
            if (text == null)
            {
                localeData.remove(textID);
            }
            else
            {
                localeData.put(textID, text);
            }
        }

        /**
         * Determine whether a locale has a mapping for a given text handle. Only tests the specified locale and form; does
         * not fallback to any default locale or text form.
         * 
         * @param textID Text handle.
         * @return True if a mapping exists for the text handle in the given locale.
         * @throws UnregisteredLocaleException If locale is not defined.
         */
        public Boolean hasMapping(String textID)
        {
            return (textID == null ? false : localeData[textID] != null);
        }


        public Boolean equals(Object o)
        {
            if (!(o is TableLocaleSource))
            {
                return false;
            }
            TableLocaleSource l = (TableLocaleSource)o;
            return ExtUtil.Equals(localeData, l.localeData);
        }

        public OrderedHashtable getLocalizedText()
        {
            return localeData;
        }

        public void readExternal(System.IO.BinaryReader in_Renamed, PrototypeFactory pf)
        {
            localeData = (OrderedHashtable)ExtUtil.read(in_Renamed, new ExtWrapMap(typeof(String), typeof(String), 1), pf);
        }

        public void writeExternal(BinaryWriter out_)
        {
            ExtUtil.write(out_, new ExtWrapMap(localeData));
        }
    }
}