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
namespace org.javarosa.model.xform{


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
	 * A visitor-esque class which walks a FormInstance and constructs an XML document
	 * containing its instance.
	 *
	 * The XML node elements are constructed in a depth-first manner, consistent with
	 * standard XML document parsing.
	 *
	 * @author Clayton Sims
	 *
	 */
	public class XFormSerializingVisitor : IInstanceSerializingVisitor {

		/** The XML document containing the instance that is to be returned */
		XmlDocument theXmlDoc;

		/** The serializer to be used in constructing XML for AnswerData elements */
		IAnswerDataSerializer serializer;
		
		/** The root of the xml document which should be included in the serialization **/
		TreeReference rootRef;
		
		/** The schema to be used to serialize answer data */
		FormDef schema;	//not used
		
		ArrayList dataPointers;
		
		private void init() {
			theXmlDoc = null;
			schema = null;
			dataPointers = new ArrayList();
		}

        public virtual byte[] serializeInstance(FormInstance model, IDataReference ref_)
        {
            init();
            rootRef = model.unpackReference2(ref_);
            if (this.serializer == null)
            {
                this.setAnswerDataSerializer(new XFormAnswerDataSerializer());
            }

            model.accept(this);
            if (theXmlDoc != null)
            {
                return Encoding.UTF8.GetBytes(XFormSerializer.getString(theXmlDoc));
            }
            else
            {
                return null;
            }
        }

        public virtual byte[] serializeInstance(FormInstance model)
        {

            return serializeInstance(model, new XPathReference("/"));
        }

        public virtual byte[] serializeInstance(FormInstance model, FormDef formDef)
        {

            //LEGACY: Should remove
            init();
            this.schema = formDef;
            return serializeInstance(model);
        }

      
		public IDataPayload createSerializedPayload	(FormInstance model) {
			return createSerializedPayload(model, (IDataReference)new XPathReference("/"));
		}
		
		public IDataPayload createSerializedPayload	(FormInstance model, IDataReference ref_)  {
			init();
			rootRef = model.unpackReference2(ref_);
			if(this.serializer == null) {
				this.setAnswerDataSerializer(new XFormAnswerDataSerializer());
			}
			model.accept(this);
			if(theXmlDoc != null) {
                byte[] form = Encoding.UTF8.GetBytes(XFormSerializer.getString(theXmlDoc));
				if(dataPointers.Count == 0) {
					return new ByteArrayPayload(form, null, IDataPayload_Fields.PAYLOAD_TYPE_XML);
				}
				MultiMessagePayload payload = new MultiMessagePayload();
				payload.addPayload(new ByteArrayPayload(form, null, IDataPayload_Fields.PAYLOAD_TYPE_XML));
				IEnumerator en = dataPointers.GetEnumerator();
				while(en.MoveNext()) {
					IDataPointer pointer = (IDataPointer)en.Current;
					payload.addPayload(new DataPointerPayload(pointer));
				}
				return payload; 
			}
			else {
				return null;
			}
		}

		/*
		 * (non-Javadoc)
		 * @see org.javarosa.core.model.utils.ITreeVisitor#visit(org.javarosa.core.model.DataModelTree)
		 */
		public void visit(FormInstance tree) {
			theXmlDoc = new XmlDocument();
			//TreeElement root = tree.getRoot();
			
			TreeElement root = tree.resolveReference(rootRef);
			
			//For some reason resolveReference won't ever return the root, so we'll 
			//catch that case and just start at the root.
			if(root == null) {
				root = tree.getRoot();
			}
			
			for (int i = 0; i< root.getNumChildren(); i++){
				TreeElement childAt = root.getChildAt(i);
			}
			
			if (root != null) {
				theXmlDoc.AppendChild(serializeNode(root));
			}
			
			XmlElement top =(XmlElement)theXmlDoc.FirstChild;
			
			String[] prefixes = tree.getNamespacePrefixes();
			for(int i = 0 ; i < prefixes.Length; ++i ) {
				top.Prefix =prefixes[i];
			}
			if (tree.schema != null) {
				//top.setNamespace(tree.schema);
                top.SetAttribute(top.Name, tree.schema, top.Value);
				top.Prefix =tree.schema;
			}
		}

		public XmlElement serializeNode (TreeElement instanceNode) {

            XmlElement e = theXmlDoc.CreateElement(instanceNode.getName());//TODO Name

			//don't serialize template nodes or non-relevant nodes
			if (!instanceNode.isRelevant() || instanceNode.getMult() == TreeReference.INDEX_TEMPLATE)
				return null;
				
			if (instanceNode.getValue() != null) {
				Object serializedAnswer = serializer.serializeAnswerData(instanceNode.getValue(), instanceNode.dataType); 

				if (serializedAnswer is XmlElement) {
					e = (XmlElement)serializedAnswer;
				} else if (serializedAnswer is String) {
				
                     e = e.OwnerDocument.CreateElement(instanceNode.getName());
                 
                     XmlText xmlText = theXmlDoc.CreateTextNode( (String)serializedAnswer);//TODO name
					e.AppendChild(xmlText);				
				} else {
					throw new SystemException("Can't handle serialized output for" + instanceNode.getValue().ToString() + ", " + serializedAnswer);
				}
				
				if(serializer.containsExternalData(instanceNode.getValue())) {
					IDataPointer[] pointer = serializer.retrieveExternalDataPointer(instanceNode.getValue());
					for(int i = 0 ; i < pointer.Length ; ++i) {
						dataPointers.Add(pointer[i]);
					}
				}
			} else {
				//make sure all children of the same tag name are written en bloc
				ArrayList childNames = new ArrayList();
				for (int i = 0; i < instanceNode.getNumChildren(); i++) {
					String childName = instanceNode.getChildAt(i).getName();
					Console.WriteLine("CHILDNAME: " + childName);
					if (!childNames.Contains(childName))
						childNames.Add(childName);
				}
				
				for (int i = 0; i < childNames.Count; i++) {
					String childName = (String)childNames[i];
					int mult = instanceNode.getChildMultiplicity(childName);
					for (int j = 0; j < mult; j++) {
						XmlElement child = serializeNode(instanceNode.getChild(childName, j));
						if (child != null) {
							e.AppendChild(child);
						}
					}
				}
			}

            XmlElement e1 = e.OwnerDocument.CreateElement(instanceNode.getName());
            e.ParentNode.ReplaceChild(e1, e);
			
			// add hard-coded attributes
			for (int i = 0; i < instanceNode.getAttributeCount(); i++) {
				String namespace_ = instanceNode.getAttributeNamespace(i);
				String name		 = instanceNode.getAttributeName(i);
				String val		 = instanceNode.getAttributeValue(i);
                e.SetAttribute(name,namespace_, val);
			}

			return e;
		}

		/*
		 * (non-Javadoc)
		 * @see org.javarosa.core.model.utils.IInstanceSerializingVisitor#setAnswerDataSerializer(org.javarosa.core.model.IAnswerDataSerializer)
		 */
		public void setAnswerDataSerializer(IAnswerDataSerializer ads) {
			this.serializer = ads;
		}
		
		public IInstanceSerializingVisitor newInstance() {
			XFormSerializingVisitor modelSerializer = new XFormSerializingVisitor();
			modelSerializer.setAnswerDataSerializer(this.serializer);
			return modelSerializer;
		}

    }

}