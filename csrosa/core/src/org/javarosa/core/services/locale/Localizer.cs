using org.javarosa.core.util;
using org.javarosa.core.util.externalizable;
using System;
using System.Collections;
using System.IO;
namespace org.javarosa.core.services.locale
{

    /**
     * The Localizer object maintains mappings for locale ID's and Object
     * ID's to the String values associated with them in different 
     * locales.
     * 
     * @author Acellam Guy ,  Drew Roos/Clayton Sims
     *
     */
    public class Localizer : Externalizable
    {
        private ArrayList locales; /* ArrayList<String> */
        private OrderedHashtable localeResources; /* String -> ArrayList<LocaleDataSource> */
        private OrderedHashtable currentLocaleData; /* Hashtable{ String -> String } */
        private String defaultLocale;
        private String currentLocale;
        private Boolean fallbackDefaultLocale;
        private Boolean fallbackDefaultForm;
        private ArrayList observers;

        /**
         * Default constructor. Disables all fallback modes.
         */
        public Localizer()
            : this(false, false)
        {

        }

        /**
         * Full constructor.
         * 
         * @param fallbackDefaultLocale If true, search the default locale when no translation for a particular text handle
         * is found in the current locale.
         * @param fallbackDefaultForm If true, search the default text form when no translation is available for the
         * specified text form ('long', 'short', etc.). Note: form is specified by appending ';[form]' onto the text ID. 
         */
        public Localizer(Boolean fallbackDefaultLocale, Boolean fallbackDefaultForm)
        {
            localeResources = new OrderedHashtable();
            currentLocaleData = new OrderedHashtable();
            locales = new ArrayList();
            defaultLocale = null;
            currentLocale = null;
            observers = new ArrayList();
            this.fallbackDefaultLocale = fallbackDefaultLocale;
            this.fallbackDefaultForm = fallbackDefaultForm;
        }

        public Boolean equals(Object o)
        {
            if (o is Localizer)
            {
                Localizer l = (Localizer)o;

                //TODO: Compare all resources
                return (ExtUtil.Equals(locales, locales) &&
                        ExtUtil.Equals(localeResources, l.localeResources) &&
                        ExtUtil.Equals(defaultLocale, l.defaultLocale) &&
                        ExtUtil.Equals(currentLocale, l.currentLocale) &&
                        fallbackDefaultLocale == l.fallbackDefaultLocale &&
                        fallbackDefaultForm == l.fallbackDefaultForm);
            }
            else
            {
                return false;
            }
        }

        /**
         * Get default locale fallback mode
         * 
         * @return default locale fallback mode
         */
        public Boolean getFallbackLocale()
        {
            return fallbackDefaultLocale;
        }

        /**
         * Get default form fallback mode
         * 
         * @return default form fallback mode
         */
        public Boolean getFallbackForm()
        {
            return fallbackDefaultForm;
        }

        /* === INFORMATION ABOUT AVAILABLE LOCALES === */

        /**
         * Create a new locale (with no mappings). Do nothing if the locale is already defined.
         * 
         * @param locale Locale to add. Must not be null.
         * @return True if the locale was not already defined.
         * @throws NullPointerException if locale is null
         */
        public Boolean addAvailableLocale(String locale)
        {
            if (hasLocale(locale))
            {
                return false;
            }
            else
            {
                locales.Add(locale);
                localeResources.put(locale, new ArrayList());
                return true;
            }
        }

        /**
         * Get a list of defined locales.
         * 
         * @return Array of defined locales, in order they were created.
         */
        public String[] getAvailableLocales()
        {
            String[] data = new String[locales.Count];
            locales.CopyTo(data);
            return data;
        }

        /**
         * Get whether a locale is defined. The locale need not have any mappings.
         * 
         * @param locale Locale
         * @return Whether the locale is defined. False if null
         */
        public Boolean hasLocale(String locale)
        {
            return (locale == null ? false : locales.Contains(locale));
        }

