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
using System;
using System.Collections;
namespace org.javarosa.xpath.parser.ast
{


    public class ASTNodeAbstractExpr : ASTNode
    {
        public  const int CHILD = 1;
        public  const int TOKEN = 2;

        public ArrayList content; //mixture of tokens and ASTNodes

        public ASTNodeAbstractExpr()
        {
            content = new ArrayList();
        }

        public override ArrayList getChildren()
        {
            ArrayList children = new ArrayList();
            for (int i = 0; i < content.Count; i++)
            {
                if (getType(i) == CHILD)
                {
                    children.Add(content[i]);
                }
            }
            return children;
        }

        public override XPathExpression build()
        {
            if (content.Count == 1)
            {
                if (getType(0) == CHILD)
                {
                    return ((ASTNode)content[0]).build();
                }
                else
                {
                    switch (getTokenType(0))
                    {
                        case Token.NUM: return new XPathNumericLiteral((Double)getToken(0).val);
                        case Token.STR: return new XPathStringLiteral((String)getToken(0).val);
                        case Token.VAR: return new XPathVariableReference((XPathQName)getToken(0).val);
                        default: throw new XPathSyntaxException();
                    }
                }
            }
            else
            {
                throw new XPathSyntaxException();
            }
        }

        public Boolean isTerminal()
        {
            if (content.Count == 1)
            {
                int type = getTokenType(0);
                return (type == Token.NUM || type == Token.STR || type == Token.VAR);
            }
            else
            {
                return false;
            }
        }

        public Boolean isNormalized()
        {
            if (content.Count == 1 && getType(0) == CHILD)
            {
                ASTNode child = (ASTNode)content[0];
                if (child is ASTNodePathStep || child is ASTNodePredicate)
                    throw new SystemException("shouldn't happen");
                return true;
            }
            else
            {
                return isTerminal();
            }
        }

        public int getType(int i)
        {
            Object o = content[i];
            if (o is Token)
                return TOKEN;
            else if (o is ASTNode)
                return CHILD;
            else
                return -1;
        }

        public Token getToken(int i)
        {
            return (getType(i) == TOKEN ? (Token)content[i] : null);
        }

        public int getTokenType(int i)
        {
            Token t = getToken(i);
            return (t == null ? -1 : t.type);
        }

        //create new node containing children from [start,end)
        public ASTNodeAbstractExpr extract(int start, int end)
        {
            ASTNodeAbstractExpr node = new ASTNodeAbstractExpr();
            for (int i = start; i < end; i++)
            {
                node.content.Add(content[i]);
            }
            return node;
        }

        //remove children from [start,end) and replace with node n
        public void condense(ASTNode node, int start, int end)
        {
            for (int i = end - 1; i >= start; i--)
            {
                content.RemoveAt(i);
            }
            content.Insert(start, node);
        }

        //find the next incidence of 'target' at the current stack level
        //start points to the opening of the current stack level
        public int indexOfBalanced(int start, int target, int leftPush, int rightPop)
        {
            int depth = 0;
            int i = start + 1;
            Boolean found = false;

            while (depth >= 0 && i < content.Count)
            {
                int type = getTokenType(i);

                if (depth == 0 && type == target)
                {
                    found = true;
                    break;
                }

                if (type == leftPush)
                    depth++;
                else if (type == rightPop)
                    depth--;

                i++;
            }

            return (found ? i : -1);
        }

        public class Partition
        {
            public Partition()
            {
                pieces = new ArrayList();
                separators = new ArrayList();
            }

            public ArrayList pieces;
            public ArrayList separators;
        }

        //paritition the range [start,end), separating by any occurrence of separator
        public Partition partition(int[] separators, int start, int end)
        {
            Partition part = new Partition();
            ArrayList sepIdxs = new ArrayList();

            for (int i = start; i < end; i++)
            {
                for (int j = 0; j < separators.Length; j++)
                {
                    if (getTokenType(i) == separators[j])
                    {
                        part.separators.Add(separators[j]);
                        sepIdxs.Add(i);
                        break;
                    }
                }
            }

            for (int i = 0; i <= sepIdxs.Count; i++)
            {
                int pieceStart = (i == 0 ? start : Parser.vectInt(sepIdxs, i - 1) + 1);
                int pieceEnd = (i == sepIdxs.Count ? end : Parser.vectInt(sepIdxs, i));
                part.pieces.Add(extract(pieceStart, pieceEnd));
            }

            return part;
        }

        //partition by sep, to the end of the current stack level
        //start is the opening token of the current stack level
        public Partition partitionBalanced(int sep, int start, int leftPush, int rightPop)
        {
            Partition part = new Partition();
            ArrayList sepIdxs = new ArrayList();
            int end = indexOfBalanced(start, rightPop, leftPush, rightPop);
            if (end == -1)
                return null;

            int k = start;
            do
            {
                k = indexOfBalanced(k, sep, leftPush, rightPop);
                if (k != -1)
                {
                    sepIdxs.Add(k);
                    part.separators.Add(sep);
                }
            } while (k != -1);

            for (int i = 0; i <= sepIdxs.Count; i++)
            {
                int pieceStart = (i == 0 ? start + 1 : Parser.vectInt(sepIdxs, i - 1) + 1);
                int pieceEnd = (i == sepIdxs.Count ? end : Parser.vectInt(sepIdxs, i));
                part.pieces.Add(extract(pieceStart, pieceEnd));
            }

            return part;
        }
    }
}