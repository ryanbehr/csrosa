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

using org.javarosa.core.log;
using org.javarosa.core.model.condition;
using org.javarosa.core.model.data;
using org.javarosa.core.model.data.helper;
using org.javarosa.core.model.instance;
using org.javarosa.core.model.util.restorable;
using org.javarosa.core.model.utils;
using org.javarosa.core.services.locale;
using org.javarosa.core.services.storage;
using org.javarosa.core.util.externalizable;
using org.javarosa.model.xform;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
namespace org.javarosa.core.model
{


    /**
     * Definition of a form. This has some meta data about the form definition and a
     * collection of groups together with question branching or skipping rules.
     * 
     * @author Daniel Kayiwa, Drew Roos
     * 
     */
    public class FormDef : IFormElement, Localizable, Persistable, IMetaData
    {
        public  const String STORAGE_KEY = "FORMDEF";
        public  const int TEMPLATING_RECURSION_LIMIT = 10;

        private ArrayList children;// <IFormElement> 
        /** A collection of group definitions. */
        private int id;
        /** The numeric unique identifier of the form definition on the local device */
        private String title;
        /** The display title of the form. */
        private String name;


        /**
         * A unique external name that is used to identify the form between machines
         */
        private Localizer localizer;
        public List<Triggerable> triggerables; // <Triggerable>; this list is topologically ordered, meaning for any tA and tB in
        //the list, where tA comes before tB, evaluating tA cannot depend on any result from evaluating tB
        private Boolean triggerablesInOrder; //true if triggerables has been ordered topologically (DON'T DELETE ME EVEN THOUGH I'M UNUSED)

        private FormInstance instance;
        private ArrayList outputFragments; // <IConditionExpr> contents of <output>
        // tags that serve as parameterized
        // arguments to captions

        public IDictionary<TreeReference, List<Triggerable>> triggerIndex;  // <TreeReference, Vector<Triggerable>>
        private IDictionary<TreeReference, Condition> conditionRepeatTargetIndex; // <TreeReference, Condition>;
        // associates repeatable
        // nodes with the Condition
        // that determines their
        // relevancy
        public EvaluationContext exprEvalContext;

        private QuestionPreloader preloader = new QuestionPreloader();

        //XML ID's cannot start with numbers, so this should never conflict
        private static String DEFAULT_SUBMISSION_PROFILE = "1";

        private Hashtable submissionProfiles;

        /**
         * 
         */
        public FormDef()
        {
            ID = -1;
            Children = null;
            triggerables = new List<Triggerable>();
            triggerablesInOrder = true;
            triggerIndex = new Dictionary<TreeReference, List<Triggerable>>();
            setEvaluationContext(new EvaluationContext());
            outputFragments = new ArrayList();
            submissionProfiles = new Hashtable();
        }


        // ---------- child elements
        public void addChild(IFormElement fe)
        {
            this.children.Add(fe);
        }

        public IFormElement getChild(int i)
        {
            if (i < this.children.Count)
                return (IFormElement)this.children[i];

            throw new IndexOutOfRangeException(
                    "FormDef: invalid child index: " + i + " only "
                            + children.Count + " children");
        }

        public IFormElement getChild(FormIndex index)
        {
            IFormElement element = this;
            while (index != null && index.isInForm())
            {
                element = element.getChild(index.getLocalIndex());
                index = index.getNextLevel();
            }
            return element;
        }

