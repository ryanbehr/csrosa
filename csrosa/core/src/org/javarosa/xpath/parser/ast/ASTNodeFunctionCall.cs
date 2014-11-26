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

using org.javarosa.xpath.expr;
using System.Collections;
namespace org.javarosa.xpath.parser.ast
{


    public class ASTNodeFunctionCall : ASTNode
    {
        public XPathQName name;
        public ArrayList args;

        public ASTNodeFunctionCall(XPathQName name)
        {
            this.name = name;
            args = new ArrayList();
        }

        public override ArrayList getChildren()
        {
            return args;
        }

        public override XPathExpression build()
        {
            XPathExpression[] xargs = new XPathExpression[args.Count];
            for (int i = 0; i < args.Count; i++)
                xargs[i] = ((ASTNode)args[i]).build();

            return new XPathFuncExpr(name, xargs);
        }
    }
}