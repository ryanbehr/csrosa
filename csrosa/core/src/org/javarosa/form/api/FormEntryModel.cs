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

using org.javarosa.core.model;
using org.javarosa.core.model.data;
using org.javarosa.core.model.instance;
using System;
using System.Collections;
using System.Collections.Generic;
namespace org.javarosa.form.api
{

    /**
     * The data model used during form entry. Represents the current state of the
     * form and provides access to the objects required by the view and the
     * controller.
     */
    public class FormEntryModel
    {
        private FormDef form;
        private FormIndex currentFormIndex;

        /**
         * One of "REPEAT_STRUCUTRE_" in this class's static types,
         * represents what abstract structure repeat events should
         * be broadacast as.
         */
        private int repeatStructure = -1;

        /**
         * Repeats should be a prompted linear set of questions, either
         * with a fixed set of repetitions, or a prompt for creating a 
         * new one.
         */
        public  const int REPEAT_STRUCTURE_LINEAR = 1;

        /**
         * Repeats should be a custom juncture point with centralized
         * "Create/Remove/Interact" hub.  
         */
        public  const int REPEAT_STRUCTURE_NON_LINEAR = 2;


        public FormEntryModel(FormDef form) :
            this(form, REPEAT_STRUCTURE_LINEAR)
        {
        }

        /**
         * Creates a new entry model for the form with the appropriate
         * repeat structure
         * 
         * @param form
         * @param repeatStructure The structure of repeats (the repeat signals which should
         * be sent during form entry)
         * @throws IllegalArgumentException If repeatStructure is not valid
         */
        public FormEntryModel(FormDef form, int repeatStructure)
        {
            this.form = form;
            if (repeatStructure != REPEAT_STRUCTURE_LINEAR && repeatStructure != REPEAT_STRUCTURE_NON_LINEAR)
            {
                throw new ArgumentException(repeatStructure + ": does not correspond to a valid repeat structure");
            }
            //We need to see if there are any guessed repeat counts in the form, which prevents
            //us from being able to use the new repeat style
            //Unfortunately this is probably (A) slow and (B) might overflow the stack. It's not the only
            //recursive walk of the form, though, so (B) isn't really relevant
            if (repeatStructure == REPEAT_STRUCTURE_NON_LINEAR && containsRepeatGuesses(form))
            {
                repeatStructure = REPEAT_STRUCTURE_LINEAR;
            }
            this.repeatStructure = repeatStructure;
            this.currentFormIndex = FormIndex.createBeginningOfFormIndex();
        }

        /**
         * Given a FormIndex, returns the event this FormIndex represents.
         * 
         * @see FormEntryController
         */
        public int getEvent(FormIndex index)
        {
            if (index.isBeginningOfFormIndex())
            {
                return FormEntryController.EVENT_BEGINNING_OF_FORM;
            }
            else if (index.isEndOfFormIndex())
            {
                return FormEntryController.EVENT_END_OF_FORM;
            }

            // This came from chatterbox, and is unclear how correct it is,
            // commented out for now.
            // DELETEME: If things work fine
            // Vector defs = form.explodeIndex(index);
            // IFormElement last = (defs.size() == 0 ? null : (IFormElement)
            // defs.lastElement());
            IFormElement element = form.getChild(index);
            if (element is GroupDef)
            {
                if (((GroupDef)element).Repeat)
                {
                    if (repeatStructure != REPEAT_STRUCTURE_NON_LINEAR && form.Instance.resolveReference(form.getChildInstanceRef(index)) == null)
                    {
                        return FormEntryController.EVENT_PROMPT_NEW_REPEAT;
                    }
                    else if (repeatStructure == REPEAT_STRUCTURE_NON_LINEAR && index.getElementMultiplicity() == TreeReference.INDEX_REPEAT_JUNCTURE)
                    {
                        return FormEntryController.EVENT_REPEAT_JUNCTURE;
                    }
                    else
                    {
                        return FormEntryController.EVENT_REPEAT;
                    }
                }
                else
                {
                    return FormEntryController.EVENT_GROUP;
                }
            }
            else
            {
                return FormEntryController.EVENT_QUESTION;
            }
        }

