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

using org.javarosa.core.model;
using org.javarosa.core.model.condition;
using org.javarosa.core.model.instance;
using org.javarosa.core.model.util.restorable;
using org.javarosa.core.services;
using org.javarosa.core.services.locale;
using org.javarosa.core.util;
using org.javarosa.core.util.externalizable;
using org.javarosa.model.xform;
using org.javarosa.xform.util;
using org.javarosa.xpath;
using org.javarosa.xpath.expr;
using org.javarosa.xpath.parser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
namespace org.javarosa.xform.parse
{

    public class XFormParser
    {

        //Constants to clean up code and prevent user error
        private  const String ID_ATTR = "id";
        private  const String FORM_ATTR = "form";
        private  const String APPEARANCE_ATTR = "appearance";
        private  const String NODESET_ATTR = "nodeset";
        private  const String LABEL_ELEMENT = "label";
        private const String VALUE = "value";
        private  const String ITEXT_CLOSE = "')";
        private  const String ITEXT_OPEN = "jr:itext('";
        private  const String BIND_ATTR = "bind";
        private  const String REF_ATTR = "ref";
        private const String SELECTONE = "select1";
        private  const String SELECT = "select";

        public  const String NAMESPACE_JAVAROSA = "http://openrosa.org/javarosa";
        public  const String NAMESPACE_HTML = "http://www.w3.org/1999/xhtml";

        private  const int CONTAINER_GROUP = 1;
        private  const int CONTAINER_REPEAT = 2;

        private static IDictionary<String, IElementHandler> topLevelHandlers;
        private static IDictionary<String, IElementHandler> groupLevelHandlers;
        private static IDictionary<String, int> typeMappings;
        private static PrototypeFactoryDeprecated modelPrototypes;
        private static List<SubmissionParser> submissionParsers;

        private StreamReader _reader;
        private XmlDocument _xmldoc;
        private FormDef _f;

        private StreamReader _instReader;
        private XmlDocument _instDoc;

        private Boolean modelFound;
        private IDictionary<String, DataBinding> bindingsByID;
        private List<DataBinding> bindings;
        private List<TreeReference> actionTargets;
        private List<TreeReference> repeats;
        private List<ItemsetBinding> itemsets;
        private List<TreeReference> selectOnes;
        private List<TreeReference> selectMultis;
        private XmlElement instanceNode; //top-level data node of the instance; saved off so it can be processed after the <bind>s
        private String defaultNamespace;
        private List<String> itextKnownForms;

        private FormInstance repeatTree; //pseudo-data model tree that describes the repeat structure of the instance;
        //useful during instance processing and validation

        //incremented to provide unique question ID for each question
        private int serialQuestionID = 1;


        private static void staticInit()
        {
            initProcessingRules();
            initTypeMappings();
            modelPrototypes = new PrototypeFactoryDeprecated();
            submissionParsers = new List<SubmissionParser>();
        }

        private class AnonymousClassIElementHandler : IElementHandler
        {
            public virtual void handle(XFormParser p, XmlElement e, System.Object parent)
            {
                p.parseTitle(e);
            }
        }
        private class AnonymousClassIElementHandler1 : IElementHandler
        {
            public virtual void handle(XFormParser p, XmlElement e, System.Object parent)
            {
                p.parseMeta(e);
            }
        }
        private class AnonymousClassIElementHandler2 : IElementHandler
        {
            public virtual void handle(XFormParser p, XmlElement e, System.Object parent)
            {
                p.parseModel(e);
            }
        }
        private class AnonymousClassIElementHandler3 : IElementHandler
        {
            public virtual void handle(XFormParser p, XmlElement e, System.Object parent)
            {
                p.parseControl((IFormElement)parent, e, Constants.CONTROL_INPUT);
            }
        }
        private class AnonymousClassIElementHandler4 : IElementHandler
        {
            public virtual void handle(XFormParser p, XmlElement e, System.Object parent)
            {
                p.parseControl((IFormElement)parent, e, Constants.CONTROL_SECRET);
            }
        }
        private class AnonymousClassIElementHandler5 : IElementHandler
        {
            public virtual void handle(XFormParser p, XmlElement e, System.Object parent)
            {
                p.parseControl((IFormElement)parent, e, Constants.CONTROL_SELECT_MULTI);
            }
        }
        private class AnonymousClassIElementHandler6 : IElementHandler
        {
            public virtual void handle(XFormParser p, XmlElement e, System.Object parent)
            {
                p.parseControl((IFormElement)parent, e, Constants.CONTROL_SELECT_ONE);
            }
        }
        private class AnonymousClassIElementHandler7 : IElementHandler
        {
            public virtual void handle(XFormParser p, XmlElement e, System.Object parent)
            {
                p.parseGroup((IFormElement)parent, e, org.javarosa.xform.parse.XFormParser.CONTAINER_GROUP);
            }
        }
        private class AnonymousClassIElementHandler8 : IElementHandler
        {
            public virtual void handle(XFormParser p, XmlElement e, System.Object parent)
            {
                p.parseGroup((IFormElement)parent, e, org.javarosa.xform.parse.XFormParser.CONTAINER_REPEAT);
            }
        }
        private class AnonymousClassIElementHandler9 : IElementHandler
        {
            public virtual void handle(XFormParser p, XmlElement e, System.Object parent)
            {
                p.parseGroupLabel((GroupDef)parent, e);
            }
        }
        private class AnonymousClassIElementHandler10 : IElementHandler
        {
            public virtual void handle(XFormParser p, XmlElement e, System.Object parent)
            {
                p.parseControl((IFormElement)parent, e, Constants.CONTROL_TRIGGER);
            }
        }
        private class AnonymousClassIElementHandler11 : IElementHandler
        {
            public virtual void handle(XFormParser p, XmlElement e, System.Object parent)
            {
                p.parseUpload((IFormElement)parent, e, Constants.CONTROL_UPLOAD);
            }
        }


        private static void initProcessingRules()
        {
            IElementHandler title = new AnonymousClassIElementHandler();
            IElementHandler meta = new AnonymousClassIElementHandler1();
            IElementHandler model = new AnonymousClassIElementHandler2();
            IElementHandler input = new AnonymousClassIElementHandler3();
            IElementHandler secret = new AnonymousClassIElementHandler4();
            IElementHandler select = new AnonymousClassIElementHandler5();
            IElementHandler select1 = new AnonymousClassIElementHandler6();
            IElementHandler group = new AnonymousClassIElementHandler7();
            IElementHandler repeat = new AnonymousClassIElementHandler8();
            IElementHandler groupLabel = new AnonymousClassIElementHandler9();
            IElementHandler trigger = new AnonymousClassIElementHandler10();
            IElementHandler upload = new AnonymousClassIElementHandler11();

            groupLevelHandlers = new Dictionary<String, IElementHandler>();
            groupLevelHandlers.Add("input", input);
            groupLevelHandlers.Add("secret", secret);
            groupLevelHandlers.Add(SELECT, select);
            groupLevelHandlers.Add(SELECTONE, select1);
            groupLevelHandlers.Add("group", group);
            groupLevelHandlers.Add("repeat", repeat);
            groupLevelHandlers.Add("trigger", trigger); //multi-purpose now; need to dig deeper
            groupLevelHandlers.Add(Constants.XFTAG_UPLOAD, upload);

            topLevelHandlers = new Dictionary<String, IElementHandler>();
            for (IEnumerator en = groupLevelHandlers.GetEnumerator(); en.MoveNext(); )
            {
                String key = (String)en.Current;
                topLevelHandlers.Add(key, groupLevelHandlers[key]);
            }
            topLevelHandlers.Add("model", model);
            topLevelHandlers.Add("title", title);
            topLevelHandlers.Add("meta", meta);

            groupLevelHandlers.Add(LABEL_ELEMENT, groupLabel);
        }

        private static void initTypeMappings()
        {
            typeMappings = new Dictionary<String, int>();
            typeMappings.Add("string", (int)(Constants.DATATYPE_TEXT));               //xsd:
            typeMappings.Add("int", (int)(Constants.DATATYPE_INTEGER));           //xsd:
            typeMappings.Add("long", (int)(Constants.DATATYPE_LONG));                 //xsd:
            typeMappings.Add("int", (int)(Constants.DATATYPE_INTEGER));               //xsd:
            typeMappings.Add("decimal", (int)(Constants.DATATYPE_DECIMAL));           //xsd:
            typeMappings.Add("double", (int)(Constants.DATATYPE_DECIMAL));            //xsd:
            typeMappings.Add("float", (int)(Constants.DATATYPE_DECIMAL));             //xsd:
            typeMappings.Add("dateTime", (int)(Constants.DATATYPE_DATE_TIME));        //xsd:
            typeMappings.Add("date", (int)(Constants.DATATYPE_DATE));                 //xsd:
            typeMappings.Add("time", (int)(Constants.DATATYPE_TIME));                 //xsd:
            typeMappings.Add("gYear", (int)(Constants.DATATYPE_UNSUPPORTED));         //xsd:
            typeMappings.Add("gMonth", (int)(Constants.DATATYPE_UNSUPPORTED));        //xsd:
            typeMappings.Add("gDay", (int)(Constants.DATATYPE_UNSUPPORTED));          //xsd:
            typeMappings.Add("gYearMonth", (int)(Constants.DATATYPE_UNSUPPORTED));    //xsd:
            typeMappings.Add("gMonthDay", (int)(Constants.DATATYPE_UNSUPPORTED));     //xsd:
            typeMappings.Add("Boolean", (int)(Constants.DATATYPE_BOOLEAN));           //xsd:
            typeMappings.Add("base64Binary", (int)(Constants.DATATYPE_UNSUPPORTED));  //xsd:
            typeMappings.Add("hexBinary", (int)(Constants.DATATYPE_UNSUPPORTED));     //xsd:
            typeMappings.Add("anyURI", (int)(Constants.DATATYPE_UNSUPPORTED));        //xsd:
            typeMappings.Add("listItem", (int)(Constants.DATATYPE_CHOICE));           //xforms:
            typeMappings.Add("listItems", (int)(Constants.DATATYPE_CHOICE_LIST));	    //xforms:	
            typeMappings.Add(SELECTONE, (int)(Constants.DATATYPE_CHOICE));	        //non-standard	
            typeMappings.Add(SELECT, (int)(Constants.DATATYPE_CHOICE_LIST));        //non-standard
            typeMappings.Add("geopoint", (int)(Constants.DATATYPE_GEOPOINT));         //non-standard
            typeMappings.Add("barcode", (int)(Constants.DATATYPE_BARCODE));           //non-standard
            typeMappings.Add("binary", (int)(Constants.DATATYPE_BINARY));             //non-standard
        }

        private void initState()
        {
            modelFound = false;
            bindingsByID = new Dictionary<String, DataBinding>();
            bindings = new List<DataBinding>();
            repeats = new List<TreeReference>();
            itemsets = new List<ItemsetBinding>();
            selectOnes = new List<TreeReference>();
            selectMultis = new List<TreeReference>();
            instanceNode = null;
            repeatTree = null;
            defaultNamespace = null;

            itextKnownForms = new List<String>();
            itextKnownForms.Add("long");
            itextKnownForms.Add("short");
            itextKnownForms.Add("image");
            itextKnownForms.Add("audio");
        }


        public XFormParser(StreamReader reader)
        {
            _reader = reader;
            try
            {
                staticInit();
            }
            catch (Exception e)
            {
                Logger.die("xfparser-static-init", e);
            }
        }

        public XFormParser(XmlDocument doc)
        {
            _xmldoc = doc;
            try
            {
                staticInit();
            }
            catch (Exception e)
            {
                Logger.die("xfparser-static-init", e);
            }
        }

        public XFormParser(StreamReader form, StreamReader instance)
        {
            _reader = form;
            _instReader = instance;
            try
            {
                staticInit();
            }
            catch (Exception e)
            {
                Logger.die("xfparser-static-init", e);
            }
        }

        public XFormParser(XmlDocument form, XmlDocument instance)
        {
            _xmldoc = form;
            _instDoc = instance;
            try
            {
                staticInit();
            }
            catch (Exception e)
            {
                Logger.die("xfparser-static-init", e);
            }
        }

        public FormDef parse()
        {
            if (_f == null)
            {
                Console.WriteLine("Parsing form...");

                if (_xmldoc == null)
                {
                    _xmldoc = getXMLDocument(_reader);
                }

                parseDoc();

                //load in a custom xml instance, if applicable
                if (_instReader != null)
                {
                    loadXmlInstance(_f, _instReader);
                }
                else if (_instDoc != null)
                {
                    loadXmlInstance(_f, _instDoc);
                }
            }
            return _f;
        }

