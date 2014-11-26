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

    public class ASTNodeBinaryOp : ASTNode
    {
        public  const int ASSOC_LEFT = 1;
        public  const int ASSOC_RIGHT = 2;

        public int associativity;
        public ArrayList exprs;
        public ArrayList ops;

        public ASTNodeBinaryOp()
        {
            exprs = new ArrayList();
            ops = new ArrayList();
        }

        public override ArrayList getChildren()
        {
            return exprs;
        }

        public override XPathExpression build()
        {
            XPathExpression x;

            if (associativity == ASSOC_LEFT)
            {
                x = ((ASTNode)exprs[0]).build();
                for (int i = 1; i < exprs.Count; i++)
                {
                    x = getBinOpExpr(Parser.vectInt(ops, i - 1), x, ((ASTNode)exprs[i]).build());
                }
            }
            else
            {
                x = ((ASTNode)exprs[(exprs.Count - 1)]).build();
                for (int i = exprs.Count - 2; i >= 0; i--)
                {
                    x = getBinOpExpr(Parser.vectInt(ops, i), ((ASTNode)exprs[i]).build(), x);
                }
            }

            return x;
        }

        private XPathBinaryOpExpr getBinOpExpr(int op, XPathExpression a, XPathExpression b)
        {
            switch (op)
            {
                case Token.OR: return new XPathBoolExpr(XPathBoolExpr.OR, a, b);
                case Token.AND: return new XPathBoolExpr(XPathBoolExpr.AND, a, b);
                case Token.EQ: return new XPathEqExpr(true, a, b);
                case Token.NEQ: return new XPathEqExpr(false, a, b);
                case Token.LT: return new XPathCmpExpr(XPathCmpExpr.LT, a, b);
                case Token.LTE: return new XPathCmpExpr(XPathCmpExpr.LTE, a, b);
                case Token.GT: return new XPathCmpExpr(XPathCmpExpr.GT, a, b);
                case Token.GTE: return new XPathCmpExpr(XPathCmpExpr.GTE, a, b);
                case Token.PLUS: return new XPathArithExpr(XPathArithExpr.ADD, a, b);
                case Token.MINUS: return new XPathArithExpr(XPathArithExpr.SUBTRACT, a, b);
                case Token.MULT: return new XPathArithExpr(XPathArithExpr.MULTIPLY, a, b);
                case Token.DIV: return new XPathArithExpr(XPathArithExpr.DIVIDE, a, b);
                case Token.MOD: return new XPathArithExpr(XPathArithExpr.MODULO, a, b);
                case Token.UNION: return new XPathUnionExpr(a, b);
                default: throw new XPathSyntaxException();
            }
        }
    }
}