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

using org.javarosa.core.util;
using org.javarosa.core.util.externalizable;
/**
 * 
 */
using System;
using System.IO;
namespace org.javarosa.core.services.locale
{

/**
 * @author Acellam Guy ,  Clayton Sims
 * @date Jun 1, 2009 
 *
 */
public class ResourceFileDataSource : LocaleDataSource {
	
	String resourceURI;
	
	/**
	 * NOTE: FOR SERIALIZATION ONLY!
	 */
	public ResourceFileDataSource() {
		
	}
	
	/**
	 * Creates a new Data Source for Locale data with the given resource URI.
	 * 
	 * @param resourceURI a URI to the resource file from which data should be loaded
	 * @throws NullPointerException if resourceURI is null
	 */
	public ResourceFileDataSource(String resourceURI) {
		if(resourceURI == null) {
			throw new NullReferenceException("Resource URI cannot be null when creating a Resource File Data Source");
		}
		this.resourceURI = resourceURI;
	}

	/* (non-Javadoc)
	 * @see org.javarosa.core.services.locale.LocaleDataSource#getLocalizedText()
	 */
	public OrderedHashtable getLocalizedText() {
		return loadLocaleResource(resourceURI);
	}

	public virtual void  readExternal(System.IO.BinaryReader in_Renamed, PrototypeFactory pf)
		{
			//UPGRADE_ISSUE: Method 'java.io.DataInputStream.readUTF' was not converted. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1000_javaioDataInputStreamreadUTF'"
			resourceURI = in_Renamed.ReadString();
		}

	/* (non-Javadoc)
		* @see org.javarosa.core.util.externalizable.Externalizable#writeExternal(java.io.DataOutputStream)
		*/
		//UPGRADE_TODO: Class 'java.io.DataOutputStream' was converted to 'System.IO.BinaryWriter' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioDataOutputStream'"
		public virtual void  writeExternal(System.IO.BinaryWriter out_Renamed)
		{
			//UPGRADE_ISSUE: Method 'java.io.DataOutputStream.writeUTF' was not converted. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1000_javaioDataOutputStreamwriteUTF_javalangString'"
			out_Renamed.Write(resourceURI);
		}

	/**
	 * @param resourceName A path to a resource file provided in the current environment
	 *
	 * @return a dictionary of key/value locale pairs from a file in the resource directory 
	 */
	private OrderedHashtable loadLocaleResource(String resourceName) {
		System.IO.Stream is_Renamed = typeof(Type).Assembly.GetManifestResourceStream(resourceName);
		// TODO: This might very well fail. Best way to handle?
		OrderedHashtable locale = new OrderedHashtable();
		int chunk = 100;
		BinaryReader isr;
		try {
			isr = new BinaryReader(is_Renamed);
		}
		catch (Exception e) {
			throw new SystemException("Failed to load locale resource " + resourceName + ". Is it in the dll?");
		}
		Boolean done = false;
		char[] cbuf = new char[chunk];
		int offset = 0;
		int curline = 0;

		try {
			String line = "";
			while (!done) {
				int read = isr.Read(cbuf, offset, chunk - offset);
				if(read == -1) {
					done = true;
					if(line != "") {
						parseAndAdd(locale, line, curline);
					}
					break;
				}
				String stringchunk = new String(cbuf,offset,read);
				
				int index = 0;
				
				while(index != -1) {
					int nindex = stringchunk.IndexOf('\n',index);
					//UTF-8 often doesn't encode with newline, but with CR, so if we 
					//didn't find one, we'll try that
					if(nindex == -1) { nindex = stringchunk.IndexOf('\r',index); }
					if(nindex == -1) {
						line += stringchunk.Substring(index);
						break;
					}
					else {
						line += stringchunk.Substring(index,nindex);
						//Newline. process our string and start the next one.
						curline++;
						parseAndAdd(locale, line, curline);
						line = "";
					}
					index = nindex + 1;
				}
			}
		} catch (IOException e) {
			// TODO Auto-generated catch block
			Console.WriteLine(e.StackTrace);
		} finally {
			try {
				is_Renamed.Close();
			} catch (IOException e) {
				Console.Out.WriteLine("Binary Reader for resource file " + resourceURI + " failed to close. This will eat up your memory! Fix Problem! [" + e.Message + "]");
                Console.WriteLine(e.StackTrace);
			}
		}
		return locale;
	}

	private void parseAndAdd(OrderedHashtable locale, String line, int curline) {

		//trim whitespace.
		line = line.Trim();
		
		//clear comments
		while(line.IndexOf("#") != -1) {
			line = line.Substring(0, line.IndexOf("#"));
		}
		if(line.IndexOf('=') == -1) {
			// TODO: Invalid line. Empty lines are fine, especially with comments,
			// but it might be hard to get all of those.
			if(line.Trim().Equals("")) {
				//Empty Line
			} else {
				 Console.WriteLine("Invalid line (#" + curline + ") read: " + line);
			}
		} else {
			//Check to see if there's anything after the '=' first. Otherwise there
			//might be some big problems.
			if(line.IndexOf('=') != line.Length-1) {
				String value = line.Substring(line.IndexOf('=') + 1,line.Length);
				locale.put(line.Substring(0, line.IndexOf('=')), value);
			}
			 else {
                 Console.WriteLine("Invalid line (#" + curline + ") read: '" + line + "'. No value follows the '='.");
			}
		}
	}

}

}