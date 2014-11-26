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
using System.IO;
using System.Text;
using PrototypeManager = org.javarosa.core.services.PrototypeManager;
using OrderedMap = org.javarosa.core.util.OrderedHashtable;
using System.Collections.Generic;
using org.javarosa.core.model;
namespace org.javarosa.core.util.externalizable
{

    public class ExtUtil
    {
        public static byte[] serialize(Object o)
        {
            System.IO.MemoryStream baos = new System.IO.MemoryStream();
            try
            {
                write(new System.IO.BinaryWriter(baos), o);
            }
            catch (IOException ioe)
            {
                throw new SystemException("IOException writing to ByteArrayOutputStream; shouldn't happen!");
            }
            return (byte[])(Array)baos.ToArray();
        }

        public static System.Object deserialize(sbyte[] data, System.Type type)
        {
            System.IO.MemoryStream bais = new System.IO.MemoryStream((byte[])(Array)data);
            try
            {
                return read(new System.IO.BinaryReader(bais), type);
            }
            catch (System.IO.EndOfStreamException eofe)
            {
                throw new DeserializationException("Unexpectedly reached end of stream when deserializing");
            }
            catch (System.IO.IOException udfe)
            {
                throw new DeserializationException("Unexpectedly reached end of stream when deserializing");
            }
            finally
            {
                try
                {
                    bais.Close();
                }
                catch (System.IO.IOException e)
                {
                    //already closed. Don't sweat it
                }
            }
        }

        public static System.Object deserialize(sbyte[] data, ExternalizableWrapper ew)
        {
            System.IO.MemoryStream bais = new System.IO.MemoryStream((byte[])(Array)data);
            try
            {
                return read(new System.IO.BinaryReader(bais), ew);
            }
            catch (System.IO.EndOfStreamException eofe)
            {
                throw new DeserializationException("Unexpectedly reached end of stream when deserializing");
            }
            catch (System.IO.IOException udfe)
            {
                throw new DeserializationException("Unexpectedly reached end of stream when deserializing");
            }
            finally
            {
                try
                {
                    bais.Close();
                }
                catch (System.IO.IOException e)
                {
                    //already closed. Don't sweat it
                }
            }
        }

        public static int getSize(Object o)
        {
            return serialize(o).Length;
        }

        public static PrototypeFactory defaultPrototypes()
        {
            return PrototypeManager.Default;
        }

        public static void write(System.IO.BinaryWriter out_Renamed, System.Object data)
        {
            if (data is Externalizable)
            {
                ((Externalizable)data).writeExternal(out_Renamed);
            }
            else if (data is System.SByte)
            {
                writeNumeric(out_Renamed, (sbyte)((System.SByte)data));
            }
            else if (data is System.Int16)
            {
                writeNumeric(out_Renamed, (short)((System.Int16)data));
            }
            else if (data is System.Int32)
            {
                writeNumeric(out_Renamed, ((System.Int32)data));
            }
            else if (data is System.Int64)
            {
                writeNumeric(out_Renamed, (long)((System.Int64)data));
            }
            else if (data is System.Char)
            {
                writeChar(out_Renamed, ((System.Char)data));
            }
            else if (data is System.Single)
            {
                //UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Float.floatValue' may return a different value. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1043'"
                writeDecimal(out_Renamed, (float)((System.Single)data));
            }
            else if (data is System.Double)
            {
                writeDecimal(out_Renamed, ((System.Double)data));
            }
            else if (data is System.Boolean)
            {
                writeBool(out_Renamed, ((System.Boolean)data));
            }
            else if (data is System.String)
            {
                writeString(out_Renamed, (System.String)data);
            }
            else if (data is System.DateTime)
            {
                //UPGRADE_NOTE: ref keyword was added to struct-type parameters. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1303'"
                writeDate(out_Renamed, ref new System.DateTime[] { (System.DateTime)data }[0]);
            }
            else if (data is sbyte[])
            {
                writeBytes(out_Renamed, (sbyte[])data);
            }
            else
            {
                //UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Class.getName' may return a different value. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1043'"
                throw new System.InvalidCastException("Not a serializable datatype: " + data.GetType().FullName);
            }
        }


        public static void writeNumeric(System.IO.BinaryWriter out_Renamed, long val)
        {
            writeNumeric(out_Renamed, val, new ExtWrapIntEncodingUniform());
        }

        //UPGRADE_TODO: Class 'java.io.DataOutputStream' was converted to 'System.IO.BinaryWriter' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataOutputStream'"
        public static void writeNumeric(System.IO.BinaryWriter out_Renamed, long val, ExtWrapIntEncoding encoding)
        {
            write(out_Renamed, encoding.clone((System.Object)val));
        }

