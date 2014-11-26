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

using org.javarosa.core.model.data;
using org.javarosa.core.model.data.helper;
using org.javarosa.core.services.storage;
using org.javarosa.core.util.externalizable;
using System;
using System.Collections;
using System.IO;
namespace org.javarosa.core.model.instance.utils{

/**
 * An alternate serialization format for FormInstances (saved form instances) that drastically reduces the 
 * resultant record size by cutting out redundant information. Size savings are typically 90-95%. The trade-off is
 * that in order to deserialize, a template FormInstance (typically from the original FormDef) must be provided.
 * 
 * In general, the format is thus:
 * 1) write the fields from the FormInstance object (e.g., date saved), excluding those that never change for a given
 *    form type (e.g., schema).
 * 2) walk the tree depth-first. for each node: if repeatable, write the number of repetitions at the current level; if
 *    not, write a boolean indicating if the node is relevant. non-relevant nodes are not descended into. repeated nodes
 *    (i.e., several nodes with the same name at the current level) are handled in order
 * 3) for each leaf (data) node, write a boolean whether the node is empty or has data
 * 4) if the node has data, serialize the data. do not specify the data type -- it can be determined from the template.
 *    multiple choice questions use a more compact format than normal.
 * 4a) in certain situations where the data differs from its prescribed data type (can happen as the result of 'calculate'
 *    expressions), flag the actual data type by hijacking the 'empty' flag above
 * 
 * @author Acellam Guy ,  Drew Roos
 *
 */
    public class CompactInstanceWrapper : WrappingStorageUtility.SerializationWrapper
    {
        public  const int CHOICE_VALUE = 0;	/* serialize multiple-select choices by writing out the <value> */
        public  const int CHOICE_INDEX = 1;   /* serialize multiple-select choices by writing out only the index of the
	                                             * choice; much more compact than CHOICE_VALUE, but the deserialized
	                                             * instance must be explicitly re-attached to the parent FormDef (not just
	                                             * the template data instance) before the instance can be serialized to xml
	                                             * (otherwise the actual xml <value>s are still unknown)
	                                             */

        public  const int CHOICE_MODE = CHOICE_INDEX;

        private InstanceTemplateManager templateMgr;	/* instance template provider; provides templates needed for deserialization. */
        private FormInstance instance;					/* underlying FormInstance to serialize/deserialize */

        public CompactInstanceWrapper()
            : this(null)
        {

        }

        /**
         * 
         * @param templateMgr template provider; if null, template is always fetched on-demand from RMS (slow!)
         */
        public CompactInstanceWrapper(InstanceTemplateManager templateMgr)
        {
            this.templateMgr = templateMgr;
        }

        public Type baseType()
        {
            return typeof(FormInstance);
        }

        public void setData(Externalizable e)
        {
            this.instance = (FormInstance)e;
        }

        public Externalizable Data
        {
            get{return instance;}
            set{
                this.instance = (FormInstance)value;
          }
        }

        /**
         * deserialize a compact instance. note the retrieval of the template data instance
         */
        public void readExternal(BinaryReader in_renamed, PrototypeFactory pf)
        {
            int formID = ExtUtil.readInt(in_renamed);
            instance = getTemplateInstance(formID).clone();

            instance.ID = (ExtUtil.readInt(in_renamed));
            instance.setDateSaved((DateTime)ExtUtil.read(in_renamed, new ExtWrapNullable(typeof(DateTime))));
            //formID, name, schema, versions, and namespaces are all invariants of the template instance

            TreeElement root = instance.getRoot();
            readTreeElement(root, in_renamed, pf);
        }

        /**
         * serialize a compact instance
         */
        public void writeExternal(BinaryWriter out_renamed)
        {
            if (instance == null)
            {
                throw new SystemException("instance has not yet been set via setData()");
            }

            ExtUtil.writeNumeric(out_renamed, instance.getFormId());
            ExtUtil.writeNumeric(out_renamed, instance.ID);
            ExtUtil.write(out_renamed, new ExtWrapNullable(instance.getDateSaved()));

            writeTreeElement(out_renamed, instance.getRoot());
        }

