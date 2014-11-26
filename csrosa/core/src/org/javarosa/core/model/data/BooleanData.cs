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

/// <summary> </summary>
using System;
using DeserializationException = org.javarosa.core.util.externalizable.DeserializationException;
using PrototypeFactory = org.javarosa.core.util.externalizable.PrototypeFactory;
namespace org.javarosa.core.model.data
{
	
	/// <author>  Clayton Sims
	/// </author>
	/// <date>  May 19, 2009 </date>
	/// <summary> 
	/// </summary>
	public class BooleanData : IAnswerData, System.ICloneable
	{
		virtual public System.String DisplayText
		{
			get
			{
				if (data)
				{
					return "True";
				}
				else
				{
					return "False";
				}
			}
			
		}
		virtual public System.Object Value
		{
			get
			{
				return data;
			}
			
			set
			{
				data = ((System.Boolean) value);
			}
			
		}
		
		internal bool data;
		
		/// <summary> NOTE: ONLY FOR SERIALIZATION</summary>
		public BooleanData()
		{
		}
		
		public BooleanData(bool data)
		{
			this.data = data;
		}
		
		
        public virtual void  readExternal(System.IO.BinaryReader in_Renamed, PrototypeFactory pf)
		{
			data = in_Renamed.ReadBoolean();
		}
		
		/* (non-Javadoc)
		* @see org.javarosa.core.util.externalizable.Externalizable#writeExternal(java.io.DataOutputStream)
		*/
		
		//UPGRADE_TODO: Class 'java.io.DataOutputStream' was converted to 'System.IO.BinaryWriter' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataOutputStream'"
		public virtual void  writeExternal(System.IO.BinaryWriter out_Renamed)
		{
			out_Renamed.Write(data);
		}

        public  UncastData uncast()
		{
			return new UncastData(data?"1":"0");
		}


        public object Clone()
        {
            return new BooleanData(data);
        }

        IAnswerData IAnswerData.cast(UncastData data)
        {
            if ("1".Equals(data))
            {
                return new BooleanData(true);
            }

            if ("0".Equals(data))
            {
                return new BooleanData(false);
            }

            throw new System.ArgumentException("Invalid cast of data [" + data.value_Renamed + "] to type Boolean");
        }
    }
}