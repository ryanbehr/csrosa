using org.javarosa.core.model.condition;
using org.javarosa.core.model.data;
using org.javarosa.core.model.instance.utils;
using org.javarosa.core.model.util.restorable;
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
using org.javarosa.core.util.externalizable;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
namespace org.javarosa.core.model.instance
{

    /**
     * An element of a FormInstance.
     * 
     * TreeElements represent an XML node in the instance. It may either have a value (e.g., <name>Drew</name>),
     * a number of TreeElement children (e.g., <meta><device /><timestamp /><user_id /></meta>), or neither (e.g.,
     * <empty_node />)
     * 
     * TreeElements can also represent attributes. Attributes are unique from normal elements in that they are
     * not "children" of their parent, and are always leaf nodes: IE cannot have children.
     * 
     * @author Acellam Guy ,  Clayton Sims
     * 
     */

    public class TreeElement : Externalizable
    {
        private String name; // can be null only for hidden root node
        public int multiplicity; // see TreeReference for special values
        private TreeElement parent;
        public Boolean repeatable;
        public Boolean isAttribute;

        private IAnswerData value;
        private ArrayList children = new ArrayList();

        /* model properties */
        public int dataType = Constants.DATATYPE_NULL; //TODO
        public Boolean required = false;// TODO
        private Constraint constraint = null;
        private String preloadHandler = null;
        private String preloadParams = null;

        private Boolean relevant = true;
        private Boolean enabled = true;
        // inherited properties 
        private Boolean relevantInherited = true;
        private Boolean enabledInherited = true;

        private ArrayList observers;

        private List<TreeElement> attributes;

        private String namespace_;


        /**
         * TreeElement with null name and 0 multiplicity? (a "hidden root" node?)
         */
        public TreeElement()
            : this(null, TreeReference.DEFAULT_MUTLIPLICITY)
        {

        }

        public TreeElement(String name)
            : this(name, TreeReference.DEFAULT_MUTLIPLICITY)
        {

        }

        public TreeElement(String name, int multiplicity)
        {
            this.name = name;
            this.multiplicity = multiplicity;
            this.parent = null;
            attributes = new List<TreeElement>(0);
        }

        /**
         * Construct a TreeElement which represents an attribute with the provided 
         * namespace and name.
         *  
         * @param namespace
         * @param name
         * @return A new instance of a TreeElement
         */
        public static TreeElement constructAttributeElement(String namespace_, String name)
        {
            TreeElement element = new TreeElement(name);
            element.isAttribute = true;
            element.namespace_ = namespace_;
            element.multiplicity = TreeReference.INDEX_ATTRIBUTE;
            return element;
        }

        public Boolean isLeaf()
        {
            return (children.Count == 0);
        }

        public Boolean isChildable()
        {
            return (value == null);
        }

        public void setValue(IAnswerData value)
        {
            if (isLeaf())
            {
                this.value = value;
            }
            else
            {
                throw new SystemException("Can't set data value for node that has children!");
            }
        }

        public TreeElement getChild(String name, int multiplicity)
        {
            if (name.Equals(TreeReference.NAME_WILDCARD))
            {
                if (multiplicity == TreeReference.INDEX_TEMPLATE || this.children.Count < multiplicity + 1)
                {
                    return null;
                }
                return (TreeElement)this.children[multiplicity]; //droos: i'm suspicious of this
            }
            else
            {
                for (int i = 0; i < this.children.Count; i++)
                {
                    TreeElement child = (TreeElement)this.children[i];
                    if (name.Equals(child.getName()) && child.getMult() == multiplicity)
                    {
                        return child;
                    }
                }
            }

            return null;
        }

        /**
         * 
         * Get all the child nodes of this element, with specific name
         * 
         * @param name
         * @return
         */
        public List<TreeElement> getChildrenWithName(String name)
        {
            return getChildrenWithName(name, false);
        }

        private List<TreeElement> getChildrenWithName(String name, Boolean includeTemplate)
        {
            List<TreeElement> v = new List<TreeElement>();

            for (int i = 0; i < this.children.Count; i++)
            {
                TreeElement child = (TreeElement)this.children[i];
                if ((child.getName().Equals(name) || name.Equals(TreeReference.NAME_WILDCARD))
                        && (includeTemplate || child.multiplicity != TreeReference.INDEX_TEMPLATE))
                    v.Add(child);
            }

            return v;
        }

