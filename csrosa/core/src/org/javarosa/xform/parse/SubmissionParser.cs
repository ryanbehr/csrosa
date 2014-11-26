using org.javarosa.core.model;
/**
 * 
 */
using System;
using System.Collections.Generic;
using System.Xml;
namespace org.javarosa.xform.parse
{

    /**
     * A Submission Profile 
     * 
     * @author Acellam Guy ,  ctsims
     *
     */
    public class SubmissionParser
    {

        public SubmissionProfile parseSubmission(String method, String action, IDataReference ref_, XmlElement element)
        {
            String mediatype = element.GetAttribute(null, "mediatype");
            Dictionary<String, String> attributeMap = new Dictionary<String, String>();
            int nAttr = element.Attributes.Count;
            for (int i = 0; i < nAttr; ++i)
            {
                String name = element.Attributes[i].Name;
                if (name.Equals("ref_")) continue;
                if (name.Equals("bind")) continue;
                if (name.Equals("method")) continue;
                if (name.Equals("action")) continue;
                String value = element.Attributes[i].Value;
                attributeMap.Add(name, value);
            }
            return new SubmissionProfile(ref_, method, action, mediatype, attributeMap);
        }

        public Boolean matchesCustomMethod(String method)
        {
            return false;
        }
    }
}