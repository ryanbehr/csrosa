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

using org.javarosa.core.util.externalizable;
using System;
using System.IO;
namespace org.javarosa.xpath.expr
{

    public class XPathQName : Externalizable
    {
        public String namespace_;
        public String name;

        public XPathQName() { } //for deserialization

        public XPathQName(String qname)
        {
            int sep = (qname == null ? -1 : qname.IndexOf(":"));
            if (sep == -1)
            {
                init(null, qname);
            }
            else
            {
                init(qname.Substring(0, sep), qname.Substring(sep + 1));
            }
        }

        public XPathQName(String namespace_, String name)
        {
            init(namespace_, name);
        }

        private void init(String namespace_, String name)
        {
            if (name == null ||
                    (name != null && name.Length == 0) ||
                    (namespace_ != null && namespace_.Length == 0))
                throw new ArgumentException("Invalid QName");

            this.namespace_ = namespace_;
            this.name = name;
        }

        public String ToString()
        {
            return (namespace_ == null ? name : namespace_ + ":" + name);
        }

        public Boolean equals(Object o)
        {
            if (o is XPathQName)
            {
                XPathQName x = (XPathQName)o;
                return ExtUtil.Equals(namespace_, x.namespace_) && name.Equals(x.name);
            }
            else
            {
                return false;
            }
        }

        public void readExternal(BinaryReader in_Renamed, PrototypeFactory pf) {
		namespace_ = (String)ExtUtil.read(in_Renamed, new ExtWrapNullable(typeof(String)));
		name = ExtUtil.readString(in_Renamed);
	}

        public void writeExternal(BinaryWriter out_renamed)
        {
            ExtUtil.write(out_renamed, new ExtWrapNullable(namespace_));
            ExtUtil.writeString(out_renamed, name);
        }
    }
}