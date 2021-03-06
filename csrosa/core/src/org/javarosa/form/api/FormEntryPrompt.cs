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
using org.javarosa.core.model.condition;
using org.javarosa.core.model.condition.pivot;
using org.javarosa.core.model.data;
using org.javarosa.core.model.data.helper;
using org.javarosa.core.model.instance;
using org.javarosa.core.services;
using org.javarosa.core.util;
using org.javarosa.formmanager.view;
using System;
using System.Collections.Generic;
namespace org.javarosa.form.api
{


    /**
     * This class gives you all the information you need to display a question when
     * your current FormIndex references a QuestionEvent.
     * 
     * @author Acellam Guy ,  Yaw Anokwa
     */
    public class FormEntryPrompt : FormEntryCaption
    {

        TreeElement mTreeElement;
        Boolean dynamicChoicesPopulated = false;

        /**
         * This empty constructor exists for convenience of any supertypes of this prompt
         */
        protected FormEntryPrompt()
        {
        }

        /**
         * Creates a FormEntryPrompt for the element at the given index in the form.
         * 
         * @param form
         * @param index
         */
        public FormEntryPrompt(FormDef form, FormIndex index)
            : base(form, index)
        {

            if (!(element is QuestionDef))
            {
                throw new ArgumentException("FormEntryPrompt can only be created for QuestionDef elements");
            }
            this.mTreeElement = form.Instance.resolveReference(index.getReference());
        }

        public int getControlType()
        {
            return getQuestion().ControlType;
        }

        public int getDataType()
        {
            return mTreeElement.dataType;
        }

        // attributes available in the bind, instance and body
        public String getPromptAttributes()
        {
            // TODO: implement me.
            return null;
        }

        //note: code overlap with FormDef.copyItemsetAnswer
        public IAnswerData getAnswerValue()
        {
            QuestionDef q = getQuestion();

            ItemsetBinding itemset = q.getDynamicChoices();
            if (itemset != null)
            {
                if (itemset.valueRef != null)
                {
                    List<SelectChoice> choices = getSelectChoices();
                    List<String> preselectedValues = new List<String>();

                    //determine which selections are already present in the answer
                    if (itemset.copyMode)
                    {
                        TreeReference destRef = itemset.getDestRef().contextualize(mTreeElement.getRef());
                        List<TreeReference> subNodes = form.Instance.expandReference(destRef);
                        for (int i = 0; i < subNodes.Count; i++)
                        {
                            TreeElement node = form.Instance.resolveReference(subNodes[i]);
                            String value = itemset.getRelativeValue().evalReadable(form.Instance, new EvaluationContext(form.exprEvalContext, node.getRef()));
                            preselectedValues.Add(value);
                        }
                    }
                    else
                    {
                        List<Selection> sels = new List<Selection>();
                        IAnswerData data = mTreeElement.getValue();
                        if (data is SelectMultiData)
                        {
                            sels = (List<Selection>)data.Value;
                        }
                        else if (data is SelectOneData)
                        {
                            sels = new List<Selection>();
                            sels.Add((Selection)data.Value);
                        }
                        for (int i = 0; i < sels.Count; i++)
                        {
                            preselectedValues.Add(sels[i].xmlValue);
                        }
                    }

                    //populate 'selection' with the corresponding choices (matching 'value') from the dynamic choiceset
                    List<Selection> selection = new List<Selection>();
                    for (int i = 0; i < preselectedValues.Count; i++)
                    {
                        String value = preselectedValues[i];
                        SelectChoice choice = null;
                        for (int j = 0; j < choices.Count; j++)
                        {
                            SelectChoice ch = choices[j];
                            if (value.Equals(ch.Value))
                            {
                                choice = ch;
                                break;
                            }
                        }

                        selection.Add(choice.selection());
                    }

                    //convert to IAnswerData
                    if (selection.Count == 0)
                    {
                        return null;
                    }
                    else if (q.ControlType == Constants.CONTROL_SELECT_MULTI)
                    {
                        return new SelectMultiData(selection);
                    }
                    else if (q.ControlType == Constants.CONTROL_SELECT_ONE)
                    {
                        return new SelectOneData(selection[0]); //do something if more than one selected?
                    }
                    else
                    {
                        throw new SystemException("can't happen");
                    }
                }
                else
                {
                    return null; //cannot map up selections without <value>
                }
            }
            else
            { //static choices
                return mTreeElement.getValue();
            }
        }


