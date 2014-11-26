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
namespace org.javarosa.core.model.instance
{

    public class TreeReference : Externalizable
    {
        public  const int DEFAULT_MUTLIPLICITY = 0;//multiplicity
        public  const int INDEX_UNBOUND = -1;//multiplicity
        public  const int INDEX_TEMPLATE = -2;//multiplicity
        public  const int INDEX_ATTRIBUTE = -4;//multiplicity flag for an attribute
        public  const int INDEX_REPEAT_JUNCTURE = -10;

        public  const int REF_ABSOLUTE = -1;

        public  const String NAME_WILDCARD = "*";

        private int refLevel; //0 = context node, 1 = parent, 2 = grandparent ...
        private ArrayList names; //ArrayList<String>
        private ArrayList multiplicity; //ArrayList<Integer>

        public static TreeReference rootRef()
        {
            TreeReference root = new TreeReference();
            root.refLevel = REF_ABSOLUTE;
            return root;
        }

        public static TreeReference selfRef()
        {
            TreeReference self = new TreeReference();
            self.refLevel = 0;
            return self;
        }

        public TreeReference()
        {
            names = new ArrayList(0);
            multiplicity = new ArrayList(0);
        }

        public int getMultiplicity(int index)
        {
            return ((int)multiplicity[index]);
        }

        public String getName(int index)
        {
            return (String)names[index];
        }

        public int getMultLast()
        {
            return ((int)multiplicity[multiplicity.LastIndexOf(multiplicity)]);
        }

        public String getNameLast()
        {
            return (String)names[names.LastIndexOf(names)];
        }

        public void setMultiplicity(int i, int mult)
        {
            multiplicity[i] = mult;
        }

        public int size()
        {
            return names.Count;
        }

        public void add(String name, int index)
        {
            names.Add(name);
            multiplicity.Add(index);
        }

        public int getRefLevel()
        {
            return refLevel;
        }

        public void setRefLevel(int refLevel)
        {
            this.refLevel = refLevel;
        }

        public void incrementRefLevel()
        {
            if (!isAbsolute())
            {
                refLevel++;
            }
        }

        public Boolean isAbsolute()
        {
            return refLevel == REF_ABSOLUTE;
        }

        //return true if this ref contains any unbound multiplicities... ie, there is ANY chance this ref
        //could ambiguously refer to more than one instance node.
        public Boolean isAmbiguous()
        {
            //ignore level 0, as /data implies /data[0]
            for (int i = 1; i < size(); i++)
            {
                if (getMultiplicity(i) == INDEX_UNBOUND)
                {
                    return true;
                }
            }
            return false;
        }

        //return a copy of the ref
        public TreeReference clone()
        {
            TreeReference newRef = new TreeReference();
            newRef.setRefLevel(this.refLevel);
            for (int i = 0; i < this.size(); i++)
            {
                newRef.add(this.getName(i), this.getMultiplicity(i));
            }
            return newRef;
        }

        /*
         * chop the lowest level off the ref so that the ref now represents the parent of the original ref
         * return true if we successfully got the parent, false if there were no higher levels
         */
        public Boolean removeLastLevel()
        {
            int size = this.size();
            if (size == 0)
            {
                if (isAbsolute())
                {
                    return false;
                }
                else
                {
                    refLevel++;
                    return true;
                }
            }
            else
            {
                names.RemoveAt(size - 1);
                multiplicity.RemoveAt(size - 1);
                return true;
            }
        }

        public TreeReference getParentRef()
        {
            //TODO: level
            TreeReference ref_ = this.clone();
            if (ref_.removeLastLevel())
            {
                return ref_;
            }
            else
            {
                return null;
            }
        }

        //return a new reference that is this reference anchored to a passed-in parent reference
        //if this reference is absolute, return self
        //if this ref has 'parent' steps (..), it can only be anchored if the parent ref is a relative ref consisting only of other 'parent' steps
        //return null in these invalid situations
        public TreeReference parent(TreeReference parentRef)
        {
            if (isAbsolute())
            {
                return this;
            }
            else
            {
                TreeReference newRef = parentRef.clone();

                if (refLevel > 0)
                {
                    if (!parentRef.isAbsolute() && parentRef.size() == 0)
                    {
                        parentRef.refLevel += refLevel;
                    }
                    else
                    {
                        return null;
                    }
                }

                for (int i = 0; i < names.Count; i++)
                {
                    newRef.add(this.getName(i), this.getMultiplicity(i));
                }

                return newRef;
            }
        }


        //very similar to parent(), but assumes contextRef refers to a singular, existing node in the model
        //this means we can do '/a/b/c + ../../d/e/f = /a/d/e/f', which we couldn't do in parent()
        //return null if context ref is not absolute, or we parent up past the root node
        //NOTE: this function still works even when contextRef contains INDEX_UNBOUND multiplicites... conditions depend on this behavior,
        //  even though it's slightly icky
        public TreeReference anchor(TreeReference contextRef)
        {
            if (isAbsolute())
            {
                return this.clone();
            }
            else if (!contextRef.isAbsolute())
            {
                return null;
            }
            else
            {
                TreeReference newRef = contextRef.clone();
                int contextSize = contextRef.size();
                if (refLevel > contextSize)
                {
                    return null; //tried to do '/..'
                }
                else
                {
                    for (int i = 0; i < refLevel; i++)
                    {
                        newRef.removeLastLevel();
                    }
                    for (int i = 0; i < size(); i++)
                    {
                        newRef.add(this.getName(i), this.getMultiplicity(i));
                    }
                    return newRef;
                }
            }
        }

        //TODO: merge anchor() and parent()

