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
using org.javarosa.core.model.instance.utils;
using org.javarosa.core.model.util.restorable;
using org.javarosa.core.model.utils;
using org.javarosa.core.services.storage;
using org.javarosa.core.util.externalizable;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
namespace org.javarosa.core.model.instance
{


    /**
     * This class represents the xform model instance
     */
    public class FormInstance : Persistable, Restorable
    {

        public  const String STORAGE_KEY = "FORMDATA";

        /** The root of this tree */
        private TreeElement root = new TreeElement();
        // represents '/'; always has one and only one child -- the top-level
        // instance data node
        // this node is never returned or manipulated directly

        /** The name for this data model */
        private String name;

        /** The integer Id of the model */
        private int id;

        /** The ID of the form that this is a model for */
        private int formId;

        /** The date that this model was taken and recorded */
        private DateTime dateSaved;

        public String schema;
        public String formVersion;
        public String uiVersion;

        private Hashtable namespaces = new Hashtable();

        public FormInstance()
        {
        }

        /**
         * Creates a new data model using the root given.
         * 
         * @param root
         *            The root of the tree for this data model.
         */
        public FormInstance(TreeElement root)
        {
           ID = -1;
            setFormId(-1);
            setRoot(root);
        }

        /**
         * Sets the root element of this Model's tree
         * 
         * @param root
         *            The root of the tree for this data model.
         */
        public void setRoot(TreeElement topLevel)
        {
            root = new TreeElement();
            if (topLevel != null)
                root.addChild(topLevel);
        }

        /**
         * TODO: confusion between root and its first child?
         * 
         * @return This model's root tree element
         */
        public TreeElement getRoot()
        {

            if (root.getNumChildren() == 0)
                throw new SystemException("root node has no children");

            return root.getChildAt(0);
        }

        // throws classcastexception if not using XPathReference
        public  static TreeReference unpackReference(IDataReference ref_)
        {
            return (TreeReference)ref_.Reference;
        }
        public TreeReference unpackReference2(IDataReference ref_)
        {
            return (TreeReference)ref_.Reference;
        }

        public TreeReference copyNode(TreeReference from, TreeReference to)
        {
            if (!from.isAbsolute())
            {
                throw new InvalidReferenceException("Source reference must be absolute for copying", from);
            }

            TreeElement src = resolveReference(from);
            if (src == null)
            {
                throw new InvalidReferenceException("Null Source reference while attempting to copy node", from);
            }

            return copyNode(src, to).getRef();
        }

        // for making new repeat instances; 'from' and 'to' must be unambiguous
        // references EXCEPT 'to' may be ambiguous at its final step
        // return true is successfully copied, false otherwise
        public TreeElement copyNode(TreeElement src, TreeReference to)
        {
            if (!to.isAbsolute())
                throw new InvalidReferenceException("Destination reference must be absolute for copying", to);

            // strip out dest node info and get dest parent
            String dstName = to.getNameLast();
            int dstMult = to.getMultLast();
            TreeReference toParent = to.getParentRef();

            TreeElement parent = resolveReference(toParent);
            if (parent == null)
            {
                throw new InvalidReferenceException("Null parent reference whle attempting to copy", toParent);
            }
            if (!parent.isChildable())
            {
                throw new InvalidReferenceException("Invalid Parent Node: cannot accept children.", toParent);
            }

            if (dstMult == TreeReference.INDEX_UNBOUND)
            {
                dstMult = parent.getChildMultiplicity(dstName);
            }
            else if (parent.getChild(dstName, dstMult) != null)
            {
                throw new InvalidReferenceException("Destination already exists!", to);
            }

            TreeElement dest = src.deepCopy(false);
            dest.setName(dstName);
            dest.multiplicity = dstMult;
            parent.addChild(dest);
            return dest;
        }