        private FormInstance getTemplateInstance(int formID)
        {
            if (templateMgr != null)
            {
                return templateMgr.getTemplateInstance(formID);
            }
            else
            {
                FormInstance template = loadTemplateInstance(formID);
                if (template == null)
                {
                    throw new SystemException("no formdef found for form id [" + formID + "]");
                }
                return template;
            }
        }

        /**
         * load a template instance fresh from the original FormDef, retrieved from RMS
         * @param formID
         * @return
         */
        public static FormInstance loadTemplateInstance(int formID)
        {
            IStorageUtility forms = StorageManager.getStorage(FormDef.STORAGE_KEY);
            FormDef f = (FormDef)forms.read(formID);
            return (f != null ? f.Instance : null);
        }

        /**
         * recursively read in a node of the instance, by filling out the template instance
         * @param e
         * @param ref
         * @param in
         * @param pf
         * @throws IOException
         * @throws DeserializationException
         */
        private void readTreeElement(TreeElement e, BinaryReader in_, PrototypeFactory pf)
        {
            TreeElement templ = instance.getTemplatePath(e.getRef());
            Boolean isGroup = !templ.isLeaf();

            if (isGroup)
            {
                ArrayList childTypes = new ArrayList();
                for (int i = 0; i < templ.getNumChildren(); i++)
                {
                    String childName = templ.getChildAt(i).getName();
                    if (!childTypes.Contains(childName))
                    {
                        childTypes.Add(childName);
                    }
                }

                for (int i = 0; i < childTypes.Count; i++)
                {
                    String childName = (String)childTypes[i];

                    TreeReference childTemplRef = e.getRef().extendRef(childName, 0);
                    TreeElement childTempl = instance.getTemplatePath(childTemplRef);

                    Boolean repeatable = childTempl.repeatable;
                    int n = ExtUtil.readInt(in_);

                    Boolean relevant = (n > 0);
                    if (!repeatable && n > 1)
                    {
                        throw new DeserializationException("Detected repeated instances of a non-repeatable node");
                    }

                    if (repeatable)
                    {
                        int mult = e.getChildMultiplicity(childName);
                        for (int j = mult - 1; j >= 0; j--)
                        {
                            e.removeChild(childName, j);
                        }

                        for (int j = 0; j < n; j++)
                        {
                            TreeReference dstRef = e.getRef().extendRef(childName, j);
                            try
                            {
                                instance.copyNode(childTempl, dstRef);
                            }
                            catch (InvalidReferenceException ire)
                            {
                                //If there is an invalid reference, this is a malformed instance,
                                //so we'll throw a Deserialization exception.
                                TreeReference r = ire.InvalidReference;
                                if (r == null)
                                {
                                    throw new DeserializationException("Null Reference while attempting to deserialize! " + ire.Message);
                                }
                                else
                                {
                                    throw new DeserializationException("Invalid Reference while attemtping to deserialize! Reference: " + r.toString(true) + " | " + ire.Message);
                                }

                            }

                            TreeElement child = e.getChild(childName, j);
                            child.setRelevant(true);
                            readTreeElement(child, in_, pf);
                        }
                    }
                    else
                    {
                        TreeElement child = e.getChild(childName, 0);
                        child.setRelevant(relevant);
                        if (relevant)
                        {
                            readTreeElement(child, in_, pf);
                        }
                    }
                }
            }
            else
            {
                e.setValue((IAnswerData)ExtUtil.read(in_, new ExtWrapAnswerData(e.dataType)));
            }
        }

