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
namespace org.javarosa.form.api
{

    /**
     * This class is used to navigate through an xform and appropriately manipulate
     * the FormEntryModel's state.
     */
    public class FormEntryController
    {
        public  const int ANSWER_OK = 0;
        public  const int ANSWER_REQUIRED_BUT_EMPTY = 1;
        public  const int ANSWER_CONSTRAINT_VIOLATED = 2;

        public  const int EVENT_BEGINNING_OF_FORM = 0;
        public  const int EVENT_END_OF_FORM = 1;
        public  const int EVENT_PROMPT_NEW_REPEAT = 2;
        public  const int EVENT_QUESTION = 4;
        public  const int EVENT_GROUP = 8;
        public  const int EVENT_REPEAT = 16;
        public  const int EVENT_REPEAT_JUNCTURE = 32;

        FormEntryModel model;

        /**
         * Creates a new form entry controller for the model provided
         * 
         * @param model
         */
        public FormEntryController(FormEntryModel model)
        {
            this.model = model;
        }


        public FormEntryModel getModel()
        {
            return model;
        }


        /**
         * Attempts to save answer at the current FormIndex into the datamodel.
         * 
         * @param data
         * @return
         */
        public int answerQuestion(IAnswerData data)
        {
            return answerQuestion(model.getFormIndex(), data);
        }


        /**
         * Attempts to save the answer at the specified FormIndex into the
         * datamodel.
         * 
         * @param index
         * @param data
         * @return OK if save was successful, error if a constraint was violated.
         */
        public int answerQuestion(FormIndex index, IAnswerData data)
        {
            QuestionDef q = model.getQuestionPrompt(index).getQuestion();
            if (model.getEvent(index) != FormEntryController.EVENT_QUESTION)
            {
                throw new SystemException("Non-Question object at the form index.");
            }
            TreeElement element = model.getTreeElement(index);
            Boolean complexQuestion = q.isComplex();

            Boolean hasConstraints = false;
            if (element.required && data == null)
            {
                return ANSWER_REQUIRED_BUT_EMPTY;
            }
            else if (!complexQuestion && !model.getForm().evaluateConstraint(index.getReference(), data))
            {
                return ANSWER_CONSTRAINT_VIOLATED;
            }
            else if (!complexQuestion)
            {
                commitAnswer(element, index, data);
                return ANSWER_OK;
            }
            else if (complexQuestion && hasConstraints)
            {
                //TODO: itemsets: don't currently evaluate constraints for itemset/copy -- haven't figured out how handle it yet
                throw new SystemException("Itemsets do not currently evaluate constraints. Your constraint will not work, please remove it before proceeding.");
            }
            else
            {
                try
                {
                    model.getForm().copyItemsetAnswer(q, element, data);
                }
                catch (InvalidReferenceException ire)
                {
                    Console.WriteLine(ire.StackTrace);
                    throw new SystemException("Invalid reference while copying itemset answer: " + ire.Message);
                }
                return ANSWER_OK;
            }
        }


        /**
         * saveAnswer attempts to save the current answer into the data model
         * without doing any constraint checking. Only use this if you know what
         * you're doing. For normal form filling you should always use
         * answerQuestion or answerCurrentQuestion.
         * 
         * @param index
         * @param data
         * @return true if saved successfully, false otherwise.
         */
        public Boolean saveAnswer(FormIndex index, IAnswerData data)
        {
            if (model.getEvent(index) != FormEntryController.EVENT_QUESTION)
            {
                throw new SystemException("Non-Question object at the form index.");
            }
            TreeElement element = model.getTreeElement(index);
            return commitAnswer(element, index, data);
        }


        /**
         * saveAnswer attempts to save the current answer into the data model
         * without doing any constraint checking. Only use this if you know what
         * you're doing. For normal form filling you should always use
         * answerQuestion().
         * 
         * @param index
         * @param data
         * @return true if saved successfully, false otherwise.
         */
        public Boolean saveAnswer(IAnswerData data)
        {
            return saveAnswer(model.getFormIndex(), data);
        }


        /**
         * commitAnswer actually saves the data into the datamodel.
         * 
         * @param element
         * @param index
         * @param data
         * @return true if saved successfully, false otherwise
         */
        private Boolean commitAnswer(TreeElement element, FormIndex index, IAnswerData data)
        {
            if (data != null || element.getValue() != null)
            {
                // we should check if the data to be saved is already the same as
                // the data in the model, but we can't (no IAnswerData.equals())
                model.getForm().setValue(data, index.getReference(), element);
                return true;
            }
            else
            {
                return false;
            }
        }


        /**
         * Navigates forward in the form.
         * 
         * @return the next event that should be handled by a view.
         */
        public int stepToNextEvent()
        {
            return stepEvent(true);
        }


        /**
         * Navigates backward in the form.
         * 
         * @return the next event that should be handled by a view.
         */
        public int stepToPreviousEvent()
        {
            return stepEvent(false);
        }


        /**
         * Moves the current FormIndex to the next/previous relevant position.
         * 
         * @param forward
         * @return
         */
        private int stepEvent(Boolean forward)
        {
            FormIndex index = model.getFormIndex();

            do
            {
                if (forward)
                {
                    index = model.incrementIndex(index);
                }
                else
                {
                    index = model.decrementIndex(index);
                }
            } while (index.isInForm() && !model.isIndexRelevant(index));

            return jumpToIndex(index);
        }


        /**
         * Jumps to a given FormIndex.
         * 
         * @param index
         * @return EVENT for the specified Index.
         */
        public int jumpToIndex(FormIndex index)
        {
            model.setQuestionIndex(index);
            return model.getEvent(index);
        }

        public FormIndex descendIntoRepeat(int n)
        {
            jumpToIndex(model.getForm().descendIntoRepeat(model.getFormIndex(), n));
            return model.getFormIndex();
        }

        public FormIndex descendIntoNewRepeat()
        {
            jumpToIndex(model.getForm().descendIntoRepeat(model.getFormIndex(), -1));
            newRepeat(model.getFormIndex());
            return model.getFormIndex();
        }

        /**
         * Creates a new repeated instance of the group referenced by the specified
         * FormIndex.
         * 
         * @param questionIndex
         */
        public void newRepeat(FormIndex questionIndex)
        {
            try
            {
                model.getForm().createNewRepeat(questionIndex);
            }
            catch (InvalidReferenceException ire)
            {
                throw new SystemException("Invalid reference while copying itemset answer: " + ire.Message);
            }
        }


        /**
         * Creates a new repeated instance of the group referenced by the current
         * FormIndex.
         * 
         * @param questionIndex
         */
        public void newRepeat()
        {
            newRepeat(model.getFormIndex());
        }


        /**
         * Deletes a repeated instance of a group referenced by the specified
         * FormIndex.
         * 
         * @param questionIndex
         * @return
         */
        public FormIndex deleteRepeat(FormIndex questionIndex)
        {
            return model.getForm().deleteRepeat(questionIndex);
        }


        /**
         * Deletes a repeated instance of a group referenced by the current
         * FormIndex.
         * 
         * @param questionIndex
         * @return
         */
        public FormIndex deleteRepeat()
        {
            return deleteRepeat(model.getFormIndex());
        }

        public void deleteRepeat(int n)
        {
            deleteRepeat(model.getForm().descendIntoRepeat(model.getFormIndex(), n));
        }

        /**
         * Sets the current language.
         * @param language
         */
        public void setLanguage(String language)
        {
            model.setLanguage(language);
        }
    }
}