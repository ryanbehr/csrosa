using org.javarosa.core.model.data.helper;
using org.javarosa.core.model.instance;
using org.javarosa.core.services.locale;
using org.javarosa.core.util.externalizable;
using org.javarosa.xform.parse;
using System;
using System.IO;
namespace org.javarosa.core.model
{

    public class SelectChoice : Externalizable, Localizable
    {

        private String labelInnerText;
        private String textID;
        private Boolean isLocalizable_;
        private String value;
        private int index = -1;

        public TreeElement copyNode; //if this choice represents part of an <itemset>, and the itemset uses 'copy'
        //answer mode, this points to the node to be copied if this selection is chosen
        //this field only has meaning for dynamic choices, thus is unserialized

        //for deserialization only
        public SelectChoice()
        {

        }

        public SelectChoice(String labelID, String value)
            : this(labelID, null, value, true)
        {

        }

        /**
         * 
         * @param labelID can be null
         * @param labelInnerText can be null
         * @param value should not be null
         * @param isLocalizable
         * @throws XFormParseException if value is null
         */
        public SelectChoice(String labelID, String labelInnerText, String value, Boolean isLocalizable)
        {
            this.isLocalizable_ = isLocalizable;
            this.textID = labelID;
            this.labelInnerText = labelInnerText;
            if (value != null)
            {
                this.value = value;
            }
            else
            {
                throw new XFormParseException("SelectChoice{id,innerText}:{" + labelID + "," + labelInnerText + "}, has null Value!");
            }
        }

        public SelectChoice(String labelOrID, String Value, Boolean isLocalizable) :
            this(isLocalizable ? labelOrID : null,
                   isLocalizable ? null : labelOrID,
                   Value, isLocalizable)
        {

        }

        virtual public int Index
        {
            get
            {
                if (index == -1)
                {
                    throw new System.SystemException("trying to access choice index before it has been set!");
                }

                return index;
            }

            set
            {
                this.index = value;
            }

        }
        virtual public System.String Value
        {
            get
            {
                return value;
            }

        }
        virtual public bool Localizable
        {
            get
            {
                return isLocalizable_;
            }

            set
            {
                this.isLocalizable_ = value;
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
                this.textID = value;
            }

        }

        public String LabelInnerText
        {
            get { return labelInnerText; }
        }




        public void localeChanged(String locale, Localizer localizer)
        {
            //		if (captionLocalizable) {
            //			caption = localizer.getLocalizedText(captionID);
            //		}
        }

        public void readExternal(BinaryReader in_renamed, PrototypeFactory pf)
        {
            isLocalizable_ = ExtUtil.readBool(in_renamed);
            setLabelInnerText(ExtUtil.nullIfEmpty(ExtUtil.readString(in_renamed)));
            TextID = ExtUtil.nullIfEmpty(ExtUtil.readString(in_renamed));
            value = ExtUtil.nullIfEmpty(ExtUtil.readString(in_renamed));
            //index will be set by questiondef
        }

        public void writeExternal(BinaryWriter out_renamed)
        {
            ExtUtil.writeBool(out_renamed, isLocalizable_);
            ExtUtil.writeString(out_renamed, ExtUtil.emptyIfNull(labelInnerText));
            ExtUtil.writeString(out_renamed, ExtUtil.emptyIfNull(textID));
            ExtUtil.writeString(out_renamed, ExtUtil.emptyIfNull(value));
            //don't serialize index; it will be restored from questiondef
        }

        private void setLabelInnerText(String labelInnerText)
        {
            this.labelInnerText = labelInnerText;
        }

        public Selection selection()
        {
            return new Selection(this);
        }



        public String ToString()
        {
            return ((textID != null && textID != "") ? "{" + textID + "}" : "") + (labelInnerText != null ? labelInnerText : "") + " => " + value;
        }



    }
}