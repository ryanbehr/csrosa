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

using org.javarosa.core.model.instance;
using org.javarosa.core.services.transport.payload;
namespace org.javarosa.core.model.utils
{

    /**
     * An IInstanceSerializingVisitor serializes a DataModel
     * 
     * @author Clayton Sims
     *
     */
    public interface IInstanceSerializingVisitor : IInstanceVisitor
    {

        //LEGACY: Should remove
        byte[] serializeInstance(FormInstance model, FormDef formDef);

        byte[] serializeInstance(FormInstance model, IDataReference ref_);
        byte[] serializeInstance(FormInstance model);

         IDataPayload createSerializedPayload(FormInstance model, IDataReference ref_);
         IDataPayload createSerializedPayload(FormInstance model);

        void setAnswerDataSerializer(IAnswerDataSerializer ads);

         IInstanceSerializingVisitor newInstance();

    }
}