        public void copyItemsetNode(TreeElement copyNode, TreeReference destRef, FormDef f)
        {
            TreeElement templateNode = getTemplate(destRef);
            TreeElement newNode = this.copyNode(templateNode, destRef);
            newNode.populateTemplate(copyNode, f);
        }

        // don't think this is used anymore
        public IAnswerData getDataValue(IDataReference questionReference)
        {
            TreeElement element = resolveReference(questionReference);
            if (element != null)
            {
                return element.getValue();
            }
            else
            {
                return null;
            }
        }

        // take a ref that unambiguously refers to a single node and return that node
        // return null if ref is ambiguous, node does not exist, ref is relative, or ref is '/'
        // can be used to retrieve template nodes
        public TreeElement resolveReference(TreeReference ref_)
        {
            if (!ref_.isAbsolute())
            {
                return null;
            }

            TreeElement node = root;
            for (int i = 0; i < ref_.size(); i++)
            {
                String name = ref_.getName(i);
                int mult = ref_.getMultiplicity(i);

                if (mult == TreeReference.INDEX_ATTRIBUTE)
                {
                    //Should we possibly just return here? 
                    //I guess technically we could step back...
                    node = node.getAttribute(null, name);
                    continue;
                }
                if (mult == TreeReference.INDEX_UNBOUND)
                {
                    if (node.getChildMultiplicity(name) == 1)
                    {
                        mult = 0;
                    }
                    else
                    {
                        // reference is not unambiguous
                        node = null;
                        break;
                    }
                }

                node = node.getChild(name, mult);
                if (node == null)
                    break;
            }

            return (node == root ? null : node); // never return a reference to '/'
        }

        // same as resolveReference but return a vector containing all interstitial
        // nodes: top-level instance data node first, and target node last
        // returns null in all the same situations as resolveReference EXCEPT ref
        // '/' will instead return empty vector
        public ArrayList explodeReference(TreeReference ref_)
        {
            if (!ref_.isAbsolute())
                return null;

            ArrayList nodes = new ArrayList();
            TreeElement cur = root;
            for (int i = 0; i < ref_.size(); i++)
            {
                String name = ref_.getName(i);
                int mult = ref_.getMultiplicity(i);

                //If the next node down the line is an attribute
                if (mult == TreeReference.INDEX_ATTRIBUTE)
                {
                    //This is not the attribute we're testing
                    if (cur != root)
                    {
                        //Add the current node
                        nodes.Add(cur);
                    }
                    cur = cur.getAttribute(null, name);
                }

                //Otherwise, it's another child element
                else
                {
                    if (mult == TreeReference.INDEX_UNBOUND)
                    {
                        if (cur.getChildMultiplicity(name) == 1)
                        {
                            mult = 0;
                        }
                        else
                        {
                            // reference is not unambiguous
                            return null;
                        }
                    }

                    if (cur != root)
                    {
                        nodes.Add(cur);
                    }

                    cur = cur.getChild(name, mult);
                    if (cur == null)
                    {
                        return null;
                    }
                }
            }
            return nodes;
        }

        public List<TreeReference> expandReference(TreeReference ref_)
        {
            return expandReference(ref_, false);
        }

        // take in a potentially-ambiguous ref, and return a vector of refs for all nodes that match the passed-in ref
        // meaning, search out all repeated nodes that match the pattern of the passed-in ref
        // every ref in the returned vector will be unambiguous (no index will ever be INDEX_UNBOUND)
        // does not return template nodes when matching INDEX_UNBOUND, but will match templates when INDEX_TEMPLATE is explicitly set
        // return null if ref is relative, otherwise return vector of refs (but vector will be empty is no refs match)
        // '/' returns {'/'}
        // can handle sub-repetitions (e.g., {/a[1]/b[1], /a[1]/b[2], /a[2]/b[1]})
        public List<TreeReference> expandReference(TreeReference ref_, Boolean includeTemplates)
        {
            if (!ref_.isAbsolute())
                return null;

            List<TreeReference> v = new List<TreeReference>();
            expandReference(ref_, root, v, includeTemplates);
            return v;
        }

