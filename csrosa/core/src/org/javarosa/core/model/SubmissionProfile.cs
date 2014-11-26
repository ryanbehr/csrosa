/**
 * 
 */
using org.javarosa.core.util.externalizable;
using System;
using System.Collections.Generic;
using System.IO;
namespace org.javarosa.core.model
{

    /**
     * A Submission Profile is a class which is responsible for
     * holding and processing the details of how a submission
     * should be handled. 
     * 
     * @author Acellam Guy ,  ctsims
     *
     */
    public class SubmissionProfile : Externalizable
    {

        IDataReference ref_;
        String method;
        String action;
        String mediaType;
        Dictionary<String, String> attributeMap;

        public SubmissionProfile()
        {

        }

        public SubmissionProfile(IDataReference ref_, String method, String action, String mediatype, Dictionary<String, String> attributeMap)
        {
            this.method = method;
            this.ref_ = ref_;
            this.action = action;
            this.mediaType = mediatype;
            this.attributeMap = attributeMap;
        }

        public IDataReference getRef()
        {
            return ref_;
        }

        public String getMethod()
        {
            return method;
        }

        public String getAction()
        {
            return action;
        }

        public String getMediaType()
        {
            return mediaType;
        }

        public String getAttribute(String name)
        {
            return attributeMap[name];
        }

        public void readExternal(BinaryReader in_, PrototypeFactory pf)
        {
            ref_ = (IDataReference)ExtUtil.read(in_, new ExtWrapTagged(typeof(IDataReference)));
            method = ExtUtil.readString(in_);
            action = ExtUtil.readString(in_);
            mediaType = ExtUtil.nullIfEmpty(ExtUtil.readString(in_));
            attributeMap = (Dictionary<String, String>)ExtUtil.read(in_, new ExtWrapMap(typeof(String), typeof(String)));
        }

        public void writeExternal(BinaryWriter out_)
        {
            ExtUtil.write(out_, new ExtWrapTagged(ref_));
            ExtUtil.writeString(out_, method);
            ExtUtil.writeString(out_, action);
            ExtUtil.writeString(out_, ExtUtil.emptyIfNull(mediaType));
            ExtUtil.write(out_, new ExtWrapMap(attributeMap));
        }


    }
}
