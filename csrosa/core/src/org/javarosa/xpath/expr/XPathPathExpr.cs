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

using org.javarosa.core.model.condition;
using org.javarosa.core.model.data;
using org.javarosa.core.model.data.helper;
using org.javarosa.core.model.instance;
using org.javarosa.core.util.externalizable;
using org.javarosa.xform.util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace org.javarosa.xpath.expr{



public class XPathPathExpr : XPathExpression {
	public  const int INIT_CONTEXT_ROOT = 0;
	public  const int INIT_CONTEXT_RELATIVE = 1;
	public  const int INIT_CONTEXT_EXPR = 2;

	public int init_context;
	public XPathStep[] steps;

	//for INIT_CONTEXT_EXPR only
	public XPathFilterExpr filtExpr;

	public XPathPathExpr () { } //for deserialization

	public XPathPathExpr (int init_context, XPathStep[] steps) {
		this.init_context = init_context;
		this.steps = steps;
	}

	public XPathPathExpr (XPathFilterExpr filtExpr, XPathStep[] steps):
		this(INIT_CONTEXT_EXPR, steps) {
		this.filtExpr = filtExpr;
	}
	
	public TreeReference getReference () {
		return getReference(false);
	}
	
	/**
	 * translate an xpath path reference into a TreeReference
	 * TreeReferences only support a subset of true xpath paths; restrictions are:
	 *   simple child name tests 'child::name', '.', and '..' allowed only
	 *   no predicates
	 *   all '..' steps must come before anything else
	 */
	public TreeReference getReference (Boolean allowPredicates) {
		TreeReference ref_ = new TreeReference();
		Boolean parentsAllowed;
		
		switch (init_context) {
		case XPathPathExpr.INIT_CONTEXT_ROOT:
			ref_.setRefLevel(TreeReference.REF_ABSOLUTE);
			parentsAllowed = false;
			break;
		case XPathPathExpr.INIT_CONTEXT_RELATIVE:
			ref_.setRefLevel(0);
			parentsAllowed = true;
			break;
		default: throw new XPathUnsupportedException("filter expression");
		}
		
		for (int i = 0; i < steps.Length; i++) {
			XPathStep step = steps[i];
			
			if (!allowPredicates && step.predicates.Length > 0) {
				throw new XPathUnsupportedException("predicates");
			}
			
			if (step.axis == XPathStep.AXIS_SELF) {
				if (step.test != XPathStep.TEST_TYPE_NODE) {
					throw new XPathUnsupportedException("step other than 'child::name', '.', '..'");
				}
			} else if (step.axis == XPathStep.AXIS_PARENT) {
				if (!parentsAllowed || step.test != XPathStep.TEST_TYPE_NODE) {
					throw new XPathUnsupportedException("step other than 'child::name', '.', '..'");
				} else {
					ref_.incrementRefLevel();
				}
			} else if (step.axis == XPathStep.AXIS_ATTRIBUTE) {
				if (step.test == XPathStep.TEST_NAME) {
					ref_.add(step.name.ToString(), TreeReference.INDEX_ATTRIBUTE);
					parentsAllowed = false;
					//TODO: Can you step back from an attribute, or should this always be
					//the last step?
				} else {
					throw new XPathUnsupportedException("attribute step other than 'attribute::name");
				}
			}else if (step.axis == XPathStep.AXIS_CHILD) {
				if (step.test == XPathStep.TEST_NAME) {
					ref_.add(step.name.ToString(), TreeReference.INDEX_UNBOUND);
					parentsAllowed = false;
				} else if(step.test == XPathStep.TEST_NAME_WILDCARD) {
					ref_.add(TreeReference.NAME_WILDCARD, TreeReference.INDEX_UNBOUND);
					parentsAllowed = false;
				} else {
					throw new XPathUnsupportedException("step other than 'child::name', '.', '..'");
				}
			} else {
				throw new XPathUnsupportedException("step other than 'child::name', '.', '..'");
			}
		}		
		
		return ref_;
	}

    public override object eval(FormInstance m, EvaluationContext evalContext)
    {
		TreeReference genericRef = getReference();
		if (genericRef.isAbsolute() && m.getTemplatePath(genericRef) == null) {
			throw new XPathTypeMismatchException("Node " + genericRef.toString() + " does not exist!");
		}
		
		TreeReference ref_ = genericRef.contextualize(evalContext.ContextRef);
		List<TreeReference> nodesetRefs = m.expandReference(ref_);
		
		//to fix conditions based on non-relevant data, filter the nodeset by relevancy
		for (int i = 0; i < nodesetRefs.Count; i++) {
			if (!m.resolveReference((TreeReference)nodesetRefs[i]).isRelevant()) {
				nodesetRefs.RemoveAt(i);
				i--;
			}
		}
		
		return new XPathNodeset(nodesetRefs, m, evalContext);
	}

//	
//	boolean nodeset = forceNodeset;
//	if (!nodeset) {
//		//is this a nodeset? it is if the ref contains any unbound multiplicities AND the unbound nodes are repeatable
//		//the way i'm calculating this sucks; there has got to be an easier way to find out if a node is repeatable
//		TreeReference repeatTestRef = TreeReference.rootRef();
//		for (int i = 0; i < ref.size(); i++) {
//			repeatTestRef.add(ref.getName(i), ref.getMultiplicity(i));
//			if (ref.getMultiplicity(i) == TreeReference.INDEX_UNBOUND) {
//				if (m.getTemplate(repeatTestRef) != null) {
//					nodeset = true;
//					break;
//				}
//			}
//		}
//	}

	public static Object getRefValue (FormInstance model, EvaluationContext ec, TreeReference ref_) {
		if (ec.isConstraint && ref_.Equals(ec.ContextRef)) {
			//ITEMSET TODO: need to update this; for itemset/copy constraints, need to simulate a whole xml sub-tree here
			return unpackValue(ec.candidateValue);
		} else {
			TreeElement node = model.resolveReference(ref_);
			if (node == null) {
				//shouldn't happen -- only existent nodes should be in nodeset
				throw new XPathTypeMismatchException("Node " + ref_.toString() + " does not exist!");
			}
			
			return unpackValue(node.isRelevant() ? node.getValue() : null);
		}
	}
	
	public static Object unpackValue (IAnswerData val) {
		if (val == null) {
			return "";
		} else if (val is UncastData) {
			return val.Value;
		} else if (val is IntegerData) {
			return ( Double)(((int)val.Value));
		} else if (val is LongData) {
			return( Double)(((long)val.Value));	
		} else if (val is DecimalData) {
			return val.Value;			
		} else if (val is StringData) {
			return val.Value;
		} else if (val is SelectOneData) {
			return ((Selection)val.Value).Value;
		} else if (val is SelectMultiData) {
			return (new XFormAnswerDataSerializer()).serializeAnswerData(val);
		} else if (val is DateData) {
			return val.Value;
		} else if (val is BooleanData) {
			return val.Value;
		} else {
			Console.WriteLine("warning: unrecognized data type in xpath expr: " + val.GetType().Name);
			return val.Value; //is this a good idea?
		}
	}
	
	public String toString () {
		StringBuilder sb = new StringBuilder();
		
		sb.Append("{path-expr:");
		switch (init_context) {
		case INIT_CONTEXT_ROOT: sb.Append("abs"); break;
		case INIT_CONTEXT_RELATIVE: sb.Append("rel"); break;
		case INIT_CONTEXT_EXPR: sb.Append(filtExpr.toString()); break;
		}
		sb.Append(",{");
		for (int i = 0; i < steps.Length; i++) {
			sb.Append(steps[i].ToString());
			if (i < steps.Length - 1)
				sb.Append(",");
		}
		sb.Append("}}");
		
		return sb.ToString();
	}

	public Boolean equals (Object o) {
		if (o is XPathPathExpr) {
			XPathPathExpr x = (XPathPathExpr)o;
			
			//Shortcuts for easily comparable values
			if(init_context != x.init_context || steps.Length != x.steps.Length) {
				return false;
			}
			
			return ExtUtil.arrayEquals(steps, x.steps) && (init_context == INIT_CONTEXT_EXPR ? filtExpr.equals(x.filtExpr) : true);
		} else {
			return false;
		}
	}
	
	public void readExternal(BinaryReader in_, PrototypeFactory pf) {
		init_context = ExtUtil.readInt(in_);
		if (init_context == INIT_CONTEXT_EXPR) {
			filtExpr = (XPathFilterExpr)ExtUtil.read(in_, typeof(XPathFilterExpr), pf);
		}
		
		ArrayList v = (ArrayList)ExtUtil.read(in_, new ExtWrapList(typeof(XPathStep)), pf);
		steps = new XPathStep[v.Count];
		for (int i = 0; i < steps.Length; i++)
			steps[i] = (XPathStep)v[i];
	}

	public void writeExternal(BinaryWriter out_)  {
		ExtUtil.writeNumeric(out_, init_context);
		if (init_context == INIT_CONTEXT_EXPR) {
			ExtUtil.write(out_, filtExpr);
		}
		
		ArrayList v = new ArrayList();
		for (int i = 0; i < steps.Length; i++)
			v.Add(steps[i]);
		ExtUtil.write(out_, new ExtWrapList(v));
	}
	
	public static XPathPathExpr fromRef (TreeReference out_) {
		XPathPathExpr path = new XPathPathExpr();
		path.init_context = (out_.isAbsolute() ? INIT_CONTEXT_ROOT : INIT_CONTEXT_RELATIVE);
		path.steps = new XPathStep[out_.size()];
		for (int i = 0; i < path.steps.Length; i++) {
			if (out_.getName(i).Equals(TreeReference.NAME_WILDCARD)) {
				path.steps[i] = new XPathStep(XPathStep.AXIS_CHILD, XPathStep.TEST_NAME_WILDCARD);
			} else {
				path.steps[i] = new XPathStep(XPathStep.AXIS_CHILD, new XPathQName(out_.getName(i)));
			}
		}
		return path;
	}
	
	public Object pivot (FormInstance model, EvaluationContext evalContext, List<Object> pivots, Object sentinal) {
		TreeReference out_ = this.getReference();
		//Either concretely the sentinal, or "."
        if (out_.Equals(sentinal) || (out_.getRefLevel() == 0))
        {
			return sentinal;
		}
		else { 
			return this.eval(model, evalContext);
		}
	}
}
}