        public static XmlDocument getXMLDocument(StreamReader reader)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.LoadXml(reader.ReadToEnd());
            }
            catch (XmlException e)
            {
                String errorMsg = "XML Syntax Error at Line: " + e.LineNumber + ", Column: " + e.LinePosition + "!";
                Console.Error.WriteLine(errorMsg);
                Console.WriteLine(e.StackTrace);
                throw new XFormParseException(errorMsg);
            }
            catch (Exception e)
            {
                //#if debug.output==verbose || debug.output==exception
                String errorMsg = "Unhandled Exception while Parsing XForm";
                Console.Error.WriteLine(errorMsg);
                Console.WriteLine(e.StackTrace);
                throw new XFormParseException(errorMsg);
                //#endif
            }

            try
            {
                reader.Close();
            }
            catch (IOException e)
            {
                Console.WriteLine("Error closing reader");
                Console.WriteLine(e.StackTrace);
            }

            return doc;
        }

        private void parseDoc()
        {
            _f = new FormDef();

            initState();
            defaultNamespace = _xmldoc.NamespaceURI; //TODO root element name space
            parseElement(_xmldoc.DocumentElement, _f, topLevelHandlers);
            collapseRepeatGroups(_f);

            if (instanceNode != null)
            {
                parseInstance(instanceNode);
            }
        }

        private void parseElement(XmlElement e, Object parent, IDictionary<String, IElementHandler> handlers)
        { //,
            //			Boolean allowUnknownElements, Boolean allowText, Boolean recurseUnknown) {
            String name = e.Name;

            String[] suppressWarningArr = {
			"html",
			"head",
			"body",
			"xform",
			"chooseCaption",
			"addCaption",
			"addEmptyCaption",
			"delCaption",
			"doneCaption",
			"doneEmptyCaption",
			"mainHeader",
			"entryHeader",
			"delHeader"
		};
            List<String> suppressWarning = new List<String>();
            for (int i = 0; i < suppressWarningArr.Length; i++)
            {
                suppressWarning.Add(suppressWarningArr[i]);
            }

            IElementHandler eh = handlers[name];
            if (eh != null)
            {
                eh.handle(this, e, parent);
            }
            else
            {


                if (!suppressWarning.Contains(name))
                {
                    //#if debug.output==verbose
                    Console.Error.WriteLine("XForm Parse: Unrecognized element [" + name + "]. Ignoring and processing children..." + getVagueLocation(e));
                    //#endif
                }
                for (int i = 0; i < e.ChildNodes.Count; i++)
                {
                    if (e.ChildNodes[i].NodeType == XmlNodeType.Element)
                    {
                        parseElement((XmlElement)e.ChildNodes[i], parent, handlers);
                    }
                }
            }
        }

        private void parseTitle(XmlElement e)
        {
            ArrayList usedAtts = new ArrayList(); //no attributes parsed in title.
            String title = getXMLText(e, true);
            Console.WriteLine("Title: \"" + title + "\"");
            _f.Title = (title);
            if (_f.Name == null)
            {
                //Jan 9, 2009 - ctsims
                //We don't really want to allow for forms without
                //some unique ID, so if a title is available, use
                //that.
                _f.Name = (title);
            }


            if (XFormUtils.showUnusedAttributeWarning(e, usedAtts))
            {
                Console.WriteLine(XFormUtils.unusedAttWarning(e, usedAtts));
            }
        }

        private void parseMeta(XmlElement e)
        {
            ArrayList usedAtts = new ArrayList();
            int attributes = e.Attributes.Count;
            for (int i = 0; i < attributes; ++i)
            {
                String name = e.Attributes[i].Name;
                String value = e.Attributes[i].Value;
                if ("name".Equals(name))
                {
                    _f.Name = (value);
                }
            }


            usedAtts.Add("name");
            if (XFormUtils.showUnusedAttributeWarning(e, usedAtts))
            {
                Console.WriteLine(XFormUtils.unusedAttWarning(e, usedAtts));
            }
        }

        //for ease of parsing, we assume a model comes before the controls, which isn't necessarily mandated by the xforms spec
        private void parseModel(XmlElement e)
        {
            ArrayList usedAtts = new ArrayList();
            List<XmlElement> submissionBlocks = new List<XmlElement>();


            if (modelFound)
            {
                //#if debug.output==verbose
                Console.Error.WriteLine("Multiple models not supported. Ignoring subsequent models." + getVagueLocation(e));
                //#endif
                return;
            }
            modelFound = true;

            if (XFormUtils.showUnusedAttributeWarning(e, usedAtts))
            {
                Console.WriteLine(XFormUtils.unusedAttWarning(e, usedAtts));
            }

            for (int i = 0; i < e.ChildNodes.Count; i++)
            {

                XmlNodeType type = e.ChildNodes[i].NodeType;
                XmlElement child = (type == XmlNodeType.Element ? (XmlElement)e.ChildNodes[i] : null);
                String childName = (child != null ? child.Name : null);

                if ("itext".Equals(childName))
                {
                    parseIText(child);
                }
                else if ("instance".Equals(childName))
                {
                    //we save parsing the instance node until the end, giving us the information we need about
                    //binds and data types and such
                    saveInstanceNode(child);
                }
                else if (BIND_ATTR.Equals(childName))
                { //<instance> must come before <bind>s
                    parseBind(child);
                }
                else if ("submission".Equals(childName))
                {
                    submissionBlocks.Add(child);
                }
                else
                { //invalid model content
                    if (type == XmlNodeType.Element)
                    {
                        throw new XFormParseException("Unrecognized top-level tag [" + childName + "] found within <model>", child);
                    }
                    else if (type == XmlNodeType.Text && getXMLText(e, i, true).Length != 0)
                    {
                        throw new XFormParseException("Unrecognized text content found within <model>: \"" + getXMLText(e, i, true) + "\"", child);
                    }
                }

                if (child == null || BIND_ATTR.Equals(childName) || "itext".Equals(childName))
                {
                    //Clayton Sims - Jun 17, 2009 - This code is used when the stinginess flag
                    //is set for the build. It dynamically wipes out old model nodes once they're
                    //used. This is sketchy if anything else plans on touching the nodes.
                    //This code can be removed once we're pull-parsing
                    //#if org.javarosa.xform.stingy
                    e.RemoveChild(e.ChildNodes[i]);
                    --i;
                    //#endif
                }
            }

            //Now parse out the submission blocks (we needed the binds to all be set before we could)
            foreach (XmlElement child in submissionBlocks)
            {
                parseSubmission(child);
            }
        }
        private void parseSubmission(XmlElement submission)
        {
            String id = submission.GetAttribute(ID_ATTR);

            //These two are always required
            String method = submission.GetAttribute("method");
            String action = submission.GetAttribute("action");

            SubmissionParser parser = new SubmissionParser();
            foreach (SubmissionParser p in submissionParsers)
            {
                if (p.matchesCustomMethod(method))
                {
                    parser = p;
                }
            }

            //These two might exist, but if neither do, we just assume you want the entire instance.
            String ref_ = submission.GetAttribute(REF_ATTR);
            String bind = submission.GetAttribute(BIND_ATTR);

            IDataReference dataRef = null;
            Boolean refFromBind = false;

            if (bind != null)
            {
                DataBinding binding = bindingsByID[bind];
                if (binding == null)
                {
                    throw new XFormParseException("XForm Parse: invalid binding ID in submit'" + bind + "'", submission);
                }
                dataRef = binding.Reference;
                refFromBind = true;
            }
            else if (ref_ != null)
            {
                dataRef = new XPathReference(ref_);
            }
            else
            {
                //no reference! No big deal, assume we want the root reference
                dataRef = new XPathReference("/");
            }

            if (dataRef != null)
            {
                if (!refFromBind)
                {
                    dataRef = getAbsRef(dataRef, TreeReference.rootRef());
                }
            }

            SubmissionProfile profile = parser.parseSubmission(method, action, dataRef, submission);

            if (id == null)
            {
                //default submission profile
                _f.setDefaultSubmission(profile);
            }
            else
            {
                //typed submission profile
                _f.addSubmissionProfile(id, profile);
            }
        }

        private void saveInstanceNode(XmlElement instance)
        {
            if (instanceNode != null)
            {
                Console.Error.WriteLine("Multiple instances not supported. Ignoring subsequent instances." + getVagueLocation(instance));
                return;
            }

            for (int i = 0; i < instance.ChildNodes.Count; i++)
            {
                if (instance.NodeType == XmlNodeType.Element)
                {
                    if (instanceNode != null)
                    {
                        throw new XFormParseException("XForm Parse: <instance> has more than one child element", instance);
                    }
                    else
                    {
                        instanceNode = (XmlElement)instance.ChildNodes[i];
                    }
                }
            }
        }

        protected QuestionDef parseUpload(IFormElement parent, XmlElement e, int controlUpload)
        {
            ArrayList usedAtts = new ArrayList();
            QuestionDef question = parseControl(parent, e, controlUpload);
            String mediaType = e.GetAttribute("mediatype");
            if ("image/*".Equals(mediaType))
            {
                // NOTE: this could be further expanded. 
                question.ControlType = Constants.CONTROL_IMAGE_CHOOSE;
            }
            else if ("audio/*".Equals(mediaType))
            {
                question.ControlType = Constants.CONTROL_AUDIO_CAPTURE;
            }
            else if ("video/*".Equals(mediaType))
            {
                question.ControlType = Constants.CONTROL_VIDEO_CAPTURE;
            }

            usedAtts.Add("mediatype");
            if (XFormUtils.showUnusedAttributeWarning(e, usedAtts))
            {
                Console.WriteLine(XFormUtils.unusedAttWarning(e, usedAtts));
            }

            return question;
        }

        protected QuestionDef parseControl(IFormElement parent, XmlElement e, int controlType)
        {
            QuestionDef question = new QuestionDef();
            question.ID = serialQuestionID++; //until we come up with a better scheme

            ArrayList usedAtts = new ArrayList();
            usedAtts.Add(REF_ATTR);
            usedAtts.Add(BIND_ATTR);
            usedAtts.Add(APPEARANCE_ATTR);

            IDataReference dataRef = null;
            Boolean refFromBind = false;

            String ref_ = e.GetAttribute(REF_ATTR);
            String bind = e.GetAttribute(BIND_ATTR);

            if (bind != null)
            {
                DataBinding binding = bindingsByID[bind];
                if (binding == null)
                {
                    throw new XFormParseException("XForm Parse: invalid binding ID '" + bind + "'", e);
                }
                dataRef = binding.Reference;
                refFromBind = true;
            }
            else if (ref_ != null)
            {
                dataRef = new XPathReference(ref_);
            }
            else
            {
                if (controlType == Constants.CONTROL_TRIGGER)
                {
                    //TODO: special handling for triggers? also, not all triggers created equal
                }
                else
                {
                    throw new XFormParseException("XForm Parse: input control with neither 'ref' nor 'bind'", e);
                }
            }

            if (dataRef != null)
            {
                if (!refFromBind)
                {
                    dataRef = getAbsRef(dataRef, parent);
                }
                question.Bind = dataRef;

                if (controlType == Constants.CONTROL_SELECT_ONE)
                {
                    selectOnes.Add((TreeReference)dataRef.Reference);
                }
                else if (controlType == Constants.CONTROL_SELECT_MULTI)
                {
                    selectMultis.Add((TreeReference)dataRef.Reference);
                }
            }

            Boolean isSelect = (controlType == Constants.CONTROL_SELECT_MULTI || controlType == Constants.CONTROL_SELECT_ONE);
            question.ControlType = controlType;
            question.AppearanceAttr = e.GetAttribute(APPEARANCE_ATTR);

            for (int i = 0; i < e.ChildNodes.Count; i++)
            {
                XmlNodeType type = e.ChildNodes[i].NodeType;

                XmlElement child = (type == XmlNodeType.Element ? (XmlElement)e.ChildNodes[i] : null);
                String childName = (child != null ? child.Name : null);

                if (LABEL_ELEMENT.Equals(childName))
                {
                    parseQuestionLabel(question, child);
                }
                else if ("hint".Equals(childName))
                {
                    parseHint(question, child);
                }
                else if (isSelect && "item".Equals(childName))
                {
                    parseItem(question, child);
                }
                else if (isSelect && "itemset".Equals(childName))
                {
                    parseItemset(question, child, parent);
                }
            }
            if (isSelect)
            {
                if (question.getNumChoices() > 0 && question.getDynamicChoices() != null)
                {
                    throw new XFormParseException("Select question contains both literal choices and <itemset>");
                }
                else if (question.getNumChoices() == 0 && question.getDynamicChoices() == null)
                {
                    throw new XFormParseException("Select question has no choices");
                }
            }

            parent.addChild(question);


            if (XFormUtils.showUnusedAttributeWarning(e, usedAtts))
            {
                Console.WriteLine(XFormUtils.unusedAttWarning(e, usedAtts));
            }


            return question;
        }

        private void parseQuestionLabel(QuestionDef q, XmlElement e)
        {
            String label = getLabel(e);
            String ref_ = e.GetAttribute(REF_ATTR);

            ArrayList usedAtts = new ArrayList();
            usedAtts.Add(REF_ATTR);

            if (ref_ != null)
            {
                if (ref_.StartsWith(ITEXT_OPEN) && ref_.EndsWith(ITEXT_CLOSE))
                {
                    String textRef = ref_.Substring(ITEXT_OPEN.Length, ref_.IndexOf(ITEXT_CLOSE));

                    verifyTextMappings(textRef, "Question <label>", true);
                    q.TextID = textRef;
                }
                else
                {
                    throw new SystemException("malformed ref [" + ref_ + "] for <label>");
                }
            }
            else
            {
                q.LabelInnerText = label;
            }


            if (XFormUtils.showUnusedAttributeWarning(e, usedAtts))
            {
                Console.WriteLine(XFormUtils.unusedAttWarning(e, usedAtts));
            }
        }

        private void parseGroupLabel(GroupDef g, XmlElement e)
        {
            if (g.Repeat)
                return; //ignore child <label>s for <repeat>; the appropriate <label> must be in the wrapping <group>

            ArrayList usedAtts = new ArrayList();
            usedAtts.Add(REF_ATTR);


            String label = getLabel(e);
            String ref_ = e.GetAttribute(REF_ATTR);

            if (ref_ != null)
            {
                if (ref_.StartsWith(ITEXT_OPEN) && ref_.EndsWith(ITEXT_CLOSE))
                {
                    String textRef = ref_.Substring(ITEXT_OPEN.Length, ref_.IndexOf(ITEXT_CLOSE));

                    verifyTextMappings(textRef, "Group <label>", true);
                    g.TextID = textRef;
                }
                else
                {
                    throw new SystemException("malformed ref [" + ref_ + "] for <label>");
                }
            }
            else
            {
                g.LabelInnerText = (label);
            }


            if (XFormUtils.showUnusedAttributeWarning(e, usedAtts))
            {
                Console.WriteLine(XFormUtils.unusedAttWarning(e, usedAtts));
            }
        }

        private String getLabel(XmlElement e)
        {
            if (e.ChildNodes.Count == 0) return null;

            recurseForOutput(e);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < e.ChildNodes.Count; i++)
            {
                if (e.ChildNodes[i].NodeType != XmlNodeType.Text && !(e.ChildNodes[i].InnerText is String))
                {

                    XmlElement child = (XmlElement)e.ChildNodes[i];

                    //If the child is in the HTML namespace, retain it. 
                    if (NAMESPACE_HTML.Equals(child.NamespaceURI))
                    {
                        sb.Append(XFormSerializer.elementToString(child));
                    }
                    else
                    {
                        //Otherwise, ignore it.
                        Console.WriteLine("Unrecognized tag inside of text: <" + child.Name + ">. " +
                                "Did you intend to use HTML markup? If so, ensure that the element is defined in " +
                                "the HTML namespace.");
                    }
                }
                else
                {
                    sb.Append((XmlText)e.ChildNodes[i]);
                }
            }

            String s = sb.ToString().Trim();

            return s;
        }

        private void recurseForOutput(XmlElement e)
        {
            if (e.ChildNodes.Count == 0) return;

            for (int i = 0; i < e.ChildNodes.Count; i++)
            {
                XmlNodeType kidType = e.ChildNodes[i].NodeType;
                if (kidType == XmlNodeType.Text) { continue; }
                if (e.ChildNodes[i].InnerText is String) { continue; }
                XmlElement kid = (XmlElement)e.ChildNodes[i];

                //is just text
                if (kidType == XmlNodeType.Element && XFormUtils.isOutput(kid))
                {
                    String s = "${" + parseOutput(kid) + "}";
                    e.RemoveChild(e.ChildNodes[i]);
                    XmlText xmltxt = _xmldoc.CreateTextNode(s);
                    e.AppendChild(xmltxt);

                    //has kids? Recurse through them and swap output tag for parsed version
                }
                else if (kid.ChildNodes.Count != 0)
                {
                    recurseForOutput(kid);
                    //is something else
                }
                else
                {
                    continue;
                }
            }
        }

        private String parseOutput(XmlElement e)
        {
            ArrayList usedAtts = new ArrayList();
            usedAtts.Add(REF_ATTR);
            usedAtts.Add(VALUE);

            String xpath = e.GetAttribute(REF_ATTR);
            if (xpath == null)
            {
                xpath = e.GetAttribute(VALUE);
            }
            if (xpath == null)
            {
                throw new XFormParseException("XForm Parse: <output> without 'ref' or 'value'", e);
            }

            XPathConditional expr = null;
            try
            {
                expr = new XPathConditional(xpath);
            }
            catch (XPathSyntaxException xse)
            {
                //#if debug.output==verbose
                Console.Error.WriteLine("Invalid XPath expression [" + xpath + "]!");
                return "";
                //#endif
            }

            int index = -1;
            if (_f.OutputFragments.Contains(expr))
            {
                index = _f.OutputFragments.IndexOf(expr);
            }
            else
            {
                index = _f.OutputFragments.Count;
                _f.OutputFragments.Add(expr);
            }

            if (XFormUtils.showUnusedAttributeWarning(e, usedAtts))
            {
                Console.WriteLine(XFormUtils.unusedAttWarning(e, usedAtts));
            }

            return Convert.ToString(index);
        }

        private void parseHint(QuestionDef q, XmlElement e)
        {
            ArrayList usedAtts = new ArrayList();
            usedAtts.Add(REF_ATTR);
            String hint = getXMLText(e, true);
            String hintInnerText = getLabel(e);
            String ref_ = e.GetAttribute(REF_ATTR);

            if (ref_ != null)
            {
                if (ref_.StartsWith(ITEXT_OPEN) && ref_.EndsWith(ITEXT_CLOSE))
                {
                    String textRef = ref_.Substring(ITEXT_OPEN.Length, ref_.IndexOf(ITEXT_CLOSE));

                    verifyTextMappings(textRef, "<hint>", false);
                    q.HelpTextID = textRef;
                }
                else
                {
                    throw new SystemException("malformed ref [" + ref_ + "] for <hint>");
                }
            }
            else
            {
                q.HelpInnerText = hintInnerText;
                q.HelpText = hint;
            }

            if (XFormUtils.showUnusedAttributeWarning(e, usedAtts))
            {
                Console.WriteLine(XFormUtils.unusedAttWarning(e, usedAtts));
            }
        }

        private void parseItem(QuestionDef q, XmlElement e)
        {
            const int MAX_VALUE_LEN = 32;

            //catalogue of used attributes in this method/element
            ArrayList usedAtts = new ArrayList();
            ArrayList labelUA = new ArrayList();
            ArrayList valueUA = new ArrayList();
            labelUA.Add(REF_ATTR);
            valueUA.Add(FORM_ATTR);

            String labelInnerText = null;
            String textRef = null;
            String value = null;

            for (int i = 0; i < e.ChildNodes.Count; i++)
            {
                XmlNodeType type = e.ChildNodes[i].NodeType;
                XmlElement child = (type == XmlNodeType.Element ? (XmlElement)e.ChildNodes[i] : null);
                String childName = (child != null ? child.Name : null);

                if (LABEL_ELEMENT.Equals(childName))
                {

                    //print attribute warning for child element
                    if (XFormUtils.showUnusedAttributeWarning(child, labelUA))
                    {
                        Console.WriteLine(XFormUtils.unusedAttWarning(child, labelUA));
                    }
                    labelInnerText = getLabel(child);
                    String ref_ = child.GetAttribute(REF_ATTR);

                    if (ref_ != null)
                    {
                        if (ref_.StartsWith(ITEXT_OPEN) && ref_.EndsWith(ITEXT_CLOSE))
                        {
                            textRef = ref_.Substring(ITEXT_OPEN.Length, ref_.IndexOf(ITEXT_CLOSE));

                            verifyTextMappings(textRef, "Item <label>", true);
                        }
                        else
                        {
                            throw new XFormParseException("malformed ref [" + ref_ + "] for <item>", child);
                        }
                    }
                }
                else if (VALUE.Equals(childName))
                {
                    value = getXMLText(child, true);

                    //print attribute warning for child element
                    if (XFormUtils.showUnusedAttributeWarning(child, valueUA))
                    {
                        Console.WriteLine(XFormUtils.unusedAttWarning(child, valueUA));
                    }

                    if (value != null)
                    {
                        if (value.Length > MAX_VALUE_LEN)
                        {
                            Console.Error.WriteLine("WARNING: choice value [" + value + "] is too long; max. suggested length " + MAX_VALUE_LEN + " chars" + getVagueLocation(child));
                        }

                        //validate
                        for (int k = 0; k < value.Length; k++)
                        {
                            char c = value[k];

                            if (" \n\t\f\r\'\"`".IndexOf(c) >= 0)
                            {
                                Boolean isMultiSelect = (q.ControlType == Constants.CONTROL_SELECT_MULTI);
                                Console.Error.WriteLine("XForm Parse WARNING: " + (isMultiSelect ? SELECT : SELECTONE) + " question <value>s [" + value + "] " +
                                        (isMultiSelect ? "cannot" : "should not") + " contain spaces, and are recommended not to contain apostraphes/quotation marks" + getVagueLocation(child));
                                break;
                            }
                        }
                    }
                }
            }

            if (textRef == null && labelInnerText == null)
            {
                throw new XFormParseException("<item> without proper <label>", e);
            }
            if (value == null)
            {
                throw new XFormParseException("<item> without proper <value>", e);
            }

            if (textRef != null)
            {
                q.addSelectChoice(new SelectChoice(textRef, value));
            }
            else
            {
                q.addSelectChoice(new SelectChoice(null, labelInnerText, value, false));
            }

            //print unused attribute warning message for parent element
            if (XFormUtils.showUnusedAttributeWarning(e, usedAtts))
            {
                Console.WriteLine(XFormUtils.unusedAttWarning(e, usedAtts));
            }
        }

        private void parseItemset(QuestionDef q, XmlElement e, IFormElement qparent)
        {
            ItemsetBinding itemset = new ItemsetBinding();

            ////////////////USED FOR PARSER WARNING OUTPUT ONLY
            //catalogue of used attributes in this method/element
            ArrayList usedAtts = new ArrayList();
            ArrayList labelUA = new ArrayList(); //for child with name 'label'
            ArrayList valueUA = new ArrayList(); //for child with name 'value'
            ArrayList copyUA = new ArrayList(); //for child with name 'copy'
            usedAtts.Add(NODESET_ATTR);
            labelUA.Add(REF_ATTR);
            valueUA.Add(REF_ATTR);
            valueUA.Add(FORM_ATTR);
            copyUA.Add(REF_ATTR);
            ////////////////////////////////////////////////////

            String nodesetStr = e.GetAttribute(NODESET_ATTR);
            if (nodesetStr == null) throw new SystemException("No nodeset attribute in element: [" + e.Name + "]. This is required. (Element Printout:" + XFormSerializer.elementToString(e) + ")");
            XPathPathExpr path = XPathReference.getPathExpr(nodesetStr);
            itemset.nodesetExpr = new XPathConditional(path);
            itemset.contextRef = getFormElementRef(qparent);
            itemset.nodesetRef = FormInstance.unpackReference(getAbsRef(new XPathReference(path.getReference(true)), itemset.contextRef));

            for (int i = 0; i < e.ChildNodes.Count; i++)
            {
                XmlNodeType type = e.ChildNodes[i].NodeType;
                XmlElement child = (type == XmlNodeType.Element ? (XmlElement)e.ChildNodes[i] : null);
                String childName = (child != null ? child.Name : null);

                if (LABEL_ELEMENT.Equals(childName))
                {
                    String labelXpath = child.GetAttribute(REF_ATTR);
                    Boolean labelItext = false;

                    //print unused attribute warning message for child element
                    if (XFormUtils.showUnusedAttributeWarning(child, labelUA))
                    {
                        Console.WriteLine(XFormUtils.unusedAttWarning(child, labelUA));
                    }
                    /////////////////////////////////////////////////////////////

                    if (labelXpath != null)
                    {
                        if (labelXpath.StartsWith("jr:itext(") && labelXpath.EndsWith(")"))
                        {
                            labelXpath = labelXpath.Substring("jr:itext(".Length, labelXpath.IndexOf(")"));
                            labelItext = true;
                        }
                    }
                    else
                    {
                        throw new XFormParseException("<label> in <itemset> requires 'ref'");
                    }

                    XPathPathExpr labelPath = XPathReference.getPathExpr(labelXpath);
                    itemset.labelRef = FormInstance.unpackReference(getAbsRef(new XPathReference(labelPath), itemset.nodesetRef));
                    itemset.labelExpr = new XPathConditional(labelPath);
                    itemset.labelIsItext = labelItext;
                }
                else if ("copy".Equals(childName))
                {
                    String copyRef = child.GetAttribute(REF_ATTR);

                    //print unused attribute warning message for child element
                    if (XFormUtils.showUnusedAttributeWarning(child, copyUA))
                    {
                        Console.WriteLine(XFormUtils.unusedAttWarning(child, copyUA));
                    }

                    if (copyRef == null)
                    {
                        throw new XFormParseException("<copy> in <itemset> requires 'ref'");
                    }

                    itemset.copyRef = FormInstance.unpackReference(getAbsRef(new XPathReference(copyRef), itemset.nodesetRef));
                    itemset.copyMode = true;
                }
                else if (VALUE.Equals(childName))
                {
                    String valueXpath = child.GetAttribute(REF_ATTR);

                    //print unused attribute warning message for child element
                    if (XFormUtils.showUnusedAttributeWarning(child, valueUA))
                    {
                        Console.WriteLine(XFormUtils.unusedAttWarning(child, valueUA));
                    }

                    if (valueXpath == null)
                    {
                        throw new XFormParseException("<value> in <itemset> requires 'ref'");
                    }

                    XPathPathExpr valuePath = XPathReference.getPathExpr(valueXpath);
                    itemset.valueRef = FormInstance.unpackReference(getAbsRef(new XPathReference(valuePath), itemset.nodesetRef));
                    itemset.valueExpr = new XPathConditional(valuePath);
                    itemset.copyMode = false;
                }
            }

            if (itemset.labelRef == null)
            {
                throw new XFormParseException("<itemset> requires <label>");
            }
            else if (itemset.copyRef == null && itemset.valueRef == null)
            {
                throw new XFormParseException("<itemset> requires <copy> or <value>");
            }

            if (itemset.copyRef != null)
            {
                if (itemset.valueRef == null)
                {
                    Console.Error.WriteLine("WARNING: <itemset>s with <copy> are STRONGLY recommended to have <value> as well; pre-selecting, default answers, and display of answers will not work properly otherwise");
                }
                else if (!itemset.copyRef.isParentOf(itemset.valueRef, false))
                {
                    throw new XFormParseException("<value> is outside <copy>");
                }
            }

            q.setDynamicChoices(itemset);
            itemsets.Add(itemset);

            //print unused attribute warning message for parent element
            if (XFormUtils.showUnusedAttributeWarning(e, usedAtts))
            {
                Console.WriteLine(XFormUtils.unusedAttWarning(e, usedAtts));
            }
        }

        private void parseGroup(IFormElement parent, XmlElement e, int groupType)
        {
            GroupDef group = new GroupDef();
            group.ID = serialQuestionID++; //until we come up with a better scheme
            IDataReference dataRef = null;
            Boolean refFromBind = false;

            ArrayList usedAtts = new ArrayList();
            usedAtts.Add(REF_ATTR);
            usedAtts.Add(NODESET_ATTR);
            usedAtts.Add(BIND_ATTR);
            usedAtts.Add(APPEARANCE_ATTR);
            usedAtts.Add("count");
            usedAtts.Add("noAddRemove");

            if (groupType == CONTAINER_REPEAT)
            {
                group.Repeat = (true);
            }

            String ref_ = e.GetAttribute(REF_ATTR);
            String nodeset = e.GetAttribute(NODESET_ATTR);
            String bind = e.GetAttribute(BIND_ATTR);
            group.setAppearanceAttr(e.GetAttribute(APPEARANCE_ATTR));

            if (bind != null)
            {
                DataBinding binding = bindingsByID[bind];
                if (binding == null)
                {
                    throw new XFormParseException("XForm Parse: invalid binding ID [" + bind + "]", e);
                }
                dataRef = binding.Reference;
                refFromBind = true;
            }
            else
            {
                if (group.Repeat)
                {
                    if (nodeset != null)
                    {
                        dataRef = new XPathReference(nodeset);
                    }
                    else
                    {
                        throw new XFormParseException("XForm Parse: <repeat> with no binding ('bind' or 'nodeset')", e);
                    }
                }
                else
                {
                    if (ref_ != null)
                    {
                        dataRef = new XPathReference(ref_);
                    } //<group> not required to have a binding
                }
            }

            if (!refFromBind)
            {
                dataRef = getAbsRef(dataRef, parent);
            }
            group.Bind = dataRef;

            if (group.Repeat)
            {
                repeats.Add((TreeReference)dataRef.Reference);

                String countRef = e.GetAttribute("count", NAMESPACE_JAVAROSA);
                if (countRef != null)
                {
                    group.count = getAbsRef(new XPathReference(countRef), parent);
                    group.noAddRemove = true;
                }
                else
                {
                    group.noAddRemove = (e.GetAttribute("noAddRemove", NAMESPACE_JAVAROSA) != null);
                }
            }

            for (int i = 0; i < e.ChildNodes.Count; i++)
            {
                XmlNodeType type = e.ChildNodes[i].NodeType;
                XmlElement child = (type == XmlNodeType.Element ? (XmlElement)e.ChildNodes[i] : null);
                String childName = (child != null ? child.Name : null);
                String childNamespace = (child != null ? child.NamespaceURI : null);

                if (group.Repeat && NAMESPACE_JAVAROSA.Equals(childNamespace))
                {
                    if ("chooseCaption".Equals(childName))
                    {
                        group.chooseCaption = getLabel(child);
                    }
                    else if ("addCaption".Equals(childName))
                    {
                        group.addCaption = getLabel(child);
                    }
                    else if ("delCaption".Equals(childName))
                    {
                        group.delCaption = getLabel(child);
                    }
                    else if ("doneCaption".Equals(childName))
                    {
                        group.doneCaption = getLabel(child);
                    }
                    else if ("addEmptyCaption".Equals(childName))
                    {
                        group.addEmptyCaption = getLabel(child);
                    }
                    else if ("doneEmptyCaption".Equals(childName))
                    {
                        group.doneEmptyCaption = getLabel(child);
                    }
                    else if ("entryHeader".Equals(childName))
                    {
                        group.entryHeader = getLabel(child);
                    }
                    else if ("delHeader".Equals(childName))
                    {
                        group.delHeader = getLabel(child);
                    }
                    else if ("mainHeader".Equals(childName))
                    {
                        group.mainHeader = getLabel(child);
                    }
                }
            }

            //the case of a group wrapping a repeat is cleaned up in a post-processing step (collapseRepeatGroups)

            for (int i = 0; i < e.ChildNodes.Count; i++)
            {
                if (e.ChildNodes[i].NodeType == XmlNodeType.Element)
                {
                    parseElement((XmlElement)e.ChildNodes[i], group, groupLevelHandlers);
                }
            }

            //print unused attribute warning message for parent element
            if (XFormUtils.showUnusedAttributeWarning(e, usedAtts))
            {
                Console.WriteLine(XFormUtils.unusedAttWarning(e, usedAtts));
            }

            parent.addChild(group);
        }

        private TreeReference getFormElementRef(IFormElement fe)
        {
            if (fe is FormDef)
            {
                TreeReference ref_ = TreeReference.rootRef();
                ref_.add(instanceNode.Name, 0);
                return ref_;
            }
            else
            {
                return (TreeReference)fe.Bind.Reference;
            }
        }

        private IDataReference getAbsRef(IDataReference ref_, IFormElement parent)
        {
            return getAbsRef(ref_, getFormElementRef(parent));
        }

        //take a (possibly relative) reference, and make it absolute based on its parent
        private static IDataReference getAbsRef(IDataReference ref_, TreeReference parentRef)
        {
            TreeReference tref;

            if (!parentRef.isAbsolute())
            {
                throw new SystemException("XFormParser.getAbsRef: parentRef must be absolute");
            }

            if (ref_ != null)
            {
                tref = (TreeReference)ref_.Reference;
            }
            else
            {
                tref = TreeReference.selfRef(); //only happens for <group>s with no binding
            }

            tref = tref.parent(parentRef);
            if (tref == null)
            {
                throw new XFormParseException("Binding path [" + tref + "] not allowed with parent binding of [" + parentRef + "]");
            }

            return new XPathReference(tref);
        }

        //collapse groups whose only child is a repeat into a single repeat that uses the label of the wrapping group
        private static void collapseRepeatGroups(IFormElement fe)
        {
            if (fe.Children == null)
                return;

            for (int i = 0; i < fe.Children.Count; i++)
            {
                IFormElement child = fe.getChild(i);
                GroupDef group = null;
                if (child is GroupDef)
                    group = (GroupDef)child;

                if (group != null)
                {
                    if (!group.Repeat && group.Children.Count == 1)
                    {
                        IFormElement grandchild = (IFormElement)group.Children[0];
                        GroupDef repeat = null;
                        if (grandchild is GroupDef)
                            repeat = (GroupDef)grandchild;

                        if (repeat != null && repeat.Repeat)
                        {
                            //collapse the wrapping group

                            //merge group into repeat
                            //id - later
                            //name - later
                            repeat.LabelInnerText = (group.LabelInnerText);
                            repeat.TextID = (group.TextID);
                            //						repeat.setLongText(group.getLongText());
                            //						repeat.setShortText(group.getShortText());
                            //						repeat.setLongTextID(group.getLongTextID(), null);
                            //						repeat.setShortTextID(group.getShortTextID(), null);						
                            //don't merge binding; repeat will always already have one

                            //replace group with repeat
                            fe.Children[i] = repeat;
                            group = repeat;
                        }
                    }

                    collapseRepeatGroups(group);
                }
            }
        }

        private void parseIText(XmlElement itext)
        {
            Localizer l = new Localizer(true, true);
            _f.setLocalizer(l);
            l.registerLocalizable(_f);

            ArrayList usedAtts = new ArrayList(); //used for warning message

            for (int i = 0; i < itext.ChildNodes.Count; i++)
            {
                XmlElement trans = (XmlElement)itext.ChildNodes[i];
                if (trans == null || !trans.Name.Equals("translation"))
                    continue;

                parseTranslation(l, trans);
            }

            if (l.getAvailableLocales().Length == 0)
                throw new XFormParseException("no <translation>s defined", itext);

            if (l.getDefaultLocale() == null)
                l.setDefaultLocale(l.getAvailableLocales()[0]);

            //print unused attribute warning message for parent element
            if (XFormUtils.showUnusedAttributeWarning(itext, usedAtts))
            {
                Console.WriteLine(XFormUtils.unusedAttWarning(itext, usedAtts));
            }
        }

        private void parseTranslation(Localizer l, XmlElement trans)
        {
            /////for warning message
            ArrayList usedAtts = new ArrayList();
            usedAtts.Add("lang");
            usedAtts.Add("default");
            /////////////////////////

            String lang = trans.GetAttribute("lang");
            if (lang == null || lang.Length == 0)
            {
                throw new XFormParseException("no language specified for <translation>", trans);
            }
            String isDefault = trans.GetAttribute("default");

            if (!l.addAvailableLocale(lang))
            {
                throw new XFormParseException("duplicate <translation> for language '" + lang + "'", trans);
            }

            if (isDefault != null)
            {
                if (l.getDefaultLocale() != null)
                    throw new XFormParseException("more than one <translation> set as default", trans);
                l.setDefaultLocale(lang);
            }

            TableLocaleSource source = new TableLocaleSource();

            for (int j = 0; j < trans.ChildNodes.Count; j++)
            {
                XmlElement text = (XmlElement)trans.ChildNodes[j];
                if (text == null || !text.Name.Equals("text"))
                {
                    continue;
                }

                parseTextHandle(source, text);
                //Clayton Sims - Jun 17, 2009 - This code is used when the stinginess flag
                //is set for the build. It dynamically wipes out old model nodes once they're
                //used. This is sketchy if anything else plans on touching the nodes.
                //This code can be removed once we're pull-parsing
                //#if org.javarosa.xform.stingy
                trans.RemoveChild(trans.ChildNodes[j]);
                --j;
                //#endif
            }

            //print unused attribute warning message for parent element
            if (XFormUtils.showUnusedAttributeWarning(trans, usedAtts))
            {
                Console.WriteLine(XFormUtils.unusedAttWarning(trans, usedAtts));
            }

            l.registerLocaleResource(lang, source);
        }

        private void parseTextHandle(TableLocaleSource l, XmlElement text)
        {
            String id = text.GetAttribute(ID_ATTR);

            //used for parser warnings...
            ArrayList usedAtts = new ArrayList();
            ArrayList childUsedAtts = new ArrayList();
            usedAtts.Add(ID_ATTR);
            usedAtts.Add(FORM_ATTR);
            childUsedAtts.Add(FORM_ATTR);
            childUsedAtts.Add(ID_ATTR);
            //////////

            if (id == null || id.Length == 0)
            {
                throw new XFormParseException("no id defined for <text>", text);
            }

            for (int k = 0; k < text.ChildNodes.Count; k++)
            {
                XmlElement value = (XmlElement)text.ChildNodes[k];
                if (value == null) continue;
                if (!value.Name.Equals(VALUE))
                {
                    throw new XFormParseException("Unrecognized element [" + value.Name + "] in Itext->translation->text");
                }

                String form = value.GetAttribute(FORM_ATTR);
                if (form != null && form.Length == 0)
                {
                    form = null;
                }
                String data = getLabel(value);
                if (data == null)
                {
                    data = "";
                }

                String textID = (form == null ? id : id + ";" + form);  //kind of a hack
                if (l.hasMapping(textID))
                {
                    throw new XFormParseException("duplicate definition for text ID \"" + id + "\" and form \"" + form + "\"" + ". Can only have one definition for each text form.", text);
                }
                l.setLocaleMapping(textID, data);

                //print unused attribute warning message for child element
                if (XFormUtils.showUnusedAttributeWarning(value, childUsedAtts))
                {
                    Console.WriteLine(XFormUtils.unusedAttWarning(value, childUsedAtts));
                }
            }

            //print unused attribute warning message for parent element
            if (XFormUtils.showUnusedAttributeWarning(text, usedAtts))
            {
                Console.WriteLine(XFormUtils.unusedAttWarning(text, usedAtts));
            }
        }

        private Boolean hasITextMapping(String textID, String locale)
        {
            Localizer l = _f.Localizer;
            return l.hasMapping(locale == null ? l.getDefaultLocale() : locale, textID);
        }

        private void verifyTextMappings(String textID, String type, Boolean allowSubforms)
        {
            Localizer l = _f.Localizer;
            String[] locales = l.getAvailableLocales();

            for (int i = 0; i < locales.Length; i++)
            {
                //Test whether there is a default translation, or whether there is any special form available.
                if (!(hasITextMapping(textID, locales[i]) ||
                        (allowSubforms && hasSpecialFormMapping(textID, locales[i]))))
                {
                    if (locales[i].Equals(l.getDefaultLocale()))
                    {
                        throw new XFormParseException(type + " '" + textID + "': text is not localizable for default locale [" + l.getDefaultLocale() + "]!");
                    }
                    else
                    {
                        Console.Error.WriteLine("Warning: " + type + " '" + textID + "': text is not localizable for locale " + locales[i] + ".");
                    }
                }
            }
        }

        /**
         * Tests whether or not there is any form (default or special) for the provided
         * text id.
         * 
         * @return True if a translation is present for the given textID in the form. False otherwise
         */
        private Boolean hasSpecialFormMapping(String textID, String locale)
        {
            //First check our guesses
            foreach (String guess in itextKnownForms)
            {
                if (hasITextMapping(textID + ";" + guess, locale))
                {
                    return true;
                }
            }
            //Otherwise this sucks and we have to test the keys
            OrderedHashtable table = _f.Localizer.getLocaleData(locale);
            for (IEnumerator keys = table.keys(); keys.MoveNext(); )
            {
                String key = (String)keys.Current;
                if (key.StartsWith(textID + ";"))
                {
                    //A key is found, pull it out, add it to the list of guesses, and return positive
                    String textForm = key.Substring(key.IndexOf(";") + 1, key.Length);
                    Console.WriteLine("adding unexpected special itext form: " + textForm + " to list of expected forms");
                    itextKnownForms.Add(textForm);
                    return true;
                }
            }
            return false;
        }

        protected DataBinding processStandardBindAttributes(ArrayList usedAtts, XmlElement e)
        {
            usedAtts.Add(ID_ATTR);
            usedAtts.Add(NODESET_ATTR);
            usedAtts.Add("type");
            usedAtts.Add("relevant");
            usedAtts.Add("required");
            usedAtts.Add("readonly");
            usedAtts.Add("constraint");
            usedAtts.Add("constraintMsg");
            usedAtts.Add("calculate");
            usedAtts.Add("preload");
            usedAtts.Add("preloadParams");

            DataBinding binding = new DataBinding();


            binding.ID = (e.GetAttribute(ID_ATTR));

            String nodeset = e.GetAttribute(NODESET_ATTR);
            if (nodeset == null)
            {
                throw new XFormParseException("XForm Parse: <bind> without nodeset", e);
            }
            IDataReference ref_ = new XPathReference(nodeset);
            ref_ = getAbsRef(ref_, _f);
            binding.Reference = (ref_);

            binding.DataType = (getDataType(e.GetAttribute("type")));

            String xpathRel = e.GetAttribute("relevant");
            if (xpathRel != null)
            {
                if ("true()".Equals(xpathRel))
                {
                    binding.relevantAbsolute = true;
                }
                else if ("false()".Equals(xpathRel))
                {
                    binding.relevantAbsolute = false;
                }
                else
                {
                    Condition c = buildCondition(xpathRel, "relevant", ref_);
                    c = (Condition)_f.addTriggerable(c);
                    binding.relevancyCondition = c;
                }
            }

            String xpathReq = e.GetAttribute("required");
            if (xpathReq != null)
            {
                if ("true()".Equals(xpathReq))
                {
                    binding.requiredAbsolute = true;
                }
                else if ("false()".Equals(xpathReq))
                {
                    binding.requiredAbsolute = false;
                }
                else
                {
                    Condition c = buildCondition(xpathReq, "required", ref_);
                    c = (Condition)_f.addTriggerable(c);
                    binding.requiredCondition = c;
                }
            }

            String xpathRO = e.GetAttribute("readonly");
            if (xpathRO != null)
            {
                if ("true()".Equals(xpathRO))
                {
                    binding.readonlyAbsolute = true;
                }
                else if ("false()".Equals(xpathRO))
                {
                    binding.readonlyAbsolute = false;
                }
                else
                {
                    Condition c = buildCondition(xpathRO, "readonly", ref_);
                    c = (Condition)_f.addTriggerable(c);
                    binding.readonlyCondition = c;
                }
            }

            String xpathConstr = e.GetAttribute(null, "constraint");
            if (xpathConstr != null)
            {
                try
                {
                    binding.constraint = new XPathConditional(xpathConstr);
                }
                catch (XPathSyntaxException xse)
                {
                    //#if debug.output==verbose
                    Console.Error.WriteLine("Invalid XPath expression [" + xpathConstr + "]!" + getVagueLocation(e));
                    //#endif
                }
                binding.constraintMessage = e.GetAttribute("constraintMsg", NAMESPACE_JAVAROSA);
            }

            String xpathCalc = e.GetAttribute("calculate");
            if (xpathCalc != null)
            {
                Recalculate r = buildCalculate(xpathCalc, ref_);
                r = (Recalculate)_f.addTriggerable(r);
                binding.calculate = r;
            }

            binding.Preload = (e.GetAttribute("preload", NAMESPACE_JAVAROSA));
            binding.PreloadParams = (e.GetAttribute("preloadParams", NAMESPACE_JAVAROSA));

            return binding;
        }

        protected void parseBind(XmlElement e)
        {
            ArrayList usedAtts = new ArrayList();

            DataBinding binding = processStandardBindAttributes(usedAtts, e);

            //print unused attribute warning message for parent element
            if (XFormUtils.showUnusedAttributeWarning(e, usedAtts))
            {
                Console.WriteLine(XFormUtils.unusedAttWarning(e, usedAtts));
            }

            addBinding(binding);
        }

        private static Condition buildCondition(String xpath, String type, IDataReference contextRef)
        {
            XPathConditional cond;
            int trueAction = -1, falseAction = -1;

            if ("relevant".Equals(type))
            {
                trueAction = Condition.ACTION_SHOW;
                falseAction = Condition.ACTION_HIDE;
            }
            else if ("required".Equals(type))
            {
                trueAction = Condition.ACTION_REQUIRE;
                falseAction = Condition.ACTION_DONT_REQUIRE;
            }
            else if ("readonly".Equals(type))
            {
                trueAction = Condition.ACTION_DISABLE;
                falseAction = Condition.ACTION_ENABLE;
            }

            try
            {
                cond = new XPathConditional(xpath);
            }
            catch (XPathSyntaxException xse)
            {
                //#if debug.output==verbose
                Console.Error.WriteLine("Invalid XPath expression [" + xpath + "]!");
                //#endif
                return null;
            }

            Condition c = new Condition(cond, trueAction, falseAction, FormInstance.unpackReference(contextRef));
            return c;
        }

        private static Recalculate buildCalculate(String xpath, IDataReference contextRef)
        {
            XPathConditional calc;

            try
            {
                calc = new XPathConditional(xpath);
            }
            catch (XPathSyntaxException xse)
            {
                //#if debug.output==verbose
                Console.Error.WriteLine("Invalid XPath expression [" + xpath + "]!");
                //#endif
                return null;
            }

            Recalculate r = new Recalculate(calc, FormInstance.unpackReference(contextRef));
            return r;
        }

        protected void addBinding(DataBinding binding)
        {
            bindings.Add(binding);

            if (binding.ID != null)
            {
                try
                {
                    bindingsByID.Add(binding.ID, binding);
                }
                catch
                {
                    throw new XFormParseException("XForm Parse: <bind>s with duplicate ID: '" + binding.ID + "'");
                }
            }
        }

        //e is the top-level _data_ node of the instance (immediate (and only) child of <instance>)
        private void parseInstance(XmlElement e)
        {
            TreeElement root = buildInstanceStructure(e, null);
            FormInstance instanceModel = new FormInstance(root);
            instanceModel.setName(_f.Title);

            ArrayList usedAtts = new ArrayList();
            usedAtts.Add("version");
            usedAtts.Add("uiVersion");

            String schema = e.NamespaceURI;
            if (schema != null && schema.Length > 0 && !schema.Equals(defaultNamespace))
            {
                instanceModel.schema = schema;
            }
            instanceModel.formVersion = e.GetAttribute("version");
            instanceModel.uiVersion = e.GetAttribute("uiVersion");

            loadNamespaces(e, instanceModel);

            processRepeats(instanceModel);
            verifyBindings(instanceModel);
            applyInstanceProperties(instanceModel);
            loadInstanceData(e, root, _f);

            checkDependencyCycles();
            _f.Instance = (instanceModel);
            _f.finalizeTriggerables();

            //print unused attribute warning message for parent element
            if (XFormUtils.showUnusedAttributeWarning(e, usedAtts))
            {
                Console.WriteLine(XFormUtils.unusedAttWarning(e, usedAtts));
            }
        }

        private static IDictionary<String, String> loadNamespaces(XmlElement e, FormInstance tree)
        {
            IDictionary<String, String> prefixes = new Dictionary<String, String>();
            XmlNamespaceManager nsMgr = new XmlNamespaceManager(e.OwnerDocument.NameTable);

            // Retrieve the namespaces into a Generic dictionary with string keys.
            IDictionary<string, string> ns = nsMgr.GetNamespacesInScope(XmlNamespaceScope.All);


            foreach(var str in ns)
            {
                String prefix = str.ToString();
                String uri = ns[prefix];

                if (uri != null && prefix != null)
                {
                    tree.addNamespace(prefix, uri);
                }
            }
            return prefixes;
        }

        //parse instance hierarchy and turn into a skeleton model; ignoring data content, but respecting repeated nodes and 'template' flags
        public static TreeElement buildInstanceStructure(XmlElement node, TreeElement parent)
        {
            TreeElement element = null;

            //catch when text content is mixed with children
            int numChildren = node.ChildNodes.Count;
            Boolean hasText = false;
            Boolean hasElements = false;
            for (int i = 0; i < numChildren; i++)
            {
                switch (node.ChildNodes[i].NodeType)
                {
                    case XmlNodeType.Element:
                        hasElements = true; break;
                    case XmlNodeType.Text:
                        if (((XmlText)node.ChildNodes[i]).Value.ToString().Trim().Length > 0)
                            hasText = true;
                        break;
                }
            }
            if (hasElements && hasText)
            {
                Console.WriteLine("Warning: instance node '" + node.Name + "' contains both elements and text as children; text ignored");
            }

            //check for repeat templating
            String name = node.Name;
            int multiplicity;
            if (node.GetAttribute("template", NAMESPACE_JAVAROSA) != null)
            {
                multiplicity = TreeReference.INDEX_TEMPLATE;
                if (parent != null && parent.getChild(name, TreeReference.INDEX_TEMPLATE) != null)
                {
                    throw new XFormParseException("More than one node declared as the template for the same repeated set [" + name + "]", node);
                }
            }
            else
            {
                multiplicity = (parent == null ? 0 : parent.getChildMultiplicity(name));
            }


            String modelType = node.GetAttribute("modeltype", NAMESPACE_JAVAROSA);
            //create node; handle children
            if (modelType == null)
            {
                element = new TreeElement(name, multiplicity);
            }
            else
            {
                if (typeMappings[modelType] == null)
                {
                    throw new XFormParseException("ModelType " + modelType + " is not recognized.", node);
                }
                element = (TreeElement)modelPrototypes.getNewInstance(((int)typeMappings[modelType]).ToString());
                if (element == null)
                {
                    element = new TreeElement(name, multiplicity);
                    Console.WriteLine("No model type prototype available for " + modelType);
                }
                else
                {
                    element.setName(name);
                    element.setMult(multiplicity);
                }
            }

            if (hasElements)
            {
                for (int i = 0; i < numChildren; i++)
                {
                    if (node.ChildNodes[i].NodeType == XmlNodeType.Element)
                    {
                        element.addChild(buildInstanceStructure((XmlElement)node.ChildNodes[i], element));
                    }
                }
            }

            //handle attributes
            if (node.Attributes.Count > 0)
            {
                for (int i = 0; i < node.Attributes.Count; i++)
                {
                    String attrNamespace = node.Attributes[i].NamespaceURI;
                    String attrName = node.Attributes[i].Name;
                    if (attrNamespace.Equals(NAMESPACE_JAVAROSA) && attrName.Equals("template"))
                        continue;
                    if (attrNamespace.Equals(NAMESPACE_JAVAROSA) && attrName.Equals("recordset"))
                        continue;

                    element.setAttribute(attrNamespace, attrName, node.Attributes[i].Value);
                }
            }

            return element;
        }

        private List<TreeReference> getRepeatableRefs()
        {
            List<TreeReference> refs = new List<TreeReference>();

            for (int i = 0; i < repeats.Count; i++)
            {
                refs.Add((TreeReference)repeats[i]);
            }

            for (int i = 0; i < itemsets.Count; i++)
            {
                ItemsetBinding itemset = (ItemsetBinding)itemsets[i];
                TreeReference srcRef = itemset.nodesetRef;
                if (!refs.Contains(srcRef))
                {
                    //CTS: Being an itemset root is not sufficient to mark
                    //a node as repeatable. It has to be nonstatic (which it
                    //must be inherently unless there's a wildcard).
                    Boolean nonstatic = true;
                    for (int j = 0; j < srcRef.size(); ++j)
                    {
                        if (TreeReference.NAME_WILDCARD.Equals(srcRef.getName(j)))
                        {
                            nonstatic = false;
                        }
                    }
                    if (nonstatic)
                    {
                        refs.Add(srcRef);
                    }
                }

                if (itemset.copyMode)
                {
                    TreeReference destRef = itemset.getDestRef();
                    if (!refs.Contains(destRef))
                    {
                        refs.Add(destRef);
                    }
                }
            }

            return refs;
        }

        //pre-process and clean up instance regarding repeats; in particular:
        // 1) flag all repeat-related nodes as repeatable
        // 2) catalog which repeat template nodes are explicitly defined, and note which repeats bindings lack templates
        // 3) remove template nodes that are not valid for a repeat binding
        // 4) generate template nodes for repeat bindings that do not have one defined explicitly
        // 5) give a stern warning for any repeated instance nodes that do not correspond to a repeat binding
        // 6) verify that all sets of repeated nodes are homogeneous
        private void processRepeats(FormInstance instance)
        {
            flagRepeatables(instance);
            processTemplates(instance);
            checkDuplicateNodesAreRepeatable(instance.getRoot());
            checkHomogeneity(instance);
        }

        //flag all nodes identified by repeat bindings as repeatable
        private void flagRepeatables(FormInstance instance)
        {
            List<TreeReference> refs = getRepeatableRefs();
            for (int i = 0; i < refs.Count; i++)
            {
                TreeReference ref_ = refs[i];
                List<TreeReference> nodes = instance.expandReference(ref_, true);
                for (int j = 0; j < nodes.Count; j++)
                {
                    TreeReference nref = nodes[j];
                    TreeElement node = instance.resolveReference(nref);
                    if (node != null)
                    { // catch '/'
                        node.repeatable = true;
                    }
                }
            }
        }

        private void processTemplates(FormInstance instance)
        {
            repeatTree = buildRepeatTree(getRepeatableRefs(), instance.getRoot().getName());

            List<TreeReference> missingTemplates = new List<TreeReference>();
            checkRepeatsForTemplate(instance, repeatTree, missingTemplates);

            removeInvalidTemplates(instance, repeatTree);
            createMissingTemplates(instance, missingTemplates);
        }

        //build a pseudo-data model tree that describes the repeat structure of the instance
        //result is a FormInstance collapsed where all indexes are 0, and repeatable nodes are flagged as such
        //return null if no repeats
        //ignores (invalid) repeats that bind outside the top-level instance data node
        private static FormInstance buildRepeatTree(List<TreeReference> repeatRefs, String topLevelName)
        {
            TreeElement root = new TreeElement(null, 0);

            for (int i = 0; i < repeatRefs.Count; i++)
            {
                TreeReference repeatRef = repeatRefs[i];
                if (repeatRef.size() <= 1)
                {
                    //invalid repeat: binds too high. ignore for now and error will be raised in verifyBindings
                    continue;
                }

                TreeElement cur = root;
                for (int j = 0; j < repeatRef.size(); j++)
                {
                    String name = repeatRef.getName(j);
                    TreeElement child = cur.getChild(name, 0);
                    if (child == null)
                    {
                        child = new TreeElement(name, 0);
                        cur.addChild(child);
                    }

                    cur = child;
                }
                cur.repeatable = true;
            }

            if (root.getNumChildren() == 0)
                return null;
            else
                return new FormInstance(root.getChild(topLevelName, TreeReference.DEFAULT_MUTLIPLICITY));
        }

        //checks which repeat bindings have explicit template nodes; returns a vector of the bindings that do not
        private static void checkRepeatsForTemplate(FormInstance instance, FormInstance repeatTree, List<TreeReference> missingTemplates)
        {
            if (repeatTree != null)
                checkRepeatsForTemplate(repeatTree.getRoot(), TreeReference.rootRef(), instance, missingTemplates);
        }

        //helper function for checkRepeatsForTemplate
        private static void checkRepeatsForTemplate(TreeElement repeatTreeNode, TreeReference ref_, FormInstance instance, List<TreeReference> missing)
        {
            String name = repeatTreeNode.getName();
            int mult = (repeatTreeNode.repeatable ? TreeReference.INDEX_TEMPLATE : 0);
            ref_ = ref_.extendRef(name, mult);

            if (repeatTreeNode.repeatable)
            {
                TreeElement template = instance.resolveReference(ref_);
                if (template == null)
                {
                    missing.Add(ref_);
                }
            }

            for (int i = 0; i < repeatTreeNode.getNumChildren(); i++)
            {
                checkRepeatsForTemplate(repeatTreeNode.getChildAt(i), ref_, instance, missing);
            }
        }

        //iterates through instance and removes template nodes that are not valid. a template is invalid if:
        //  it is declared for a node that is not repeatable
        //  it is for a repeat that is a child of another repeat and is not located within the parent's template node
        private static void removeInvalidTemplates(FormInstance instance, FormInstance repeatTree)
        {
            removeInvalidTemplates(instance.getRoot(), (repeatTree == null ? null : repeatTree.getRoot()), true);
        }

        //helper function for removeInvalidTemplates
        private static Boolean removeInvalidTemplates(TreeElement instanceNode, TreeElement repeatTreeNode, Boolean templateAllowed)
        {
            int mult = instanceNode.getMult();
            Boolean repeatable = (repeatTreeNode == null ? false : repeatTreeNode.repeatable);

            if (mult == TreeReference.INDEX_TEMPLATE)
            {
                if (!templateAllowed)
                {
                    Console.WriteLine("Warning: template nodes for sub-repeats must be located within the template node of the parent repeat; ignoring template... [" + instanceNode.getName() + "]");
                    return true;
                }
                else if (!repeatable)
                {
                    Console.WriteLine("Warning: template node found for ref that is not repeatable; ignoring... [" + instanceNode.getName() + "]");
                    return true;
                }
            }

            if (repeatable && mult != TreeReference.INDEX_TEMPLATE)
                templateAllowed = false;

            for (int i = 0; i < instanceNode.getNumChildren(); i++)
            {
                TreeElement child = instanceNode.getChildAt(i);
                TreeElement rchild = (repeatTreeNode == null ? null : repeatTreeNode.getChild(child.getName(), 0));

                if (removeInvalidTemplates(child, rchild, templateAllowed))
                {
                    instanceNode.removeChildAt(i);
                    i--;
                }
            }
            return false;
        }

        //if repeatables have no template node, duplicate first as template
        private static void createMissingTemplates(FormInstance instance, List<TreeReference> missingTemplates)
        {
            //it is VERY important that the missing template refs are listed in depth-first or breadth-first order... namely, that
            //every ref is listed after a ref that could be its parent. checkRepeatsForTemplate currently behaves this way
            for (int i = 0; i < missingTemplates.Count; i++)
            {
                TreeReference templRef = missingTemplates[i];
                TreeReference firstMatch;

                //make template ref generic and choose first matching node
                TreeReference ref_ = templRef.clone();
                for (int j = 0; j < ref_.size(); j++)
                {
                    ref_.setMultiplicity(j, TreeReference.INDEX_UNBOUND);
                }
                List<TreeReference> nodes = instance.expandReference(ref_);
                if (nodes.Count == 0)
                {
                    //binding error; not a single node matches the repeat binding; will be reported later
                    continue;
                }
                else
                {
                    firstMatch = nodes[0];
                }

                try
                {
                    instance.copyNode(firstMatch, templRef);
                }
                catch (InvalidReferenceException e)
                {
                    Console.WriteLine("WARNING! Could not create a default repeat template; this is almost certainly a homogeneity error! Your form will not work! (Failed on " + templRef.ToString() + ")");
                    Console.WriteLine(e.StackTrace);
                }
                trimRepeatChildren(instance.resolveReference(templRef));
            }
        }

        //trim repeatable children of newly created template nodes; we trim because the templates are supposed to be devoid of 'data',
        //  and # of repeats for a given repeat node is a kind of data. trust me
        private static void trimRepeatChildren(TreeElement node)
        {
            for (int i = 0; i < node.getNumChildren(); i++)
            {
                TreeElement child = node.getChildAt(i);
                if (child.repeatable)
                {
                    node.removeChildAt(i);
                    i--;
                }
                else
                {
                    trimRepeatChildren(child);
                }
            }
        }

        private static void checkDuplicateNodesAreRepeatable(TreeElement node)
        {
            int mult = node.getMult();
            if (mult > 0)
            { //repeated node
                if (!node.repeatable)
                {
                    Console.WriteLine("Warning: repeated nodes [" + node.getName() + "] detected that have no repeat binding in the form; DO NOT bind questions to these nodes or their children!");
                    //we could do a more comprehensive safety check in the future
                }
            }

            for (int i = 0; i < node.getNumChildren(); i++)
            {
                checkDuplicateNodesAreRepeatable(node.getChildAt(i));
            }
        }

        //check repeat sets for homogeneity
        private void checkHomogeneity(FormInstance instance)
        {
            List<TreeReference> refs = getRepeatableRefs();
            for (int i = 0; i < refs.Count; i++)
            {
                TreeReference ref_ = refs[i];
                TreeElement template = null;
                List<TreeReference> nodes = instance.expandReference(ref_);
                for (int j = 0; j < nodes.Count; j++)
                {
                    TreeReference nref = nodes[j];
                    TreeElement node = instance.resolveReference(nref);
                    if (node == null) //don't crash on '/'... invalid repeat binding will be caught later
                        continue;

                    if (template == null)
                        template = instance.getTemplate(nref);

                    if (!FormInstance.isHomogeneous(template, node))
                    {
                        Console.WriteLine("WARNING! Not all repeated nodes for a given repeat binding [" + nref.ToString() + "] are homogeneous! This will cause serious problems!");
                    }
                }
            }
        }

        private void verifyBindings(FormInstance instance)
        {
            //check <bind>s (can't bind to '/', bound nodes actually exist)
            for (int i = 0; i < bindings.Count; i++)
            {
                DataBinding bind = bindings[i];
                TreeReference ref_ = FormInstance.unpackReference(bind.Reference);

                if (ref_.size() == 0)
                {
                    Console.WriteLine("Cannot bind to '/'; ignoring bind...");
                    bindings.RemoveAt(i);
                    i--;
                }
                else
                {
                    List<TreeReference> nodes = instance.expandReference(ref_, true);
                    if (nodes.Count == 0)
                    {
                        Console.WriteLine("WARNING: Bind [" + ref_.ToString() + "] matches no nodes; ignoring bind...");
                    }
                }
            }

            //check <repeat>s (can't bind to '/' or '/data')
            List<TreeReference> refs = getRepeatableRefs();
            for (int i = 0; i < refs.Count; i++)
            {
                TreeReference ref_ = refs[i];

                if (ref_.size() <= 1)
                {
                    throw new XFormParseException("Cannot bind repeat to '/' or '/" + instanceNode.Name + "'");
                }
            }

            //check control/group/repeat bindings (bound nodes exist, question can't bind to '/')
            List<String> bindErrors = new List<String>();
            verifyControlBindings(_f, instance, bindErrors);
            if (bindErrors.Count > 0)
            {
                String errorMsg = "";
                for (int i = 0; i < bindErrors.Count; i++)
                {
                    errorMsg += bindErrors[i] + "\n";
                }
                throw new XFormParseException(errorMsg);
            }

            //check that repeat members bind to the proper scope (not above the binding of the parent repeat, and not within any sub-repeat (or outside repeat))
            verifyRepeatMemberBindings(_f, instance, null);

            //check that label/copy/value refs are children of nodeset ref, and exist
            verifyItemsetBindings(instance);

            verifyItemsetSrcDstCompatibility(instance);
        }

        private static void verifyControlBindings(IFormElement fe, FormInstance instance, List<String> errors)
        { //throws XmlPullParserException {
            if (fe.Children == null)
                return;

            for (int i = 0; i < fe.Children.Count; i++)
            {
                IFormElement child = fe.Children[i];
                IDataReference ref_ = null;
                String type = null;

                if (child is GroupDef)
                {
                    ref_ = ((GroupDef)child).Bind;
                    type = (((GroupDef)child).Repeat ? "Repeat" : "Group");
                }
                else if (child is QuestionDef)
                {
                    ref_ = ((QuestionDef)child).Bind;
                    type = "Control";
                }
                TreeReference tref = FormInstance.unpackReference(ref_);

                if (child is QuestionDef && tref.size() == 0)
                {
                    Console.WriteLine("Warning! Cannot bind control to '/'"); //group can bind to '/'; repeat can't, but that's checked above
                }
                else
                {
                    List<TreeReference> nodes = instance.expandReference(tref, true);
                    if (nodes.Count == 0)
                    {
                        String error = "ERROR: " + type + " binding [" + tref.ToString() + "] matches no nodes";
                        Console.Error.WriteLine(error);
                        errors.Add(error);
                    }
                    //we can't check whether questions map to the right kind of node ('data' node vs. 'sub-tree' node) as that depends
                    //on the question's data type, which we don't know yet
                }

                verifyControlBindings(child, instance, errors);
            }
        }

        private void verifyRepeatMemberBindings(IFormElement fe, FormInstance instance, GroupDef parentRepeat)
        {
            if (fe.Children == null)
                return;

            for (int i = 0; i < fe.Children.Count; i++)
            {
                IFormElement child = fe.Children[i];
                Boolean isRepeat = (child is GroupDef && ((GroupDef)child).Repeat);

                //get bindings of current node and nearest enclosing repeat
                TreeReference repeatBind = (parentRepeat == null ? TreeReference.rootRef() : FormInstance.unpackReference(parentRepeat.Bind));
                TreeReference childBind = FormInstance.unpackReference(child.Bind);

                //check if current binding is within scope of repeat binding
                if (!repeatBind.isParentOf(childBind, false))
                {
                    //catch <repeat nodeset="/a/b"><input ref="/a/c" /></repeat>: repeat question is not a child of the repeated node
                    throw new XFormParseException("<repeat> member's binding [" + childBind.ToString() + "] is not a descendant of <repeat> binding [" + repeatBind.ToString() + "]!");
                }
                else if (repeatBind.Equals(childBind) && isRepeat)
                {
                    //catch <repeat nodeset="/a/b"><repeat nodeset="/a/b">...</repeat></repeat> (<repeat nodeset="/a/b"><input ref="/a/b" /></repeat> is ok)
                    throw new XFormParseException("child <repeat>s [" + childBind.ToString() + "] cannot bind to the same node as their parent <repeat>; only questions/groups can");
                }

                //check that, in the instance, current node is not within the scope of any closer repeat binding
                //build a list of all the node's instance ancestors
                List<TreeElement> repeatAncestry = new List<TreeElement>();
                TreeElement repeatNode = (repeatTree == null ? null : repeatTree.getRoot());
                if (repeatNode != null)
                {
                    repeatAncestry.Add(repeatNode);
                    for (int j = 1; j < childBind.size(); j++)
                    {
                        repeatNode = repeatNode.getChild(childBind.getName(j), 0);
                        if (repeatNode != null)
                        {
                            repeatAncestry.Add(repeatNode);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                //check that no nodes between the parent repeat and the target are repeatable
                for (int k = repeatBind.size(); k < childBind.size(); k++)
                {
                    TreeElement rChild = (k < repeatAncestry.Count ? repeatAncestry[k] : null);
                    Boolean repeatable = (rChild == null ? false : rChild.repeatable);
                    if (repeatable && !(k == childBind.size() - 1 && isRepeat))
                    {
                        //catch <repeat nodeset="/a/b"><input ref="/a/b/c/d" /></repeat>...<repeat nodeset="/a/b/c">...</repeat>:
                        //  question's/group's/repeat's most immediate repeat parent in the instance is not its most immediate repeat parent in the form def
                        throw new XFormParseException("<repeat> member's binding [" + childBind.ToString() + "] is within the scope of a <repeat> that is not its closest containing <repeat>!");
                    }
                }

                verifyRepeatMemberBindings(child, instance, (isRepeat ? (GroupDef)child : parentRepeat));
            }
        }

        private void verifyItemsetBindings(FormInstance instance)
        {
            for (int i = 0; i < itemsets.Count; i++)
            {
                ItemsetBinding itemset = itemsets[i];

                //check proper parent/child relationship
                if (!itemset.nodesetRef.isParentOf(itemset.labelRef, false))
                {
                    throw new XFormParseException("itemset nodeset ref is not a parent of label ref");
                }
                else if (itemset.copyRef != null && !itemset.nodesetRef.isParentOf(itemset.copyRef, false))
                {
                    throw new XFormParseException("itemset nodeset ref is not a parent of copy ref");
                }
                else if (itemset.valueRef != null && !itemset.nodesetRef.isParentOf(itemset.valueRef, false))
                {
                    throw new XFormParseException("itemset nodeset ref is not a parent of value ref");
                }

                //check label/value/copy nodes exist
                if (instance.getTemplatePath(itemset.labelRef) == null)
                {
                    throw new XFormParseException("<label> node for itemset doesn't exist! [" + itemset.labelRef + "]");
                }
                else if (itemset.copyRef != null && instance.getTemplatePath(itemset.copyRef) == null)
                {
                    throw new XFormParseException("<copy> node for itemset doesn't exist! [" + itemset.copyRef + "]");
                }
                else if (itemset.valueRef != null && instance.getTemplatePath(itemset.valueRef) == null)
                {
                    throw new XFormParseException("<value> node for itemset doesn't exist! [" + itemset.valueRef + "]");
                }
            }
        }

        private void verifyItemsetSrcDstCompatibility(FormInstance instance)
        {
            for (int i = 0; i < itemsets.Count; i++)
            {
                ItemsetBinding itemset = itemsets[i];

                Boolean destRepeatable = (instance.getTemplate(itemset.getDestRef()) != null);
                if (itemset.copyMode)
                {
                    if (!destRepeatable)
                    {
                        throw new XFormParseException("itemset copies to node(s) which are not repeatable");
                    }

                    //validate homogeneity between src and dst nodes
                    TreeElement srcNode = instance.getTemplatePath(itemset.copyRef);
                    TreeElement dstNode = instance.getTemplate(itemset.getDestRef());

                    if (!FormInstance.isHomogeneous(srcNode, dstNode))
                    {
                        Console.WriteLine("WARNING! Source [" + srcNode.getRef().ToString() + "] and dest [" + dstNode.getRef().ToString() +
                                "] of itemset appear to be incompatible!");
                    }

                    //TODO: i feel like, in theory, i should additionally check that the repeatable children of src and dst
                    //match up (Achild is repeatable <--> Bchild is repeatable). isHomogeneous doesn't check this. but i'm
                    //hard-pressed to think of scenarios where this would actually cause problems
                }
                else
                {
                    if (destRepeatable)
                    {
                        throw new XFormParseException("itemset sets value on repeatable nodes");
                    }
                }
            }
        }

        private void applyInstanceProperties(FormInstance instance)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                DataBinding bind = bindings[i];
                TreeReference ref_ = FormInstance.unpackReference(bind.Reference);
                List<TreeReference> nodes = instance.expandReference(ref_, true);

                if (nodes.Count > 0)
                {
                    attachBindGeneral(bind);
                }
                for (int j = 0; j < nodes.Count; j++)
                {
                    TreeReference nref = nodes[j];
                    attachBind(instance.resolveReference(nref), bind);
                }
            }

            applyControlProperties(instance);
        }

        private static void attachBindGeneral(DataBinding bind)
        {
            TreeReference ref_ = FormInstance.unpackReference(bind.Reference);

            if (bind.relevancyCondition != null)
            {
                bind.relevancyCondition.addTarget(ref_);
            }
            if (bind.requiredCondition != null)
            {
                bind.requiredCondition.addTarget(ref_);
            }
            if (bind.readonlyCondition != null)
            {
                bind.readonlyCondition.addTarget(ref_);
            }
            if (bind.calculate != null)
            {
                bind.calculate.addTarget(ref_);
            }
        }

        private static void attachBind(TreeElement node, DataBinding bind)
        {
            node.dataType = bind.DataType;

            if (bind.relevancyCondition == null)
            {
                node.setRelevant(bind.relevantAbsolute);
            }
            if (bind.requiredCondition == null)
            {
                node.setRequired(bind.requiredAbsolute);
            }
            if (bind.readonlyCondition == null)
            {
                node.setEnabled(!bind.readonlyAbsolute);
            }
            if (bind.constraint != null)
            {
                node.setConstraint(new Constraint(bind.constraint, bind.constraintMessage));
            }

            node.setPreloadHandler(bind.Preload);
            node.setPreloadParams(bind.PreloadParams);
        }

        //apply properties to instance nodes that are determined by controls bound to those nodes
        //this should make you feel slightly dirty, but it allows us to be somewhat forgiving with the form
        //(e.g., a select question bound to a 'text' type node) 
        private void applyControlProperties(FormInstance instance)
        {
            for (int h = 0; h < 2; h++)
            {
                List<TreeReference> selectRefs = (h == 0 ? selectOnes : selectMultis);
                int type = (h == 0 ? Constants.DATATYPE_CHOICE : Constants.DATATYPE_CHOICE_LIST);

                for (int i = 0; i < selectRefs.Count; i++)
                {
                    TreeReference ref_ = selectRefs[i];
                    List<TreeReference> nodes = instance.expandReference(ref_, true);
                    for (int j = 0; j < nodes.Count; j++)
                    {
                        TreeElement node = instance.resolveReference(nodes[j]);
                        if (node.dataType == Constants.DATATYPE_CHOICE || node.dataType == Constants.DATATYPE_CHOICE_LIST)
                        {
                            //do nothing
                        }
                        else if (node.dataType == Constants.DATATYPE_NULL || node.dataType == Constants.DATATYPE_TEXT)
                        {
                            node.dataType = type;
                        }
                        else
                        {
                            Console.WriteLine("Warning! Type incompatible with select question node [" + ref_.ToString() + "] detected!");
                        }
                    }
                }
            }
        }

        //TODO: hook here for turning sub-trees into complex IAnswerData objects (like for immunizations)
        //FIXME: the 'ref' and FormDef parameters (along with the helper function above that initializes them) are only needed so that we
        //can fetch QuestionDefs bound to the given node, as the QuestionDef reference is needed to properly represent answers
        //to select questions. obviously, we want to fix this.
        private static void loadInstanceData(XmlElement node, TreeElement cur, FormDef f)
        {
            int numChildren = node.ChildNodes.Count;
            Boolean hasElements = false;
            for (int i = 0; i < numChildren; i++)
            {
                if (node.ChildNodes[i].NodeType == XmlNodeType.Element)
                    hasElements = true;
            }

            if (hasElements)
            {
                IDictionary<String, int> multiplicities = new Dictionary<String, int>(); //stores max multiplicity seen for a given node name thus far
                for (int i = 0; i < numChildren; i++)
                {
                    if (node.ChildNodes[i].NodeType == XmlNodeType.Element)
                    {
                        XmlElement child = (XmlElement)node.ChildNodes[i];

                        String name = child.Name;
                        int index;
                        Boolean isTemplate = (child.GetAttribute("template", NAMESPACE_JAVAROSA) != null);

                        if (isTemplate)
                        {
                            index = TreeReference.INDEX_TEMPLATE;
                        }
                        else
                        {
                            //update multiplicity counter
                            int mult = multiplicities[name];
                            index = (mult == null ? 0 : mult + 1);
                            multiplicities.Add(name, (int)(index));
                        }

                        loadInstanceData(child, cur.getChild(name, index), f);
                    }
                }
            }
            else
            {
                String text = getXMLText(node, true);
                if (text != null && text.Trim().Length > 0)
                { //ignore text that is only whitespace
                    //TODO: custom data types? modelPrototypes?

                    cur.setValue(XFormAnswerDataParser.getAnswerData(text, cur.dataType, ghettoGetQuestionDef(cur.dataType, f, cur.getRef())));
                }
            }
        }

        //find a questiondef that binds to ref, if the data type is a 'select' question type
        public static QuestionDef ghettoGetQuestionDef(int dataType, FormDef f, TreeReference ref_)
        {
            if (dataType == Constants.DATATYPE_CHOICE || dataType == Constants.DATATYPE_CHOICE_LIST)
            {
                return FormDef.findQuestionByRef(ref_, f);
            }
            else
            {
                return null;
            }
        }

        private void checkDependencyCycles()
        {
            ArrayList vertices = new ArrayList();
            ArrayList edges = new ArrayList();

            //build graph
            for (IEnumerator e = _f.triggerIndex.GetEnumerator(); e.MoveNext(); )
            {
                TreeReference trigger = (TreeReference)e.Current;
                if (!vertices.Contains(trigger))
                    vertices.Add(trigger);

                List<Triggerable> triggered = (List<Triggerable>)_f.triggerIndex[trigger];
                ArrayList targets = new ArrayList();
                for (int i = 0; i < triggered.Count; i++)
                {
                    Triggerable t = (Triggerable)triggered[i];
                    for (int j = 0; j < t.getTargets().Count; j++)
                    {
                        TreeReference target = (TreeReference)t.getTargets()[j];
                        if (!targets.Contains(target))
                            targets.Add(target);
                    }
                }

                for (int i = 0; i < targets.Count; i++)
                {
                    TreeReference target = (TreeReference)targets[i];
                    if (!vertices.Contains(target))
                        vertices.Add(target);

                    TreeReference[] edge = { trigger, target };
                    edges.Add(edge);
                }
            }

            //find cycles
            Boolean acyclic = true;
            while (vertices.Count > 0)
            {
                //determine leaf nodes
                ArrayList leaves = new ArrayList();
                for (int i = 0; i < vertices.Count; i++)
                {
                    leaves.Add(vertices[i]);
                }
                for (int i = 0; i < edges.Count; i++)
                {
                    TreeReference[] edge = (TreeReference[])edges[i];
                    leaves.Remove(edge[0]);
                }

                //if no leaf nodes while graph still has nodes, graph has cycles
                if (leaves.Count == 0)
                {
                    acyclic = false;
                    break;
                }

                //remove leaf nodes and edges pointing to them
                for (int i = 0; i < leaves.Count; i++)
                {
                    TreeReference leaf = (TreeReference)leaves[i];
                    vertices.Remove(leaf);
                }
                for (int i = edges.Count - 1; i >= 0; i--)
                {
                    TreeReference[] edge = (TreeReference[])edges[i];
                    if (leaves.Contains(edge[1]))
                        edges.RemoveAt(i);
                }
            }

            if (!acyclic)
            {
                Console.Error.WriteLine("XPath Dependency Cycle:");
                for (int i = 0; i < edges.Count; i++)
                {
                    TreeReference[] edge = (TreeReference[])edges[i];
                    Console.Error.WriteLine(edge[0].ToString() + " => " + edge[1].ToString());
                }
                throw new SystemException("Dependency cycles amongst the xpath expressions in relevant/calculate");
            }
        }

        public static void loadXmlInstance(FormDef f, StreamReader xmlReader)
        {
            loadXmlInstance(f, getXMLDocument(xmlReader));
        }

        /**
         * Load a compatible xml instance into FormDef f
         * 
         * call before f.initialize()!
         */
        public static void loadXmlInstance(FormDef f, XmlDocument xmlInst)
        {
            TreeElement savedRoot = XFormParser.restoreDataModel(xmlInst, null).getRoot();
            TreeElement templateRoot = f.Instance.getRoot().deepCopy(true);

            // weak check for matching forms
            // TODO: should check that namespaces match?
            if (!savedRoot.getName().Equals(templateRoot.getName()) || savedRoot.getMult() != 0)
            {
                throw new SystemException("Saved form instance does not match template form definition");
            }

            // populate the data model
            TreeReference tr = TreeReference.rootRef();
            tr.add(templateRoot.getName(), TreeReference.INDEX_UNBOUND);
            templateRoot.populate(savedRoot, f);

            // populated model to current form
            f.Instance.setRoot(templateRoot);

            // if the new instance is inserted into the formdef before f.initialize() is called, this
            // locale refresh is unnecessary
            //   Localizer loc = f.getLocalizer();
            //   if (loc != null) {
            //       f.localeChanged(loc.getLocale(), loc);
            //	 }
        }

        //returns data type corresponding to type string; doesn't handle defaulting to 'text' if type unrecognized/unknown
        private static int getDataType(String type)
        {
            int dataType = Constants.DATATYPE_NULL;

            if (type != null)
            {
                //cheap out and ignore namespace
                if (type.IndexOf(":") != -1)
                {
                    type = type.Substring(type.IndexOf(":") + 1);
                }

                if (typeMappings.ContainsKey(type))
                {
                    dataType = ((int)typeMappings[type]);
                }
                else
                {
                    dataType = Constants.DATATYPE_UNSUPPORTED;
                    //#if debug.output==verbose
                    Console.Error.WriteLine("XForm Parse WARNING: unrecognized data type [" + type + "]");
                    //#endif
                }
            }

            return dataType;
        }

        public static void addModelPrototype(int type, TreeElement element)
        {
            modelPrototypes.addNewPrototype(type.ToString(), element.GetType());
        }

        public static void addDataType(String type, int dataType)
        {
            typeMappings.Add(type, (int)(dataType));
        }

        private class anonymosIH : IElementHandler
        {
            int typeId;
            public anonymosIH(int t)
            {
                typeId = t;
            }
            public void handle(XFormParser p, XmlElement e, Object parent)
            {
                p.parseControl((IFormElement)parent, e, typeId);
            }
        }

        public static void registerControlType(String type, int typeId)
        {
            IElementHandler newHandler = new anonymosIH(typeId);
            topLevelHandlers.Add(type, newHandler);
            groupLevelHandlers.Add(type, newHandler);
        }

        public static void registerHandler(String type, IElementHandler handler)
        {
            topLevelHandlers.Add(type, handler);
            groupLevelHandlers.Add(type, handler);
        }

        public static String getXMLText(XmlNode n, Boolean trim)
        {
            return (n.ChildNodes.Count == 0 ? null : getXMLText(n, 0, trim));
        }

        /**
        * reads all subsequent text nodes and returns the combined string
        * needed because escape sequences are parsed into consecutive text nodes
        * e.g. "abc&amp;123" --> (abc)(&)(123)
        **/
        public static String getXMLText(XmlNode node, int i, Boolean trim)
        {
            StringBuilder strBuff = null;

            String text = node.ChildNodes[i].InnerText;
            if (text == null)
                return null;

            for (i++; i < node.ChildNodes.Count && node.ChildNodes[i].NodeType == XmlNodeType.Text; i++)
            {
                if (strBuff == null)
                    strBuff = new StringBuilder(text);

                strBuff.Append(node.ChildNodes[i].InnerText);
            }
            if (strBuff != null)
                text = strBuff.ToString();

            if (trim)
                text = text.Trim();

            return text;
        }

        public static FormInstance restoreDataModel(System.IO.Stream input, System.Type restorableType)
        {
            XmlDocument doc = getXMLDocument(new StreamReader(input));
            if (doc == null)
            {
                throw new SystemException("syntax error in XML instance; could not parse");
            }
            return restoreDataModel(doc, restorableType);
        }

        public static FormInstance restoreDataModel(XmlDocument doc, Type restorableType)
        {
            Restorable r = (restorableType != null ? (Restorable)PrototypeFactory.getInstance(restorableType) : null);

            XmlElement e = doc.DocumentElement;

            TreeElement te = buildInstanceStructure(e, null);
            FormInstance dm = new FormInstance(te);
            loadNamespaces(e, dm);
            if (r != null)
            {
                RestoreUtils.templateData(r, dm, null);
            }
            loadInstanceData(e, te, null);

            return dm;
        }

        public static FormInstance restoreDataModel(byte[] data, Type restorableType)
        {
            return restoreDataModel(new System.IO.MemoryStream(data), restorableType);
        }

        public static String getVagueLocation(XmlElement e)
        {
            String path = e.Name;
            XmlElement walker = e;
            while (walker != null)
            {
                XmlNode n = walker.ParentNode;
                if (n is XmlElement)
                {
                    walker = (XmlElement)n;
                    String step = walker.Name;
                    for (int i = 0; i < walker.Attributes.Count; ++i)
                    {
                        step += "[@" + walker.Attributes[i].Name + "=";
                        step += walker.Attributes[i].Value + "]";
                    }
                    path = step + "/" + path;
                }
                else
                {
                    walker = null;
                    path = "/" + path;
                }
            }

            String elementString = getVagueElementPrintout(e, 2);

            String fullmsg = "\n    Problem found at nodeset: " + path;
            fullmsg += "\n    With element " + elementString + "\n";
            return fullmsg;
        }

        public static String getVagueElementPrintout(XmlElement e, int maxDepth)
        {
            String elementString = "<" + e.Name;
            for (int i = 0; i < e.Attributes.Count; ++i)
            {
                elementString += " " + e.Attributes[i].Name + "=\"";
                elementString += e.Attributes[i].Value + "\"";
            }
            if (e.ChildNodes.Count > 0)
            {
                elementString += ">";
                if (e.ChildNodes[0].NodeType == XmlNodeType.Element)
                {
                    if (maxDepth > 0)
                    {
                        elementString += getVagueElementPrintout((XmlElement)e.ChildNodes[0], maxDepth - 1);
                    }
                    else
                    {
                        elementString += "...";
                    }
                }
            }
            else
            {
                elementString += "/>";
            }
            return elementString;
        }
    }
}