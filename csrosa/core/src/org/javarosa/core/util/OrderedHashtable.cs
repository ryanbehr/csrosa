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
using System.Text;
namespace org.javarosa.core.util
{

    public class OrderedHashtable : Hashtable
    {
        private ArrayList orderedKeys;

        public OrderedHashtable():base()
        {
            orderedKeys = new ArrayList();
        }

        public OrderedHashtable(int initialCapacity):base(initialCapacity)
        {
            orderedKeys = new ArrayList(initialCapacity);
        }

        public void clear()
        {
            orderedKeys.Clear();
            base.Clear();
        }

        public Object elementAt(int index)
        {
           
            return (keyAt(index));
        }

        public IEnumerator elements()
        {
            ArrayList elements = new ArrayList();
            for (int i = 0; i < Count; i++)
            {
                elements.Add(elementAt(i));
            }
            return elements.GetEnumerator();
        }

        public int indexOfKey(Object key)
        {
            return orderedKeys.IndexOf(key);
        }

        public Object keyAt(int index)
        {
            return orderedKeys.IndexOf(index);
        }

        public IEnumerator keys()
        {
            return orderedKeys.GetEnumerator();
        }

        public Object put(Object key, Object value)
        {
            if (key == null)
            {
                throw new NullReferenceException();
            }

            int i = orderedKeys.IndexOf(key);
            if (i == -1)
            {
                orderedKeys.Add(key);
            }
            else
            {
                orderedKeys.Insert(i, key);
            }
            base.Add(key, value);
            return base[key];
        }

        public Object remove(Object key)
        {
            orderedKeys.Remove(key);
            Object ob = base[key];
            base.Remove(key);
            return ob;
        }

        public void removeAt(int i)
        {
            remove(keyAt(i));
            orderedKeys.RemoveAt(i);
        }

        public String toString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            for (IEnumerator e = keys(); e.MoveNext(); )
            {
                Object key = e;
                sb.Append(key.ToString());
                sb.Append(" => ");
                sb.Append(this[key].ToString());
                //if (e.hasMoreElements())
                sb.Append(", ");
            }
            sb.Append("]");
            return sb.ToString();
        }
    }
}