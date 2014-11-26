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
using FormInstance = org.javarosa.core.model.instance.FormInstance;
namespace org.javarosa.core.model.utils
{

    /// <summary> An interface for classes which are capable of parsing and performing actions
    /// on Data Model objects.
    /// 
    /// </summary>
    /// <author>  Clayton Sims
    /// </author>
    /// <date>  Jan 27, 2009  </date>
    /// <summary> 
    /// </summary>
    public interface IInstanceProcessor
    {

        /// <summary> Processes the provided data model.
        /// 
        /// </summary>
        /// <param name="tree">The data model that will be handled.
        /// </param>
        void processInstance(FormInstance tree);
    }
}