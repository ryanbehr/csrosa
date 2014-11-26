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
using org.javarosa.core.model.data;
using org.javarosa.core.model.data.helper;
using org.javarosa.core.model.utils;
using System;
using System.Collections;
using System.Collections.Generic;
namespace org.javarosa.xform.util
{


    /**
     * The XFormAnswerDataParser is responsible for taking XForms elements and
     * parsing them into a specific type of IAnswerData.
     * 
     * @author Acellam Guy ,  Clayton Sims
     * 
     */

    /*
    int
    text
    float
    datetime
    date
    time
    choice
    choice list
    */

    public class XFormAnswerDataParser
    {
        //FIXME: the QuestionDef parameter is a hack until we find a better way to represent AnswerDatas for select questions

        public static IAnswerData getAnswerData(String text, int dataType)
        {
            return getAnswerData(text, dataType, null);
        }
        public static IAnswerData getAnswerData(String text, int dataType, QuestionDef q)
        {
            String trimmedText = text.Trim();
            if (trimmedText.Length == 0)
                trimmedText = null;

            switch (dataType)
            {
                case Constants.DATATYPE_NULL:
                case Constants.DATATYPE_UNSUPPORTED:
                case Constants.DATATYPE_TEXT:
                case Constants.DATATYPE_BARCODE:
                case Constants.DATATYPE_BINARY:

                    return new StringData(text);

                case Constants.DATATYPE_INTEGER:

                    try
                    {
                        return (trimmedText == null ? null : new IntegerData(int.Parse(trimmedText)));
                    }
                    catch (FormatException nfe)
                    {
                        return null;
                    }

                case Constants.DATATYPE_LONG:

                    try
                    {
                        return (trimmedText == null ? null : new LongData(long.Parse(trimmedText)));
                    }
                    catch (FormatException nfe)
                    {
                        return null;
                    }

                case Constants.DATATYPE_DECIMAL:

                    try
                    {
                        return (trimmedText == null ? null : new DecimalData(Double.Parse(trimmedText)));
                    }
                    catch (FormatException nfe)
                    {
                        return null;
                    }

                case Constants.DATATYPE_CHOICE:

                    ArrayList selections = getSelections(text, q);
                    return (selections.Count == 0 ? null : new SelectOneData((Selection)selections[0]));

                case Constants.DATATYPE_CHOICE_LIST:

                    return new SelectMultiData(getSelections(text, q));

                case Constants.DATATYPE_DATE_TIME:

                    DateTime dt = (trimmedText == null ? DateTime.Now : DateUtils.parseDateTime(trimmedText));
                    return (dt == null ? null : new DateTimeData(ref dt));

                case Constants.DATATYPE_DATE:

                    DateTime d = (trimmedText == null ? DateTime.Now : DateUtils.parseDate(trimmedText));
                    return (d == null ? null : new DateData(ref d));

                case Constants.DATATYPE_TIME:

                    DateTime t = (trimmedText == null ? DateTime.Now : DateUtils.parseTime(trimmedText));
                    return (t == null ? null : new TimeData(ref t));

                case Constants.DATATYPE_BOOLEAN:

                    if (trimmedText == null)
                    {
                        return null;
                    }
                    else
                    {
                        if (trimmedText.Equals("1")) { return new BooleanData(true); }
                        if (trimmedText.Equals("0")) { return new BooleanData(false); }
                        return trimmedText.Equals("t") ? new BooleanData(true) : new BooleanData(false);
                    }

                case Constants.DATATYPE_GEOPOINT:

                    try
                    {
                        List<String> gpv = (trimmedText == null ? null : DateUtils.split(trimmedText, " ", false));

                        int len = gpv.Count;
                        double[] gp = new double[len];
                        for (int i = 0; i < len; i++)
                        {
                            gp[i] = Double.Parse(((String)gpv[i]));
                        }
                        return new GeoPointData(gp);
                    }
                    catch (FormatException nfe)
                    {
                        return null;
                    }

                default:
                    return new UncastData(trimmedText);
            }
        }

        private static ArrayList getSelections(String text, QuestionDef q)
        {
            ArrayList v = new ArrayList();

            List<String> choices = DateUtils.split(text, XFormAnswerDataSerializer.DELIMITER, true);
            for (int i = 0; i < choices.Count; i++)
            {
                Selection s = getSelection((String)choices[i], q);
                if (s != null)
                    v.Add(s);
            }

            return v;
        }

        private static Selection getSelection(String choiceValue, QuestionDef q)
        {
            Selection s;

            if (q == null || q.getDynamicChoices() != null)
            {
                s = new Selection(choiceValue);
            }
            else
            {
                SelectChoice choice = q.getChoiceForValue(choiceValue);
                s = (choice != null ? choice.selection() : null);
            }

            return s;
        }
    }
}