        public int getNumChildren()
        {
            return this.children.Count;
        }

        public TreeElement getChildAt(int i)
        {
            return (TreeElement)children[i];
        }

        /**
         * Add a child to this element
         * 
         * @param child
         */
        public void addChild(TreeElement child)
        {
            addChild(child, false);
        }

        private void addChild(TreeElement child, Boolean checkDuplicate)
        {
            if (!isChildable())
            {
                throw new SystemException("Can't add children to node that has data value!");
            }

            if (child.multiplicity == TreeReference.INDEX_UNBOUND)
            {
                throw new SystemException("Cannot add child with an unbound index!");
            }

            if (checkDuplicate)
            {
                TreeElement existingChild = getChild(child.name, child.multiplicity);
                if (existingChild != null)
                {
                    throw new SystemException("Attempted to add duplicate child!");
                }
            }

            // try to keep things in order
            int i = children.Count;
            if (child.getMult() == TreeReference.INDEX_TEMPLATE)
            {
                TreeElement anchor = getChild(child.getName(), 0);
                if (anchor != null)
                    i = children.IndexOf(anchor);
            }
            else
            {
                TreeElement anchor = getChild(child.getName(),
                        (child.getMult() == 0 ? TreeReference.INDEX_TEMPLATE : child.getMult() - 1));
                if (anchor != null)
                    i = children.IndexOf(anchor) + 1;
            }
            children.Insert(i, child);
            child.setParent(this);

            child.setRelevant(isRelevant(), true);
            child.setEnabled(isEnabled(), true);
        }

        public void removeChild(TreeElement child)
        {
            children.Remove(child);
        }

        public void removeChild(String name, int multiplicity)
        {
            TreeElement child = getChild(name, multiplicity);
            if (child != null)
            {
                removeChild(child);
            }
        }

        public void removeChildren(String name)
        {
            removeChildren(name, false);
        }

        public void removeChildren(String name, Boolean includeTemplate)
        {
            List<TreeElement> v = getChildrenWithName(name, includeTemplate);
            for (int i = 0; i < v.Count; i++)
            {
                removeChild((TreeElement)v[i]);
            }
        }

        public void removeChildAt(int i)
        {
            children.RemoveAt(i);

        }

        public int getChildMultiplicity(String name)
        {
            return getChildrenWithName(name, false).Count;
        }

        public TreeElement shallowCopy()
        {
            TreeElement newNode = new TreeElement(name, multiplicity);
            newNode.parent = parent;
            newNode.repeatable = repeatable;
            newNode.dataType = dataType;
            newNode.relevant = relevant;
            newNode.required = required;
            newNode.enabled = enabled;
            newNode.constraint = constraint;
            newNode.preloadHandler = preloadHandler;
            newNode.preloadParams = preloadParams;

            newNode.setAttributesFromSingleStringVector(getSingleStringAttributeVector());
            if (value != null)
            {
                newNode.value = (IAnswerData)value.Clone();
            }

            newNode.children = children;
            return newNode;
        }

        public TreeElement deepCopy(Boolean includeTemplates) {
		TreeElement newNode = shallowCopy();

		newNode.children = new ArrayList();
		for (int i = 0; i < children.Count; i++) {
			TreeElement child = (TreeElement) children[i];
			if (includeTemplates || child.getMult() != TreeReference.INDEX_TEMPLATE) {
				newNode.addChild(child.deepCopy(includeTemplates));
			}
		}

		return newNode;
	}

        /* ==== MODEL PROPERTIES ==== */

        // factoring inheritance rules
        public Boolean isRelevant()
        {
            return relevantInherited && relevant;
        }

        // factoring in inheritance rules
        public Boolean isEnabled()
        {
            return enabledInherited && enabled;
        }

        /* ==== SPECIAL SETTERS (SETTERS WITH SIDE-EFFECTS) ==== */

