using org.javarosa.core.model.condition;
using org.javarosa.core.model.instance;
using org.javarosa.core.model.util.restorable;
using org.javarosa.core.services.locale;
using org.javarosa.core.util.externalizable;
using System;
using System.Collections.Generic;
using System.IO;
namespace org.javarosa.core.model
{


    public class ItemsetBinding : Externalizable, Localizable
    {

        /**
         * note that storing both the ref and expr for everything is kind of redundant, but we're forced
         * to since it's nearly impossible to convert between the two w/o having access to the underlying
         * xform/xpath classes, which we don't from the core model project
         */

        public TreeReference nodesetRef;   //absolute ref of itemset source nodes
        public IConditionExpr nodesetExpr; //path expression for source nodes; may be relative, may contain predicates
        public TreeReference contextRef;   //context ref for nodesetExpr; ref of the control parent (group/formdef) of itemset question
        //note: this is only here because its currently impossible to both (a) get a form control's parent, and (b)
        //convert expressions into refs while preserving predicates. once these are fixed, this field can go away

        public TreeReference labelRef;     //absolute ref of label
        public IConditionExpr labelExpr;   //path expression for label; may be relative, no predicates  
        public Boolean labelIsItext;       //if true, content of 'label' is an itext id

        public Boolean copyMode;           //true = copy subtree; false = copy string value
        public TreeReference copyRef;      //absolute ref to copy
        public TreeReference valueRef;     //absolute ref to value
        public IConditionExpr valueExpr;   //path expression for value; may be relative, no predicates (must be relative if copy mode)

        private TreeReference destRef; //ref that identifies the repeated nodes resulting from this itemset
        //not serialized -- set by QuestionDef.setDynamicChoices()
        private List<SelectChoice> choices; //dynamic choices -- not serialized, obviously

        public List<SelectChoice> getChoices()
        {
            return choices;
        }

        public void setChoices(List<SelectChoice> choices, Localizer localizer)
        {
            if (this.choices != null)
            {
                Console.WriteLine("warning: previous choices not cleared out");
                clearChoices();
            }
            this.choices = choices;

            //init localization
            if (localizer != null)
            {
                String curLocale = localizer.Locale;
                if (curLocale != null)
                {
                    localeChanged(curLocale, localizer);
                }
            }
        }

        public void clearChoices()
        {
            this.choices = null;
        }

        public void localeChanged(String locale, Localizer localizer)
        {
            if (choices != null)
            {
                for (int i = 0; i < choices.Count; i++)
                {
                    choices[i].localeChanged(locale, localizer);
                }
            }
        }

        public void setDestRef(QuestionDef q)
        {
            destRef = FormInstance.unpackReference(q.Bind).clone();
            if (copyMode)
            {
                destRef.add(copyRef.getNameLast(), TreeReference.INDEX_UNBOUND);
            }
        }

        public TreeReference getDestRef()
        {
            return destRef;
        }

        public IConditionExpr getRelativeValue()
        {
            TreeReference relRef = null;

            if (copyRef == null)
            {
                relRef = valueRef; //must be absolute in this case
            }
            else if (valueRef != null)
            {
                relRef = valueRef.relativize(copyRef);
            }

            return relRef != null ? RestoreUtils.xfFact.refToPathExpr(relRef) : null;
        }

        public void readExternal(BinaryReader in_renamed, PrototypeFactory pf)
        {
            nodesetRef = (TreeReference)ExtUtil.read(in_renamed, typeof(TreeReference), pf);
            nodesetExpr = (IConditionExpr)ExtUtil.read(in_renamed, new ExtWrapTagged(), pf);
            contextRef = (TreeReference)ExtUtil.read(in_renamed, typeof(TreeReference), pf);
            labelRef = (TreeReference)ExtUtil.read(in_renamed, typeof(TreeReference), pf);
            labelExpr = (IConditionExpr)ExtUtil.read(in_renamed, new ExtWrapTagged(), pf);
            valueRef = (TreeReference)ExtUtil.read(in_renamed, new ExtWrapNullable(typeof(TreeReference)), pf);
            valueExpr = (IConditionExpr)ExtUtil.read(in_renamed, new ExtWrapNullable(new ExtWrapTagged()), pf);
            copyRef = (TreeReference)ExtUtil.read(in_renamed, new ExtWrapNullable(typeof(TreeReference)), pf);
            labelIsItext = ExtUtil.readBool(in_renamed);
            copyMode = ExtUtil.readBool(in_renamed);
        }

        public void writeExternal(BinaryWriter out_)
        {
            ExtUtil.write(out_, nodesetRef);
            ExtUtil.write(out_, new ExtWrapTagged(nodesetExpr));
            ExtUtil.write(out_, contextRef);
            ExtUtil.write(out_, labelRef);
            ExtUtil.write(out_, new ExtWrapTagged(labelExpr));
            ExtUtil.write(out_, new ExtWrapNullable(valueRef));
            ExtUtil.write(out_, new ExtWrapNullable(valueExpr == null ? null : new ExtWrapTagged(valueExpr)));
            ExtUtil.write(out_, new ExtWrapNullable(copyRef));
            ExtUtil.writeBool(out_, labelIsItext);
            ExtUtil.writeBool(out_, copyMode);
        }

    }
}