        /**
         * Return the next locale in order, for cycling through locales.
         * 
         * @return Next locale following the current locale (if the current locale is the last, cycle back to the beginning).
         * If the current locale is not set, return the default locale. If the default locale is not set, return null.
         */
        public String getNextLocale()
        {
            return currentLocale == null ? defaultLocale
                                         : (String)locales[(locales.IndexOf(currentLocale) + 1) % locales.Count];
        }

        /* === MANAGING CURRENT AND DEFAULT LOCALES === */

        /**
         * Get the current locale.
         * 
         * @return Current locale.
         */
        public String Locale
        {
            get { return currentLocale; }
            set
            {
                if (!hasLocale(value))
                    throw new UnregisteredLocaleException("Attempted to set to a locale that is not defined. Attempted Locale: " + value);

                if (!value.Equals(this.currentLocale))
                {
                    this.currentLocale = value;
                }
                loadCurrentLocaleResources();
                alertLocalizables();
            }
        }

        /**
         * Get the default locale.
         * 
         * @return Default locale.
         */
        public String getDefaultLocale()
        {
            return defaultLocale;
        }

        /**
         * Set the default locale. The locale must be defined.
         * 
         * @param defaultLocale Default locale. Must be defined. May be null, in which case there will be no default locale.
         * @throws UnregisteredLocaleException If locale is not defined.
         */
        public void setDefaultLocale(String defaultLocale)
        {
            if (defaultLocale != null && !hasLocale(defaultLocale))
                throw new UnregisteredLocaleException("Attempted to set default to a locale that is not defined");

            this.defaultLocale = defaultLocale;
        }

        /**
         * Set the current locale to the default locale. The default locale must be set.
         * 
         * @throws IllegalStateException If default locale is not set.
         */
        public void ToDefault()
        {
            
                if (defaultLocale == null)
                    throw new SystemException("Attempted to set to default locale when default locale not set");

                Locale = defaultLocale;
            
        }

        /**
         * Constructs a body of local resources to be the set of Current Locale Data.
         * 
         * After loading, the current locale data will contain definitions for each
         * entry defined by the current locale resources, as well as definitions for any
         * entry present in the fallback resources but not in those of the current locale.
         *  
         * The procedure to accomplish this set is as follows, with overwritting occuring 
         * when a collision occurs:
         * 
         * 1. Load all of the in memory definitions for the default locale if fallback is enabled
         * 2. For each resource file for the default locale, load each definition if fallback is enabled
         * 3. Load all of the in memory definitions for the current locale
         * 4. For each resource file for the current locale, load each definition
         */
        private void loadCurrentLocaleResources()
        {
            this.currentLocaleData = getLocaleData(currentLocale);
        }

        /**
         * Moves all relevant entries in the source dictionary into the destination dictionary
         * @param destination A dictionary of key/value locale pairs that will be modified 
         * @param source A dictionary of key/value locale pairs that will be copied into 
         * destination 
         */
        private void loadTable(OrderedHashtable destination, OrderedHashtable source)
        {
            for (IEnumerator en = source.GetEnumerator(); en.MoveNext(); )
            {
                String key = (String)en.Current;
                destination.put(key, (String)source[key]);
            }
        }

        /* === MANAGING LOCALE DATA (TEXT MAPPINGS) === */

        /**
         * Registers a resource file as a source of locale data for the specified
         * locale.  
         * 
         * @param locale The locale of the definitions provided. 
         * @param resource A LocaleDataSource containing string data for the locale provided
         * @throws NullPointerException if resource or locale are null
         */
        public void registerLocaleResource(String locale, LocaleDataSource resource)
        {
            if (locale == null)
            {
                throw new NullReferenceException("Attempt to register a data source to a null locale in the localizer");
            }
            if (resource == null)
            {
                throw new NullReferenceException("Attempt to register a null data source in the localizer");
            }
            ArrayList resources = new ArrayList();
            if (localeResources.ContainsKey(locale))
            {
                resources = (ArrayList)localeResources[locale];
            }
            resources.Add(resource);
            localeResources.put(locale, resources);

            if (locale.Equals(currentLocale))
            {
                loadCurrentLocaleResources();
            }
        }

