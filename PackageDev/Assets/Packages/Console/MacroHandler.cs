using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Xml.Serialization;

namespace FTF.Console
{
	public class MacroHandler : MonoBehaviour, IConsoleSubscriber
	{
		const string MACRO_PREFIX = "macro.";
		const string MACRO_ARG_PREFIX = "%";

		public MacroListing macroListing;

		void Start()
		{
			string path = Path.Combine(Application.streamingAssetsPath, "macro_listing.xml");
			Console.Log(path);

			if (!File.Exists(path))
			{
				Console.Warn("No macro listing found");
				Destroy(this);
				return;
			}

			Console.SubscribeToConsoleActivationEvent(OnConsoleActivationEvent);

			string json = File.ReadAllText(path);

			XmlSerializer serializer = new XmlSerializer(typeof(MacroListing));

			using(FileStream stream = new FileStream(path, FileMode.Open))
			{
				try
				{
					macroListing = serializer.Deserialize(stream) as MacroListing;
					stream.Close();

					CleanDescriptions();

					if (ValidateMacros())
					{
						Console.Log("<b>Loaded macro_listing.xml!</b>", new Color(.1f, .4f, 1.0f));
						Console.Subscribe(this);
					}
					else
						Console.Error("<b>Failed to validate macros!</b>\nMacros are unavailable.");
				}catch (System.InvalidOperationException e){
					Console.Error(e);
					Destroy(this);
				}catch(System.Xml.XmlException e)
				{
					Console.Error(e);
					Destroy(this);
				}
			}
		}

		// Update is called once per frame
		void Update()
		{
			for(int i=0; i< macroListing.macros.Count; i++)
			{
				if (macroListing.macros[i].keyName == null || macroListing.macros[i].keyName.Length == 0)
					continue;

				if (Input.GetKeyDown(macroListing.macros[i].keyName))
				{
					//Build replacement tuples
					Dictionary<string, string> replacementTuples = new Dictionary<string, string>();
					for (int argIndex = 0; argIndex < macroListing.macros[i].args.Count; argIndex++)
					{
						MacroArg arg = macroListing.macros[i].args[argIndex];
						replacementTuples.Add(MACRO_ARG_PREFIX + arg.id, arg.defaultValue);
					}

					//Submit commands
					foreach (string macroCommand in macroListing.macros[i].commands)
					{
						string finalCommand = macroCommand;
						//Do string replacement for arguments
						foreach (string key in replacementTuples.Keys)
						{
							finalCommand = finalCommand.Replace(key, replacementTuples[key]);
						}
						Console.Post(finalCommand);
					}
					break;
				}
			}
		}

		private void OnConsoleActivationEvent(bool isActive)
		{
			enabled = !isActive;
		}

		private void OnDestroy()
		{
			Console.UnsubscribeFromConsoleActivationEvent(OnConsoleActivationEvent);
			Console.Unsubscribe(this);
		}

		/// <summary>
		/// Cleans all the descriptions to remove the ugly whitespace from the XML
		/// </summary>
		private void CleanDescriptions()
		{
			for (int i = 0; i < macroListing.macros.Count; i++)
			{
				macroListing.macros[i].desc = CleanDescription(macroListing.macros[i].desc);
			}
		}

		/// <summary>
		/// Helper method to clean desctiptions.
		/// Splits the description string on new lines, trims, and rebuilds.
		/// </summary>
		/// <param name="desc">The desciption to clean</param>
		/// <returns>The cleaned description</returns>
		private string CleanDescription(string desc)
		{
			if (desc == null || desc.Length == 0)
				return "";
			System.Text.StringBuilder builder = new System.Text.StringBuilder();
			foreach(string line in desc.Split('\n'))
			{
				builder.AppendLine(line.Trim());
			}
			return builder.ToString();
		}