        // recursive helper function for expandReference
        // sourceRef: original path we're matching against
        // node: current node that has matched the sourceRef thus far
        // templateRef: explicit path that refers to the current node
        // refs: Vector to collect matching paths; if 'node' is a target node that
        // matches sourceRef, templateRef is added to refs
        private void expandReference(TreeReference sourceRef, TreeElement node, List<TreeReference> refs, Boolean includeTemplates)
        {
            int depth = node.getDepth();

            if (depth == sourceRef.size())
            {
                refs.Add(node.getRef());
            }
            else
            {
                String name = sourceRef.getName(depth);
                int mult = sourceRef.getMultiplicity(depth);
                List<TreeElement> set = new List<TreeElement>();

                if (node.getNumChildren() > 0)
                {
                    if (mult == TreeReference.INDEX_UNBOUND)
                    {
                        int count = node.getChildMultiplicity(name);
                        for (int i = 0; i < count; i++)
                        {
                            TreeElement child = node.getChild(name, i);
                            if (child != null)
                            {
                                set.Add(child);
                            }
                            else
                            {
                                throw new InvalidOperationException(); // missing/non-sequential
                                // nodes
                            }
                        }
                        if (includeTemplates)
                        {
                            TreeElement template = node.getChild(name, TreeReference.INDEX_TEMPLATE);
                            if (template != null)
                            {
                                set.Add(template);
                            }
                        }
                    }
                    else if (mult != TreeReference.INDEX_ATTRIBUTE)
                    {
                        //TODO: Make this test mult >= 0?
                        //If the multiplicity is a simple integer, just get
                        //the appropriate child
                        TreeElement child = node.getChild(name, mult);
                        if (child != null)
                        {
                            set.Add(child);
                        }
                    }
                }

                if (mult == TreeReference.INDEX_ATTRIBUTE)
                {
                    TreeElement attribute = node.getAttribute(null, name);
                    set.Add(attribute);
                }

                for (IEnumerator e = set.GetEnumerator(); e.MoveNext(); )
                {
                    expandReference(sourceRef, (TreeElement)e.Current, refs, includeTemplates);
                }
            }
        }

        // retrieve the template node for a given repeated node ref may be ambiguous
        // return null if node is not repeatable
        // assumes templates are built correctly and obey all data model validity rules
        public TreeElement getTemplate(TreeReference ref_)
        {
            TreeElement node = getTemplatePath(ref_);
            return (node == null ? null : node.repeatable ? node : null);
        }

        public TreeElement getTemplatePath(TreeReference ref_)
        {
            if (!ref_.isAbsolute())
                return null;

            TreeElement node = root;
            for (int i = 0; i < ref_.size(); i++)
            {
                String name = ref_.getName(i);

                if (ref_.getMultiplicity(i) == TreeReference.INDEX_ATTRIBUTE)
                {
                    node = node.getAttribute(null, name);
                }
                else
                {

                    TreeElement newNode = node.getChild(name, TreeReference.INDEX_TEMPLATE);
                    if (newNode == null)
                    {
                        newNode = node.getChild(name, 0);
                    }
                    if (newNode == null)
                    {
                        return null;
                    }
                    node = newNode;
                }
            }

            return node;
        }