        //UPGRADE_TODO: Class 'java.io.DataOutputStream' was converted to 'System.IO.BinaryWriter' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataOutputStream'"
        public static void writeChar(System.IO.BinaryWriter out_Renamed, char val)
        {
            out_Renamed.Write((System.Char)val);
        }

        //UPGRADE_TODO: Class 'java.io.DataOutputStream' was converted to 'System.IO.BinaryWriter' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataOutputStream'"
        public static void writeDecimal(System.IO.BinaryWriter out_Renamed, double val)
        {
            out_Renamed.Write(val);
        }

        //UPGRADE_TODO: Class 'java.io.DataOutputStream' was converted to 'System.IO.BinaryWriter' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataOutputStream'"
        public static void writeBool(System.IO.BinaryWriter out_Renamed, bool val)
        {
            out_Renamed.Write(val);
        }

        public static void writeString(System.IO.BinaryWriter out_Renamed, System.String val)
        {
            //UPGRADE_ISSUE: Method 'java.io.DataOutputStream.writeUTF' was not converted. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1000_javaioDataOutputStreamwriteUTF_javalangString'"
            out_Renamed.Write(val);
            //we could easily come up with more efficient default encoding for string
        }

        public static void writeDate(System.IO.BinaryWriter out_Renamed, ref System.DateTime val)
        {
            //UPGRADE_TODO: Method 'java.util.Date.getTime' was converted to 'System.DateTime.Ticks' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javautilDategetTime'"
            writeNumeric(out_Renamed, val.Ticks);
            //time zone?
        }

        //UPGRADE_TODO: Class 'java.io.DataOutputStream' was converted to 'System.IO.BinaryWriter' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataOutputStream'"
        public static void writeBytes(System.IO.BinaryWriter out_Renamed, sbyte[] bytes)
        {
            ExtUtil.writeNumeric(out_Renamed, bytes.Length);
            if (bytes.Length > 0)
                //i think writing zero-length array might close the stream
                out_Renamed.Write((byte[])(Array)bytes);
        }

        //functions like these are bad; they should use the built-in list serialization facilities
        //UPGRADE_TODO: Class 'java.io.DataOutputStream' was converted to 'System.IO.BinaryWriter' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataOutputStream'"
        public static void writeInts(System.IO.BinaryWriter out_Renamed, int[] ints)
        {
            ExtUtil.writeNumeric(out_Renamed, ints.Length);

            foreach (int i in ints)
            {
                ExtUtil.writeNumeric(out_Renamed, i);
            }
        }

        public static Object read(System.IO.BinaryReader in_Renamed, System.Type type)
        {
            return read(in_Renamed, type, null);
        }

        public static System.Object read(System.IO.BinaryReader in_Renamed, System.Type type, PrototypeFactory pf)
        {
            if (typeof(Externalizable).IsAssignableFrom(type))
            {
                Externalizable ext = (Externalizable)PrototypeFactory.getInstance(type);
                ext.readExternal(in_Renamed, pf == null ? defaultPrototypes() : pf);
                return ext;
            }
            else if (type == typeof(System.SByte))
            {
                return (sbyte)readByte(in_Renamed);
            }
            else if (type == typeof(System.Int16))
            {
                return (short)readShort(in_Renamed);
            }
            else if (type == typeof(System.Int32))
            {
                return (System.Int32)readInt(in_Renamed);
            }
            else if (type == typeof(System.Int64))
            {
                return (long)readNumeric(in_Renamed);
            }
            else if (type == typeof(System.Char))
            {
                return readChar(in_Renamed);
            }
            else if (type == typeof(System.Single))
            {
                //UPGRADE_WARNING: Data types in Visual C# might be different.  Verify the accuracy of narrowing conversions. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1042'"
                return (float)readDecimal(in_Renamed);
            }
            else if (type == typeof(System.Double))
            {
                return (double)readDecimal(in_Renamed);
            }
            else if (type == typeof(System.Boolean))
            {
                return readBool(in_Renamed);
            }
            else if (type == typeof(System.String))
            {
                return readString(in_Renamed);
            }
            else if (type == typeof(System.DateTime))
            {
                return readDate(in_Renamed);
            }
            else if (type == typeof(sbyte[]))
            {
                return readBytes(in_Renamed);
            }
            else
            {
                //UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Class.getName' may return a different value. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1043'"
                throw new System.InvalidCastException("Not a deserializable datatype: " + type.FullName);
            }
        }

