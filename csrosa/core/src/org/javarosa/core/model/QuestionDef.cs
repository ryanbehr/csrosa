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

using org.javarosa.core.model.utils;
using org.javarosa.core.services.locale;
using org.javarosa.core.util.externalizable;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
namespace org.javarosa.core.model
{

    /** 
     * The definition of a Question to be presented to users when
     * filling out a form.
     * 
     * QuestionDef requires that any IDataReferences that are used
     * are contained in the FormDefRMS's PrototypeFactoryDeprecated in order
     * to be properly deserialized. If they aren't, an exception
     * will be thrown at the time of deserialization. 
     * 
     * @author Daniel Kayiwa/Drew Roos
     *
     */
    public class QuestionDef : IFormElement, Localizable
    {
        private int id;
        private IDataReference binding;	/** reference to a location in the model to store data in */

        private int controlType;  /* The type of widget. eg TextInput,Slider,List etc. */
        private String appearanceAttr;
        private String helpTextID;
        private String labelInnerText;
        private String helpText;
        private String textID; /* The id (ref) pointing to the localized values of (pic-URIs,audio-URIs,text) */
        private String helpInnerText;



        private List<SelectChoice> choices;
        private ItemsetBinding dynamicChoices;

        internal System.Collections.ArrayList observers;

        public QuestionDef()
            : this(Constants.NULL_ID, Constants.DATATYPE_TEXT)
        {
        }

        public QuestionDef(int id, int controlType)
        {
            ID = id;
            ControlType = controlType;
            observers = new ArrayList();
        }

        virtual public int ID
        {
            get
            {
                return id;
            }

            set
            {
                this.id = value;
            }

        }
        virtual public IDataReference Bind
        {
            get
            {
                return binding;
            }

            set
            {
                this.binding = value;
            }

        }
        virtual public int ControlType
        {
            get
            {
                return controlType;
            }

            set
            {
                this.controlType = value;
            }

        }
        virtual public System.String AppearanceAttr
        {
            get
            {
                return appearanceAttr;
            }

            set
            {
                this.appearanceAttr = value;
            }

        }
        //UPGRADE_NOTE: Respective javadoc comments were merged.  It should be changed in order to comply with .NET documentation conventions. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1199'"
        /// <summary> Only if there is no localizable version of the &lthint&gt available should this method be used</summary>
        /// <summary> Only if there is no localizable version of the &lthint&gtavailable should this method be used</summary>
        virtual public System.String HelpText
        {
            get
            {
                return helpText;
            }

            set
            {
                this.helpText = value;
            }

        }
        virtual public System.String HelpTextID
        {
            get
            {
                return helpTextID;
            }

            set
            {
                this.helpTextID = value;
            }

        }
        virtual public int NumChoices
        {
            get
            {
                return (choices != null ? choices.Count : 0);
            }

        }
        virtual public ItemsetBinding DynamicChoices
        {
            get
            {
                return dynamicChoices;
            }

            set
            {
                if (value != null)
                {
                    value.setDestRef(this);
                }
                this.dynamicChoices = value;
            }

        }
        /// <summary> true if the answer to this question yields xml tree data, not a simple string value</summary>
        virtual public bool Complex
        {
            get
            {
                return (dynamicChoices != null && dynamicChoices.copyMode);
            }

        }

        virtual public int DeepChildCount
        {
            /*
            * (non-Javadoc)
            * @see org.javarosa.core.model.IFormElement#getDeepChildCount()
            */

            get
            {
                return 1;
            }

        }
        virtual public System.String LabelInnerText
        {
            get
            {
                return labelInnerText;
            }

            set
            {
                this.labelInnerText = value;
            }

        }
        virtual public System.String HelpInnerText
        {
            get
            {
                return helpInnerText;
            }

            set
            {
                this.helpInnerText = value;
            }

        }
        virtual public System.String TextID
        {
            get
            {
                return textID;
            }

            set
            {
                if (DateUtils.stringContains(value, ";"))
                {
                    System.Console.Error.WriteLine("Warning: TextID contains ;form modifier:: \"" + value.Substring(value.IndexOf(";")) + "\"... will be stripped.");
                    value = value.Substring(0, (value.IndexOf(";")) - (0)); //trim away the form specifier
                }
                this.textID = value;
            }

        }
        public void addSelectChoice(SelectChoice choice)
        {
            if (choices == null)
            {
                choices = new List<SelectChoice>();
            }
            choice.Index = choices.Count;
            choices.Add(choice);
        }

        public void removeSelectChoice(SelectChoice choice)
        {
            if (choices == null)
            {
                choice.Index = 0;
                return;
            }

            if (choices.Contains(choice))
            {
                choices.Remove(choice);
            }
        }

        public void removeAllSelectChoices()
        {
            if (choices != null)
            {
                choices.Clear();
            }
        }

        public List<SelectChoice> getChoices()
        {
            return choices;
        }

        public SelectChoice getChoice(int i)
        {
            return (SelectChoice)choices[i];
        }