        /**
         * 
         * @param index
         * @return
         */
        public TreeElement getTreeElement(FormIndex index)
        {
            return form.Instance.resolveReference(index.getReference());
        }


        /**
         * @return the event for the current FormIndex
         * @see FormEntryController
         */
        public int getEvent()
        {
            return getEvent(currentFormIndex);
        }


        /**
         * @return Form title
         */
        public String FormTitle
        {
            get { return form.Title; }
        }


        /**
         * 
         * @param index
         * @return Returns the FormEntryPrompt for the specified FormIndex if the
         *         index represents a question.
         */
        public FormEntryPrompt getQuestionPrompt(FormIndex index)
        {
            if (form.getChild(index) is QuestionDef)
            {
                return new FormEntryPrompt(form, index);
            }
            else
            {
                throw new SystemException(
                        "Invalid query for Question prompt. Non-Question object at the form index");
            }
        }


        /**
         * 
         * @param index
         * @return Returns the FormEntryPrompt for the current FormIndex if the
         *         index represents a question.
         */
        public FormEntryPrompt getQuestionPrompt()
        {
            return getQuestionPrompt(currentFormIndex);
        }


        /**
         * When you have a non-question event, a CaptionPrompt will have all the
         * information needed to display to the user.
         * 
         * @param index
         * @return Returns the FormEntryCaption for the given FormIndex if is not a
         *         question.
         */
        public FormEntryCaption getCaptionPrompt(FormIndex index)
        {
            return new FormEntryCaption(form, index);
        }



        /**
         * When you have a non-question event, a CaptionPrompt will have all the
         * information needed to display to the user.
         * 
         * @param index
         * @return Returns the FormEntryCaption for the current FormIndex if is not
         *         a question.
         */
        public FormEntryCaption getCaptionPrompt()
        {
            return getCaptionPrompt(currentFormIndex);
        }


        /**
         * 
         * @return an array of Strings of the current langauges. Null if there are
         *         none.
         */
        public String[] getLanguages()
        {
            if (form.Localizer != null)
            {
                return form.Localizer.getAvailableLocales();
            }
            return null;
        }


        /**
         * Not yet implemented
         * 
         * Should get the number of completed questions to this point.
         */
        public int getCompletedRelevantQuestionCount()
        {
            // TODO: Implement me.
            return 0;
        }


        /**
         * Not yet implemented
         * 
         * Should get the total possible questions given the current path through the form.
         */
        public int getTotalRelevantQuestionCount()
        {
            // TODO: Implement me.
            return 0;
        }

        /**
         * @return total number of questions in the form, regardless of relevancy
         */
        public int getNumQuestions()
        {
            return form.DeepChildCount;
        }


        /**
         * 
         * @return Returns the current FormIndex referenced by the FormEntryModel.
         */
        public FormIndex getFormIndex()
        {
            return currentFormIndex;
        }


        public void setLanguage(String language)
        {
            if (form.Localizer != null)
            {
                form.Localizer.Locale =language;
            }
        }


        /**
         * 
         * @return Returns the currently selected language.
         */
        public String getLanguage()
        {
            return form.Localizer.Locale;
        }


        /**
         * Set the FormIndex for the current question.
         * 
         * @param index
         */
        public void setQuestionIndex(FormIndex index)
        {
            if (!currentFormIndex.Equals(index))
            {
                // See if a hint exists that says we should have a model for this
                // already
                createModelIfNecessary(index);
                currentFormIndex = index;
            }
        }


        /**
         * 
         * @return
         */
        public FormDef getForm()
        {
            return form;
        }