        public static Object read(BinaryReader in_r, ExternalizableWrapper ew)
        {
            return read(in_r, ew, null);
        }

        public static Object read(BinaryReader in_r, ExternalizableWrapper ew, PrototypeFactory pf)
        {
            ew.readExternal(in_r, pf == null ? defaultPrototypes() : pf);
            return ew.val;
        }

        //UPGRADE_TODO: Class 'java.io.DataInputStream' was converted to 'System.IO.BinaryReader' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataInputStream'"
        public static long readNumeric(System.IO.BinaryReader in_Renamed)
        {
            return readNumeric(in_Renamed, new ExtWrapIntEncodingUniform());
        }

        //UPGRADE_TODO: Class 'java.io.DataInputStream' was converted to 'System.IO.BinaryReader' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataInputStream'"
        public static long readNumeric(System.IO.BinaryReader in_Renamed, ExtWrapIntEncoding encoding)
        {
            try
            {
                return (long)((System.Int64)read(in_Renamed, encoding));
            }
            catch (DeserializationException de)
            {
                throw new System.SystemException("Shouldn't happen: Base-type encoding wrappers should never touch prototypes");
            }
        }

        public static int readInt(BinaryReader in_r)
        {
            return toInt(readNumeric(in_r));
        }

        public static short readShort(BinaryReader in_r)
        {
            return toShort(readNumeric(in_r));
        }

        public static byte readByte(BinaryReader in_r)
        {
            return toByte(readNumeric(in_r));
        }

        //UPGRADE_TODO: Class 'java.io.DataInputStream' was converted to 'System.IO.BinaryReader' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataInputStream'"
        public static char readChar(System.IO.BinaryReader in_Renamed)
        {
            return in_Renamed.ReadChar();
        }

        //UPGRADE_TODO: Class 'java.io.DataInputStream' was converted to 'System.IO.BinaryReader' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataInputStream'"
        public static double readDecimal(System.IO.BinaryReader in_Renamed)
        {
            return in_Renamed.ReadDouble();
        }

        //UPGRADE_TODO: Class 'java.io.DataInputStream' was converted to 'System.IO.BinaryReader' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataInputStream'"
        public static bool readBool(System.IO.BinaryReader in_Renamed)
        {
            return in_Renamed.ReadBoolean();
        }


        //UPGRADE_TODO: Class 'java.io.DataInputStream' was converted to 'System.IO.BinaryReader' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataInputStream'"
        public static System.String readString(System.IO.BinaryReader in_Renamed)
        {
            //UPGRADE_ISSUE: Method 'java.io.DataInputStream.readUTF' was not converted. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1000_javaioDataInputStreamreadUTF'"
            System.String s = in_Renamed.ReadString();

            return s;
        }

        //UPGRADE_TODO: Class 'java.io.DataInputStream' was converted to 'System.IO.BinaryReader' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataInputStream'"
        public static System.DateTime readDate(System.IO.BinaryReader in_Renamed)
        {
            //UPGRADE_TODO: Constructor 'java.util.Date.Date' was converted to 'System.DateTime.DateTime' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javautilDateDate_long'"
            return new System.DateTime(readNumeric(in_Renamed));
            //time zone?
        }

        //UPGRADE_TODO: Class 'java.io.DataInputStream' was converted to 'System.IO.BinaryReader' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataInputStream'"
        public static sbyte[] readBytes(System.IO.BinaryReader in_Renamed)
        {
            int size = (int)ExtUtil.readNumeric(in_Renamed);
            sbyte[] bytes = new sbyte[size];
            int read = 0;
            int toread = size;
            while (read != size)
            {
                read = in_Renamed.Read((byte[])(Array)bytes, 0, toread);
                toread -= read;
            }
            return bytes;
        }
        //bad
        public static int[] readInts(BinaryReader in_r)
        {
            int size = (int)ExtUtil.readNumeric(in_r);
            int[] ints = new int[size];
            for (int i = 0; i < size; ++i)
            {
                ints[i] = (int)ExtUtil.readNumeric(in_r);
            }
            return ints;
        }

        public static int toInt(long l)
        {
            if (l < int.MinValue || l > int.MaxValue)
                throw new ArithmeticException("Value (" + l + ") cannot fit into int");
            return (int)l;
        }

        public static short toShort(long l)
        {
            if (l < short.MinValue || l > short.MaxValue)
                throw new ArithmeticException("Value (" + l + ") cannot fit into short");
            return (short)l;
        }