        public int getNumChoices()
        {
            return (choices != null ? choices.Count : 0);
        }

        public SelectChoice getChoiceForValue(String value)
        {
            for (int i = 0; i < getNumChoices(); i++)
            {
                if (getChoice(i).Value.Equals(value))
                {
                    return getChoice(i);
                }
            }
            return null;
        }

        public ItemsetBinding getDynamicChoices()
        {
            return dynamicChoices;
        }

        public void setDynamicChoices(ItemsetBinding ib)
        {
            if (ib != null)
            {
                ib.setDestRef(this);
            }
            this.dynamicChoices = ib;
        }

        /**
         * true if the answer to this question yields xml tree data, not a simple string value
         */
        public Boolean isComplex()
        {
            return (dynamicChoices != null && dynamicChoices.copyMode);
        }

        //Deprecated
        public void localeChanged(String locale, Localizer localizer)
        {
            if (choices != null)
            {
                for (int i = 0; i < choices.Count; i++)
                {
                    ((SelectChoice)choices[i]).localeChanged(null, localizer);
                }
            }

            if (dynamicChoices != null)
            {
                dynamicChoices.localeChanged(locale, localizer);
            }

            alertStateObservers(FormElementStateListener_Fields.CHANGE_LOCALE);
        }

        virtual public List<IFormElement> Children
        {
            get
            {
                return null;
            }

            set
            {
                throw new System.SystemException("Can't set children on question def");
            }

        }

        public virtual void addChild(IFormElement fe)
        {
            throw new System.SystemException("Can't add children to question def");
        }

        public IFormElement getChild(int i)
        {
            return null;
        }

        /*
         * (non-Javadoc)
         * @see org.javarosa.core.util.Externalizable#readExternal(java.io.DataInputStream)
         */
        public void readExternal(BinaryReader dis, PrototypeFactory pf)
        {
            ID = ExtUtil.readInt(dis);
            binding = (IDataReference)ExtUtil.read(dis, new ExtWrapNullable(new ExtWrapTagged()), pf);
            AppearanceAttr = ((System.String)ExtUtil.read(dis, new ExtWrapNullable(typeof(System.String)), pf));
            TextID = ((System.String)ExtUtil.read(dis, new ExtWrapNullable(typeof(System.String)), pf));
            LabelInnerText = ((System.String)ExtUtil.read(dis, new ExtWrapNullable(typeof(System.String)), pf));
            HelpText = ((System.String)ExtUtil.read(dis, new ExtWrapNullable(typeof(System.String)), pf));
            HelpTextID = ((System.String)ExtUtil.read(dis, new ExtWrapNullable(typeof(System.String)), pf));
            HelpInnerText = ((System.String)ExtUtil.read(dis, new ExtWrapNullable(typeof(System.String)), pf));

            ControlType = ExtUtil.readInt(dis);

            choices = ExtUtil.nullIfEmpty((List<SelectChoice>)ExtUtil.read(dis, new ExtWrapList(typeof(SelectChoice)), pf));
            for (int i = 0; i < NumChoices; i++)
            {
                ((SelectChoice)choices[i]).Index = i;
            }
            DynamicChoices = (ItemsetBinding)ExtUtil.read(dis, new ExtWrapNullable(typeof(ItemsetBinding)));
        }

        /*
         * (non-Javadoc)
         * @see org.javarosa.core.util.Externalizable#writeExternal(java.io.DataOutputStream)
         */
        public void writeExternal(System.IO.BinaryWriter dos)
        {
            ExtUtil.writeNumeric(dos, ID);
            ExtUtil.write(dos, new ExtWrapNullable(binding == null ? null : new ExtWrapTagged(binding)));
            ExtUtil.write(dos, new ExtWrapNullable(AppearanceAttr));
            ExtUtil.write(dos, new ExtWrapNullable(TextID));
            ExtUtil.write(dos, new ExtWrapNullable(LabelInnerText));
            ExtUtil.write(dos, new ExtWrapNullable(HelpText));
            ExtUtil.write(dos, new ExtWrapNullable(HelpTextID));
            ExtUtil.write(dos, new ExtWrapNullable(HelpInnerText));

            ExtUtil.writeNumeric(dos, ControlType);

            ExtUtil.write(dos, new ExtWrapList(ExtUtil.emptyIfNull(choices)));
            ExtUtil.write(dos, new ExtWrapNullable(dynamicChoices));
        }

        /* === MANAGING OBSERVERS === */

        public void registerStateObserver(FormElementStateListener qsl)
        {
            if (!observers.Contains(qsl))
            {
                observers.Add(qsl);
            }
        }

        public void unregisterStateObserver(FormElementStateListener qsl)
        {
            observers.Remove(qsl);
        }

        public void unregisterAll()
        {
            observers.Clear();
        }

        public void alertStateObservers(int changeFlags)
        {
            for (IEnumerator e = observers.GetEnumerator(); e.MoveNext(); )
                ((FormElementStateListener)e.Current).formElementStateChanged(this, changeFlags);
        }

    }
}