        /**
         * Returns a hierarchical list of FormEntryCaption objects for the given
         * FormIndex
         * 
         * @param index
         * @return list of FormEntryCaptions in hierarchical order
         */
        public FormEntryCaption[] getCaptionHierarchy(FormIndex index)
        {
            List<FormEntryCaption> captions = new List<FormEntryCaption>();
            FormIndex remaining = index;
            while (remaining != null)
            {
                remaining = remaining.getNextLevel();
                FormIndex localIndex = index.diff(remaining);
                IFormElement element = form.getChild(localIndex);
                if (element != null)
                {
                    FormEntryCaption caption = null;
                    if (element is GroupDef)
                        caption = new FormEntryCaption(getForm(), localIndex);
                    else if (element is QuestionDef)
                        caption = new FormEntryPrompt(getForm(), localIndex);

                    if (caption != null)
                    {
                        captions.Add(caption);
                    }
                }
            }
            FormEntryCaption[] captionArray = new FormEntryCaption[captions.Count];
            captions.CopyTo(captionArray);
            return captionArray;
        }


        /**
         * Returns a hierarchical list of FormEntryCaption objects for the current
         * FormIndex
         * 
         * @param index
         * @return list of FormEntryCaptions in hierarchical order
         */
        public FormEntryCaption[] getCaptionHierarchy()
        {
            return getCaptionHierarchy(currentFormIndex);
        }


        /**
         * @param index
         * @return true if the element at the specified index is read only
         */
        public Boolean isIndexReadonly(FormIndex index)
        {
            if (index.isBeginningOfFormIndex() || index.isEndOfFormIndex())
                return true;

            TreeReference ref_ = form.getChildInstanceRef(index);
            Boolean isAskNewRepeat = (getEvent(index) == FormEntryController.EVENT_PROMPT_NEW_REPEAT ||
                                      getEvent(index) == FormEntryController.EVENT_REPEAT_JUNCTURE);

            if (isAskNewRepeat)
            {
                return false;
            }
            else
            {
                TreeElement node = form.Instance.resolveReference(ref_);
                return !node.isEnabled();
            }
        }


        /**
         * @param index
         * @return true if the element at the current index is read only
         */
        public Boolean isIndexReadonly()
        {
            return isIndexReadonly(currentFormIndex);
        }


        /**
         * Determine if the current FormIndex is relevant. Only relevant indexes
         * should be returned when filling out a form.
         * 
         * @param index
         * @return true if current element at FormIndex is relevant
         */
        public Boolean isIndexRelevant(FormIndex index)
        {
            TreeReference ref_ = form.getChildInstanceRef(index);
            Boolean isAskNewRepeat = (getEvent(index) == FormEntryController.EVENT_PROMPT_NEW_REPEAT);
            Boolean isRepeatJuncture = (getEvent(index) == FormEntryController.EVENT_REPEAT_JUNCTURE);

            Boolean relevant;
            if (isAskNewRepeat)
            {
                relevant = form.isRepeatRelevant(ref_) && form.canCreateRepeat(ref_, index);
                //repeat junctures are still relevant if no new repeat can be created; that option
                //is simply missing from the menu
            }
            else if (isRepeatJuncture)
            {
                relevant = form.isRepeatRelevant(ref_);
            }
            else
            {
                TreeElement node = form.Instance.resolveReference(ref_);
                relevant = node.isRelevant(); // check instance flag first
            }

            if (relevant)
            { // if instance flag/condition says relevant, we still
                // have to check the <group>/<repeat> hierarchy

                FormIndex ancestorIndex = index;
                while (!ancestorIndex.isTerminal())
                {
                    // This should be safe now that the TreeReference is contained
                    // in the ancestor index itself
                    TreeElement ancestorNode =
                            form.Instance.resolveReference(ancestorIndex.getLocalReference());

                    if (!ancestorNode.isRelevant())
                    {
                        relevant = false;
                        break;
                    }
                    ancestorIndex = ancestorIndex.getNextLevel();
                }
            }

            return relevant;
        }


        /**
         * Determine if the current FormIndex is relevant. Only relevant indexes
         * should be returned when filling out a form.
         * 
         * @param index
         * @return true if current element at FormIndex is relevant
         */
        public Boolean isIndexRelevant()
        {
            return isIndexRelevant(currentFormIndex);
        }