        public TreeReference contextualize(TreeReference contextRef)
        {
            if (!contextRef.isAbsolute())
                return null;

            TreeReference newRef = anchor(contextRef);

            for (int i = 0; i < contextRef.size() && i < newRef.size(); i++)
            {

                //If the the contextRef can provide a definition for a wildcard, do so
                if (TreeReference.NAME_WILDCARD.Equals(newRef.getName(i)) && !TreeReference.NAME_WILDCARD.Equals(contextRef.getName(i)))
                {
                    newRef.names[i] = contextRef.getName(i);
                }

                if (contextRef.getName(i).Equals(newRef.getName(i)))
                {
                    newRef.setMultiplicity(i, contextRef.getMultiplicity(i));
                }
                else
                {
                    break;
                }
            }

            return newRef;
        }

        public TreeReference relativize(TreeReference parent)
        {
            if (parent.isParentOf(this, false))
            {
                TreeReference relRef = selfRef();
                for (int i = parent.size(); i < this.size(); i++)
                {
                    relRef.add(this.getName(i), INDEX_UNBOUND);
                }
                return relRef;
            }
            else
            {
                return null;
            }
        }

        //turn unambiguous ref into a generic ref
        public TreeReference genericize()
        {
            TreeReference genericRef = clone();
            for (int i = 0; i < genericRef.size(); i++)
            {
                genericRef.setMultiplicity(i, INDEX_UNBOUND);
            }
            return genericRef;
        }

        //returns true if 'this' is parent of 'child'
        //return true if 'this' equals 'child' only if properParent is false
        public Boolean isParentOf(TreeReference child, Boolean properParent)
        {
            if (refLevel != child.refLevel)
                return false;
            if (child.size() < size() + (properParent ? 1 : 0))
                return false;

            for (int i = 0; i < size(); i++)
            {
                if (!this.getName(i).Equals(child.getName(i)))
                {
                    return false;
                }

                int parMult = this.getMultiplicity(i);
                int childMult = child.getMultiplicity(i);
                if (parMult != INDEX_UNBOUND && parMult != childMult && !(i == 0 && parMult == 0 && childMult == INDEX_UNBOUND))
                {
                    return false;
                }
            }

            return true;
        }

        /**
         * clone and extend a reference by one level
         * @param ref
         * @param name
         * @param mult
         * @return
         */
        public TreeReference extendRef(String name, int mult)
        {
            //TODO: Shouldn't work for this if this is an attribute ref;
            TreeReference childRef = this.clone();
            childRef.add(name, mult);
            return childRef;
        }

        public Boolean Equals(Object o)
        {
            if (this == o)
            {
                return true;
            }
            else if (o is TreeReference)
            {
                TreeReference ref_ = (TreeReference)o;

                if (this.refLevel == ref_.refLevel && this.size() == ref_.size())
                {
                    for (int i = 0; i < this.size(); i++)
                    {
                        String nameA = this.getName(i);
                        String nameB = ref_.getName(i);
                        int multA = this.getMultiplicity(i);
                        int multB = ref_.getMultiplicity(i);

                        if (!nameA.Equals(nameB))
                        {
                            return false;
                        }
                        else if (multA != multB)
                        {
                            if (i == 0 && (multA == 0 || multA == INDEX_UNBOUND) && (multB == 0 || multB == INDEX_UNBOUND))
                            {
                                // /data and /data[0] are functionally the same
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public int hashCode()
        {
            int hash = (refLevel).GetHashCode();
            for (int i = 0; i < size(); i++)
            {
                //NOTE(ctsims): It looks like this is only using Integer to
                //get the hashcode method, but that method
                //is just returning the int value, I think, so
                //this should potentially just be replaced by
                //an int.
                int mult = getMultiplicity(i);
                if (i == 0 && mult == INDEX_UNBOUND)
                    mult = 0;

                hash ^= getName(i).GetHashCode();
                hash ^= mult.GetHashCode();
            }
            return hash;
        }

        public String toString()
        {
            return toString(true);
        }

        public String toString(Boolean includePredicates)
        {
            StringBuilder sb = new StringBuilder();
            if (isAbsolute())
            {
                sb.Append("/");
            }
            else
            {
                for (int i = 0; i < refLevel; i++)
                    sb.Append("../");
            }
            for (int i = 0; i < size(); i++)
            {
                String name = getName(i);
                int mult = getMultiplicity(i);

                if (mult == INDEX_ATTRIBUTE)
                {
                    sb.Append("@");
                }
                sb.Append(name);

                if (includePredicates)
                {
                    switch (mult)
                    {
                        case INDEX_UNBOUND: break;
                        case INDEX_TEMPLATE: sb.Append("[@template]"); break;
                        case INDEX_REPEAT_JUNCTURE: sb.Append("[@juncture]"); break;
                        default:
                            if (i > 0 || mult != 0)
                                sb.Append("[" + (mult + 1) + "]");
                            break;
                    }
                }

                if (i < size() - 1)
                    sb.Append("/");
            }
            return sb.ToString();
        }


        public void readExternal(BinaryReader in_Renamed, PrototypeFactory pf)
        {
            refLevel = ExtUtil.readInt(in_Renamed);
            names = (ArrayList)ExtUtil.read(in_Renamed, new ExtWrapList(typeof(String)), pf);
            multiplicity = (ArrayList)ExtUtil.read(in_Renamed, new ExtWrapList(typeof(int)), pf);
        }

        public void writeExternal(BinaryWriter out_renamed)
        {
            ExtUtil.writeNumeric(out_renamed, refLevel);
            ExtUtil.write(out_renamed, new ExtWrapList(names));
            ExtUtil.write(out_renamed, new ExtWrapList(multiplicity));
        }
    }
}