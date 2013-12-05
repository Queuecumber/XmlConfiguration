using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Dynamic;
using System.Linq;
using System.Xml.Linq;

namespace XmlConfiguration
{
    /// <summary>
	/// Represents a specific configuration value and its attributes. Objects of 
	/// this class represent values and therefore have an implicit string conversion,
	/// however, using an object of this class without conversion provides access to 
	/// any attributes of the configuration value
	/// </summary>
	public class ConfigurationValue : DynamicObject
	{
		//name and value
		//
        public string Name { get; private set; }
        public string Value { get; private set; }

        /// <summary>
        /// Get an enumerable collection of the names of any child 
        /// elements
        /// </summary>
        public IEnumerable<string> ChildNames
        {
            get
            {
                return _ChildDict.Keys;
            }
        }

		//attributes
		private Dictionary<string, dynamic> _ChildDict = new Dictionary<string,dynamic>();

        public ConfigurationValue(string xml)
            : this(XElement.Parse(xml))
        {

        }

		/// <summary>
		/// Creates a new configuration value with the given name, value, and attributes
		/// </summary>
		/// <param name="name">Name of this configuration value</param>
		/// <param name="value">Configuration value itself</param>
		/// <param name="attrs">Any attributes pertaining to the the configuration value</param>
		public ConfigurationValue(XElement el)
		{
			//set name and value
			//
            Name = el.Name.ToString();

            //add attributes
            foreach (var attr in el.Attributes())
            {
                _ChildDict.Add(attr.Name.ToString(), new ConfigurationValue(attr));
            }

            if (el.Elements().Count() == 0)
            {
                Value = el.Value;
            }
            else
            {
                foreach (var child in el.Elements())
                {
                    _ChildDict.Add(child.Name.ToString(), new ConfigurationValue(child));
                }
            }
		}

        public ConfigurationValue(XAttribute attr)
        {
            Name = attr.Name.ToString();
            Value = attr.Value;
        }

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			//get name of desired property
			string name = binder.Name;
	
			//look up the property
			//
			if (_ChildDict.ContainsKey(name))
			{
				dynamic value = _ChildDict[name];

				result = value;
				return true;
			}
			else
			{
                //if not maybe it pertains to something farther up the inheritance line,
                //always return true from this, if a value isnt found assign null. This
                //allows use of the null coalescing operator to set defaults
                //
                bool res = base.TryGetMember(binder, out result);
                if (!res)
                    result = null;

                return true;
			}
		}
			
		/// <summary>
		/// Gets an attribute value the old fashioned way. Again this is 
		/// provided as a legacy convenience. Also useful for iterating 
        /// over the children of a config value
		/// 
		/// For example, calling
		/// <code>Configuration.Instance.Get("Property1").Get("Attr1");</code>
		/// is the same as calling
		/// <code>Configuration.Instance.Property1.Attr1;</code>
		/// 
		/// However the second way is much cleaner and is the preferred 
		/// way of accessing the attributes of a configuration value
		/// </summary>
		/// <param name="name">name of the attribute</param>
		/// <returns>the string value of that attribute</returns>
		public dynamic Get(string name)
		{
			return _ChildDict[name];
		}
			
		public override string ToString()
		{
			return Value;
		}

        #region Implicit conversions to scalar types

        /// <summary>
		/// Implicit string conversion operator. Allows
		/// a programmer to treat the configuration values
		/// as first class data.
		/// 
		/// For example, 
		/// <code>string p = Configuration.Instance.Property1;</code>
		/// is a valid assignment as is
		/// <code>int a = int.Parse(Configuration.Instance.Property1.Attr1);</code>
		/// 
		/// Using this method, the compiler can convert a configuration value
		/// to a string without an explicit cast or creation of a new string object
		/// explicitly.
		/// 
		/// This method should never be called directly
		/// </summary>
		/// <param name="val">configuration value to be converted</param>
		/// <returns>string value of this configuration value</returns>
		public static implicit operator string(ConfigurationValue val)
		{
			return val.ToString();
		}

        //Implicit arithmatic conversions. These will attempt to 
        //implicitly convert the string value to any arithmatic type. 
        //When used correctly this will work fine, but when used incorrectly
        //they will fail with an exception. If a configuration value is missing, null
        //will be returned from it
        //
        public static implicit operator short(ConfigurationValue val)
        {
            return short.Parse(val);
        }

        public static implicit operator int(ConfigurationValue val)
        {
            return int.Parse(val);
        }

        public static implicit operator long(ConfigurationValue val)
        {
            return long.Parse(val);
        }

        public static implicit operator float(ConfigurationValue val)
        {
            return float.Parse(val);
        }

        public static implicit operator double(ConfigurationValue val)
        {
            return double.Parse(val);
        }

        public static implicit operator bool(ConfigurationValue val)
        {
            //doing this manually for more flexiblility
            //
            string strVal = ((string)val).ToLower();

            switch (strVal)
            {
                case "yes":
                case "true":
                case "on":
                    return true;
                    
                case "no":
                case "false":
                case "off":
                    return false;
                    
                default:
                    throw new FormatException("Cannot convert: " + val + " to type bool");
	        }
        }

