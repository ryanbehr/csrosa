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
using System.IO;
namespace org.javarosa.core.util
{

    public class MD5InputStream
    {
        virtual public System.String HashCode
        {
            get
            {

                MD5 md5 = new MD5(null);
                sbyte[] bytes = new sbyte[8192];

                int bytesRead = 0;
               /* while ((bytesRead = in_Renamed is BufferedStream ? ((BufferedStream)in_Renamed).Read(bytes) : in_Renamed.Read((byte[])(Array)bytes, 0, bytes.Length)) != -1)
                {
                    md5.update(bytes, 0, bytesRead);
                }*/
                return MD5.toHex(md5.doFinal());//TODO
            }

        }
        internal System.IO.Stream in_Renamed;

        public MD5InputStream(System.IO.Stream i)
        {
            in_Renamed = i;
        }
    }
}