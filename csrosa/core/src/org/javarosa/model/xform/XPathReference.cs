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

using org.javarosa.core.model;
/**
 * 
 */
using org.javarosa.core.model.instance;
using org.javarosa.core.util.externalizable;
using org.javarosa.xpath;
using org.javarosa.xpath.expr;
using org.javarosa.xpath.parser;
using System;
using System.IO;
namespace org.javarosa.model.xform
{

    /**
     * 
     */
    public class XPathReference : IDataReference
    {
        private TreeReference ref_;
        private String nodeset;

        public XPathReference()
        {

        }

        public XPathReference(String nodeset)
        {
            ref_ = getPathExpr(nodeset).getReference();
            this.nodeset = nodeset;
        }

        public static XPathPathExpr getPathExpr(String nodeset)
        {
            XPathExpression path;
            Boolean validNonPathExpr = false;

            try
            {
                path = XPathParseTool.parseXPath(nodeset);
                if (!(path is XPathPathExpr))
                {
                    validNonPathExpr = true;
                    throw new XPathSyntaxException();
                }

            }
            catch (XPathSyntaxException xse)
            {
                //make these checked exceptions?
                if (validNonPathExpr)
                {
                    throw new SystemException("Expected XPath path, got XPath expression: [" + nodeset + "]");
                }
                else
                {
                    throw new SystemException("Parse error in XPath path: [" + nodeset + "]");
                }
            }

            return (XPathPathExpr)path;
        }

        public XPathReference(XPathPathExpr path)
        {
            ref_ = path.getReference();
        }

        public XPathReference(TreeReference ref_)
        {
            this.ref_ = ref_;
        }

       

        public Boolean equals(Object o)
        {
            if (o is XPathReference)
            {
                return ref_.Equals(((XPathReference)o).ref_);
            }
            else
            {
                return false;
            }
        }

        public int hashCode()
        {
            return ref_.hashCode();
        }

        /* (non-Javadoc)
         * @see org.javarosa.core.services.storage.utilities.Externalizable#readExternal(java.io.DataInputStream)
         */
        public void readExternal(BinaryReader in_, PrototypeFactory pf)
        {
            nodeset = ExtUtil.nullIfEmpty(ExtUtil.readString(in_));
            ref_ = (TreeReference)ExtUtil.read(in_, typeof(TreeReference), pf);
        }

        /* (non-Javadoc)
         * @see org.javarosa.core.services.storage.utilities.Externalizable#writeExternal(java.io.DataOutputStream)
         */
        public void writeExternal(BinaryWriter out_)
        {
            ExtUtil.writeString(out_, ExtUtil.emptyIfNull(nodeset));
            ExtUtil.write(out_, ref_);
        }

        public object Reference
        {
            get
            {
                return ref_;
            }
            set
            {
                ref_ = (TreeReference)value;
            }
        }
    }
}