		private bool ValidateMacros()
		{
			bool valid = true;
			int count = 0;
			HashSet<string> usedNames = new HashSet<string>();
			HashSet<string> usedKeys = new HashSet<string>();
			for (int macroIndex = 0; macroIndex < macroListing.macros.Count; macroIndex++)
			{
				Macro macro = macroListing.macros[macroIndex];

				//check for valid key name
				if(macro.keyName != null)
				{
					if (macro.keyName.Length > 0)
					{
						try
						{
							Input.GetKey(macro.keyName);
							//keyName maps to a valid key, but let's make sure it's a unique kayName
							if (usedKeys.Contains(macro.keyName))
							{
								Console.Warn("=> Macro " + macroIndex + " attribute `keyName` " + macro.keyName + " is already in use.\nRemoving keyName.");
								macro.keyName = null;
							}
							else
								usedKeys.Add(macro.keyName);
						}
						catch
						{
							Console.Warn("=> Macro " + macroIndex + " attribute `keyName` has invalid key: " + macro.keyName + ".\nRemoving keyName.");
							macro.keyName = null;
						}
					}
					else
						macro.keyName = null;
				}

				//Macros need to have a command name
				if (macro.commandName == null || macro.commandName.Length == 0)
				{
					Console.Error("=> Macro " + macroIndex + " missing required attribute `commandName`");
					valid = false;
				}
				else if (usedNames.Contains(macro.commandName))
				{
					Console.Error("=> Macro " + macroIndex + " `commandName` is already in use.\nChoose a different Command name.");
					valid = false;
				}
				else
					usedNames.Add(macro.commandName);



				bool prevWasOptional=false;
				for(int i=0; i<macro.args.Count; i++)
				{
					if (macro.args[i].defaultValue != null && macro.args[i].defaultValue.Length > 0)
						prevWasOptional = true;
					else if (prevWasOptional)
					{
						valid = false;
						Console.Error("=> Macro " + macroIndex + ", Arg " + i + " is required but follows an optional argument.");
						break;
					}
				}

				count++;
			}
			return valid;
		}


		#region Command Bus
		public void HandleCommand(Command command)
		{
			//Check for the macro prefix
			if (!command.msg.StartsWith(MACRO_PREFIX))
			{
				//If it isn't there, lets bail because we don't want to check every macro :|
				return;
			}

			//Get the part of the message that is relevant for matching
			string msg = command.msg.Substring(MACRO_PREFIX.Length);

			for (int i = 0; i < macroListing.macros.Count; i++)
			{
				if (msg.Equals(macroListing.macros[i].commandName))
				{
					//Build replacement tuples
					Dictionary<string, string> replacementTuples = new Dictionary<string, string>();
					for(int argIndex = 0; argIndex < macroListing.macros[i].args.Count; argIndex++)
					{
						MacroArg arg = macroListing.macros[i].args[argIndex];
						string argVal;
						if (command.args.Length > argIndex)
							argVal = command.args[argIndex];
						else if(arg.defaultValue != null && arg.defaultValue.Length > 0)
							argVal = arg.defaultValue;
						else
						{
							Console.Error("<b>Macro Error:</b>\nArgument " + argIndex + " is required.");
							return;
						}
						replacementTuples.Add(MACRO_ARG_PREFIX + arg.id, argVal);
					}

					//Submit commands
					foreach (string macroCommand in macroListing.macros[i].commands)
					{
						string finalCommand = macroCommand;
						//Do string replacement for arguments
						foreach(string key in replacementTuples.Keys)
						{
							finalCommand = finalCommand.Replace(key, replacementTuples[key]);
						}
						Console.Post(finalCommand);
					}
					break;
				}
			}

		}

		public List<CommandFormatDesc> GetCommands()
		{
			List<CommandFormatDesc> commandList = new List<CommandFormatDesc>(macroListing.macros.Count);

			//build the list of commands
			foreach (Macro macro in macroListing.macros)
			{
				//build arg descriptors
				CommandArgDesc[] argDescs = new CommandArgDesc[macro.args.Count];
				for(int i=0; i<argDescs.Length; i++)
				{
					bool opt = (macro.args[i].defaultValue != null && macro.args[i].defaultValue.Length > 0);
					argDescs[i] = new CommandArgDesc(macro.args[i].name, macro.args[i].type, macro.args[i].desc, opt);
				}
				commandList.Add(new CommandFormatDesc(MACRO_PREFIX + macro.commandName, argDescs, macro.desc));
			}

			return commandList;
		}
		#endregion
	}

	#region Macro data classes
	[System.Serializable]
	public class MacroArg
	{
		[XmlAttribute("id")]
		public string id;

		[XmlAttribute("type")]
		public string type;

		[XmlAttribute("name")]
		public string name;

		[XmlAttribute("desc")]
		public string desc;

		[XmlText]
		public string defaultValue;
	}

	[System.Serializable]
	public class Macro
	{
		[XmlAttribute("keyName")]
		public string keyName;

		[XmlAttribute("commandName")]
		public string commandName;

		[XmlElement("command")]
		public string[] commands;

		[XmlElement("desc")]
		public string desc;

		[XmlArray("args")]
		[XmlArrayItem("arg")]
		public List<MacroArg> args = new List<MacroArg>();
	}

	[System.Serializable]
	[XmlRoot("MacroListing")]
	public class MacroListing
	{
		[XmlArray("macros")]
		[XmlArrayItem("macro")]
		public List<Macro> macros = new List<Macro>();
	}
	#endregion
}

