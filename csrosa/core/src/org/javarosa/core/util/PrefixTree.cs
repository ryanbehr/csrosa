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

using System;
using System.Collections;
namespace org.javarosa.core.util
{
    public class PrefixTree
    {
        private PrefixTreeNode root;

        public PrefixTree()
        {
            root = new PrefixTreeNode("");
        }

        public static int sharedPrefixLength(String a, String b)
        {
            int len;

            for (len = 0; len < a.Length && len < b.Length; len++)
            {
                if (a[len] != b[len])
                    break;
            }

            return len;
        }

        public void addString(String s)
        {
            PrefixTreeNode current = root;

            while (s.Length > 0)
            {
                int len = 0;
                PrefixTreeNode node = null;

                if (current.children != null)
                {
                    for (IEnumerator e = current.children.GetEnumerator(); e.MoveNext(); )
                    {
                        node = (PrefixTreeNode)e;
                        len = sharedPrefixLength(s, node.prefix);
                        if (len > 0)
                            break;
                        node = null;
                    }
                }

                if (node == null)
                {
                    node = new PrefixTreeNode(s);
                    len = s.Length;

                    if (current.children == null)
                        current.children = new ArrayList();
                    current.children.Add(node);
                }
                else if (len < node.prefix.Length)
                {
                    String prefix = s.Substring(0, len);
                    PrefixTreeNode interimNode = new PrefixTreeNode(prefix);

                    current.children.Remove(node);
                    node.prefix = node.prefix.Substring(len);

                    current.children.Add(interimNode);
                    interimNode.children = new ArrayList();
                    interimNode.children.Add(node);

                    node = interimNode;
                }

                current = node;
                s = s.Substring(len);
            }

            current.terminal = true;
        }

        public ArrayList getStrings()
        {
            ArrayList v = new ArrayList();
            root.decompose(v, "");
            return v;
        }

        public String toString()
        {
            return root.toString();
        }
    }
}