        /**
         * Get the set of mappings for a locale.
         * 
         * @param locale Locale
         * @returns Hashtable representing text mappings for this locale. Returns null if locale not defined or null.
         */
        public OrderedHashtable getLocaleData(String locale)
        {
            if (locale == null || !this.locales.Contains(locale))
            {
                return null;
            }

            //It's very important that any default locale contain the appropriate strings to localize the interface
            //for any possible language. As such, we'll keep around a table with only the default locale keys to
            //ensure that there are no localizations which are only present in another locale, which causes ugly
            //and difficult to trace errors.
            OrderedHashtable defaultLocaleKeys = new OrderedHashtable();

            //This table will be loaded with the default values first (when applicable), and then with any
            //language specific translations overwriting the existing values.
            OrderedHashtable data = new OrderedHashtable();

            // If there's a default locale, we load all of its elements into memory first, then allow
            // the current locale to overwrite any differences between the two.    
            if (fallbackDefaultLocale && defaultLocale != null)
            {
                ArrayList defaultResources = (ArrayList)localeResources[defaultLocale];
                for (int i = 0; i < defaultResources.Count; ++i)
                {
                    loadTable(data, ((LocaleDataSource)defaultResources[i]).getLocalizedText());
                }
                for (IEnumerator en = data.GetEnumerator(); en.MoveNext(); )
                {
                    defaultLocaleKeys.put(en.Current, Boolean.TrueString);
                }
            }

            ArrayList resources = (ArrayList)localeResources[locale];
            for (int i = 0; i < resources.Count; ++i)
            {
                loadTable(data, ((LocaleDataSource)resources[i]).getLocalizedText());
            }

            //If we're using a default locale, now we want to make sure that it has all of the keys
            //that the locale we want to use does. Otherwise, the app will crash when we switch to 
            //a locale that doesn't contain the key.
            if (fallbackDefaultLocale && defaultLocale != null)
            {
                String missingKeys = "";
                int keysmissing = 0;
                for (IEnumerator en = data.GetEnumerator(); en.MoveNext(); )
                {
                    String key = (String)en.Current;
                    if (!defaultLocaleKeys.ContainsKey(key))
                    {
                        missingKeys += key + ",";
                        keysmissing++;
                    }
                }
                if (keysmissing > 0)
                {
                    //Is there a good way to localize these exceptions?
                    throw new NoLocalizedTextException("Error loading locale " + locale +
                            ". There were " + keysmissing + " keys which were contained in this locale, but were not " +
                            "properly registered in the default Locale. Any keys which are added to a locale should always " +
                            "be added to the default locale to ensure appropriate functioning.\n" +
                            "The missing translations were for the keys: " + missingKeys, missingKeys, defaultLocale);
                }
            }

            return data;
        }

        /**
         * Get the mappings for a locale, but throw an exception if locale is not defined.
         * 
         * @param locale Locale
         * @return Text mappings for locale.
         * @throws UnregisteredLocaleException If locale is not defined or null.
         */
        public OrderedHashtable getLocaleMap(String locale)
        {
            OrderedHashtable mapping = getLocaleData(locale);
            if (mapping == null)
                throw new UnregisteredLocaleException("Attempted to access an undefined locale.");
            return mapping;
        }

