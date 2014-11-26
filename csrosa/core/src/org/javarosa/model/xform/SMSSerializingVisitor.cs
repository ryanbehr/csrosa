using org.javarosa.core.data;
using org.javarosa.core.model;
using org.javarosa.core.model.instance;
using org.javarosa.core.model.utils;
using org.javarosa.core.services.transport.payload;
using org.javarosa.xform.util;
using System;
using System.Collections;
using System.Text;
using System.Xml;
namespace org.javarosa.model.xform
{

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
     * A modified version of Clayton's XFormSerializingVisitor that constructs
     * SMS's.
     * 
     * @author Munaf Sheikh, Cell-Life
     * 
     */
    public class SMSSerializingVisitor : IInstanceSerializingVisitor
    {

        private String theSmsStr = null; // sms string to be returned
        private String nodeSet = null; // which nodeset the sms contents are in
        private String xmlns = null;
        private String delimeter = null;
        private String prefix = null;
        private String method = null;
        private TreeReference rootRef;

        /** The serializer to be used in constructing XML for AnswerData elements */
        IAnswerDataSerializer serializer;

        /** The schema to be used to serialize answer data */
        FormDef schema; // not used

        ArrayList dataPointers;

        private void init()
        {
            theSmsStr = null;
            schema = null;
            dataPointers = new ArrayList();
            theSmsStr = "";
        }

        public byte[] serializeInstance(FormInstance model, FormDef formDef)
        {
            init();
            this.schema = formDef;
            return serializeInstance(model);
        }


        /*
         * (non-Javadoc)
         * @see org.javarosa.core.model.utils.IInstanceSerializingVisitor#serializeInstance(org.javarosa.core.model.instance.FormInstance)
         */
        public byte[] serializeInstance(FormInstance model)
        {
            return this.serializeInstance(model, new XPathReference("/"));
        }

        /*
         * (non-Javadoc)
         * @see org.javarosa.core.model.utils.IInstanceSerializingVisitor#serializeInstance(org.javarosa.core.model.instance.FormInstance, org.javarosa.core.model.IDataReference)
         */
        public byte[] serializeInstance(FormInstance model, IDataReference ref_)
        {
            init();
            rootRef = model.unpackReference2(ref_);
            if (this.serializer == null)
            {
                this.setAnswerDataSerializer(new XFormAnswerDataSerializer());
            }
            model.accept(this);
            if (theSmsStr != null)
            {
                //Encode in UTF-16 by default, since it's the default for complex messages
                return Encoding.UTF8.GetBytes(theSmsStr);
            }
            else
            {
                return null;
            }
        }

        /*
         * (non-Javadoc)
         * @see org.javarosa.core.model.utils.IInstanceSerializingVisitor#createSerializedPayload(org.javarosa.core.model.instance.FormInstance)
         */
        public IDataPayload createSerializedPayload(FormInstance model)
        {
            return createSerializedPayload(model, new XPathReference("/"));
        }

        public IDataPayload createSerializedPayload(FormInstance model, IDataReference ref_)
        {
            init();
            rootRef = model.unpackReference2(ref_);
            if (this.serializer == null)
            {
                this.setAnswerDataSerializer(new XFormAnswerDataSerializer());
            }
            model.accept(this);
            if (theSmsStr != null)
            {
                byte[] form = Encoding.UTF8.GetBytes(theSmsStr);
                return new ByteArrayPayload(form, null, IDataPayload_Fields.PAYLOAD_TYPE_SMS);
            }
            else
            {
                return null;
            }
        }

        /*
         * (non-Javadoc)
         * 
         * @see
         * org.javarosa.core.model.utils.ITreeVisitor#visit(org.javarosa.core.model
         * .DataModelTree)
         */
        public void visit(FormInstance tree)
        {
            nodeSet = "";

            //TreeElement root = tree.getRoot();
            TreeElement root = tree.resolveReference(rootRef);

            xmlns = root.getAttributeValue("", "xmlns");
            delimeter = root.getAttributeValue("", "delimeter");
            prefix = root.getAttributeValue("", "prefix");

            xmlns = (xmlns != null) ? xmlns : " ";
            delimeter = (delimeter != null) ? delimeter : " ";
            prefix = (prefix != null) ? prefix : " ";

            //Don't bother adding any delimiters, yet. Delimiters are
            //added before tags/data
            theSmsStr = prefix;

            // serialize each node to get it's answers
            for (int j = 0; j < root.getNumChildren(); j++)
            {
                TreeElement tee = root.getChildAt(j);
                String e = serializeNode(tee);
                if (e != null)
                {
                    theSmsStr += e;
                }
            }
            theSmsStr = theSmsStr.Trim();
        }

        public String serializeNode(TreeElement instanceNode)
        {
            String ae = "";
            // don't serialize template nodes or non-relevant nodes
            if (!instanceNode.isRelevant()
                    || instanceNode.getMult() == TreeReference.INDEX_TEMPLATE)
                return null;

            if (instanceNode.getValue() != null)
            {
                Object serializedAnswer = serializer.serializeAnswerData(
                        instanceNode.getValue(), instanceNode.dataType);

                if (serializedAnswer is XmlElement)
                {
                    // DON"T handle this.
                    throw new SystemException("Can't handle serialized output for"
                            + instanceNode.getValue().ToString() + ", "
                            + serializedAnswer);
                }
                else if (serializedAnswer is String)
                {
                    XmlDocument theXmlDoc = new XmlDocument();
                    XmlElement e = theXmlDoc.CreateElement("TreeElement");//TODO
                    XmlText xmlText = theXmlDoc.CreateTextNode((String)serializedAnswer);
                    e.AppendChild(xmlText);

                    String tag = instanceNode.getAttributeValue("", "tag");
                    ae += ((tag != null) ? tag + delimeter : delimeter); // tag
                    // might
                    // be
                    // null

                    for (int k = 0; k < e.ChildNodes.Count; k++)
                    {
                        ae += e.ChildNodes[k].InnerText.ToString() + delimeter;
                    }

                }
                else
                {
                    throw new SystemException("Can't handle serialized output for"
                            + instanceNode.getValue().ToString() + ", "
                            + serializedAnswer);
                }

                if (serializer.containsExternalData(instanceNode.getValue()))
                {
                    IDataPointer[] pointer = serializer
                            .retrieveExternalDataPointer(instanceNode.getValue());
                    for (int i = 0; i < pointer.Length; ++i)
                    {
                        dataPointers.Add(pointer[i]);
                    }
                }
            }
            return ae;
        }

        /*
         * (non-Javadoc)
         * 
         * @seeorg.javarosa.core.model.utils.IInstanceSerializingVisitor#
         * setAnswerDataSerializer(org.javarosa.core.model.IAnswerDataSerializer)
         */
        

        public IInstanceSerializingVisitor newInstance()
        {
            XFormSerializingVisitor modelSerializer = new XFormSerializingVisitor();
            modelSerializer.setAnswerDataSerializer(this.serializer);
            return modelSerializer;
        }



        public void setAnswerDataSerializer(IAnswerDataSerializer ads)
        {
            this.serializer = ads;
        }
    }

}