        public String getAnswerText()
        {
            IAnswerData data = this.getAnswerValue();

            if (data == null)
                return null;
            else
            {
                String text;

                //csims@dimagi.com - Aug 11, 2010 - Added special logic to
                //capture and display the appropriate value for selections
                //and multi-selects.
                if (data is SelectOneData)
                {
                    text = this.getSelectItemText((Selection)data);
                }
                else if (data is SelectMultiData)
                {
                    String returnValue = "";
                    List<Selection> values = (List<Selection>)data;
                    foreach (Selection value in values)
                    {
                        returnValue += this.getSelectItemText(value) + " ";
                    }
                    text = returnValue;
                }
                else
                {
                    text = data.DisplayText;
                }

                if (getControlType() == Constants.CONTROL_SECRET)
                {
                    String obfuscated = "";
                    for (int i = 0; i < text.Length; ++i)
                    {
                        obfuscated += "*";
                    }
                    text = obfuscated;
                }
                return text;
            }
        }

        public String getConstraintText()
        {
            return getConstraintText(null);
        }

        public String getConstraintText(IAnswerData attemptedValue)
        {
            return getConstraintText(null, attemptedValue);
        }

        public String getConstraintText(String textForm, IAnswerData attemptedValue)
        {
            if (mTreeElement.getConstraint() == null)
            {
                return null;
            }
            else
            {
                EvaluationContext ec = new EvaluationContext(form.exprEvalContext, mTreeElement.getRef());
                if (textForm != null)
                {
                    ec.OutputTextForm =textForm;
                }
                if (attemptedValue != null)
                {
                    ec.isConstraint = true;
                    ec.candidateValue = attemptedValue;
                }
                return mTreeElement.getConstraint().getConstraintMessage(ec, form.Instance);
            }
        }

        public List<SelectChoice> getSelectChoices()
        {
            QuestionDef q = getQuestion();

            ItemsetBinding itemset = q.getDynamicChoices();
            if (itemset != null)
            {
                if (!dynamicChoicesPopulated)
                {
                    form.populateDynamicChoices(itemset, mTreeElement.getRef());
                    dynamicChoicesPopulated = true;
                }
                return itemset.getChoices();
            }
            else
            { //static choices
                return q.getChoices();
            }
        }

        public void expireDynamicChoices()
        {
            dynamicChoicesPopulated = false;
            ItemsetBinding itemset = getQuestion().getDynamicChoices();
            if (itemset != null)
            {
                itemset.clearChoices();
            }
        }



        public Boolean isRequired()
        {
            return mTreeElement.required;
        }

        public Boolean isReadOnly()
        {
            return !mTreeElement.isEnabled();
        }

        public QuestionDef getQuestion()
        {
            return (QuestionDef)element;
        }

        //==== observer pattern ====//

        public void register(IQuestionWidget viewWidget)
        {
            base.register(viewWidget);
            mTreeElement.registerStateObserver(this);
        }

        public void unregister()
        {
            mTreeElement.unregisterStateObserver(this);
            base.unregister();
        }

        public void formElementStateChanged(TreeElement instanceNode, int changeFlags)
        {
            if (this.mTreeElement != instanceNode)
                throw new InvalidOperationException("Widget received event from foreign question");
            if (viewWidget != null)
                viewWidget.refreshWidget(changeFlags);
        }