        /**
         * For the current index: Checks whether the index represents a node which
         * should exist given a non-interactive repeat, along with a count for that
         * repeat which is beneath the dynamic level specified.
         * 
         * If this index does represent such a node, the new model for the repeat is
         * created behind the scenes and the index for the initial question is
         * returned.
         * 
         * Note: This method will not prevent the addition of new repeat elements in
         * the interface, it will merely use the xforms repeat hint to create new
         * nodes that are assumed to exist
         * 
         * @param The index to be evaluated as to whether the underlying model is
         *        hinted to exist
         */
        private void createModelIfNecessary(FormIndex index)
        {
            if (index.isInForm())
            {
                IFormElement e = getForm().getChild(index);
                if (e is GroupDef)
                {
                    GroupDef g = (GroupDef)e;
                    if (g.Repeat && g.CountReference != null)
                    {
                        IAnswerData count = getForm().Instance.getDataValue(g.CountReference);
                        if (count != null)
                        {
                            long fullcount = ((int)count.Value);
                            TreeReference ref_ = getForm().getChildInstanceRef(index);
                            TreeElement element = getForm().Instance.resolveReference(ref_);
                            if (element == null)
                            {
                                if (index.getInstanceIndex() < fullcount)
                                {

                                    try
                                    {
                                        getForm().createNewRepeat(index);
                                    }
                                    catch (InvalidReferenceException ire)
                                    {
                                        Console.WriteLine(ire.StackTrace);
                                        throw new SystemException("Invalid Reference while creting new repeat!" + ire.Message);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


        public Boolean isIndexCompoundContainer()
        {
            return isIndexCompoundContainer(getFormIndex());
        }

        public Boolean isIndexCompoundContainer(FormIndex index)
        {
            FormEntryCaption caption = getCaptionPrompt(index);
            return getEvent(index) == FormEntryController.EVENT_GROUP && caption.getAppearanceHint() != null && caption.getAppearanceHint().ToLower().Equals("full");
        }

        public Boolean isIndexCompoundElement()
        {
            return isIndexCompoundElement(getFormIndex());
        }

        public Boolean isIndexCompoundElement(FormIndex index)
        {
            //Can't be a subquestion if it's not even a question!
            if (getEvent(index) != FormEntryController.EVENT_QUESTION)
            {
                return false;
            }

            //get the set of nested groups that this question is in.
            FormEntryCaption[] captions = getCaptionHierarchy(index);
            foreach (FormEntryCaption caption in captions)
            {

                //If one of this question's parents is a group, this question is inside of it.
                if (isIndexCompoundContainer(caption.getIndex()))
                {
                    return true;
                }
            }
            return false;
        }

        public FormIndex[] getCompoundIndices()
        {
            return getCompoundIndices(getFormIndex());
        }

        public FormIndex[] getCompoundIndices(FormIndex container)
        {
            //ArrayLists are a no-go for J2ME
            List<FormIndex> indices = new List<FormIndex>();
            FormIndex walker = incrementIndex(container);
            while (FormIndex.isSubElement(container, walker))
            {
                if (isIndexRelevant(walker))
                {
                    indices.Add(walker);
                }
                walker = incrementIndex(walker);
            }
            FormIndex[] array = new FormIndex[indices.Count];
            for (int i = 0; i < indices.Count; ++i)
            {
                array[i] = indices[i];
            }
            return array;
        }


        /**
         * @return The Current Repeat style which should be used.
         */
        public int getRepeatStructure()
        {
            return this.repeatStructure;
        }

        public FormIndex incrementIndex(FormIndex index)
        {
            return incrementIndex(index, true);
        }

        public FormIndex incrementIndex(FormIndex index, Boolean descend)
        {
            ArrayList indexes = new ArrayList();
            ArrayList multiplicities = new ArrayList();
            ArrayList elements = new ArrayList();

            if (index.isEndOfFormIndex())
            {
                return index;
            }
            else if (index.isBeginningOfFormIndex())
            {
                if (form.Children == null || form.Children.Count == 0)
                {
                    return FormIndex.createEndOfFormIndex();
                }
            }
            else
            {
                form.collapseIndex(index, indexes, multiplicities, elements);
            }

            incrementHelper(indexes, multiplicities, elements, descend);

            if (indexes.Count == 0)
            {
                return FormIndex.createEndOfFormIndex();
            }
            else
            {
                return form.buildIndex(indexes, multiplicities, elements);
            }
        }

        private void incrementHelper(ArrayList indexes, ArrayList multiplicities, ArrayList elements, Boolean descend)
        {
            int i = indexes.Count - 1;
            Boolean exitRepeat = false; //if exiting a repetition? (i.e., go to next repetition instead of one level up)

            if (i == -1 || elements[i] is GroupDef)
            {
                // current index is group or repeat or the top-level form

                if (i >= 0)
                {
                    // find out whether we're on a repeat, and if so, whether the
                    // specified instance actually exists
                    GroupDef group = (GroupDef)elements[i];
                    if (group.Repeat)
                    {
                        if (repeatStructure == REPEAT_STRUCTURE_NON_LINEAR)
                        {

                            if (((System.Int32)multiplicities[multiplicities.Count - 1]) == TreeReference.INDEX_REPEAT_JUNCTURE)
                            {

                                descend = false;
                                exitRepeat = true;
                            }

                        }
                        else
                        {

                            if (form.Instance.resolveReference(form.getChildInstanceRef(elements, multiplicities)) == null)
                            {
                                descend = false; // repeat instance does not exist; do not descend into it
                                exitRepeat = true;
                            }

                        }
                    }
                }

                if (descend)
                {
                    indexes.Add(0);
                    multiplicities.Add(0);
                    elements.Add((i == -1 ? form : (IFormElement)elements[i]).getChild(0));

                    if (repeatStructure == REPEAT_STRUCTURE_NON_LINEAR)
                    {
                        if (elements[elements.Count - 1] is GroupDef && ((GroupDef)elements[elements.Count - 1]).Repeat)
                        {
                            multiplicities[multiplicities.Count - 1] = (System.Int32)TreeReference.INDEX_REPEAT_JUNCTURE;
                        }
                    }

                    return;
                }
            }

            while (i >= 0)
            {
                // if on repeat, increment to next repeat EXCEPT when we're on a
                // repeat instance that does not exist and was not created
                // (repeat-not-existing can only happen at lowest level; exitRepeat
                // will be true)
                if (!exitRepeat && elements[i] is GroupDef && ((GroupDef)elements[i]).Repeat)
                {
                    if (repeatStructure == REPEAT_STRUCTURE_NON_LINEAR)
                    {

                        multiplicities[i] = (System.Int32)TreeReference.INDEX_REPEAT_JUNCTURE;
                    }
                    else
                    {

                        multiplicities[i] = (System.Int32)(((System.Int32)multiplicities[i]) + 1);
                    }
                    return;
                }

                IFormElement parent = (i == 0 ? form : (IFormElement)elements[i - 1]);
                int curIndex = ((System.Int32)indexes[i]);

                // increment to the next element on the current level
                if (curIndex + 1 >= parent.Children.Count)
                {
                    // at the end of the current level; move up one level and start
                    // over
                    indexes.RemoveAt(i);
                    multiplicities.RemoveAt(i);
                    elements.RemoveAt(i);
                    i--;
                    exitRepeat = false;
                }
                else
                {
                    indexes[i] = (System.Int32)(curIndex + 1);
                    multiplicities[i] = 0;
                    elements[i] = parent.getChild(curIndex + 1);

                    if (repeatStructure == REPEAT_STRUCTURE_NON_LINEAR)
                    {
                        if (elements[elements.Count - 1] is GroupDef && ((GroupDef)elements[elements.Count - 1]).Repeat)
                        {
                            multiplicities[multiplicities.Count - 1] = (System.Int32)TreeReference.INDEX_REPEAT_JUNCTURE;
                        }
                    }

                    return;
                }
            }
        }

        public FormIndex decrementIndex(FormIndex index)
        {
            ArrayList indexes = new ArrayList();
            ArrayList multiplicities = new ArrayList();
            ArrayList elements = new ArrayList();

            if (index.isBeginningOfFormIndex())
            {
                return index;
            }
            else if (index.isEndOfFormIndex())
            {
                if (form.Children == null || form.Children.Count == 0)
                {
                    return FormIndex.createBeginningOfFormIndex();
                }
            }
            else
            {
                form.collapseIndex(index, indexes, multiplicities, elements);
            }

            decrementHelper(indexes, multiplicities, elements);

            if (indexes.Count == 0)
            {
                return FormIndex.createBeginningOfFormIndex();
            }
            else
            {
                return form.buildIndex(indexes, multiplicities, elements);
            }
        }

        private void decrementHelper(ArrayList indexes, ArrayList multiplicities, ArrayList elements)
        {
            int i = indexes.Count - 1;

            if (i != -1)
            {
                int curIndex = ((int)indexes[i]);
                int curMult = ((int)multiplicities[i]);

                if (repeatStructure == REPEAT_STRUCTURE_NON_LINEAR &&
                    elements[elements.Count - 1] is GroupDef && ((GroupDef)elements[elements.Count - 1]).Repeat &&
                    ((int)multiplicities[elements.Count - 1]) != TreeReference.INDEX_REPEAT_JUNCTURE)
                {
                    multiplicities[TreeReference.INDEX_REPEAT_JUNCTURE] = i;
                    return;
                }
                else if (repeatStructure != REPEAT_STRUCTURE_NON_LINEAR && curMult > 0)
                {
                    multiplicities[curMult - 1] = i;
                }
                else if (curIndex > 0)
                {
                    // set node to previous element
                    indexes[curIndex - 1] = i;
                    multiplicities[0] = i;
                    elements[i] = (i == 0 ? form : (IFormElement)elements[i - 1]).getChild(curIndex - 1);

                    if (setRepeatNextMultiplicity(elements, multiplicities))
                        return;
                }
                else
                {
                    // at absolute beginning of current level; index to parent
                    indexes.RemoveAt(i);
                    multiplicities.RemoveAt(i);
                    elements.RemoveAt(i);
                    return;
                }
            }

            IFormElement element = (i < 0 ? form : (IFormElement)elements[i]);
            while (!(element is QuestionDef))
            {
                int subIndex = element.Children.Count - 1;
                element = element.getChild(subIndex);

                indexes.Add(subIndex);
                multiplicities.Add(0);
                elements.Add(element);

                if (setRepeatNextMultiplicity(elements, multiplicities))
                    return;
            }
        }

        private Boolean setRepeatNextMultiplicity(ArrayList elements, ArrayList multiplicities)
        {
            // find out if node is repeatable
            TreeReference nodeRef = form.getChildInstanceRef(elements, multiplicities);
            TreeElement node = form.Instance.resolveReference(nodeRef);
            if (node == null || node.repeatable)
            { // node == null if there are no
                // instances of the repeat
                int mult;
                if (node == null)
                {
                    mult = 0; // no repeats; next is 0
                }
                else
                {
                    String name = node.getName();
                    TreeElement parentNode = form.Instance.resolveReference(nodeRef.getParentRef());
                    mult = parentNode.getChildMultiplicity(name);
                }
                multiplicities[multiplicities.Count - 1] = (System.Int32)(repeatStructure == REPEAT_STRUCTURE_NON_LINEAR ? TreeReference.INDEX_REPEAT_JUNCTURE : mult);
                return true;
            }
            else
            {
                return false;
            }
        }

        /**
         * This method does a recursive check of whether there are any repeat guesses 
         * in the element or its subtree. This is a necessary step when initializing
         * the model to be able to identify whether new repeats can be used.
         * 
         * @param parent The form element to begin checking
         * @return true if the element or any of its descendants is a repeat
         * which has a count guess, false otherwise.
         */
        private Boolean containsRepeatGuesses(IFormElement parent)
        {
            if (parent is GroupDef)
            {
                GroupDef g = (GroupDef)parent;
                if (g.Repeat && g.CountReference != null)
                {
                    return true;
                }
            }

            List<IFormElement> children = parent.Children;
            if (children == null) { return false; }
            for (IEnumerator en = children.GetEnumerator(); en.MoveNext(); )
            {
                if (containsRepeatGuesses((IFormElement)en.Current)) { return true; }
            }
            return false;
        }

    }
}