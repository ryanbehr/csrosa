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

/**
 * 
 */

using org.javarosa.core.log;
using System;
using System.IO;


namespace org.javarosa.core.api{
/**
 * IIncidentLogger's are used for instrumenting applications to identify usage
 * patterns, usability errors, and general trajectories through applications.
 * 
 * @author Clayton Sims
 * @date Apr 10, 2009 
 *
 */
public interface ILogger {
	
	 void log(string type, string message, ref DateTime logDate);
	
	 void clearLogs();
	
	//public <T> T serializeLogs(IFullLogSerializer<T> serializer);

     T serializelogs<T>(T input) where T : IFullLogSerializer<T>;

    //public T themethod<T>();
	
	 void serializeLogs(StreamLogSerializer serializer);
	 void serializeLogs(StreamLogSerializer serializer, int limit);
	
	 void panic();
	
	 int logSize();
	
	 void halt();
}
}