        public static byte toByte(long l)
        {
            if (l < byte.MinValue || l > byte.MaxValue)
                throw new ArithmeticException("Value (" + l + ") cannot fit into byte");
            return (byte)l;
        }

        public static long toLong(System.Object o)
        {
            if (o is System.SByte)
            {
                return (sbyte)((System.SByte)o);
            }
            else if (o is System.Int16)
            {
                return (short)((System.Int16)o);
            }
            else if (o is System.Int32)
            {
                return ((System.Int32)o);
            }
            else if (o is System.Int64)
            {
                return (long)((System.Int64)o);
            }
            else if (o is System.Char)
            {
                return ((System.Char)o);
            }
            else
            {
                throw new System.InvalidCastException();
            }
        }

        public static byte[] nullIfEmpty(byte[] ba)
        {
            return (ba == null ? null : (ba.Length == 0 ? null : ba));
        }

        public static String nullIfEmpty(String s)
        {
            return (s == null ? null : (s.Length == 0 ? null : s));
        }

        public static ArrayList nullIfEmpty(ArrayList v)
        {
            return (v == null ? null : (v.Count == 0 ? null : v));
        }

        public static List<SelectChoice> nullIfEmpty(List<SelectChoice> v)
        {
            return (v == null ? null : (v.Count == 0 ? null : v));
        }

        public static Hashtable nullIfEmpty(Hashtable h)
        {
            return (h == null ? null : (h.Count == 0 ? null : h));
        }

        public static byte[] emptyIfNull(byte[] ba)
        {
            return ba == null ? new byte[0] : ba;
        }

        public static String emptyIfNull(String s)
        {
            return s == null ? "" : s;
        }

        public static ArrayList emptyIfNull(ArrayList v)
        {
            return v == null ? new ArrayList() : v;
        }
        public static List<SelectChoice> emptyIfNull( List<SelectChoice> v)
        {
            return v == null ? new List<SelectChoice>() : v;
        }
        /*public static List<SelectChoice> emptyIfNull(List<SelectChoice> v)
        {
            return v == null ? new List<SelectChoice>() : v;
        }*/
        public static Hashtable emptyIfNull(Hashtable h)
        {
            return h == null ? new Hashtable() : h;
        }

        public static Object unwrap(Object o)
        {
            return (o is ExternalizableWrapper ? ((ExternalizableWrapper)o).baseValue() : o);
        }

        public static Boolean Equals(Object a, Object b)
        {
            a = unwrap(a);
            b = unwrap(b);

            if (a == null)
            {
                return b == null;
            }
            else if (a is ArrayList)
            {
                return (b is ArrayList && vectorEquals((ArrayList)a, (ArrayList)b));
            }
            else if (a is Hashtable)
            {
                return (b is Hashtable && hashtableEquals((Hashtable)a, (Hashtable)b));
            }
            else
            {
                return a.Equals(b);
            }
        }

        public static Boolean vectorEquals(ArrayList a, ArrayList b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < a.Count; i++)
                {
                    if (!Equals(a[i], b[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public static Boolean arrayEquals(Object[] a, Object[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < a.Length; i++)
                {
                    if (!Equals(a[i], b[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public static Boolean hashtableEquals(Hashtable a, Hashtable b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }
            else if (a is OrderedHashtable != b is OrderedHashtable)
            {
                return false;
            }
            else
            {
                for (IEnumerator ea = a.GetEnumerator(); ea.MoveNext(); )
                {

                    if (!Equals(a[ea], b[ea]))
                    {
                        return false;
                    }
                }

                if (a is OrderedHashtable && b is OrderedHashtable)
                {
                    IEnumerator ea = a.GetEnumerator();
                    IEnumerator eb = b.GetEnumerator();

                    while (ea.MoveNext())
                    {
                        Object keyA = ea;
                        Object keyB = eb.MoveNext();

                        if (!keyA.Equals(keyB))
                        { //must use built-in equals for keys, as that's what hashtable uses
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        public static String printBytes(byte[] data)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            for (int i = 0; i < data.Length; i++)
            {
                String hex = data[i].ToString("X8");
                if (hex.Length == 1)
                    hex = "0" + hex;
                else
                    hex = hex.Substring(hex.Length - 2);
                sb.Append(hex);
                if (i < data.Length - 1)
                {
                    if ((i + 1) % 30 == 0)
                        sb.Append("\n ");
                    else if ((i + 1) % 10 == 0)
                        sb.Append("  ");
                    else
                        sb.Append(" ");
                }
            }
            sb.Append("]");
            return sb.ToString();
        }


    }
}