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
using System.Collections;
using System.IO;
using System.Text;
namespace org.javarosa.xpath.expr
{


    public class XPathStep : Externalizable
    {
        public  const int AXIS_CHILD = 0;
        public  const int AXIS_DESCENDANT = 1;
        public  const int AXIS_PARENT = 2;
        public  const int AXIS_ANCESTOR = 3;
        public  const int AXIS_FOLLOWING_SIBLING = 4;
        public  const int AXIS_PRECEDING_SIBLING = 5;
        public  const int AXIS_FOLLOWING = 6;
        public  const int AXIS_PRECEDING = 7;
        public  const int AXIS_ATTRIBUTE = 8;
        public  const int AXIS_NAMESPACE = 9;
        public  const int AXIS_SELF = 10;
        public  const int AXIS_DESCENDANT_OR_SELF = 11;
        public  const int AXIS_ANCESTOR_OR_SELF = 12;

        public  const int TEST_NAME = 0;
        public  const int TEST_NAME_WILDCARD = 1;
        public  const int TEST_NAMESPACE_WILDCARD = 2;
        public  const int TEST_TYPE_NODE = 3;
        public  const int TEST_TYPE_TEXT = 4;
        public  const int TEST_TYPE_COMMENT = 5;
        public  const int TEST_TYPE_PROCESSING_INSTRUCTION = 6;

        public static XPathStep ABBR_SELF()
        {
            return new XPathStep(AXIS_SELF, TEST_TYPE_NODE);
        }

        public static XPathStep ABBR_PARENT()
        {
            return new XPathStep(AXIS_PARENT, TEST_TYPE_NODE);
        }

        public static XPathStep ABBR_DESCENDANTS()
        {
            return new XPathStep(AXIS_DESCENDANT_OR_SELF, TEST_TYPE_NODE);
        }

        public int axis;
        public int test;
        public XPathExpression[] predicates;

        //test-dependent variables
        public XPathQName name; //TEST_NAME only
        public String namespace_; //TEST_NAMESPACE_WILDCARD only
        public String literal; //TEST_TYPE_PROCESSING_INSTRUCTION only

        public XPathStep() { } //for deserialization

        public XPathStep(int axis, int test)
        {
            this.axis = axis;
            this.test = test;
            this.predicates = new XPathExpression[0];
        }

        public XPathStep(int axis, XPathQName name)
            : this(axis, TEST_NAME)
        {

            this.name = name;
        }

        public XPathStep(int axis, String namespace_) :
            this(axis, TEST_NAMESPACE_WILDCARD)
        {
            this.namespace_ = namespace_;
        }

        public String ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("{step:");
            sb.Append(axisStr(axis));
            sb.Append(",");
            sb.Append(testStr());

            if (predicates.Length > 0)
            {
                sb.Append(",{");
                for (int i = 0; i < predicates.Length; i++)
                {
                    sb.Append(predicates[i].ToString());
                    if (i < predicates.Length - 1)
                        sb.Append(",");
                }
                sb.Append("}");
            }
            sb.Append("}");

            return sb.ToString();
        }

        public static String axisStr(int axis)
        {
            switch (axis)
            {
                case AXIS_CHILD: return "child";
                case AXIS_DESCENDANT: return "descendant";
                case AXIS_PARENT: return "parent";
                case AXIS_ANCESTOR: return "ancestor";
                case AXIS_FOLLOWING_SIBLING: return "following-sibling";
                case AXIS_PRECEDING_SIBLING: return "preceding-sibling";
                case AXIS_FOLLOWING: return "following";
                case AXIS_PRECEDING: return "preceding";
                case AXIS_ATTRIBUTE: return "attribute";
                case AXIS_NAMESPACE: return "namespace";
                case AXIS_SELF: return "self";
                case AXIS_DESCENDANT_OR_SELF: return "descendant-or-self";
                case AXIS_ANCESTOR_OR_SELF: return "ancestor-or-self";
                default: return null;
            }
        }

        public String testStr()
        {
            switch (test)
            {
                case TEST_NAME: return name.ToString();
                case TEST_NAME_WILDCARD: return "*";
                case TEST_NAMESPACE_WILDCARD: return namespace_ + ":*";
                case TEST_TYPE_NODE: return "node()";
                case TEST_TYPE_TEXT: return "text()";
                case TEST_TYPE_COMMENT: return "comment()";
                case TEST_TYPE_PROCESSING_INSTRUCTION: return "proc-instr(" + (literal == null ? "" : "\'" + literal + "\'") + ")";
                default: return null;
            }
        }

        public Boolean Equals(Object o)
        {
            if (o is XPathStep)
            {
                XPathStep x = (XPathStep)o;

                //shortcuts for faster evaluation
                if (axis != x.axis && test != x.test || predicates.Length != x.predicates.Length)
                {
                    return false;
                }

                switch (test)
                {
                    case TEST_NAME: if (!name.equals(x.name)) { return false; } break;
                    case TEST_NAMESPACE_WILDCARD: if (!namespace_.Equals(x.namespace_)) { return false; } break;
                    case TEST_TYPE_PROCESSING_INSTRUCTION: if (!ExtUtil.Equals(literal, x.literal)) { return false; } break;
                    default: break;
                }

                return ExtUtil.arrayEquals(predicates, x.predicates);
            }
            else
            {
                return false;
            }
        }

        public void readExternal(BinaryReader in_, PrototypeFactory pf)
        {
            axis = ExtUtil.readInt(in_);
            test = ExtUtil.readInt(in_);

            switch (test)
            {
                case TEST_NAME: name = (XPathQName)ExtUtil.read(in_, typeof(XPathQName)); break;
                case TEST_NAMESPACE_WILDCARD: namespace_ = ExtUtil.readString(in_); break;
                case TEST_TYPE_PROCESSING_INSTRUCTION: literal = (String)ExtUtil.read(in_, new ExtWrapNullable(typeof(String))); break;
            }

            ArrayList v = (ArrayList)ExtUtil.read(in_, new ExtWrapListPoly(), pf);
            predicates = new XPathExpression[v.Count];
            for (int i = 0; i < predicates.Length; i++)
                predicates[i] = (XPathExpression)v[i];
        }

        public void writeExternal(BinaryWriter out_)
        {
            ExtUtil.writeNumeric(out_, axis);
            ExtUtil.writeNumeric(out_, test);

            switch (test)
            {
                case TEST_NAME: ExtUtil.write(out_, name); break;
                case TEST_NAMESPACE_WILDCARD: ExtUtil.writeString(out_, namespace_); break;
                case TEST_TYPE_PROCESSING_INSTRUCTION: ExtUtil.write(out_, new ExtWrapNullable(literal)); break;
            }

            ArrayList v = new ArrayList();
            for (int i = 0; i < predicates.Length; i++)
                v.Add(predicates[i]);
            ExtUtil.write(out_, new ExtWrapListPoly(v));
        }
    }
}