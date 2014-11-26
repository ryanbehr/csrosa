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

using org.javarosa.core.api;
using org.javarosa.core.model;
using org.javarosa.core.model.condition;
using org.javarosa.core.model.data;
using org.javarosa.core.model.instance;
using org.javarosa.core.model.util.restorable;
using org.javarosa.core.services;
using org.javarosa.core.services.transport.payload;
using org.javarosa.xform.parse;
using org.javarosa.xform.util;
using org.javarosa.xpath;
using org.javarosa.xpath.expr;
using System;
using System.IO;
namespace org.javarosa.model.xform
{



    public class XFormsModule : IModule
    {

        private class AnonymouseIXFormFactory : IXFormyFactory
        {

            public virtual TreeReference ref_Renamed(System.String refStr)
            {
                return FormInstance.unpackReference((IDataReference)(new XPathReference(refStr)));
            }

            public IDataPayload serializeInstance(FormInstance dm)
            {
                try
                {
                    return (new XFormSerializingVisitor()).createSerializedPayload(dm);
                }
                catch (IOException e)
                {
                    return null;
                }
            }

            public IAnswerData parseData(String textVal, int dataType, TreeReference ref_, FormDef f)
            {
                return XFormAnswerDataParser.getAnswerData(textVal, dataType, XFormParser.ghettoGetQuestionDef(dataType, f, ref_));
            }

            public String serializeData(IAnswerData data)
            {
                return (String)(new XFormAnswerDataSerializer().serializeAnswerData(data));
            }

            public IConditionExpr refToPathExpr(TreeReference ref_)
            {
                return new XPathConditional(XPathPathExpr.fromRef(ref_));
            }




            public FormInstance parseRestore(byte[] data, Type restorableType)
            {
                return XFormParser.restoreDataModel(data, restorableType);
            }
        }

        public void registerModule()
        {
            String[] classes = {
				"org.javarosa.model.xform.XPathReference",
				"org.javarosa.xpath.XPathConditional"
		};

            PrototypeManager.registerPrototypes(classes);
            PrototypeManager.registerPrototypes(XPathParseTool.xpathClasses);
            RestoreUtils.xfFact = new AnonymouseIXFormFactory();
        }

    }
}