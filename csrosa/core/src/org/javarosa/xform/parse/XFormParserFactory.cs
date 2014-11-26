using System.IO;
using System.Xml;
namespace org.javarosa.xform.parse
{

    /**
     * Class factory for creating an XFormParser.
     * Supports experimental extensions of XFormParser.
     * 
     * @author Acellam Guy ,  mitchellsundt@gmail.com
     *
     */
    public class XFormParserFactory : IXFormParserFactory
    {

        public XFormParserFactory()
        {
        }

        public XFormParser getXFormParser(StreamReader reader)
        {
            return new XFormParser(reader);
        }

        public XFormParser getXFormParser(XmlDocument doc)
        {
            return new XFormParser(doc);
        }

        public XFormParser getXFormParser(StreamReader form, StreamReader instance)
        {
            return new XFormParser(form, instance);
        }

        public XFormParser getXFormParser(XmlDocument form, XmlDocument instance)
        {
            return new XFormParser(form, instance);
        }

    }

}