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
using DeserializationException = org.javarosa.core.util.externalizable.DeserializationException;
using ExtUtil = org.javarosa.core.util.externalizable.ExtUtil;
using ExtWrapList = org.javarosa.core.util.externalizable.ExtWrapList;
using Externalizable = org.javarosa.core.util.externalizable.Externalizable;
using PrototypeFactory = org.javarosa.core.util.externalizable.PrototypeFactory;
using System.Collections;

namespace org.javarosa.core.util
{

    //maintain an array of integers in sorted order. no duplicates allowed.
    public class SortedIntSet : Externalizable
    {
        ArrayList v;

        public SortedIntSet()
        {
            v = new ArrayList();
        }

        //add new value; return index inserted at if value was not already present, -1 if it was
        public int add(int n)
        {
            int i = indexOf(n, false);
            if (i != -1 && get(i) == n)
            {
                return -1;
            }
            else
            {
                v.Insert(n, i + 1);
                return i + 1;
            }
        }

        //remove a value; return index of item just removed if it was present, -1 if it was not
        public int remove(int n)
        {
            int i = indexOf(n, true);
            if (i != -1)
                v.RemoveAt(i);
            return i;
        }

        //return value at index
        public int get(int i)
        {
            return ((int)v[i]);
        }

        //return whether value is present
        public Boolean contains(int n)
        {
            return (indexOf(n, true) != -1);
        }

        //if exact = true: return the index of a value, -1 if not present
        //if exact = false: return the index of the highest value <= the target value, -1 if all values are greater than the target value
        public int indexOf(int n, Boolean exact)
        {
            int lo = 0;
            int hi = v.Count - 1;

            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                int val = get(mid);

                if (val < n)
                {
                    lo = mid + 1;
                }
                else if (val > n)
                {
                    hi = mid - 1;
                }
                else
                {
                    return mid;
                }
            }

            return exact ? -1 : lo - 1;
        }

        //return number of values
        public int size()
        {
            return v.Count;
        }

        //return underlying vector (outside modification may corrupt the datastructure)
        public ArrayList getVector()
        {
            return v;
        }
        //UPGRADE_TODO: Class 'java.io.DataInputStream' was converted to 'System.IO.BinaryReader' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataInputStream'"
        public virtual void readExternal(System.IO.BinaryReader in_Renamed, PrototypeFactory pf)
        {
            v = (System.Collections.ArrayList)ExtUtil.read(in_Renamed, new ExtWrapList(typeof(System.Int32)));
        }

        //UPGRADE_TODO: Class 'java.io.DataOutputStream' was converted to 'System.IO.BinaryWriter' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataOutputStream'"
        public virtual void writeExternal(System.IO.BinaryWriter out_Renamed)
        {
            ExtUtil.write(out_Renamed, new ExtWrapList(v));
        }
    }
}