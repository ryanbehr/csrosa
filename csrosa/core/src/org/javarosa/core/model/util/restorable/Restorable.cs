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
using FormInstance = org.javarosa.core.model.instance.FormInstance;
using TreeReference = org.javarosa.core.model.instance.TreeReference;
namespace org.javarosa.core.model.util.restorable
{

    public interface Restorable
    {
        System.String RestorableType
        {
            get;

        }
        FormInstance exportData();
        void templateData(FormInstance dm, TreeReference parentRef);
        void importData(FormInstance dm);
    }
}