        /**
      * ONLY RELEVANT to Question elements!
      * Will throw runTimeException if this is called for anything that isn't a Question.
      * Returns null if no help text is available
      * @return
      */
        public String getHelpText()
        {
            if (!(element is QuestionDef))
            {
                throw new SystemException("Can't get HelpText for Elements that are not Questions!");
            }

            String textID = ((QuestionDef)element).HelpTextID;
            String helpText = ((QuestionDef)element).HelpText;
            String helpInnerText = ((QuestionDef)element).HelpInnerText;

            try
            {
                if (textID != null)
                {
                    helpText = localizer().getLocalizedText(textID);
                }
                else
                {
                    helpText = substituteStringArgs(((QuestionDef)element).HelpInnerText);
                }
            }
            catch (NoLocalizedTextException nlt)
            {
                //use fallback helptext
            }
            catch (UnregisteredLocaleException ule)
            {
                Console.Error.WriteLine("Warning: No Locale set yet (while attempting to getHelpText())");
            }
            catch (Exception e)
            {
                Logger.exception("FormEntryPrompt.getHelpText", e);
                Console.WriteLine(e.StackTrace);
            }

            return helpText;

        }



        /**
         * Attempts to return the specified Item (from a select or 1select) text.
         * Will check for text in the following order:<br/>
         * Localized Text (long form) -> Localized Text (no special form) <br />
         * If no textID is available, method will return this item's labelInnerText.
         * @param sel the selection (item), if <code>null</code> will throw a IllegalArgumentException
         * @return Question Text.  <code>null</code> if no text for this element exists (after all fallbacks).
         * @throws RunTimeException if this method is called on an element that is NOT a QuestionDef
         * @throws IllegalArgumentException if Selection is <code>null</code>
         */
        public String getSelectItemText(Selection sel)
        {
            //throw tantrum if this method is called when it shouldn't be or sel==null
            if (!(getFormElement() is QuestionDef)) throw new SystemException("Can't retrieve question text for non-QuestionDef form elements!");
            if (sel == null) throw new ArgumentException("Cannot use null as an argument!");

            //Just in case the selection hasn't had a chance to be initialized yet.
            if (sel.index == -1) { sel.attachChoice(this.getQuestion()); }

            //check for the null id case and return labelInnerText if it is so.
            String tid = sel.choice.TextID;
            if (tid == null || tid == "") return substituteStringArgs(sel.choice.LabelInnerText);

            //otherwise check for 'long' form of the textID, then for the default form and return
            String returnText;
            returnText = getIText(tid, "long");
            if (returnText == null) returnText = getIText(tid, null);

            return substituteStringArgs(returnText);
        }

        /**
         * @see getSelectItemText(Selection sel)
         */
        public String getSelectChoiceText(SelectChoice selection)
        {
            return getSelectItemText(selection.selection());
        }

        /**
         * This method is generally used to retrieve special forms for a 
         * (select or 1select) item, e.g. "audio", "video", etc.
         * 
         * @param sel - The Item whose text you're trying to retrieve.
         * @param form - Special text form of Item you're trying to retrieve. 
         * @return Special Form Text. <code>null</code> if no text for this element exists (with the specified special form).
         * @throws RunTimeException if this method is called on an element that is NOT a QuestionDef
         * @throws IllegalArgumentException if <code>sel == null</code>
         */
        public String getSpecialFormSelectItemText(Selection sel, String form)
        {
            if (sel == null) throw new ArgumentException("Cannot use null as an argument for Selection!");

            //Just in case the selection hasn't had a chance to be initialized yet.
            if (sel.index == -1) { sel.attachChoice(this.getQuestion()); }

            String textID = sel.choice.TextID;
            if (textID == null || textID.Equals("")) return null;

            String returnText = getIText(textID, form);

            return substituteStringArgs(returnText);

        }

        public String getSpecialFormSelectChoiceText(SelectChoice sel, String form)
        {
            return getSpecialFormSelectItemText(sel.selection(), form);
        }

        public void requestConstraintHint(ConstraintHint hint)
        {
            //NOTE: Technically there's some rep exposure, here. People could use this mechanism to expose the instance.
            //We could hide it by dispatching hints through a final abstract class instead.
            Constraint c = mTreeElement.getConstraint();
            if (c != null)
            {
                hint.init(new EvaluationContext(form.exprEvalContext, mTreeElement.getRef()), c.constraint, form.Instance);
            }
            else
            {
                //can't pivot what ain't there.
                throw new UnpivotableExpressionException();
            }
        }

    }
}