        /**
         * Dereference the form index and return a Vector of all interstitial nodes
         * (top-level parent first; index target last)
         * 
         * Ignore 'new-repeat' node for now; just return/stop at ref to
         * yet-to-be-created repeat node (similar to repeats that already exist)
         * 
         * @param index
         * @return
         */
        public ArrayList explodeIndex(FormIndex index)
        {
            System.Collections.ArrayList indexes = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList());
            System.Collections.ArrayList multiplicities = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList());
            System.Collections.ArrayList elements = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList());

            collapseIndex(index, indexes, multiplicities, elements);
            return elements;
        }

        // take a reference, find the instance node it refers to (factoring in
        // multiplicities)
        /**
         * @param index
         * @return
         */
        public TreeReference getChildInstanceRef(FormIndex index)
        {
            System.Collections.ArrayList indexes = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList());
            System.Collections.ArrayList multiplicities = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList());
            System.Collections.ArrayList elements = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList());

            collapseIndex(index, indexes, multiplicities, elements);
            return getChildInstanceRef(elements, multiplicities);
        }

        /**
         * @param elements
         * @param multiplicities
         * @return
         */
        public TreeReference getChildInstanceRef(ArrayList elements, ArrayList multiplicities)
        {
            if (elements.Count == 0)
                return null;

            // get reference for target element
            TreeReference ref_Renamed = FormInstance.unpackReference(((IFormElement)elements[elements.Count - 1]).Bind).clone();
            for (int i = 0; i < ref_Renamed.size(); i++)
            {
                //There has to be a better way to encapsulate this
                if (ref_Renamed.getMultiplicity(i) != TreeReference.INDEX_ATTRIBUTE)
                {
                    ref_Renamed.setMultiplicity(i, 0);
                }
            }

            // fill in multiplicities for repeats along the way
            for (int i = 0; i < elements.Count; i++)
            {
                IFormElement temp = (IFormElement)elements[i];
                if (temp is GroupDef && ((GroupDef)temp).Repeat)
                {
                    TreeReference repRef = FormInstance.unpackReference(temp.Bind);
                    if (repRef.isParentOf(ref_Renamed, false))
                    {
                        int repMult = ((int)multiplicities[i]);
                        ref_Renamed.setMultiplicity(repRef.size() - 1, repMult);
                    }
                    else
                    {
                        return null; // question/repeat hierarchy is not consistent
                        // with instance instance and bindings
                    }
                }
            }

            return ref_Renamed;
        }

        public void setLocalizer(Localizer l)
        {
            if (this.localizer != null)
            {
                this.localizer.unregisterLocalizable(this);
            }

            this.localizer = l;
            if (this.localizer != null)
            {
                this.localizer.registerLocalizable(this);
            }
        }

        // don't think this should ever be called(!)
        public IDataReference Bind
        {
            get { throw new SystemException("method not implemented"); }
        }

        public void setValue(IAnswerData data, TreeReference ref_Renamed)
        {
            setValue(data, ref_Renamed, instance.resolveReference(ref_Renamed));
        }

        public void setValue(IAnswerData data, TreeReference ref_Renamed, TreeElement node)
        {
            setAnswer(data, node);
            triggerTriggerables(ref_Renamed);
            //TODO: pre-populate fix-count repeats here?
        }

        public void setAnswer(IAnswerData data, TreeReference ref_Renamed)
        {
            setAnswer(data, instance.resolveReference(ref_Renamed));
        }

        public void setAnswer(IAnswerData data, TreeElement node)
        {
            node.setAnswer(data);
        }

        /**
         * Deletes the inner-most repeat that this node belongs to and returns the
         * corresponding FormIndex. Behavior is currently undefined if you call this
         * method on a node that is not contained within a repeat.
         * 
         * @param index
         * @return
         */
        public FormIndex deleteRepeat(FormIndex index)
        {
            System.Collections.ArrayList indexes = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList());
            System.Collections.ArrayList multiplicities = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList());
            System.Collections.ArrayList elements = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList());

            collapseIndex(index, indexes, multiplicities, elements);

            // loop backwards through the elements, removing objects from each
            // vector, until we find a repeat
            // TODO: should probably check to make sure size > 0
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                IFormElement e = (IFormElement)elements[i];
                if (e is GroupDef && ((GroupDef)e).Repeat)
                {
                    break;
                }
                else
                {
                    indexes.RemoveAt(i);
                    multiplicities.RemoveAt(i);
                    elements.RemoveAt(i);
                }
            }

            // build new formIndex which includes everything
            // up to the node we're going to remove
            FormIndex newIndex = buildIndex(indexes, multiplicities, elements);

            TreeReference deleteRef = getChildInstanceRef(newIndex);
            TreeElement deleteElement = instance.resolveReference(deleteRef);
            TreeReference parentRef = deleteRef.getParentRef();
            TreeElement parentElement = instance.resolveReference(parentRef);

            int childMult = deleteElement.getMult();
            parentElement.removeChild(deleteElement);

            // update multiplicities of other child nodes
            for (int i = 0; i < parentElement.getNumChildren(); i++)
            {
                TreeElement child = parentElement.getChildAt(i);
                if (child.getMult() > childMult)
                {
                    child.setMult(child.getMult() - 1);
                }
            }

            triggerTriggerables(deleteRef);
            return newIndex;
        }

        public void createNewRepeat(FormIndex index)
        {
            TreeReference destRef = getChildInstanceRef(index);
            TreeElement template = instance.getTemplate(destRef);

            instance.copyNode(template, destRef);

            preloadInstance(instance.resolveReference(destRef));
            triggerTriggerables(destRef); // trigger conditions that depend on the creation of this new node
            initializeTriggerables(destRef); // initialize conditions for the node (and sub-nodes)
        }

        public Boolean isRepeatRelevant(TreeReference repeatRef)
        {
            Boolean relev = true;

            Condition c = (Condition)conditionRepeatTargetIndex[repeatRef.genericize()];
            if (c != null)
            {
                relev = c.evalBool(instance, new EvaluationContext(exprEvalContext, repeatRef));
            }

            //check the relevancy of the immediate parent
            if (relev)
            {
                TreeElement templNode = instance.getTemplate(repeatRef);
                TreeReference parentPath = templNode.getParent().getRef().genericize();
                TreeElement parentNode = instance.resolveReference(parentPath.contextualize(repeatRef));
                relev = parentNode.isRelevant();
            }

            return relev;
        }

        public Boolean canCreateRepeat(TreeReference repeatRef, FormIndex repeatIndex)
        {
            GroupDef repeat = (GroupDef)this.getChild(repeatIndex);

            //Check to see if this repeat can have children added by the user
            if (repeat.noAddRemove)
            {
                //Check to see if there's a count to use to determine how many children this repeat
                //should have
                if (repeat.CountReference != null)
                {
                    int currentMultiplicity = repeatIndex.getElementMultiplicity();

                    //get the total multiplicity possible
                    long fullcount = ((int)this.Instance.getDataValue(repeat.CountReference).Value);

                    if (fullcount <= currentMultiplicity)
                    {
                        return false;
                    }
                }
                else
                {
                    //Otherwise the user can never add repeat instances 
                    return false;
                }
            }

            //TODO: If we think the node is still relevant, we also need to figure out a way to test that assumption against
            //the repeat's constraints.


            return true;
        }

        public void copyItemsetAnswer(QuestionDef q, TreeElement targetNode, IAnswerData data)
        {
            ItemsetBinding itemset = q.getDynamicChoices();
            TreeReference targetRef = targetNode.getRef();
            TreeReference destRef = itemset.getDestRef().contextualize(targetRef);

            List<Selection> selections = null;
            List<String> selectedValues = new List<String>();
            if (data is SelectMultiData)
            {
                selections = (List<Selection>)data.Value;
            }
            else if (data is SelectOneData)
            {
                selections = new List<Selection>();
                selections.Add((Selection)data.Value);
            }
            if (itemset.valueRef != null)
            {
                for (int i = 0; i < selections.Count; i++)
                {
                    selectedValues.Add(selections[i].choice.Value);
                }
            }

            //delete existing dest nodes that are not in the answer selection
            IDictionary<String, TreeElement> existingValues = new Dictionary<String, TreeElement>();
            List<TreeReference> existingNodes = Instance.expandReference(destRef);
            for (int i = 0; i < existingNodes.Count; i++)
            {
                TreeElement node = Instance.resolveReference(existingNodes[i]);

                if (itemset.valueRef != null)
                {
                    String value = itemset.getRelativeValue().evalReadable(this.Instance, new EvaluationContext(exprEvalContext, node.getRef()));
                    if (selectedValues.Contains(value))
                    {
                        existingValues.Add(value, node); //cache node if in selection and already exists
                    }
                }

                //delete from target
                targetNode.removeChild(node);
            }

            //copy in nodes for new answer; preserve ordering in answer
            for (int i = 0; i < selections.Count; i++)
            {
                Selection s = selections[i];
                SelectChoice ch = s.choice;

                TreeElement cachedNode = null;
                if (itemset.valueRef != null)
                {
                    String value = ch.Value;
                    if (existingValues.ContainsKey(value))
                    {
                        cachedNode = existingValues[value];
                    }
                }

                if (cachedNode != null)
                {
                    cachedNode.setMult(i);
                    targetNode.addChild(cachedNode);
                }
                else
                {
                    Instance.copyItemsetNode(ch.copyNode, destRef, this);
                }
            }

            triggerTriggerables(destRef); // trigger conditions that depend on the creation of these new nodes
            initializeTriggerables(destRef); // initialize conditions for the node (and sub-nodes)
            //not 100% sure this will work since destRef is ambiguous as the last step, but i think it's supposed to work
        }

        /**
         * Add a Condition to the form's Collection.
         * 
         * @param condition
         *            the condition to be set
         */
        public Triggerable addTriggerable(Triggerable t)
        {
            int existingIx = triggerables.IndexOf(t);
            if (existingIx >= 0)
            {
                //one node may control access to many nodes; this means many nodes effectively have the same condition
                //let's identify when conditions are the same, and store and calculate it only once

                //note, if the contextRef is unnecessarily deep, the condition will be evaluated more times than needed
                //perhaps detect when 'identical' condition has a shorter contextRef, and use that one instead?
                return (Triggerable)triggerables[existingIx];
            }
            else
            {
                triggerables.Add(t);
                triggerablesInOrder = false;

                List<TreeReference> triggers = t.getTriggers();
                for (int i = 0; i < triggers.Count; i++)
                {
                    TreeReference trigger = (TreeReference)triggers[i];
                    if (!triggerIndex.ContainsKey(trigger))
                    {
                        triggerIndex.Add(trigger, new List<Triggerable>());
                    }
                    List<Triggerable> triggered = (List<Triggerable>)triggerIndex[trigger];
                    if (!triggered.Contains(t))
                    {
                        triggered.Add(t);
                    }
                }

                return t;
            }
        }

        public void finalizeTriggerables()
        {
            //
            //DAGify the triggerables based on dependencies and sort them so that
            //trigbles come only after the trigbles they depend on
            //

            List<Triggerable[]> partialOrdering = new List<Triggerable[]>();
            for (int i = 0; i < triggerables.Count; i++)
            {
                Triggerable t = (Triggerable)triggerables[i];
                List<Triggerable> deps = new List<Triggerable>();

                if (t.canCascade())
                {
                    for (int j = 0; j < t.getTargets().Count; j++)
                    {
                        TreeReference target = (TreeReference)t.getTargets()[j];
                        List<Triggerable> triggered = (List<Triggerable>)triggerIndex[target];
                        if (triggered != null)
                        {
                            for (int k = 0; k < triggered.Count; k++)
                            {
                                Triggerable u = (Triggerable)triggered[k];
                                if (!deps.Contains(u))
                                    deps.Add(u);
                            }
                        }
                    }
                }

                for (int j = 0; j < deps.Count; j++)
                {
                    Triggerable u = (Triggerable)deps[j];
                    Triggerable[] edge = { t, u };
                    partialOrdering.Add(edge);
                }
            }

            List<Triggerable> vertices = new List<Triggerable>();

            for (int i = 0; i < triggerables.Count; i++)
                vertices.Add(triggerables[i]);
            triggerables.Clear();

            while (vertices.Count > 0)
            {
                //determine root nodes
                List<Triggerable> roots = new List<Triggerable>();

                for (int i = 0; i < vertices.Count; i++)
                {
                    roots.Add(vertices[i]);
                }
                for (int i = 0; i < partialOrdering.Count; i++)
                {
                    Triggerable[] edge = (Triggerable[])partialOrdering[i];
                    roots.Remove(edge[1]);
                }

                //if no root nodes while graph still has nodes, graph has cycles
                if (roots.Count == 0)
                {
                    throw new SystemException("Cannot create partial ordering of triggerables due to dependency cycle. Why wasn't this caught during parsing?");
                }

                //remove root nodes and edges originating from them
                for (int i = 0; i < roots.Count; i++)
                {
                    Triggerable root = (Triggerable)roots[i];
                    triggerables.Add(root);
                    vertices.Remove(root);
                }
                for (int i = partialOrdering.Count - 1; i >= 0; i--)
                {
                    Triggerable[] edge = (Triggerable[])partialOrdering[i];
                    if (roots.Contains(edge[0]))
                        partialOrdering.RemoveAt(i);
                }
            }

            triggerablesInOrder = true;

            //
            //build the condition index for repeatable nodes
            //


            conditionRepeatTargetIndex = new Dictionary<TreeReference, Condition>();
            for (int i = 0; i < triggerables.Count; i++)
            {
                Triggerable t = triggerables[i];
                if (t is Condition)
                {
                    System.Collections.ArrayList targets = t.getTargets();
                    for (int j = 0; j < targets.Count; j++)
                    {
                        TreeReference target = (TreeReference)targets[j];
                        if (instance.getTemplate(target) != null)
                        {
                            conditionRepeatTargetIndex.Add(target, (Condition)t);
                        }
                    }
                }
            }

        }

        public void initializeTriggerables()
        {
            initializeTriggerables(TreeReference.rootRef());
        }

        /**
         * Walks the current set of conditions, and evaluates each of them with the
         * current context.
         */
        private void initializeTriggerables(TreeReference rootRef)
        {
            TreeReference genericRoot = rootRef.genericize();

            List<Triggerable> applicable = new List<Triggerable>();
            for (int i = 0; i < triggerables.Count; i++)
            {
                Triggerable t = (Triggerable)triggerables[i];
                for (int j = 0; j < t.getTargets().Count; j++)
                {
                    TreeReference target = (TreeReference)t.getTargets()[j];
                    if (genericRoot.isParentOf(target, false))
                    {
                        applicable.Add(t);
                        break;
                    }
                }
            }

            evaluateTriggerables(applicable, rootRef);
        }

        // ref: unambiguous ref of node that just changed
        public void triggerTriggerables(TreeReference ref_name)
        {
            // turn unambiguous ref into a generic ref
            TreeReference genericRef = ref_name.genericize();

            // get conditions triggered by this node
            List<Triggerable> triggered = (List<Triggerable>)triggerIndex[genericRef];
            if (triggered == null)
                return;

            List<Triggerable> triggeredCopy = new List<Triggerable>();
            for (int i = 0; i < triggered.Count; i++)
                triggeredCopy.Add(triggered[i]);
            evaluateTriggerables(triggeredCopy, ref_name);
        }

        private void evaluateTriggerables(List<Triggerable> tv, TreeReference anchorRef)
        {
            //add all cascaded triggerables to queue
            for (int i = 0; i < tv.Count; i++)
            {
                Triggerable t = (Triggerable)tv[i];
                if (t.canCascade())
                {
                    for (int j = 0; j < t.getTargets().Count; j++)
                    {
                        TreeReference target = (TreeReference)t.getTargets()[j];
                        List<Triggerable> triggered = (List<Triggerable>)triggerIndex[target];
                        if (triggered != null)
                        {
                            for (int k = 0; k < triggered.Count; k++)
                            {
                                Triggerable u = (Triggerable)triggered[k];
                                if (!tv.Contains(u))
                                    tv.Add(u);
                            }
                        }
                    }
                }
            }

            //'triggerables' is topologically-ordered by dependencies, so evaluate the triggerables in 'tv'
            //in the order they appear in 'triggerables'
            for (int i = 0; i < triggerables.Count; i++)
            {
                Triggerable t = (Triggerable)triggerables[i];
                if (tv.Contains(t))
                {
                    evaluateTriggerable(t, anchorRef);
                }
            }
        }

        private void evaluateTriggerable(Triggerable t, TreeReference anchorRef)
        {
            TreeReference contextRef = t.contextRef.contextualize(anchorRef);
            List<TreeReference> v = instance.expandReference(contextRef);
            for (int i = 0; i < v.Count; i++)
            {
                EvaluationContext ec = new EvaluationContext(exprEvalContext, (TreeReference)v[i]);
                t.apply(instance, ec, this);
            }
        }

        public Boolean evaluateConstraint(TreeReference ref_, IAnswerData data)
        {
            if (data == null)
                return true;

            TreeElement node = instance.resolveReference(ref_);
            Constraint c = node.getConstraint();
            if (c == null)
                return true;

            EvaluationContext ec = new EvaluationContext(exprEvalContext, ref_);
            ec.isConstraint = true;
            ec.candidateValue = data;

            return c.constraint.eval(instance, ec);
        }

        /**
         * @param ec
         *            The new Evaluation Context
         */
        public void setEvaluationContext(EvaluationContext ec)
        {
            initEvalContext(ec);
            this.exprEvalContext = ec;
        }
        private class AnonymousClassIFunctionHandler : IFunctionHandler
        {
            private org.javarosa.core.model.FormDef f;
            private FormDef enclosingInstance;


            public AnonymousClassIFunctionHandler(org.javarosa.core.model.FormDef f, FormDef enclosingInstance)
            {
                InitBlock(f, enclosingInstance);
            }
            private void InitBlock(org.javarosa.core.model.FormDef f, FormDef enclosingInstance)
            {
                this.f = f;
                this.enclosingInstance = enclosingInstance;
            }
            public String Name
            {
                get { return "jr:itext"; }
            }

            public Object eval(Object[] args, EvaluationContext ec)
            {
                String textID = (String)args[0];
                try
                {
                    String text = f.Localizer.getText(textID);
                    return text == null ? "[itext:" + textID + "]" : text;
                }
                catch (NotImplementedException nsee)
                {
                    return "[nolocale]";
                }
            }

            public ArrayList Prototypes
            {
                get
                {
                    Type[] proto = { typeof(String) };
                    ArrayList v = new ArrayList();
                    v.Add(proto);
                    return v;
                }
            }

            public Boolean rawArgs()
            {
                return false;
            }

            public Boolean realTime()
            {
                return false;
            }
        }
        private class AnonymousClassIFunctionHandler1 : IFunctionHandler
        {
            private org.javarosa.core.model.FormDef f;
            private FormDef enclosingInstance;

            public AnonymousClassIFunctionHandler1(org.javarosa.core.model.FormDef f, FormDef enclosingInstance)
            {
                InitBlock(f, enclosingInstance);
            }
            private void InitBlock(org.javarosa.core.model.FormDef f, FormDef enclosingInstance)
            {
                this.f = f;
                this.enclosingInstance = enclosingInstance;
            }

            public String Name
            {
                get { return "jr:choice-name"; }
            }

            public Object eval(Object[] args, EvaluationContext ec)
            {
                
                try
                {
                    String value = (String)args[0];
                    String questionXpath = (String)args[1];
                    TreeReference ref_ = RestoreUtils.xfFact.ref_Renamed(questionXpath);


                    QuestionDef q = findQuestionByRef(ref_,f);
                    if (q == null || (q.ControlType != Constants.CONTROL_SELECT_ONE &&
                                      q.ControlType != Constants.CONTROL_SELECT_MULTI))
                    {
                        return "";
                    }

                    Console.WriteLine("here!!");

                    List<SelectChoice> choices = q.getChoices();
                    foreach (SelectChoice ch in choices)
                    {
                        if (ch.Value.Equals(value))
                        {
                            //this is really not ideal. we should hook into the existing code (FormEntryPrompt) for pulling
                            //display text for select choices. however, it's hard, because we don't really have
                            //any context to work with, and all the situations where that context would be used
                            //don't make sense for trying to reverse a select value back to a label in an unrelated
                            //expression

                            String textID = ch.TextID;
                            if (textID != null)
                            {
                                return f.localizer.getText(textID);
                            }
                            else
                            {
                                return ch.LabelInnerText;
                            }
                        }
                    }
                    return "";
                }
                catch (Exception e)
                {
                    throw new WrappedException("error in evaluation of xpath function [choice-name]", e);
                }
            }

            public ArrayList Prototypes
            {
                get
                {
                    Type[] proto = { typeof(String), typeof(String) };
                    ArrayList v = new ArrayList();
                    v.Add(proto);
                    return v;
                }
            }

            public Boolean rawArgs()
            {
                return false;
            }

            public Boolean realTime()
            {
                return false;
            }
        }
        private void initEvalContext(EvaluationContext ec)
        {
            if (!ec.FunctionHandlers.ContainsKey("jr:itext"))
            {
                FormDef f = this;
                ec.addFunctionHandler(new AnonymousClassIFunctionHandler(f, this));
            }

            /* function to reverse a select value into the display label for that choice in the question it came from
             *
             * arg 1: select value
             * arg 2: string xpath referring to origin question; must be absolute path
             * 
             * this won't work at all if the original label needed to be processed/calculated in some way (<output>s, etc.) (is this even allowed?)
             * likely won't work with multi-media labels
             * _might_ work for itemsets, but probably not very well or at all; could potentially work better if we had some context info
             * DOES work with localization
             * 
             * it's mainly intended for the simple case of reversing a question with compile-time-static fields, for use inside an <output>
             */
            if (!ec.FunctionHandlers.ContainsKey("jr:choice-name"))
            {
                FormDef f = this;

                ec.addFunctionHandler(new AnonymousClassIFunctionHandler1(f, this));
            }
        }

        public String fillTemplateString(String template, TreeReference contextRef)
        {
            return fillTemplateString(template, contextRef, new Dictionary<String,Object>());
        }

        public String fillTemplateString(String template, TreeReference contextRef, IDictionary<String, Object> variables)
        {
            Hashtable args = new Hashtable();

            int depth = 0;
            ArrayList outstandingArgs = Localizer.getArgs(template);
            while (outstandingArgs.Count > 0)
            {
                for (int i = 0; i < outstandingArgs.Count; i++)
                {
                    String argName = (String)outstandingArgs[i];
                    if (!args.ContainsKey(argName))
                    {
                        int ix = -1;
                        try
                        {
                            ix = int.Parse(argName);
                        }
                        catch (FormatException nfe)
                        {
                            Console.Error.WriteLine("Warning: expect arguments to be numeric [" + argName + "]");
                        }

                        if (ix < 0 || ix >= outputFragments.Count)
                            continue;

                        IConditionExpr expr = (IConditionExpr)outputFragments[ix];
                        EvaluationContext ec = new EvaluationContext(exprEvalContext, contextRef);
                        ec.setVariables(variables);
                        String value = expr.evalReadable(this.Instance, ec);
                        args.Add(argName, value);
                    }
                }

                template = Localizer.processArguments(template, args);
                outstandingArgs = Localizer.getArgs(template);

                depth++;
                if (depth >= TEMPLATING_RECURSION_LIMIT)
                {
                    throw new SystemException("Dependency cycle in <output>s; recursion limit exceeded!!");
                }
            }

            return template;
        }

        /**
         * Identify the itemset in the backend model, and create a set of SelectChoice 
         * objects at the current question reference based on the data in the model.
         * 
         * Will modify the itemset binding to contain the relevant choices 
         * 
         * @param itemset The binding for an itemset, where the choices will be populated
         * @param curQRef A reference to the current question's element, which will be
         * used to determine the values to be chosen from.
         */
        public void populateDynamicChoices(ItemsetBinding itemset, TreeReference curQRef)
        {
            List<SelectChoice> choices = new List<SelectChoice>();

            List<TreeReference> matches = itemset.nodesetExpr.evalNodeset(this.Instance,
                    new EvaluationContext(exprEvalContext, itemset.contextRef.contextualize(curQRef)));

            for (int i = 0; i < matches.Count; i++)
            {
                TreeReference item = matches[i];

                String label = itemset.labelExpr.evalReadable(this.Instance, new EvaluationContext(exprEvalContext, item));
                String value = null;
                TreeElement copyNode = null;

                if (itemset.copyMode)
                {
                    copyNode = this.Instance.resolveReference(itemset.copyRef.contextualize(item));
                }
                if (itemset.valueRef != null)
                {
                    value = itemset.valueExpr.evalReadable(this.Instance, new EvaluationContext(exprEvalContext, item));
                }
                //			SelectChoice choice = new SelectChoice(labelID,labelInnerText,value,isLocalizable);
                SelectChoice choice = new SelectChoice(label, value != null ? value : "dynamic:" + i, itemset.labelIsItext);
                choice.Index =i;
                if (itemset.copyMode)
                    choice.copyNode = copyNode;

                choices.Add(choice);
            }

            if (choices.Count == 0)
            {
                throw new SystemException("dynamic select question has no choices! [" + itemset.nodesetRef + "]");
            }

            itemset.setChoices(choices, this.Localizer);
        }
        //UPGRADE_NOTE: Respective javadoc comments were merged.  It should be changed in order to comply with .NET documentation conventions. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1199'"
        /// <returns> the preloads
        /// </returns>
        /// <param name="preloads">the preloads to set
        /// </param>
        virtual public QuestionPreloader Preloader
        {
            get
            {
                return preloader;
            }

            set
            {
                this.preloader = value;
            }

        }

        /*
         * (non-Javadoc)
         * 
         * @see
         * org.javarosa.core.model.utils.Localizable#localeChanged(java.lang.String,
         * org.javarosa.core.model.utils.Localizer)
         */
        public void localeChanged(String locale, Localizer localizer)
        {
            for (IEnumerator e = children.GetEnumerator(); e.MoveNext(); )
            {
                ((IFormElement)e.Current).localeChanged(locale, localizer);
            }
        }

        public override String ToString()
        {
            return Title;
        }

        /**
         * Preload the Data Model with the preload values that are enumerated in the
         * data bindings.
         */
        public void preloadInstance(TreeElement node)
        {
            // if (node.isLeaf()) {
            IAnswerData preload = null;
            if (node.getPreloadHandler() != null)
            {
                preload = preloader.getQuestionPreload(node.getPreloadHandler(),
                        node.getPreloadParams());
            }
            if (preload != null)
            { // what if we want to wipe out a value in the
                // instance?
                node.setAnswer(preload);
            }
            // } else {
            if (!node.isLeaf())
            {
                for (int i = 0; i < node.getNumChildren(); i++)
                {
                    TreeElement child = node.getChildAt(i);
                    if (child.getMult() != TreeReference.INDEX_TEMPLATE)
                        // don't preload templates; new repeats are preloaded as they're created
                        preloadInstance(child);
                }
            }
            // }
        }

        public Boolean postProcessInstance()
        {
            return postProcessInstance(instance.getRoot());
        }

        /**
         * Iterate over the form's data bindings, and evaluate all post procesing
         * calls.
         * 
         * @return true if the instance was modified in any way. false otherwise.
         */
        private Boolean postProcessInstance(TreeElement node)
        {
            // we might have issues with ordering, for example, a handler that writes a value to a node,
            // and a handler that does something external with the node. if both handlers are bound to the
            // same node, we need to make sure the one that alters the node executes first. deal with that later.
            // can we even bind multiple handlers to the same node currently?

            // also have issues with conditions. it is hard to detect what conditions are affected by the actions
            // of the post-processor. normally, it wouldn't matter because we only post-process when we are exiting
            // the form, so the result of any triggered conditions is irrelevant. however, if we save a form in the
            // interim, post-processing occurs, and then we continue to edit the form. it seems like having conditions
            // dependent on data written during post-processing is a bad practice anyway, and maybe we shouldn't support it.

            if (node.isLeaf())
            {
                if (node.getPreloadHandler() != null)
                {
                    return preloader.questionPostProcess(node, node.getPreloadHandler(), node.getPreloadParams());
                }
                else
                {
                    return false;
                }
            }
            else
            {
                Boolean instanceModified = false;
                for (int i = 0; i < node.getNumChildren(); i++)
                {
                    TreeElement child = node.getChildAt(i);
                    if (child.getMult() != TreeReference.INDEX_TEMPLATE)
                        instanceModified |= postProcessInstance(child);
                }
                return instanceModified;
            }
        }

        /**
         * Reads the form definition object from the supplied stream.
         * 
         * Requires that the instance has been set to a prototype of the instance that
         * should be used for deserialization.
         * 
         * @param dis
         *            - the stream to read from.
         * @throws IOException
         * @throws InstantiationException
         * @throws IllegalAccessException
         */
        public void readExternal(BinaryReader dis, PrototypeFactory pf)
        {
            ID= (ExtUtil.readInt(dis));
            Name=(ExtUtil.nullIfEmpty(ExtUtil.readString(dis)));
            Title=((String)ExtUtil.read(dis, new ExtWrapNullable(typeof(String)), pf));
            Children=((ArrayList)ExtUtil.read(dis, new ExtWrapListPoly(), pf));

            Instance=((FormInstance)ExtUtil.read(dis, typeof(FormInstance), pf));

            setLocalizer((Localizer)ExtUtil.read(dis, new ExtWrapNullable(typeof(Localizer)), pf));

            ArrayList vcond = (ArrayList)ExtUtil.read(dis, new ExtWrapList(typeof(Condition)), pf);
            for (IEnumerator e = vcond.GetEnumerator(); e.MoveNext(); )
                addTriggerable((Condition)e.Current);
            ArrayList vcalc = (ArrayList)ExtUtil.read(dis, new ExtWrapList(typeof(Recalculate)), pf);
            for (IEnumerator e = vcalc.GetEnumerator(); e.MoveNext(); )
                addTriggerable((Recalculate)e.Current);
            finalizeTriggerables();

            outputFragments = (ArrayList)ExtUtil.read(dis, new ExtWrapListPoly(), pf);

            submissionProfiles = (Hashtable)ExtUtil.read(dis, new ExtWrapMap(typeof(String), typeof(SubmissionProfile)));
        }

        /**
         * meant to be called after deserialization and initialization of handlers
         * 
         * @param newInstance
         *            true if the form is to be used for a new entry interaction,
         *            false if it is using an existing IDataModel
         */
        public void initialize(Boolean newInstance)
        {
            if (newInstance)
            {// only preload new forms (we may have to revisit
                // this)
                preloadInstance(instance.getRoot());
            }

            if (Localizer != null && Localizer.Locale == null)
            {
                Localizer.ToDefault();
            }

            initializeTriggerables();
        }

        /**
         * Writes the form definition object to the supplied stream.
         * 
         * @param dos
         *            - the stream to write to.
         * @throws IOException
         */
        public void writeExternal(BinaryWriter dos)
        {
            ExtUtil.writeNumeric(dos, ID);
            ExtUtil.writeString(dos, ExtUtil.emptyIfNull(Name));
            ExtUtil.write(dos, new ExtWrapNullable(Title));
            ExtUtil.write(dos, new ExtWrapListPoly(Children));
            ExtUtil.write(dos, instance);
            ExtUtil.write(dos, new ExtWrapNullable(localizer));

            ArrayList conditions = new ArrayList();
            ArrayList recalcs = new ArrayList();
            for (int i = 0; i < triggerables.Count; i++)
            {
                Triggerable t = (Triggerable)triggerables[i];
                if (t is Condition)
                {
                    conditions.Add(t);
                }
                else if (t is Recalculate)
                {
                    recalcs.Add(t);
                }
            }
            ExtUtil.write(dos, new ExtWrapList(conditions));
            ExtUtil.write(dos, new ExtWrapList(recalcs));

            ExtUtil.write(dos, new ExtWrapListPoly(outputFragments));
            ExtUtil.write(dos, new ExtWrapMap(submissionProfiles));
        }

        public void collapseIndex(FormIndex index, ArrayList indexes, ArrayList multiplicities, ArrayList elements)
        {
            if (!index.isInForm())
            {
                return;
            }

            IFormElement element = this;
            while (index != null)
            {
                int i = index.getLocalIndex();
                element = element.getChild(i);

                indexes.Add(i);
                multiplicities.Add(index.getInstanceIndex() == -1 ? 0 : index.getInstanceIndex());
                elements.Add(element);

                index = index.getNextLevel();
            }
        }

        public FormIndex buildIndex(ArrayList indexes, ArrayList multiplicities, ArrayList elements)
        {
            FormIndex cur = null;
            ArrayList curMultiplicities = new ArrayList();
            for (int j = 0; j < multiplicities.Count; ++j)
            {
                curMultiplicities.Add(multiplicities[j]);
            }

            ArrayList curElements = new ArrayList();
            for (int j = 0; j < elements.Count; ++j)
            {
                curElements.Add(elements[j]);
            }

            for (int i = indexes.Count - 1; i >= 0; i--)
            {
                int ix = (int)indexes[i];
                int mult = ((int)multiplicities[i]);

                //TODO: ... No words. Just fix it.
                TreeReference ref_ = (TreeReference)((XPathReference)((IFormElement)elements[i]).Bind).Reference;
                if (!(elements[i] is GroupDef && ((GroupDef)elements[i]).Repeat))
                {
                    mult = -1;
                }

                cur = new FormIndex(cur, ix, mult, getChildInstanceRef(curElements, curMultiplicities));
                curMultiplicities.RemoveAt(curMultiplicities.Count - 1);
                curElements.RemoveAt(curElements.Count - 1);
            }
            return cur;
        }



        public int getNumRepetitions(FormIndex index) {
		ArrayList indexes = new ArrayList();
		ArrayList multiplicities = new ArrayList();
		ArrayList elements = new ArrayList();

		if (!index.isInForm()) {
			throw new SystemException("not an in-form index");
		}
		
		collapseIndex(index, indexes, multiplicities, elements);

		if (!(elements[elements.Count-1] is GroupDef) || !((GroupDef)elements[elements.Count-1]).Repeat) {
			throw new SystemException("current element not a repeat");
		}
		
		//so painful
		TreeElement templNode = instance.getTemplate(index.getReference());
		TreeReference parentPath = templNode.getParent().getRef().genericize();
		TreeElement parentNode = instance.resolveReference(parentPath.contextualize(index.getReference()));
		return parentNode.getChildMultiplicity(templNode.getName());
	}

        //repIndex == -1 => next repetition about to be created
        public FormIndex descendIntoRepeat(FormIndex index, int repIndex)
        {
            int numRepetitions = getNumRepetitions(index);

            ArrayList indexes = new ArrayList();
            ArrayList multiplicities = new ArrayList();
            ArrayList elements = new ArrayList();
            collapseIndex(index, indexes, multiplicities, elements);

            if (repIndex == -1)
            {
                repIndex = numRepetitions;
            }
            else
            {
                if (repIndex < 0 || repIndex >= numRepetitions)
                {
                    throw new SystemException("selection exceeds current number of repetitions");
                }
            }

            multiplicities[repIndex] = multiplicities.Count - 1;

            return buildIndex(indexes, multiplicities, elements);
        }

        /*
         * (non-Javadoc)
         * 
         * @see org.javarosa.core.model.IFormElement#getDeepChildCount()
         */
        public int DeepChildCount
        {
            get
            {
                int total = 0;
                IEnumerator e = children.GetEnumerator();
                while (e.MoveNext())
                {
                    total += ((IFormElement)e.Current).DeepChildCount;
                }
                return total;
            }
        }

        public void registerStateObserver(FormElementStateListener qsl)
        {
            // NO. (Or at least not yet).
        }

        public void unregisterStateObserver(FormElementStateListener qsl)
        {
            // NO. (Or at least not yet).
        }

        public ArrayList Children
        {
            get { return children; }

            set
            {
                this.children = (children == null ? new ArrayList() : value);
            }

        }

        public String Title
        {
            get { return title; }
            set
            {
                this.title = value;
            }
        }

        public int ID
        {
            get { return id; }
            set
            {
                this.id = id;
            }
        }

        public String Name
        {
            get { return name; }
            set
            {
                this.name = value;
            }
        }

        public Localizer Localizer
        {
            get { return localizer; }
        }

        public FormInstance Instance
        {
            get { return instance; }
            set
            {
                if (instance.getFormId() != -1 && ID != Instance.getFormId())
                {
                    Console.Error.WriteLine("Warning: assigning incompatible instance (type " + instance.getFormId() + ") to a formdef (type " + ID + ")");
                }

                instance.setFormId(ID);
                this.instance = value;
                attachControlsToInstanceData();
            }
        }

        public ArrayList OutputFragments
        {
            get { return outputFragments; }
            set
            {
                this.outputFragments = value;
            }

        }

        public Hashtable getMetaData()
        {
            Hashtable metadata = new Hashtable();
            String[] fields = MetaDataFields;

            for (int i = 0; i < fields.Length; i++)
            {
                try
                {
                    metadata.Add(fields[i], getMetaData(fields[i]));
                }
                catch (NullReferenceException npe)
                {
                    if (getMetaData(fields[i]) == null)
                    {
                        Console.WriteLine("ERROR! XFORM MUST HAVE A NAME!");
                        Console.WriteLine(npe.StackTrace);
                    }
                }
            }

            return metadata;
        }

        public Object getMetaData(String fieldName)
        {
            if (fieldName.Equals("DESCRIPTOR"))
            {
                return name;
            } if (fieldName.Equals("XMLNS"))
            {
                return ExtUtil.emptyIfNull(instance.schema);
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public String[] MetaDataFields
        {
            get { return new String[] { "DESCRIPTOR", "XMLNS" }; }
        }

        /**
         * Link a deserialized instance back up with its parent FormDef. this allows select/select1 questions to be
         * internationalizable in chatterbox, and (if using CHOICE_INDEX mode) allows the instance to be serialized
         * to xml
         */
        public void attachControlsToInstanceData()
        {
            attachControlsToInstanceData(instance.getRoot());
        }

        private void attachControlsToInstanceData(TreeElement node)
        {
            for (int i = 0; i < node.getNumChildren(); i++)
            {
                attachControlsToInstanceData(node.getChildAt(i));
            }

            IAnswerData val = node.getValue();
            ArrayList selections = null;
            if (val is SelectOneData)
            {
                selections = new ArrayList();
                selections.Add(val.Value);
            }
            else if (val is SelectMultiData)
            {
                selections = (ArrayList)val.Value;
            }

            if (selections != null)
            {
                QuestionDef q = findQuestionByRef(node.getRef(), this);
                if (q == null)
                {
                    throw new SystemException("FormDef.attachControlsToInstanceData: can't find question to link");
                }

                if (q.getDynamicChoices() != null)
                {
                    //droos: i think we should do something like initializing the itemset here, so that default answers
                    //can be linked to the selectchoices. however, there are complications. for example, the itemset might
                    //not be ready to be evaluated at form initialization; it may require certain questions to be answered
                    //first. e.g., if we evaluate an itemset and it has no choices, the xform engine will throw an error
                    //itemset TODO
                }

                for (int i = 0; i < selections.Count; i++)
                {
                    Selection s = (Selection)selections[i];
                    s.attachChoice(q);
                }
            }
        }

        public static QuestionDef findQuestionByRef(TreeReference ref_, IFormElement fe)
        {
            if (fe is FormDef)
            {
                ref_ = ref_.genericize();
            }

            if (fe is QuestionDef)
            {
                QuestionDef q = (QuestionDef)fe;
                TreeReference bind = FormInstance.unpackReference(q.Bind);
                return (ref_.Equals(bind) ? q : null);
            }
            else
            {
                for (int i = 0; i < fe.Children.Count; i++)
                {
                    QuestionDef ret = findQuestionByRef(ref_, fe.getChild(i));
                    if (ret != null)
                        return ret;
                }
                return null;
            }
        }

        /**
         * Appearance isn't a valid attribute for form, but this method must be included
         * as a result of conforming to the IFormElement interface.
         */
        public String AppearanceAttr
        {
            get
            {
                throw new SystemException("This method call is not relevant for FormDefs getAppearanceAttr ()");
            }
         set{
            throw new SystemException("This method call is not relevant for FormDefs setAppearanceAttr()");
             }
        }

        /**
         * Not applicable here.
         */
        public String LabelInnerText
        {
            get { return null; }
        }

        /**
         * Not applicable
         */
        public String TextID
        {
            get { return null; }
            set
            {
                throw new SystemException("This method call is not relevant for FormDefs [setTextID()]");
            }
        }


        public void setDefaultSubmission(SubmissionProfile profile)
        {
            submissionProfiles.Add(DEFAULT_SUBMISSION_PROFILE, profile);
        }

        public void addSubmissionProfile(String submissionId, SubmissionProfile profile)
        {
            submissionProfiles.Add(submissionId, profile);
        }

        public SubmissionProfile getSubmissionProfile()
        {
            //At some point these profiles will be set by the <submit> control in the form. 
            //In the mean time, though, we can only promise that the default one will be used.

            return (SubmissionProfile)submissionProfiles[DEFAULT_SUBMISSION_PROFILE];
        }


        List<IFormElement> IFormElement.Children
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        

        string IFormElement.LabelInnerText
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
}