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
using System.Collections.Generic;
namespace org.javarosa.core.util
{

    /**
     * A Map is a data object that maintains a map from one set of data
     * objects to another. This data object is superior to a Hashtable
     * in instances where O(1) lookups are not a priority, due to its
     * smaller memory footprint.
     * 
     * Lookups in a map are accomplished in O(n) time.
     * 
     * @author Acellam Guy ,  Clayton Sims
     *
     */
    public class Map
    {
        ArrayList keys_ = new ArrayList();
        ArrayList elements = new ArrayList();


        public Map()
        {
            keys_ = new ArrayList();

            elements = new ArrayList();
        }

        public Map(int sizeHint)
        {

            keys_ = new ArrayList(sizeHint);

            elements = new ArrayList(sizeHint);
        }

        public Map(ArrayList keysSealed, ArrayList elementsSealed)
        {
            keys_ = null;
            elements = null;

            this.keys_ = keysSealed;
            this.elements = elementsSealed;
        }
        /**
         * Places the key/value pair in this map. Any existing
         * mapping keyed by the key parameter is removed.
         * 
         * @param key
         * @param value
         */
        public void put(Object key, Object value)
        {
            if (containsKey(key))
            {
                remove(key);
            }
            keys_.Add(key);
            elements.Add(value);
        }

        public IEnumerator keys()
        {
            return keys_.GetEnumerator();
        }

        public int size()
        {
            return keys_.Count;
        }

        /**
         * @param key
         * @return The object bound to the given key, if one exists. 
         * null otherwise.
         */
        public Object get(Object key)
        {
            int index = getIndex(key);
            if (index == -1)
            {
                return null;
            }
            return elements[index];
        }

        /**
         * Removes any mapping from the given key 
         * @param key
         */
        public void remove(Object key)
        {
            int index = getIndex(key);
            if (index == -1)
            {
                return;
            }
            keys_.RemoveAt(index);
            elements.RemoveAt(index);
            if (keys_.Count != elements.Count)
            {
                //This is _really bad_,
            }
        }

        /**
         * Removes all keys and values from this map.
         */
        public void clear()
        {
            keys_.Clear();
            elements.Clear();
        }

        /**
         * Whether or not the key is bound in this map
         * @param key 
         * @return True if there is an object bound to the given
         * key in this map. False otherwise.
         */
        public Boolean containsKey(Object key)
        {
            return getIndex(key) != -1;
        }

        private int getIndex(Object key)
        {
            for (int i = 0; i < keys_.Count; ++i)
            {
                if (keys_.IndexOf(i).Equals(key))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}