        /**
         * recursively write out a node of the instance
         * @param out
         * @param e
         * @param ref
         * @throws IOException
         */
        private void writeTreeElement(BinaryWriter out_, TreeElement e)
        {
            TreeElement templ = instance.getTemplatePath(e.getRef());
            Boolean isGroup = !templ.isLeaf();

            if (isGroup)
            {
                ArrayList childTypesHandled = new ArrayList();
                for (int i = 0; i < templ.getNumChildren(); i++)
                {
                    String childName = templ.getChildAt(i).getName();
                    if (!childTypesHandled.Contains(childName))
                    {
                        childTypesHandled.Add(childName);

                        int mult = e.getChildMultiplicity(childName);
                        if (mult > 0 && !e.getChild(childName, 0).isRelevant())
                        {
                            mult = 0;
                        }

                        ExtUtil.writeNumeric(out_, mult);
                        for (int j = 0; j < mult; j++)
                        {
                            writeTreeElement(out_, e.getChild(childName, j));
                        }
                    }
                }
            }
            else
            {
                ExtUtil.write(out_, new ExtWrapAnswerData(this,e.dataType, e.getValue()));
            }
        }

        /**
         * ExternalizableWrapper to handle writing out a node's data. In particular, handles:
         *   * empty nodes
         *   * ultra-compact serialization of multiple-choice answers
         *   * tagging with extra type information when the template alone will not contain sufficient information
         * 
         * @author Acellam Guy ,  Drew Roos
         *
         */
        private class ExtWrapAnswerData : ExternalizableWrapper
        {
            int dataType;
            CompactInstanceWrapper instance;

            public ExtWrapAnswerData(CompactInstanceWrapper instance,int dataType, IAnswerData val)
            {
                this.val = val;
                this.dataType = dataType;

            }

            public ExtWrapAnswerData(int dataType)
            {
                this.dataType = dataType;
            }

            public override void readExternal(BinaryReader in_Renamed, PrototypeFactory pf)
            {
                byte flag = in_Renamed.ReadByte();
			if (flag == 0x00) {
				val = null;
			} else {
				Type answerType = classForDataType(dataType);

                if (answerType == null)
                {
                    //custom data types
                    val = ExtUtil.read(in_Renamed, new ExtWrapTagged(), pf);
                }
                else if (answerType == typeof(SelectOneData))
                {
                    val = instance.getSelectOne(ExtUtil.read(in_Renamed, org.javarosa.core.model.instance.utils.CompactInstanceWrapper.CHOICE_MODE == org.javarosa.core.model.instance.utils.CompactInstanceWrapper.CHOICE_VALUE ? typeof(System.String) : typeof(System.Int32)));
                }
                else if (answerType == typeof(SelectMultiData))
                {
                    val = instance.getSelectMulti((System.Collections.ArrayList)ExtUtil.read(in_Renamed, new ExtWrapList(org.javarosa.core.model.instance.utils.CompactInstanceWrapper.CHOICE_MODE == org.javarosa.core.model.instance.utils.CompactInstanceWrapper.CHOICE_VALUE ? typeof(System.String) : typeof(System.Int32))));
                }
                else
                {
                    switch (flag)
                    {
                        case 0x40: answerType = typeof(StringData); break;
                        case 0x41: answerType = typeof(IntegerData); break;
                        case 0x42: answerType = typeof(DecimalData); break;
                        case 0x43: answerType = typeof(DateData); break;
                        case 0x44: answerType = typeof(BooleanData); break;
                    }

                    val = (IAnswerData)ExtUtil.read(in_Renamed, answerType);
                }					
			}
		}

            public override void writeExternal(BinaryWriter out_)  {
			if (val == null) {
                out_.Write(0x00);
			} else {
				byte prefix = 0x01;
				Externalizable serEntity;
				
				if (dataType < 0 || dataType >= 100) {
					//custom data types
					serEntity = new ExtWrapTagged(val);
				} else if (val is SelectOneData) {
					serEntity = new ExtWrapBase(instance.compactSelectOne((SelectOneData)val));
				} else if (val is SelectMultiData) {
					serEntity = new ExtWrapList(instance.compactSelectMulti((SelectMultiData)val));
				} else {
					serEntity = (IAnswerData)val;
					
					//flag when data type differs from the default data type in the <bind> (can happen with 'calculate's)
					if (val.GetType()!= classForDataType(dataType)) {
						if (val is StringData) {
							prefix = 0x40;
						} else if (val is IntegerData) {
							prefix = 0x41;
						} else if (val is DecimalData) {
							prefix = 0x42;
						} else if (val is DateData) {
							prefix = 0x43;
						} else if (val is BooleanData) {
							prefix = 0x44;
						} else {
							throw new SystemException("divergent data type not allowed");
						}
					}
				}

				out_.Write(prefix);
				ExtUtil.write(out_, serEntity);
			}
		}