        /**
         * Determine whether a locale has a mapping for a given text handle. Only tests the specified locale and form; does
         * not fallback to any default locale or text form.
         * 
         * @param locale Locale. Must be defined and not null.
         * @param textID Text handle.
         * @return True if a mapping exists for the text handle in the given locale.
         * @throws UnregisteredLocaleException If locale is not defined.
         */
        public Boolean hasMapping(String locale, String textID)
        {
            if (locale == null || !locales.Contains(locale))
            {
                throw new UnregisteredLocaleException("Attempted to access an undefined locale (" + locale + ") while checking for a mapping for  " + textID);
            }
            ArrayList resources = (ArrayList)localeResources[locale];
            for (IEnumerator en = resources.GetEnumerator(); en.MoveNext(); )
            {
                LocaleDataSource source = (LocaleDataSource)en.Current;
                if (source.getLocalizedText().ContainsKey(textID))
                {
                    return true;
                }
            }
            return false;
        }

        /**
         * Undefine a locale and remove all its data. Cannot be called on the current locale. If called on the default
         * locale, no default locale will be set afterward.
         * 
         * @param locale Locale to remove. Must not be null. Need not be defined. Must not be the current locale.
         * @return Whether the locale existed in the first place.
         * @throws IllegalArgumentException If locale is the current locale.
         * @throws NullPointerException if locale is null
         */
        public Boolean destroyLocale(String locale)
        {
            if (locale.Equals(currentLocale))
                throw new ArgumentException("Attempted to destroy the current locale");

            Boolean removed = hasLocale(locale);
            locales.Remove(locale);
            localeResources.remove(locale);

            if (locale.Equals(defaultLocale))
                defaultLocale = null;

            return removed;
        }

        /* === RETRIEVING LOCALIZED TEXT === */

        /**
         * Retrieve the localized text for a text handle in the current locale. See getText(String, String) for details.
         *
         * @param textID Text handle (text ID appended with optional text form). Must not be null.
         * @return Localized text. If no text is found after using all fallbacks, return null.
         * @throws UnregisteredLocaleException If current locale is not set.
         * @throws NullPointerException if textID is null
         */
        public String getText(String textID)
        {
            return getText(textID, currentLocale);
        }

        /**
         * Retrieve the localized text for a text handle in the current locale. See getText(String, String) for details.
         *
         * @param textID Text handle (text ID appended with optional text form). Must not be null.
         * @param args arguments for string variables.
         * @return Localized text
         * @throws UnregisteredLocaleException If current locale is not set.
         * @throws NullPointerException if textID is null
         * @throws NoLocalizedTextException If there is no text for the specified id
         */
        public String getText(String textID, String[] args)
        {
            String text = getText(textID, currentLocale);
            if (text != null)
            {
                text = processArguments(text, args);
            }
            else
            {
                throw new NoLocalizedTextException("The Localizer could not find a definition for ID: " + textID + " in the '" + currentLocale + "' locale.", textID, currentLocale);
            }
            return text;
        }
        /**
         * Retrieve the localized text for a text handle in the current locale. See getText(String, String) for details.
         *
         * @param textID Text handle (text ID appended with optional text form). Must not be null.
         * @param args arguments for string variables.
         * @return Localized text. If no text is found after using all fallbacks, return null.
         * @throws UnregisteredLocaleException If current locale is not set.
         * @throws NullPointerException if textID is null
         * @throws NoLocalizedTextException If there is no text for the specified id
         */
        public String getText(String textID, Hashtable args)
        {
            String text = getText(textID, currentLocale);
            if (text != null)
            {
                text = processArguments(text, args);
            }
            else
            {
                throw new NoLocalizedTextException("The Localizer could not find a definition for ID: " + textID + " in the '" + currentLocale + "' locale.", textID, currentLocale);
            }
            return text;
        }

        /**
         * Retrieve localized text for a text handle in the current locale. Like getText(String), however throws exception
         * if no localized text is found.
         * 
         * @param textID Text handle (text ID appended with optional text form). Must not be null.
         * @return Localized text
         * @throws NoLocalizedTextException If there is no text for the specified id
         * @throws UnregisteredLocaleException If current locale is not set
         * @throws NullPointerException if textID is null
         */
        public String getLocalizedText(String textID)
        {
            String text = getText(textID);
            if (text == null)
                throw new NoLocalizedTextException("Can't find localized text for current locale! text id: [" + textID + "] locale: [" + currentLocale + "]", textID, currentLocale);
            return text;
        }

