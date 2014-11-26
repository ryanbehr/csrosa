using System.IO;
using System.Xml;
namespace org.javarosa.xform.parse
{
    /**
     * Interface for class factory for creating an XFormParser.
     * Supports experimental extensions of XFormParser.
     * 
     * @author mitchellsundt@gmail.com
     *
     */
    public interface IXFormParserFactory
    {
         XFormParser getXFormParser(StreamReader reader);

         XFormParser getXFormParser(XmlDocument doc);

         XFormParser getXFormParser(StreamReader form, StreamReader instance);

         XFormParser getXFormParser(XmlDocument form, XmlDocument instance);

    }
}