            public override ExternalizableWrapper clone(Object val)
            {
                throw new SystemException("not supported");
            }

            public override void metaReadExternal(BinaryReader in_, PrototypeFactory pf)
            {
                throw new SystemException("not supported");
            }

            public override void metaWriteExternal(BinaryWriter out_)
            {
                throw new SystemException("not supported");
            }
        }

        /**
         * reduce a SelectOneData to an integer (index mode) or string (value mode)
         * @param data
         * @return Integer or String
         */
        private Object compactSelectOne(SelectOneData data)
        {
            Selection val = (Selection)data.Value;
            return extractSelection(val);
        }

        /**
         * reduce a SelectMultiData to a vector of integers (index mode) or strings (value mode)
         * @param data
         * @return
         */
        private ArrayList compactSelectMulti(SelectMultiData data)
        {
            ArrayList val = (ArrayList)data.Value;
            ArrayList choices = new ArrayList();
            for (int i = 0; i < val.Count; i++)
            {
                choices.Add(extractSelection((Selection)val[i]));
            }
            return choices;
        }

        /**
         * create a SelectOneData from an integer (index mode) or string (value mode)
         */
        private SelectOneData getSelectOne(Object o)
        {
            return new SelectOneData(makeSelection(o));
        }

        /**
         * create a SelectMultiData from a vector of integers (index mode) or strings (value mode)
         */
        private SelectMultiData getSelectMulti(ArrayList v)
        {
            ArrayList choices = new ArrayList();
            for (int i = 0; i < v.Count; i++)
            {
                choices.Add(makeSelection(v[i]));
            }
            return new SelectMultiData(choices);
        }

        /**
         * extract the value out of a Selection according to the current CHOICE_MODE
         * @param s
         * @return Integer or String
         */
        private Object extractSelection(Selection s)
        {
            switch (CHOICE_MODE)
            {
                case CHOICE_VALUE:
                    return s.Value;
                case CHOICE_INDEX:
                    if (s.index == -1)
                    {
                        throw new SystemException("trying to serialize in choice-index mode but selections do not have indexes set!");
                    }
                    return (int)(s.index);
                default: throw new ArgumentException();
            }
        }

        /**
         * build a Selection from an integer or string, according to the current CHOICE_MODE
         * @param o
         * @return
         */
        private Selection makeSelection(Object o)
        {
            if (o is String)
            {
                return new Selection((String)o);
            }
            else if (o is int)
            {
                return new Selection(((int)o));
            }
            else
            {
                throw new SystemException();
            }
        }

        /**
         * map xforms data types to the Class that represents that data in a FormInstance
         * @param dataType
         * @return
         */
        public static System.Type classForDataType(int dataType)
        {
            switch (dataType)
            {

                case Constants.DATATYPE_NULL:
                    return typeof(StringData);
                case Constants.DATATYPE_TEXT:
                    return typeof(StringData);
                case Constants.DATATYPE_INTEGER:
                    return typeof(IntegerData);
                case Constants.DATATYPE_LONG:
                    return typeof(LongData);
                case Constants.DATATYPE_DECIMAL:
                    return typeof(DecimalData);
                case Constants.DATATYPE_BOOLEAN:
                    return typeof(BooleanData);
                case Constants.DATATYPE_DATE:
                    return typeof(DateData);
                case Constants.DATATYPE_TIME:
                    return typeof(TimeData);
                case Constants.DATATYPE_DATE_TIME:
                    return typeof(DateTimeData);
                case Constants.DATATYPE_CHOICE:
                    return typeof(SelectOneData);
                case Constants.DATATYPE_CHOICE_LIST:
                    return typeof(SelectMultiData);
                case Constants.DATATYPE_GEOPOINT:
                    return typeof(GeoPointData);
                default: return null;

            }
        }
    }
}
