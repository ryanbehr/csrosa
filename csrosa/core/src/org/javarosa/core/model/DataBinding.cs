using org.javarosa.core.model.condition;
using org.javarosa.core.util.externalizable;
using System;
using System.IO;
namespace org.javarosa.core.model
{

    /**
     * A data binding is an object that represents how a
     * data element is to be used in a form entry interaction.
     * 
     * It contains a reference to where the data should be retreived
     * and stored, as well as the preload parameters, and the
     * conditional logic for the question.
     * 
     * The class relies on any Data References that are used
     * in a form to be registered with the FormDefRMSUtility's
     * prototype factory in order to properly deserialize.
     * 
     * @author Drew Roos
     *
     */
    public class DataBinding : Externalizable
    {
        private String id;
        private IDataReference ref_;
        private int dataType;

        public Condition relevancyCondition;
        public Boolean relevantAbsolute;
        public Condition requiredCondition;
        public Boolean requiredAbsolute;
        public Condition readonlyCondition;
        public Boolean readonlyAbsolute;
        public IConditionExpr constraint;
        public Recalculate calculate;

        private String preload;
        private String preloadParams;
        public String constraintMessage;

        public DataBinding()
        {
            relevantAbsolute = true;
            requiredAbsolute = false;
            readonlyAbsolute = false;
        }

        /**
         * @return The data reference
         */
        public IDataReference Reference
        {
            get { return ref_; }
            set
            {
                this.ref_ = value;
            }
        }

        /**
         * @return the id
         */
        public String ID
        {
            get { return id; }
            set
            {
                this.id = value;
            }
        }

        /**
         * @return the dataType
         */
        public int DataType
        {
            get {return dataType;}
            set
            {
                this.dataType = value;
            }

        }

        /**
         * @return the preload
         */
        public String Preload
        {
            get { return preload; }
            set
            {
                this.preload = value;
            }
        }

        /**
         * @return the preloadParams
         */
        public String PreloadParams
        {
            get { return preloadParams; }
            set
            {
                this.preloadParams = value;
            }
        }

        /* (non-Javadoc)
         * @see org.javarosa.core.services.storage.utilities.Externalizable#readExternal(java.io.DataInputStream)
         */
        public void readExternal(BinaryReader in_, PrototypeFactory pf)
        {
            ID=((String)ExtUtil.read(in_, new ExtWrapNullable(typeof(String)), pf));
            DataType=(ExtUtil.readInt(in_));
            Preload=((String)ExtUtil.read(in_, new ExtWrapNullable(typeof(String)), pf));
            PreloadParams=((String)ExtUtil.read(in_, new ExtWrapNullable(typeof(String)), pf));
            ref_ = (IDataReference)ExtUtil.read(in_, new ExtWrapTagged());

            //don't bother reading relevancy/required/readonly/constraint/calculate right now; they're only used during parse anyway		
        }

        /* (non-Javadoc)
         * @see org.javarosa.core.services.storage.utilities.Externalizable#writeExternal(java.io.DataOutputStream)
         */
        public void writeExternal(BinaryWriter out_)
        {
            ExtUtil.write(out_, new ExtWrapNullable(ID));
            ExtUtil.writeNumeric(out_, DataType);
            ExtUtil.write(out_, new ExtWrapNullable(Preload));
            ExtUtil.write(out_, new ExtWrapNullable(PreloadParams));
            ExtUtil.write(out_, new ExtWrapTagged(ref_));

            //don't bother writing relevancy/required/readonly/constraint/calculate right now; they're only used during parse anyway
        }


    }


}