        /**
         * Retrieve the localized text for a text handle in the given locale. If no mapping is found initially, then,
         * depending on enabled fallback modes, other places will be searched until a mapping is found.
         * <p>
         * The search order is thus:
         * 1) Specified locale, specified text form
         * 2) Specified locale, default text form
         * 3) Default locale, specified text form
         * 4) Default locale, default text form
         * <p>
         * (1) and (3) are only searched if a text form ('long', 'short', etc.) is specified.
         * If a text form is specified, (2) and (4) are only searched if default-form-fallback mode is enabled.
         * (3) and (4) are only searched if default-locale-fallback mode is enabled. It is not an error in this situation
         *   if no default locale is set; (3) and (4) will simply not be searched.
         *
         * @param textID Text handle (text ID appended with optional text form). Must not be null.
         * @param locale Locale. Must be defined and not null.
         * @return Localized text. If no text is found after using all fallbacks, return null.
         * @throws UnregisteredLocaleException If the locale is not defined or null.
         * @throws NullPointerException if textID is null
         */
        public String getText(String textID, String locale)
        {
            String text = getRawText(locale, textID);
            if (text == null && fallbackDefaultForm && textID.IndexOf(";") != -1)
                text = getRawText(locale, textID.Substring(0, textID.IndexOf(";")));
            if (text == null && fallbackDefaultLocale && !locale.Equals(defaultLocale) && defaultLocale != null)
                text = getText(textID, defaultLocale);
            return text;
        }

        /**
         * Get text for locale and exact text ID only, not using any fallbacks.
         * 
         * NOTE: This call will only return the full compliment of available strings if and 
         * only if the requested locale is current. Otherwise it will only retrieve strings
         * declared at runtime.
         * 
         * @param locale Locale. Must be defined and not null.
         * @param textID Text handle (text ID appended with optional text form). Must not be null.
         * @return Localized text. Return null if none found.
         * @throws UnregisteredLocaleException If the locale is not defined or null.
         * @throws NullPointerException if textID is null
         */
        public String getRawText(String locale, String textID)
        {
            if (locale == null)
            {
                throw new UnregisteredLocaleException("Null locale when attempting to fetch text id: " + textID);
            }
            if (locale.Equals(currentLocale))
            {
                return (String)currentLocaleData[textID];
            }
            else
            {
                return (String)getLocaleMap(locale)[textID];
            }
        }

        /* === MANAGING LOCALIZABLE OBSERVERS === */

        /**
         * Register a Localizable to receive updates when the locale is changed. If the Localizable is already
         * registered, nothing happens. If a locale is currently set, the new Localizable will receive an
         * immediate 'locale changed' event.
         * 
         * @param l Localizable to register.
         */
        public void registerLocalizable(Localizable l)
        {
            if (!observers.Contains(l))
            {
                observers.Add(l);
                if (currentLocale != null)
                {
                    l.localeChanged(currentLocale, this);
                }
            }
        }

        /**
         * Unregister an Localizable from receiving locale change updates. No effect if the Localizable was never
         * registered in the first place.
         * 
         * @param l Localizable to unregister.
         */
        public void unregisterLocalizable(Localizable l)
        {
            observers.Remove(l);
        }

        /**
         * Unregister all ILocalizables.
         */
        public void unregisterAll()
        {
            observers.Clear();
        }

        /**
         * Send a locale change update to all registered ILocalizables.
         */
        private void alertLocalizables()
        {
            for (IEnumerator e = observers.GetEnumerator(); e.MoveNext(); )
                ((Localizable)e.Current).localeChanged(currentLocale, this);
        }

