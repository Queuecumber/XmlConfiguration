using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Xml.Linq;

namespace XmlConfiguration
{
	/// <summary>
	/// Represents an applications configuration as loaded from xml. 
	/// The xml must have the following structure to be loaded correctly
	///		
	///		<!-- Multi line comment -->
	///		<configuration>
	///			<ConfigElement Attr1="1" Attr2="2"> 0 </ConfigElement>
	///		</configuration>
	///		
	///	These values can then be accessed as follows:
	///	
	///		Instance.ConfigElement			//value: 0
	///		Instance.ConfigElement.Attr1	//value: 1
	///		...
	///		
	/// (note that Instance.ConfigElement can also be accessed as Instance.ConfigElement.Value)
	/// </summary>
    public class XmlConfiguration : DynamicObject
    {
		public string Version {	get; private set; }

        public IEnumerable<string> ConfigurationNames
        {
            get
            {
                return _Configuration.Keys;
            }
        }

		//map of configuration option names to their values
		private Dictionary<string, dynamic> _Configuration = new Dictionary<string,dynamic>();

        public XmlConfiguration()
        {
            
        }

        public void AddToPool(string xml)
        {
            //load the xml file
            XDocument doc = XDocument.Parse(xml);

            XAttribute ver = doc.Root.Attribute("Version") ?? doc.Root.Attribute("version");

            if (!string.IsNullOrEmpty(Version))
            {
                if (ver != null && ver.Value != Version)
                {
                    throw new Exception("Versions do not match, expected " + Version + " got " + ver.Value);
                }
            }
            else
            {
                if (ver != null)
                {
                    Version = ver.Value;
                }
                else
                {
                    Version = "";
                }
            }

            //create configuration values from xml
            //
            IEnumerable<dynamic> opts = from opt in doc.Root.Elements()
                                        select new ConfigurationValue(opt);

            //make sure none of the new value names were already loaded from a DIFFERENT file
            //this is done first so that none of the configuration values from the bad file
            //are loaded, which could leave the application in an inconsistent state. Defaults
            //(using the null coalescing operator) would be used instead for any unloaded values
            //
            foreach (dynamic d in opts)
            {
                if (_Configuration.Keys.Contains(d.Name as string))
                {
                    throw new Exception("Already have name " + d.Name);
                }
            }

            //add to dictionary
            //
            foreach (dynamic d in opts)
            {
                //make sure none of the new value names are repeated (in the SAME file)
                //
                if (_Configuration.Keys.Contains(d.Name as string))
                {
                    throw new Exception("Already have name " + d.Name);
                }

                _Configuration.Add(d.Name, d);
            }
        }

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			//get the name of the desired property and its value
			//
			string name = binder.Name;

			if(_Configuration.ContainsKey(name))
			{
				dynamic value = _Configuration[name];

				result = value;
				return true;
			}
			else
			{
				//if not maybe it pertains to something farther up the inheritance line,
                //always return true from this, if a value isn't found assign null. This
                //allows use of the null coalescing operator to set defaults
                //
                bool res = base.TryGetMember(binder, out result);
                if (!res)
                    result = null;

                return true;
			}
		}

		/// <summary>
		/// Gets a configuration value (the old fashioned way). Using 
		/// the configuration instance in this way is a legacy convenience. 
		///
		/// For example, calling
		/// <code>Configuration.Instance.Get("Property1");</code>
		/// is the same as calling
		/// <code>Configuration.Instance.Property1;</code>
		/// 
		/// However the second way is much cleaner and is the preferred 
		/// way of accessing configuration values.
		/// </summary>
		/// <param name="name"></param>
		/// <returns>Value of the configuration property</returns>
		public dynamic Get(string name)
		{
			return _Configuration[name];
		}
    }
}
