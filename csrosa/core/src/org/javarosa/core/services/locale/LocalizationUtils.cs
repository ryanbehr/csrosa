using org.javarosa.core.util;
using System;
using System.IO;
namespace org.javarosa.core.services.locale
{

/**
 * @author Acellam Guy ,  ctsims
 *
 */
public class LocalizationUtils {
	/**
	 * @param resourceName A path to a resource file provided in the current environment
	 *
	 * @return a dictionary of key/value locale pairs from a file in the resource directory 
	 * @throws IOException 
	 */
	public static OrderedHashtable parseLocaleInput(Stream stream){
			// TODO: This might very well fail. Best way to handle?
			OrderedHashtable locale = new OrderedHashtable();
			int chunk = 100;
			StreamReader isr;
			isr = new StreamReader(stream);
			Boolean done = false;
			char[] cbuf = new char[chunk];
			int offset = 0;
			int curline = 0;

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
                    String stringchunk = new System.String(cbuf, offset, read);
					
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
				stream.Close();
			return locale;
		}

		private static void parseAndAdd(OrderedHashtable locale, String line, int curline) {

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
                    System.Console.Out.WriteLine("Invalid line (#" + curline + ") read: " + line);
				}
			} else {
				//Check to see if there's anything after the '=' first. Otherwise there
				//might be some big problems.
				if(line.IndexOf('=') != line.Length-1) {
					String value = line.Substring(line.IndexOf('=') + 1,line.Length);
					locale.put(line.Substring(0, line.IndexOf('=')), value);
				}
				 else {
                     System.Console.Out.WriteLine("Invalid line (#" + curline + ") read: '" + line + "'. No value follows the '='.");
				}
			}
		}
}
}