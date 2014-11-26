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

using org.javarosa.core.services.locale;
using org.javarosa.core.util.externalizable;
using System;
using System.Collections.Generic;
namespace org.javarosa.core.model
{

    /**
     * An IFormDataElement is an element of the physical interaction for
     * a form, an example of an implementing element would be the definition
     * of a Question. 
     * 
     * @author Acellam Guy ,  Drew Roos
     *
     */
    public interface IFormElement : Localizable, Externalizable
    {

        /**
         * @return The unique ID of this element
         */
        int ID { get; set; }

        /**
         * get the TextID for this element used for localization purposes
         * @return the TextID (bare, no ;form appended to it!!)
         */
        String TextID { get; set; }


        /**
         * @return A vector containing any children that this element
         * might have. Null if the element is not able to have child
         * elements.
         */
        List<IFormElement> Children { get; set; }

      
        void addChild(IFormElement fe);

        IFormElement getChild(int i);

        /**
         * @return A recursive count of how many elements are ancestors of this element.
         */
        int DeepChildCount{get;}

        /**
         * @return The data reference for this element
         */
        IDataReference Bind { get;}

        /**
         * Registers a state observer for this element.
         * 
         * @param qsl
         */
         void registerStateObserver(FormElementStateListener qsl);

        /**
         * Unregisters a state observer for this element.
         * 
         * @param qsl
         */
         void unregisterStateObserver(FormElementStateListener qsl);

        /**
         * This method returns the regular
         * innertext betweem label tags (if present) (&ltlabel&gtinnertext&lt/label&gt).
         * @return &ltlabel&gt innertext or null (if innertext is not present).
         */
         String LabelInnerText{get;set;}


        /**
         * @return
         */
         String AppearanceAttr{get;set;}
    }
}