        public Boolean setAnswer(IAnswerData answer)
        {
            if (value != null || answer != null)
            {
                setValue(answer);
                alertStateObservers(FormElementStateListener_Fields.CHANGE_DATA);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void setRequired(Boolean required)
        {
            if (this.required != required)
            {
                this.required = required;
                alertStateObservers(FormElementStateListener_Fields.CHANGE_REQUIRED);
            }
        }

        public void setRelevant(Boolean relevant)
        {
            setRelevant(relevant, false);
        }

        private void setRelevant(Boolean relevant, Boolean inherited) {
		Boolean oldRelevancy = isRelevant();
		if (inherited) {
			this.relevantInherited = relevant;
		} else {
			this.relevant = relevant;
		}

		if (isRelevant() != oldRelevancy) {
			for (int i = 0; i < children.Count; i++) {
				((TreeElement) children[i]).setRelevant(isRelevant(),
						true);
			}
			
			for(int i = 0 ; i < attributes.Count; ++i ) {
				attributes[i].setRelevant(isRelevant(), true);
			}
			alertStateObservers(FormElementStateListener_Fields.CHANGE_RELEVANT);
		}
	}

        public void setEnabled(Boolean enabled)
        {
            setEnabled(enabled, false);
        }

        public void setEnabled(Boolean enabled, Boolean inherited) {
		Boolean oldEnabled = isEnabled();
		if (inherited) {
			this.enabledInherited = enabled;
		} else {
			this.enabled = enabled;
		}

		if (isEnabled() != oldEnabled) {
			for (int i = 0; i < children.Count; i++) {
				((TreeElement) children[i]).setEnabled(isEnabled(),
						true);
			}
			alertStateObservers(FormElementStateListener_Fields.CHANGE_ENABLED);
		}
	}

        /* ==== OBSERVER PATTERN ==== */

        public void registerStateObserver(FormElementStateListener qsl)
        {
            if (observers == null)
                observers = new ArrayList();

            if (!observers.Contains(qsl))
            {
                observers.Add(qsl);
            }
        }

        public void unregisterStateObserver(FormElementStateListener qsl)
        {
            if (observers != null)
            {
                observers.Remove(qsl);
                if (observers.Count<1)
                    observers = null;
            }
        }

        public void unregisterAll()
        {
            observers = null;
        }

        public void alertStateObservers(int changeFlags)
        {
            if (observers != null)
            {
                for (IEnumerator e = observers.GetEnumerator(); e.MoveNext(); )
                    ((FormElementStateListener)e.Current)
                            .formElementStateChanged(this, changeFlags);
            }
        }

        /* ==== VISITOR PATTERN ==== */

        /**
         * Visitor pattern acceptance method.
         * 
         * @param visitor
         *            The visitor traveling this tree
         */
        public void accept(ITreeVisitor visitor)
        {
            visitor.visit(this);

            IEnumerator en = children.GetEnumerator();
            while (en.MoveNext())
            {
                ((TreeElement)en.Current).accept(visitor);
            }

        }

        /* ==== Attributes ==== */

        /**
         * Returns the number of attributes of this element.
         */
        public int getAttributeCount()
        {
            return attributes.Count;
        }

        /**
         * get namespace of attribute at 'index' in the vector
         * 
         * @param index
         * @return String
         */
        public String getAttributeNamespace(int index)
        {
            return attributes[index].namespace_;
        }

        /**
         * get name of attribute at 'index' in the vector
         * 
         * @param index
         * @return String
         */
        public String getAttributeName(int index)
        {
            return attributes[index].name;
        }

        /**
         * get value of attribute at 'index' in the vector
         * 
         * @param index
         * @return String
         */
        public String getAttributeValue(int index)
        {
            return getAttributeValue(attributes[index]);
        }

        /**
         * Get the String value of the provided attribute 
         * 
         * @param attribute
         * @return
         */
        private String getAttributeValue(TreeElement attribute)
        {
            if (attribute.getValue() == null)
            {
                return null;
            }
            else
            {
                return attribute.getValue().uncast().String;
            }
        }


        /**
         * Retrieves the TreeElement representing the attribute at
         * the provided namespace and name, or null if none exists.
         * 
         * If 'null' is provided for the namespace, it will match the first
         * attribute with the matching name.
         * 
         * @param index
         * @return TreeElement
         */
        public TreeElement getAttribute(String namespace_, String name)
        {
            foreach (TreeElement attribute in attributes)
            {
                if (attribute.getName().Equals(name) && (namespace_ == null || namespace_.Equals(attribute.namespace_)))
                {
                    return attribute;
                }
            }
            return null;
        }

        /**
         * get value of attribute with namespace:name' in the vector
         * 
         * @param index
         * @return String
         */
        public String getAttributeValue(String namespace_, String name)
        {
            TreeElement element = getAttribute(namespace_, name);
            return element == null ? null : getAttributeValue(element);
        }

        /**
         * Sets the given attribute; a value of null removes the attribute
         * 
         * */
        public void setAttribute(String namespace_, String name, String value)
        {

            for (int i = attributes.Count - 1; i >= 0; i--)
            {
                TreeElement attribut = attributes[i];
                if (attribut.name.Equals(name) && (namespace_ == null || namespace_.Equals(attribut.namespace_)))
                {
                    if (value == null)
                    {
                        attributes.RemoveAt(i);
                    }
                    else
                    {
                        attribut.setValue(new UncastData(value));
                    }
                    return;
                }
            }

            if (namespace_ == null) { namespace_ = ""; }

            TreeElement attr = TreeElement.constructAttributeElement(namespace_, name);
            attr.setValue(new UncastData(value));
            attr.setParent(this);

            attributes.Add(attr);
        }

        /**
         * A method for producing a vector of single strings - from the current
         * attribute vector of string [] arrays.
         * 
         * @return
         */
        public ArrayList getSingleStringAttributeVector()
        {
            ArrayList strings = new ArrayList();
            if (attributes.Count == 0)
                return null;
            else
            {
                for (int i = 0; i < this.attributes.Count; i++)
                {
                    TreeElement attribute = attributes[i];
                    String value = getAttributeValue(attribute);
                    if (attribute.namespace_ == null || attribute.namespace_ == "")
                        strings.Add(attribute.getName()+ "=" + value);
                    else
                        strings.Add(attribute.namespace_ + ":" + attribute.getName()
                                + "=" + value);
                }
                return strings;
            }
        }

      /*  public List<SelectChoice> getSingleStringAttributeVector()
        {
            List<SelectChoice> strings = new List<SelectChoice>();
            if (attributes.Count == 0)
                return null;
            else
            {
                for (int i = 0; i < this.attributes.Count; i++)
                {
                    TreeElement attribute = attributes[i];
                    String value = getAttributeValue(attribute);
                    if (attribute.namespace_ == null || attribute.namespace_ == "")
                        strings.Add((attribute.getName() + "=" + value);
                    else
                        strings.Add(attribute.namespace_ + ":" + attribute.getName()
                                + "=" + value);
                }
                return strings;
            }
        }*/

        /**
         * Method to repopulate the attribute vector from a vector of singleStrings
         * 
         * @param attStrings
         */
        public void setAttributesFromSingleStringVector(ArrayList attStrings)
        {
            this.attributes = new List<TreeElement>(0);
            if (attStrings != null)
            {
                for (int i = 0; i < attStrings.Count; i++)
                {
                    addSingleAttribute(i, attStrings);
                }
            }
        }

        private void addSingleAttribute(int i, ArrayList attStrings)
        {
            String att = (String)attStrings[i];
            String[] array = new String[3];
            int start = 0;
            // get namespace

            int pos = -1;

            // Clayton Sims - Jun 1, 2009 : Updated this code:
            //	We want to find the _last_ possible ':', not the
            // first one. Namespaces can have URLs in them.
            //int pos = att.indexOf(":");
            while (att.IndexOf(":", pos + 1) != -1)
            {
                pos = att.IndexOf(":", pos + 1);
            }
            if (pos == -1)
            {
                array[0] = null;
                start = 0;
            }
            else
            {
                array[0] = att.Substring(start, pos);
                start = ++pos;
            }
            // get attribute name
            pos = att.IndexOf("=");
            array[1] = att.Substring(start, pos);
            start = ++pos;
            array[2] = att.Substring(start);
            this.setAttribute(array[0], array[1], array[2]);
        }

        /* ==== SERIALIZATION ==== */

        /*
         * TODO:
         * 
         * this new serialization scheme is kind of lame. ideally, we shouldn't have
         * to sub-class TreeElement at all; we should have an API that can
         * seamlessly represent complex data model objects (like weight history or
         * immunizations) as if they were explicity XML subtrees underneath the
         * parent TreeElement
         * 
         * failing that, we should wrap this scheme in an ExternalizableWrapper
         */

        /*
         * (non-Javadoc)
         * 
         * @see
         * org.javarosa.core.services.storage.utilities.Externalizable#readExternal
         * (java.io.DataInputStream)
         */
        public void readExternal(BinaryReader in_renamed, PrototypeFactory pf)
        {
            name = ExtUtil.nullIfEmpty(ExtUtil.readString(in_renamed));
            multiplicity = ExtUtil.readInt(in_renamed);
            repeatable = ExtUtil.readBool(in_renamed);
            value = (IAnswerData)ExtUtil.read(in_renamed, new ExtWrapNullable(new ExtWrapTagged()), pf);

            // children = ExtUtil.nullIfEmpty((Vector)ExtUtil.read(in, new
            // ExtWrapList(TreeElement.class), pf));

            // Jan 22, 2009 - csims@dimagi.com
            // old line: children = ExtUtil.nullIfEmpty((Vector)ExtUtil.read(in, new
            // ExtWrapList(TreeElement.class), pf));
            // New Child deserialization
            // 1. read null status as boolean
            // 2. read number of children
            // 3. for i < number of children
            // 3.1 if read boolean true , then create TreeElement and deserialize
            // directly.
            // 3.2 if read boolean false then create tagged element and deserialize
            // child
            if (!ExtUtil.readBool(in_renamed))
            {
                // 1.
                children = null;
            }
            else
            {
                children = new ArrayList();
                // 2.
                int numChildren = (int)ExtUtil.readNumeric(in_renamed);
                // 3.
                for (int i = 0; i < numChildren; ++i)
                {
                    Boolean normal = ExtUtil.readBool(in_renamed);
                    TreeElement child;

                    if (normal)
                    {
                        // 3.1
                        child = new TreeElement();
                        child.readExternal(in_renamed, pf);
                    }
                    else
                    {
                        // 3.2
                        child = (TreeElement)ExtUtil.read(in_renamed, new ExtWrapTagged(), pf);
                    }
                    child.setParent(this);
                    children.Add(child);
                }
            }

            // end Jan 22, 2009

            dataType = ExtUtil.readInt(in_renamed);
            relevant = ExtUtil.readBool(in_renamed);
            required = ExtUtil.readBool(in_renamed);
            enabled = ExtUtil.readBool(in_renamed);
            relevantInherited = ExtUtil.readBool(in_renamed);
            enabledInherited = ExtUtil.readBool(in_renamed);
            constraint = (Constraint)ExtUtil.read(in_renamed, new ExtWrapNullable(
                    typeof(Constraint)), pf);
            preloadHandler = ExtUtil.nullIfEmpty(ExtUtil.readString(in_renamed));
            preloadParams = ExtUtil.nullIfEmpty(ExtUtil.readString(in_renamed));

            ArrayList attStrings = ExtUtil.nullIfEmpty((ArrayList)ExtUtil.read(in_renamed,
                    new ExtWrapList(typeof(String)), pf));
            setAttributesFromSingleStringVector(attStrings);
        }

        /*
         * (non-Javadoc)
         * 
         * @see
         * org.javarosa.core.services.storage.utilities.Externalizable#writeExternal
         * (java.io.DataOutputStream)
         */
        public void writeExternal(BinaryWriter out_renamed)
        {
            ExtUtil.writeString(out_renamed, ExtUtil.emptyIfNull(name));
            ExtUtil.writeNumeric(out_renamed, multiplicity);
            ExtUtil.writeBool(out_renamed, repeatable);
            ExtUtil.write(out_renamed, new ExtWrapNullable(value == null ? null : new ExtWrapTagged(value)));

            // Jan 22, 2009 - csims@dimagi.com
            // old line: ExtUtil.write(out, new
            // ExtWrapList(ExtUtil.emptyIfNull(children)));
            // New Child serialization
            // 1. write null status as boolean
            // 2. write number of children
            // 3. for all child in children
            // 3.1 if child type == TreeElement write boolean true , then serialize
            // directly.
            // 3.2 if child type != TreeElement, write boolean false, then tagged
            // child
            if (children == null)
            {
                // 1.
                ExtUtil.writeBool(out_renamed, false);
            }
            else
            {
                // 1.
                ExtUtil.writeBool(out_renamed, true);
                // 2.
                ExtUtil.writeNumeric(out_renamed, children.Count);
                // 3.
                IEnumerator en = children.GetEnumerator();
                while (en.MoveNext())
                {
                    TreeElement child = (TreeElement)en.Current;
                    if (child.GetType() == typeof(TreeElement))
                    {
                        // 3.1
                        ExtUtil.writeBool(out_renamed, true);
                        child.writeExternal(out_renamed);
                    }
                    else
                    {
                        // 3.2
                        ExtUtil.writeBool(out_renamed, false);
                        ExtUtil.write(out_renamed, new ExtWrapTagged(child));
                    }
                }
            }

            // end Jan 22, 2009

            ExtUtil.writeNumeric(out_renamed, dataType);
            ExtUtil.writeBool(out_renamed, relevant);
            ExtUtil.writeBool(out_renamed, required);
            ExtUtil.writeBool(out_renamed, enabled);
            ExtUtil.writeBool(out_renamed, relevantInherited);
            ExtUtil.writeBool(out_renamed, enabledInherited);
            ExtUtil.write(out_renamed, new ExtWrapNullable(constraint)); // TODO: inefficient for repeats
            ExtUtil.writeString(out_renamed, ExtUtil.emptyIfNull(preloadHandler));
            ExtUtil.writeString(out_renamed, ExtUtil.emptyIfNull(preloadParams));

            ArrayList attStrings = getSingleStringAttributeVector();
            ArrayList al = ExtUtil.emptyIfNull(attStrings);
            ExtUtil.write(out_renamed, new ExtWrapList(al));
        }

        //rebuilding a node from an imported instance
        //  there's a lot of error checking we could do on the received instance, but it's
        //  easier to just ignore the parts that are incorrect
        public void populate(TreeElement incoming, FormDef f) {
		if (this.isLeaf()) {
			// check that incoming doesn't have children?

			IAnswerData value = incoming.getValue();
			if (value == null) {
				this.setValue(null);
			} else if (this.dataType == Constants.DATATYPE_TEXT
					|| this.dataType == Constants.DATATYPE_NULL) {
				this.setValue(value); // value is a StringData
			} else {
				String textVal = value.ToString();
				IAnswerData typedVal = RestoreUtils.xfFact.parseData(textVal, this.dataType, this.getRef(), f);
				this.setValue(typedVal);
			}
		} else {
			ArrayList names = new ArrayList();
			for (int i = 0; i < this.getNumChildren(); i++) {
				TreeElement child = this.getChildAt(i);
				if (!names.Contains(child.getName())) {
					names.Add(child.getName());
				}
			}

			// remove all default repetitions from skeleton data model (_preserving_ templates, though)
			for (int i = 0; i < this.getNumChildren(); i++) {
				TreeElement child = this.getChildAt(i);
				if (child.repeatable && child.getMult() != TreeReference.INDEX_TEMPLATE) {
					this.removeChildAt(i);
					i--;
				}
			}

			// make sure ordering is preserved (needed for compliance with xsd schema)
			if (this.getNumChildren() != names.Count) {
				throw new SystemException("sanity check failed");
			}
			
			for (int i = 0; i < this.getNumChildren(); i++) {
				TreeElement child = this.getChildAt(i);
				String expectedName = (String) names[i];

				if (!child.getName().Equals(expectedName)) {
					TreeElement child2 = null;
					int j;

					for (j = i + 1; j < this.getNumChildren(); j++) {
						child2 = this.getChildAt(j);
						if (child2.getName().Equals(expectedName)) {
							break;
						}
					}
					if (j == this.getNumChildren()) {
						throw new SystemException("sanity check failed");
					}

					this.removeChildAt(j);
					this.children.Insert(i,child2);
				}
			}
			// java i hate you so much

			for (int i = 0; i < this.getNumChildren(); i++) {
				TreeElement child = this.getChildAt(i);
				List<TreeElement> newChildren = incoming.getChildrenWithName(child.getName());

				if (child.repeatable) {
				    for (int k = 0; k < newChildren.Count; k++) {
				        TreeElement newChild = child.deepCopy(true);
				        newChild.setMult(k);
				        this.children.Insert(i + k + 1,newChild);
				        newChild.populate((TreeElement)newChildren[k], f);
				    }
				    i += newChildren.Count;
				} else {

					if (newChildren.Count == 0) {
						child.setRelevant(false);
					} else {
						child.populate((TreeElement)newChildren[0], f);
					}
				}
			}
		}
	}

        //this method is for copying in the answers to an itemset. the template node of the destination
        //is used for overall structure (including data types), and the itemset source node is used for
        //raw data. note that data may be coerced across types, which may result in type conversion error
        //very similar in structure to populate()
        public void populateTemplate(TreeElement incoming, FormDef f) {
		if (this.isLeaf()) {
			IAnswerData value = incoming.value;
			if (value == null) {
				this.setValue(null);
			} else {
				Type classType = CompactInstanceWrapper.classForDataType(this.dataType);
				
				if (classType == null) {
					throw new SystemException("data type [" + value.GetType().Name + "] not supported inside itemset");
				} else if (classType.IsAssignableFrom(value.GetType()) &&
							!(value is SelectOneData || value is SelectMultiData)) {
					this.setValue(value);
				} else {
					String textVal = RestoreUtils.xfFact.serializeData(value);
					IAnswerData typedVal = RestoreUtils.xfFact.parseData(textVal, this.dataType, this.getRef(), f);
					this.setValue(typedVal);
				}
			}
		} else {
			for (int i = 0; i < this.getNumChildren(); i++) {
				TreeElement child = this.getChildAt(i);
				List<TreeElement> newChildren = incoming.getChildrenWithName(child.getName());

				if (child.repeatable) {
				    for (int k = 0; k < newChildren.Count; k++) {
				    	TreeElement template = f.Instance.getTemplate(child.getRef());
				        TreeElement newChild = template.deepCopy(false);
				        newChild.setMult(k);
				        this.children.Insert(i + k + 1,newChild);
				        newChild.populateTemplate((TreeElement)newChildren[k], f);
				    }
				    i += newChildren.Count;
				} else {
					child.populateTemplate((TreeElement)newChildren[0], f);
				}
			}
		}
	}

        //return the tree reference that corresponds to this tree element
        public TreeReference getRef() {
		TreeElement elem = this;
		TreeReference ref_ = TreeReference.selfRef();
		
		while (elem != null) {
			TreeReference step;
			
			if (elem.name != null) {
				step = TreeReference.selfRef();
				step.add(elem.name, elem.multiplicity);
			} else {
				step = TreeReference.rootRef();
			}
						
			ref_ = ref_.parent(step);
			elem = elem.parent;
		}
        return ref_;
	}

        public int getDepth()
        {
            TreeElement elem = this;
            int depth = 0;

            while (elem.name != null)
            {
                depth++;
                elem = elem.parent;
            }

            return depth;
        }

        public String getPreloadHandler()
        {
            return preloadHandler;
        }

        public Constraint getConstraint()
        {
            return constraint;
        }

        public void setPreloadHandler(String preloadHandler)
        {
            this.preloadHandler = preloadHandler;
        }

        public void setConstraint(Constraint constraint)
        {
            this.constraint = constraint;
        }

        public String getPreloadParams()
        {
            return preloadParams;
        }

        public void setPreloadParams(String preloadParams)
        {
            this.preloadParams = preloadParams;
        }

        public String getName()
        {
            return name;
        }

        public void setName(String name)
        {
            this.name = name;
        }

        public int getMult()
        {
            return multiplicity;
        }

        public void setMult(int multiplicity)
        {
            this.multiplicity = multiplicity;
        }

        public void setParent(TreeElement parent)
        {
            this.parent = parent;
        }

        public TreeElement getParent()
        {
            return parent;
        }

        public IAnswerData getValue()
        {
            return value;
        }

    }
}