        /* === Managing Arguments === */

        private static String arg(String in_)
        {
            return "${" + in_ + "}";
        }

        public static ArrayList getArgs(String text)
        {
            ArrayList args = new ArrayList();
            int i = text.IndexOf("${");
            while (i != -1)
            {
                int j = text.IndexOf("}", i);
                if (j == -1)
                {
                    System.Console.Error.WriteLine("Warning: unterminated ${...} arg");
                    break;
                }

                String arg = text.Substring(i + 2, j);
                if (!args.Contains(arg))
                {
                    args.Add(arg);
                }

                i = text.IndexOf("${", j + 1);
            }
            return args;
        }

        public static String processArguments(String text, Hashtable args)
        {
            int i = text.IndexOf("${");
            while (i != -1)
            {
                int j = text.IndexOf("}", i);
                if (j == -1)
                {
                    System.Console.Error.WriteLine("Warning: unterminated ${...} arg");
                    break;
                }

                String argName = text.Substring(i + 2, j);
                String argVal = (String)args[argName];
                if (argVal != null)
                {
                    text = text.Substring(0, i) + argVal + text.Substring(j + 1);
                    j = i + argVal.Length - 1;
                }

                i = text.IndexOf("${", j + 1);
            }
            return text;
        }

        public static String processArguments(String text, String[] args)
        {
            String working = text;
            int currentArg = 0;
            while (working.IndexOf("${") != -1 && args.Length > currentArg)
            {
                String value = extractValue(text, args);
                if (value == null)
                {
                    value = args[currentArg];
                    currentArg++;
                }
                working = replaceFirstValue(working, value);
            }
            return working;
        }


        public static String clearArguments(String text)
        {
            ArrayList v = getArgs(text);
            String[] empty = new String[v.Count];
            for (int i = 0; i < empty.Length; ++i)
            {
                empty[i] = "";
            }
            return processArguments(text, empty);
        }

        private static String extractValue(String text, String[] args)
        {
            //int start = text.indexOf("${");
            //int end = text.indexOf("}");

            //String index = text.substring(start + 2, end);
            //Search for that string in the current locale, updating any arguments.
            return null;
        }

        private static String replaceFirstValue(String text, String value)
        {
            int start = text.IndexOf("${");
            int end = text.IndexOf("}");

            return text.Substring(0, start) + value + text.Substring(end + 1, text.Length);
        }

        /* === (DE)SERIALIZATION === */

        /**
         * Read the object from stream.
         */
        public void readExternal(System.IO.BinaryReader dis, PrototypeFactory pf)
        {
            fallbackDefaultLocale = ExtUtil.readBool(dis);
            fallbackDefaultForm = ExtUtil.readBool(dis);
            localeResources = (OrderedHashtable)ExtUtil.read(dis, new ExtWrapMap(typeof(String), new ExtWrapListPoly(), 1), pf); ;
            locales = (ArrayList)ExtUtil.read(dis, new ExtWrapList(typeof(String)));
            setDefaultLocale((String)ExtUtil.read(dis, new ExtWrapNullable(typeof(String)), pf));
            String currentLocale = (String)ExtUtil.read(dis, new ExtWrapNullable(typeof(String)), pf);
            if (currentLocale != null)
            {
                Locale = currentLocale;
            }
        }

        /**
         * Write the object to stream.
         */
        public void writeExternal(BinaryWriter dos)
        {
            ExtUtil.writeBool(dos, fallbackDefaultLocale);
            ExtUtil.writeBool(dos, fallbackDefaultForm);
            ExtUtil.write(dos, new ExtWrapMap(localeResources, new ExtWrapListPoly()));
            ExtUtil.write(dos, new ExtWrapList(locales));
            ExtUtil.write(dos, new ExtWrapNullable(defaultLocale));
            ExtUtil.write(dos, new ExtWrapNullable(currentLocale));
        }
    }
}