        // determine if nodes are homogeneous, meaning their descendant structure is 'identical' for repeat purposes
        // identical means all children match, and the children's children match, and so on
        // repeatable children are ignored; as they do not have to exist in the same quantity for nodes to be homogeneous
        // however, the child repeatable nodes MUST be verified amongst themselves for homogeneity later
        // this function ignores the names of the two nodes
        public static Boolean isHomogeneous(TreeElement a, TreeElement b)
        {
            if (a.isLeaf() && b.isLeaf())
            {
                return true;
            }
            else if (a.isChildable() && b.isChildable())
            {
                // verify that every (non-repeatable) node in a exists in b and vice
                // versa
                for (int k = 0; k < 2; k++)
                {
                    TreeElement n1 = (k == 0 ? a : b);
                    TreeElement n2 = (k == 0 ? b : a);

                    for (int i = 0; i < n1.getNumChildren(); i++)
                    {
                        TreeElement child1 = n1.getChildAt(i);
                        if (child1.repeatable)
                            continue;
                        TreeElement child2 = n2.getChild(child1.getName(), 0);
                        if (child2 == null)
                            return false;
                        if (child2.repeatable)
                            throw new SystemException("shouldn't happen");
                    }
                }

                // compare children
                for (int i = 0; i < a.getNumChildren(); i++)
                {
                    TreeElement childA = a.getChildAt(i);
                    if (childA.repeatable)
                        continue;
                    TreeElement childB = b.getChild(childA.getName(), 0);
                    if (!isHomogeneous(childA, childB))
                        return false;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /**
         * Resolves a binding to a particular question data element
         * 
         * @param binding
         *            The binding representing a particular question
         * @return A QuestionDataElement corresponding to the binding provided. Null
         *         if none exists in this tree.
         */
        public TreeElement resolveReference(IDataReference binding)
        {
            return resolveReference(unpackReference(binding));
        }

        public void accept(IInstanceVisitor visitor)
        {
            visitor.visit(this);

            if (visitor is ITreeVisitor)
            {
                root.accept((ITreeVisitor)visitor);
            }

        }

        public void setDateSaved(DateTime dateSaved)
        {
            this.dateSaved = dateSaved;
        }

        public void setFormId(int formId)
        {
            this.formId = formId;
        }

        public DateTime getDateSaved()
        {
            return this.dateSaved;
        }

        public int getFormId()
        {
            return this.formId;
        }

        /*
         * (non-Javadoc)
         * 
         * @see
         * org.javarosa.core.services.storage.utilities.Externalizable#readExternal
         * (java.io.DataInputStream)
         */
        public void readExternal(BinaryReader in_, PrototypeFactory pf)
        {
            id = ExtUtil.readInt(in_);
            formId = ExtUtil.readInt(in_);
            name = (String)ExtUtil.read(in_, new ExtWrapNullable(typeof(String)), pf);
            schema = (String)ExtUtil.read(in_, new ExtWrapNullable(typeof(String)), pf);
            dateSaved = (DateTime)ExtUtil.read(in_, new ExtWrapNullable(typeof(DateTime)), pf);

            namespaces = (Hashtable)ExtUtil.read(in_, new ExtWrapMap(typeof(String), typeof(String)));
            setRoot((TreeElement)ExtUtil.read(in_, typeof(TreeElement), pf));
        }

        /*
         * (non-Javadoc)
         * 
         * @see
         * org.javarosa.core.services.storage.utilities.Externalizable#writeExternal
         * (java.io.DataOutputStream)
         */
        public void writeExternal(BinaryWriter out_)
        {
            ExtUtil.writeNumeric(out_, id);
            ExtUtil.writeNumeric(out_, formId);
            ExtUtil.write(out_, new ExtWrapNullable(name));
            ExtUtil.write(out_, new ExtWrapNullable(schema));
            ExtUtil.write(out_, new ExtWrapNullable(dateSaved));
            ExtUtil.write(out_, new ExtWrapMap(namespaces));
            ExtUtil.write(out_, getRoot());
        }

        public String getName()
        {
            return name;
        }

        /**
         * Sets the name of this datamodel instance
         * 
         * @param name
         *            The name to be used
         */
        public void setName(String name)
        {
            this.name = name;
        }

        public int ID
        {
            get { return id; }
            set {
                this.id = value;
            }
        }

        public TreeReference addNode(TreeReference ambigRef)
        {
            TreeReference ref_ = ambigRef.clone();
            if (createNode(ref_) != null)
            {
                return ref_;
            }
            else
            {
                return null;
            }
        }

        public TreeReference addNode(TreeReference ambigRef, IAnswerData data, int dataType)
        {
            TreeReference ref_ = ambigRef.clone();
            TreeElement node = createNode(ref_);
            if (node != null)
            {
                if (dataType >= 0)
                {
                    node.dataType = dataType;
                }

                node.setValue(data);
                return ref_;
            }
            else
            {
                return null;
            }
        }

        /*
         * create the specified node in the tree, creating all intermediary nodes at
         * each step, if necessary. if specified node already exists, return null
         * 
         * creating a duplicate node is only allowed at the final step. it will be
         * done if the multiplicity of the last step is ALL or equal to the count of
         * nodes already there
         * 
         * at intermediate steps, the specified existing node is used; if
         * multiplicity is ALL: if no nodes exist, a new one is created; if one node
         * exists, it is used; if multiple nodes exist, it's an error
         * 
         * return the newly-created node; modify ref so that it's an unambiguous ref
         * to the node
         */
        private TreeElement createNode(TreeReference ref_)
        {

            TreeElement node = root;

            for (int k = 0; k < ref_.size(); k++)
            {
                String name = ref_.getName(k);
                int count = node.getChildMultiplicity(name);
                int mult = ref_.getMultiplicity(k);

                TreeElement child;
                if (k < ref_.size() - 1)
                {
                    if (mult == TreeReference.INDEX_UNBOUND)
                    {
                        if (count > 1)
                        {
                            return null; // don't know which node to use
                        }
                        else
                        {
                            // will use existing (if one and only one) or create new
                            mult = 0;
                            ref_.setMultiplicity(k, 0);
                        }
                    }

                    // fetch
                    child = node.getChild(name, mult);
                    if (child == null)
                    {
                        if (mult == 0)
                        {
                            // create
                            child = new TreeElement(name, count);
                            node.addChild(child);
                            ref_.setMultiplicity(k, count);
                        }
                        else
                        {
                            return null; // intermediate node does not exist
                        }
                    }
                }
                else
                {
                    if (mult == TreeReference.INDEX_UNBOUND || mult == count)
                    {
                        if (k == 0 && root.getNumChildren() != 0)
                        {
                            return null; // can only be one top-level node, and it
                            // already exists
                        }

                        if (!node.isChildable())
                        {
                            return null; // current node can't have children
                        }

                        // create new
                        child = new TreeElement(name, count);
                        node.addChild(child);
                        ref_.setMultiplicity(k, count);
                    }
                    else
                    {
                        return null; // final node must be a newly-created node
                    }
                }

                node = child;
            }

            return node;
        }

        public void addNamespace(String prefix, String URI)
        {
            namespaces.Add(prefix, URI);
        }

        public String[] getNamespacePrefixes()
        {
            String[] prefixes = new String[namespaces.Count];
            int i = 0;
            for (IEnumerator en = namespaces.GetEnumerator(); en.MoveNext(); )
            {
                prefixes[i] = (String)en.Current;
                ++i;
            }
            return prefixes;
        }

        public String getNamespaceURI(String prefix)
        {
            return (String)namespaces[prefix];
        }

        public String RestorableType
        {
            get
            {
                return "form";
            }
        }

        // TODO: include whether form was sent already (or restrict always to unsent
        // forms)

        public FormInstance exportData()
        {
            FormInstance dm = RestoreUtils.createDataModel(this);
            RestoreUtils.addData(dm, "name", name);
            RestoreUtils.addData(dm, "form-id", (int)(formId));
            RestoreUtils.addData(dm, "saved-on", dateSaved,
                    Constants.DATATYPE_DATE_TIME);
            RestoreUtils.addData(dm, "schema", schema);

            /////////////
            throw new SystemException("FormInstance.exportData(): must be updated to use new transport layer");
            //		ITransportManager tm = TransportManager._();
            //		boolean sent = (tm.getModelDeliveryStatus(id, true) == TransportMessage.STATUS_DELIVERED);
            //		RestoreUtils.addData(dm, "sent", new Boolean(sent));
            /////////////

            //		for (Enumeration e = namespaces.keys(); e.hasMoreElements(); ) {
            //			String key = (String)e.nextElement();
            //			RestoreUtils.addData(dm, "namespace/" + key, namespaces.get(key));
            //		}
            //
            //		RestoreUtils.mergeDataModel(dm, this, "data");
            //		return dm;
        }

        public void templateData(FormInstance dm, TreeReference parentRef)
        {
            RestoreUtils.applyDataType(dm, "name", parentRef, typeof(String));
            RestoreUtils.applyDataType(dm, "form-id", parentRef, typeof(int));
            RestoreUtils.applyDataType(dm, "saved-on", parentRef,
                    Constants.DATATYPE_DATE_TIME);
            RestoreUtils.applyDataType(dm, "schema", parentRef, typeof(String));
            RestoreUtils.applyDataType(dm, "sent", parentRef, typeof(Boolean));
            // don't touch data for now
        }

        public void importData(FormInstance dm)
        {
            name = (String)RestoreUtils.getValue("name", dm);
            formId = ((int)RestoreUtils.getValue("form-id", dm));
            dateSaved = (DateTime)RestoreUtils.getValue("saved-on", dm);
            schema = (String)RestoreUtils.getValue("schema", dm);

            Boolean sent = RestoreUtils.getBoolean(RestoreUtils
                    .getValue("sent", dm));

            TreeElement names = dm.resolveReference(RestoreUtils.absRef("namespace", dm));
            if (names != null)
            {
                for (int i = 0; i < names.getNumChildren(); i++)
                {
                    TreeElement child = names.getChildAt(i);
                    String name_ = child.getName();
                    Object value = RestoreUtils.getValue("namespace/" + name_, dm);
                    if (value != null)
                    {
                        namespaces.Add(name_, value);
                    }
                }
            }

            /////////////
            throw new SystemException("FormInstance.importData(): must be updated to use new transport layer");
            //		if (sent) {			
            //			ITransportManager tm = TransportManager._();
            //			tm.markSent(id, false);
            //		}
            /////////////

            //		IStorageUtility forms = StorageManager.getStorage(FormDef.STORAGE_KEY);
            //		FormDef f = (FormDef)forms.read(formId);
            //		setRoot(processSavedDataModel(dm.resolveReference(RestoreUtils.absRef("data", dm)), f.getDataModel(), f));
        }

        public TreeElement processSaved(FormInstance template, FormDef f)
        {
            TreeElement fixedInstanceRoot = template.getRoot().deepCopy(true);
            TreeElement incomingRoot = root.getChildAt(0);

            if (!fixedInstanceRoot.getName().Equals(incomingRoot.getName()) || incomingRoot.getMult() != 0)
            {
                throw new SystemException("Saved form instance to restore does not match form definition");
            }

            fixedInstanceRoot.populate(incomingRoot, f);
            return fixedInstanceRoot;
        }

        public FormInstance clone()
        {
            FormInstance cloned = new FormInstance(this.getRoot().deepCopy(true));

            cloned.ID = (this.ID);
            cloned.setFormId(this.getFormId());
            cloned.setName(this.getName());
            cloned.setDateSaved(this.getDateSaved());
            cloned.schema = this.schema;
            cloned.formVersion = this.formVersion;
            cloned.uiVersion = this.uiVersion;
            cloned.namespaces = new Hashtable();
            for (IEnumerator e = this.namespaces.GetEnumerator(); e.MoveNext(); )
            {
                Object key = e.Current;
                cloned.namespaces.Add(key, this.namespaces[key]);
            }

            return cloned;
        }

    }
}