        public static implicit operator DateTime(ConfigurationValue val)
        {
            return DateTime.Parse(val);
        }

        public static implicit operator Color(ConfigurationValue val)
        {
            string strVal = val;

            try
            {
                if (strVal.StartsWith("#") && strVal.Length == 7)
                {
                    string r = strVal.Substring(1, 2);
                    string g = strVal.Substring(3, 2);
                    string b = strVal.Substring(5, 2);

                    Color col = Color.FromArgb(int.Parse(r, System.Globalization.NumberStyles.AllowHexSpecifier),
                                               int.Parse(g, System.Globalization.NumberStyles.AllowHexSpecifier),
                                               int.Parse(b, System.Globalization.NumberStyles.AllowHexSpecifier));

                    return col;
                }
            }
            catch (Exception)
            {
                throw new FormatException("Cannot convert: " + val + " to type Color");
            }

            Color colName = Color.FromName(strVal);

            if (colName.Name != "Black" && (colName.A == colName.R) && (colName.R == colName.G) && (colName.G == colName.B) && (colName.B == 0))
            {
                throw new FormatException("Cannot convert: " + val + " to type Color");
            }

            return colName;
        }

        public static implicit operator Uri(ConfigurationValue val)
        {
            try
            {
                return new Uri(val);
            }
            catch (UriFormatException)
            {
                throw new FormatException("Cannot convert: " + val + " to type Uri");
            }
        }

        #endregion

        #region Implicit conversions to enumerables

        public IEnumerable<T> AsEnumerable<T>()
        {
            string value = this;

            string[] parts = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            return parts.Select(s => (T)Convert.ChangeType(s.Trim(), typeof(T)));
        }

        public static implicit operator ReadOnlyCollection<string>(ConfigurationValue val)
        {
            return val.AsEnumerable<string>().ToList().AsReadOnly();
        }

        public static implicit operator ReadOnlyCollection<short>(ConfigurationValue val)
        {
            return val.AsEnumerable<short>().ToList().AsReadOnly();
        }

        public static implicit operator ReadOnlyCollection<int>(ConfigurationValue val)
        {
            return val.AsEnumerable<int>().ToList().AsReadOnly();
        }

        public static implicit operator ReadOnlyCollection<long>(ConfigurationValue val)
        {
            return val.AsEnumerable<long>().ToList().AsReadOnly();
        }

        public static implicit operator ReadOnlyCollection<float>(ConfigurationValue val)
        {
            return val.AsEnumerable<float>().ToList().AsReadOnly();
        }

        public static implicit operator ReadOnlyCollection<double>(ConfigurationValue val)
        {
            return val.AsEnumerable<double>().ToList().AsReadOnly();
        }

        public static implicit operator ReadOnlyCollection<DateTime>(ConfigurationValue val)
        {
            return val.AsEnumerable<DateTime>().ToList().AsReadOnly();
        }

        #endregion

        #region Implicit conversions to dictionaries

        public IDictionary<TKey, TValue> AsDictionary<TKey, TValue>()
        {
            string value = this;

            var parts = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries));

            Func<string, Type, object> convertString = (s, t) => Convert.ChangeType(s.Trim(), t);

            return parts.ToDictionary(s => (TKey)convertString(s[0], typeof(TKey)),         // Key
                                      s => (TValue)convertString(s[1], typeof(TValue)));    // Value
        }

        public static implicit operator ReadOnlyDictionary<string, string>(ConfigurationValue val)
        {
            return new ReadOnlyDictionary<string, string>(val.AsDictionary<string, string>());
        }

        public static implicit operator ReadOnlyDictionary<string, short>(ConfigurationValue val)
        {
            return new ReadOnlyDictionary<string, short>(val.AsDictionary<string, short>());
        }

        public static implicit operator ReadOnlyDictionary<string, int>(ConfigurationValue val)
        {
            return new ReadOnlyDictionary<string, int>(val.AsDictionary<string, int>());
        }

        public static implicit operator ReadOnlyDictionary<string, long>(ConfigurationValue val)
        {
            return new ReadOnlyDictionary<string, long>(val.AsDictionary<string, long>());
        }

        public static implicit operator ReadOnlyDictionary<string, float>(ConfigurationValue val)
        {
            return new ReadOnlyDictionary<string, float>(val.AsDictionary<string, float>());
        }

        public static implicit operator ReadOnlyDictionary<string, double>(ConfigurationValue val)
        {
            return new ReadOnlyDictionary<string, double>(val.AsDictionary<string, double>());
        }

        public static implicit operator ReadOnlyDictionary<string, DateTime>(ConfigurationValue val)
        {
            return new ReadOnlyDictionary<string, DateTime>(val.AsDictionary<string, DateTime>());
        } 

        #endregion
    }
}
