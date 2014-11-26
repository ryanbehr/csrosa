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

    /** The definition of a group in a form or questionaire. 
     * 
     * @author Acellam Guy ,  Daniel Kayiwa
     *
     */
    public class GroupDef : IFormElement, Localizable
    {
        private List<IFormElement> children;	/** A list of questions on a group. */
        private Boolean repeat;  /** True if this is a "repeat", false if it is a "group" */
        private int id;	/** The group number. */
        private IDataReference binding;	/** reference to a location in the model to store data in */


        private String labelInnerText;
        private String appearanceAttr;
        private String textID;

        //custom phrasings for repeats
        public String chooseCaption;
        public String addCaption;
        public String delCaption;
        public String doneCaption;
        public String addEmptyCaption;
        public String doneEmptyCaption;
        public String entryHeader;
        public String delHeader;
        public String mainHeader;

        ArrayList observers;

        public Boolean noAddRemove = false;
        public IDataReference count = null;

        public GroupDef()
            : this(Constants.NULL_ID, null, false)
        {

        }

        public GroupDef(int id, List<IFormElement> children, Boolean repeat)
        {
            ID = id;
            Children = children;
            Repeat = repeat;
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
        virtual public List<IFormElement> Children
        {
            get
            {
                return children;
            }

            set
            {
                this.children = (value == null ? new List<IFormElement>() : value);
            }

        }

        public void addChild(IFormElement fe)
        {
            children.Add(fe);
        }

        public IFormElement getChild(int i)
        {
            if (children == null || i >= children.Count)
            {
                return null;
            }
            else
            {
                return (IFormElement)children[i];
            }
        }

        /**
         * @return true if this represents a <repeat> element
         */
        public Boolean Repeat
        {
            get { return repeat; }
            set { this.repeat = value; }
        }

        public String LabelInnerText
        {
            get { return labelInnerText; }
            set { labelInnerText = value; }
        }

       

        public String AppearanceAttr
        {
            get { return appearanceAttr; }
            set{  this.appearanceAttr = value;}
        }

        public void setAppearanceAttr(String appearanceAttr)
        {
            this.appearanceAttr = appearanceAttr;
        }

        public void localeChanged(String locale, Localizer localizer)
        {
            for (IEnumerator e = children.GetEnumerator(); e.MoveNext(); )
            {
                ((IFormElement)e.Current).localeChanged(locale, localizer);
            }
        }

        public IDataReference CountReference
        {
            get{return count;}
        }

        public override String ToString()
        {
            return "<group>";
        }
        /*
         * (non-Javadoc)
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

        /** Reads a group definition object from the supplied stream. */
        public void readExternal(BinaryReader dis, PrototypeFactory pf)
        {
            ID = ExtUtil.readInt(dis);
            setAppearanceAttr((String)ExtUtil.read(dis, new ExtWrapNullable(typeof(String)), pf));
            Bind = (IDataReference)ExtUtil.read(dis, new ExtWrapTagged(), pf);
            TextID = (String)ExtUtil.read(dis, new ExtWrapNullable(typeof(String)), pf);
            LabelInnerText = ((String)ExtUtil.read(dis, new ExtWrapNullable(typeof(String)), pf));
            Repeat = (ExtUtil.readBool(dis));
            Children = (List<IFormElement>)ExtUtil.read(dis, new ExtWrapListPoly(), pf);

            noAddRemove = ExtUtil.readBool(dis);
            count = (IDataReference)ExtUtil.read(dis, new ExtWrapNullable(new ExtWrapTagged()), pf);

            chooseCaption = ExtUtil.nullIfEmpty(ExtUtil.readString(dis));
            addCaption = ExtUtil.nullIfEmpty(ExtUtil.readString(dis));
            delCaption = ExtUtil.nullIfEmpty(ExtUtil.readString(dis));
            doneCaption = ExtUtil.nullIfEmpty(ExtUtil.readString(dis));
            addEmptyCaption = ExtUtil.nullIfEmpty(ExtUtil.readString(dis));
            doneEmptyCaption = ExtUtil.nullIfEmpty(ExtUtil.readString(dis));
            entryHeader = ExtUtil.nullIfEmpty(ExtUtil.readString(dis));
            delHeader = ExtUtil.nullIfEmpty(ExtUtil.readString(dis));
            mainHeader = ExtUtil.nullIfEmpty(ExtUtil.readString(dis));
        }

        /** Write the group definition object to the supplied stream. */
        public void writeExternal(BinaryWriter dos)
        {
            ExtUtil.writeNumeric(dos, ID);
            ExtUtil.write(dos, new ExtWrapNullable(AppearanceAttr));
            ExtUtil.write(dos, new ExtWrapTagged(Bind));
            ExtUtil.write(dos, new ExtWrapNullable(TextID));
            ExtUtil.write(dos, new ExtWrapNullable(LabelInnerText));
            ExtUtil.writeBool(dos, Repeat);
            ExtUtil.write(dos, new ExtWrapListPoly(Children));

            ExtUtil.writeBool(dos, noAddRemove);
            ExtUtil.write(dos, new ExtWrapNullable(count != null ? new ExtWrapTagged(count) : null));

            ExtUtil.writeString(dos, ExtUtil.emptyIfNull(chooseCaption));
            ExtUtil.writeString(dos, ExtUtil.emptyIfNull(addCaption));
            ExtUtil.writeString(dos, ExtUtil.emptyIfNull(delCaption));
            ExtUtil.writeString(dos, ExtUtil.emptyIfNull(doneCaption));
            ExtUtil.writeString(dos, ExtUtil.emptyIfNull(addEmptyCaption));
            ExtUtil.writeString(dos, ExtUtil.emptyIfNull(doneEmptyCaption));
            ExtUtil.writeString(dos, ExtUtil.emptyIfNull(entryHeader));
            ExtUtil.writeString(dos, ExtUtil.emptyIfNull(delHeader));
            ExtUtil.writeString(dos, ExtUtil.emptyIfNull(mainHeader));

        }

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

        virtual public System.String TextID
        {
            get
            {
                return textID;
            }

            set
            {
                if (value == null)
                {
                    this.textID = null;
                    return;
                }
                if (DateUtils.stringContains(value, ";"))
                {
                    System.Console.Error.WriteLine("Warning: TextID contains ;form modifier:: \"" + value.Substring(value.IndexOf(";")) + "\"... will be stripped.");
                    value = value.Substring(0, (value.IndexOf(";")) - (0)); //trim away the form specifier
                }
                this.textID = value;
            }

        }
    }
}