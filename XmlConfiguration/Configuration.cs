using System;
using System.Collections.Generic;
using System.IO;

namespace XmlConfiguration
{
	/// <summary>
	/// Represents an easy way to load an applications configuration from
	/// xml 
    /// </summary>
    /// <remarks>
    /// and then access the configuration values in an extremely 
	/// fault tolerant and flexible way by leveraging dynamic .Net 4. 
	/// These files must have a specific structure in order to be read correctly. 
	/// This structure is reflected in the way that program values are accessed.
	/// In this way, the programmer feels like he is working with the 
	/// configuration file directly instead of through an intermediate module.
	/// 
	/// Example of file structure:
	/// 
	///		[XML]
	///			<!-- Mutli
	///				 line 
	///				 comment -->
	///			<Configuration Version="1.0">
	///				<ConfigValue1> value </ConfigValue1>
	///				<ConfigValue2 Attr1="1" Attr2="2" />
	///			</Configuration>
	///
	///		  
	/// Once the file is created with the proper structure, it should be loaded:
	///		Configuration.Load("file.xml","1.0");
	///		
	/// Values can then be accessed like so:
	///		Configuration.Instance.ConfigValue1			//value: value
	///	or
	///		Configuration.Instance.ConfigValue2.Attr1	//value: 1
	///		
	/// reflecting the structure of the file in a very natural manner.
	/// </remarks>
	public class Configuration
	{
		private static List<string> _FileNames = new List<string>();

		/// <summary>
		/// Version string, unused for ini files
		/// </summary>
		public static string Version { get; private set;}

		/// <summary>
		/// Contains the configuration instance. This must first
		/// be initialized by calling a load or parse function
		/// </summary>
        public static dynamic Instance { get; private set; }

		/// <summary>
		/// Parses XML from an input stream to use for the configuration.
		/// This must have the correct form as specified above
		/// </summary>
		/// <param name="read">input stream</param>
		/// <param name="version">expected version</param>
		/// <returns>Loaded configuration instance</returns>
		public static dynamic ParseXML(Stream read, string version = null)
		{
			//Set file name and version (no file name in this case since loading is done from a stream)
			//
			Version = version;

			//read all data from the stream
			//
			StreamReader sr = new StreamReader(read);
			string xml = sr.ReadToEnd();

			//add to the configuration pool
            //
            Instance.AddToPool(xml);        

			//check version number
			//
			if (Version != null && Instance.Version != Version)
			{
				throw new Exception("Versions do not match, expected " + Version + " got " + Instance.Version);
			}

			return Instance;
		}

		/// <summary>
		/// Loads a file for configuration.
		/// </summary>
		/// <param name="filename">file name to load from</param>
		/// <param name="version">expected version (optional)</param>
		/// <returns>Loaded configuration instance</returns>
		public static dynamic Load(string filename, string version = null)
		{
            if(Instance == null)
                Instance = new XmlConfiguration();

			//XML file, create an input stream and parse
			//
            StreamReader sr = null;
            try
            {
                sr = new StreamReader(filename);
                ParseXML(sr.BaseStream, version);
            }
            finally
            {
                if(sr != null)
                    sr.Close();
            }

			//set the filename
			_FileNames.Add(filename);

			return Instance;
		}

		/// <summary>
		/// Reloads the configuration from the same file to 
		/// reflect any changes that might have occurred. If the
		/// initial configuration was loaded from a stream this 
		/// has no effect
		/// </summary>
		/// <returns>Reloaded configuration instance</returns>
		public static dynamic ReLoad()
		{
			if (_FileNames.Count > 0)
			{
                Instance = new XmlConfiguration();

                foreach (string fileName in _FileNames)
                {
                    Load(fileName, Version);
                }
			}

			return Instance;
		}
	}
}
