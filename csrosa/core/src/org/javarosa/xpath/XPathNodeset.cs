using org.javarosa.core.model.condition;
using org.javarosa.core.model.instance;
using org.javarosa.xpath.expr;
using System;
using System.Collections.Generic;
using System.Text;
namespace org.javarosa.xpath
{


    public class XPathNodeset
    {

        public List<TreeReference> nodes;
        public FormInstance instance;
        public EvaluationContext ec;

        public XPathNodeset(List<TreeReference> nodes, FormInstance instance, EvaluationContext ec)
        {
            this.nodes = nodes;
            this.instance = instance;
            this.ec = ec;
        }

        public Object unpack()
        {
            if (size() == 0)
            {
                return XPathPathExpr.unpackValue(null);
            }
            else if (size() > 1)
            {
                throw new XPathTypeMismatchException("nodeset has more than one node [" + nodeContents() + "]; cannot convert to value");
            }
            else
            {
                return getValAt(0);
            }
        }

        public Object[] toArgList()
        {
            Object[] args = new Object[size()];

            for (int i = 0; i < size(); i++)
            {
                Object val = getValAt(i);

                //sanity check
                if (val == null)
                {
                    throw new SystemException("retrived a null value out of a nodeset! shouldn't happen!");
                }

                args[i] = val;
            }

            return args;
        }

        public int size()
        {
            return nodes.Count;
        }

        public TreeReference getRefAt(int i)
        {
            return nodes[i];
        }

        public Object getValAt(int i)
        {
            return XPathPathExpr.getRefValue(instance, ec, getRefAt(i));
        }

        private String nodeContents()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < nodes.Count; i++)
            {
                sb.Append(nodes[i].ToString());
                if (i < nodes.Count - 1)
                {
                    sb.Append(";");
                }
            }
            return sb.ToString();
        }
    }
}