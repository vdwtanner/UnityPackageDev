using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace FTF.Console
{
	public class ConsoleAutocomplete : MonoBehaviour, IConsoleSubscriber
	{
		private static string MATCHED_COLOR = "FFD200";
		private static string AUTOCOMPLETE_MARKER_STRING = "<b><color=#28d6a7>>></color></b>";

		public Text textField;
		public GameObject autocompleteRootUI;

		private SortedList<string, CommandFormatDesc> commandDescriptors;
		private List<CommandFormatDesc> currentCommandDescriptors;

		private int autocompleteIndex;
		private int maxAutoCompleteIndex;
		private Command currentCommand;

		private bool manPageAutoComplete;

		#region Unity Overrides
		private void Awake()
		{
			Debug.Assert(autocompleteRootUI != null);
			Debug.Assert(textField != null);

			textField.text = "";
			autocompleteRootUI.SetActive(false);
			commandDescriptors = new SortedList<string, CommandFormatDesc>();
			currentCommandDescriptors = new List<CommandFormatDesc>();
			autocompleteIndex = 0;
			maxAutoCompleteIndex = 0;
		}

		private void Start()
		{
			Console.Subscribe(this);
		}

		private void Update()
		{
			if(!(Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)))
			{
				if (Input.GetKeyDown(KeyCode.PageUp))
				{
					autocompleteIndex = Mathf.Min(autocompleteIndex + 1, maxAutoCompleteIndex);
					BuildAutoCompleteText();
				}
				else if (Input.GetKeyDown(KeyCode.PageDown))
				{
					autocompleteIndex = Mathf.Max(autocompleteIndex - 1, 0);
					BuildAutoCompleteText();
				}
			}
		}

		private void OnDestroy()
		{
			Console.Unsubscribe(this);
		}
		#endregion
		/// <summary>
		/// Add a set of command descriptors to the autocomplete listing
		/// </summary>
		/// <param name="descriptors"></param>
		public void AddDescriptors(List<CommandFormatDesc> descriptors)
		{
			for (int i = 0; i < descriptors.Count; i++)
			{
				//Only add unique descriptors, including ones with different sets of args
				if (!commandDescriptors.ContainsValue(descriptors[i]))
				{
					commandDescriptors.Add(descriptors[i].msg, descriptors[i]);
				}
			}
		}

		#region autocomplete
		/// <summary>
		/// Update the current autocomplete suggestion list with a new partial command
		/// </summary>
		/// <param name="partialCommand"></param>
		public void UpdateAutoComplete(Command partialCommand)
		{
			string msg = partialCommand.msg;
			currentCommand = partialCommand;
			manPageAutoComplete = false;

			if(msg.Length > 0)
			{
				if (!autocompleteRootUI.activeSelf)
					autocompleteRootUI.SetActive(true);
				
				//Get the relevant search text
				string searchText;
				int firstSpace = msg.IndexOf(" ");
				if (firstSpace > 0)
					searchText = msg.Substring(0, firstSpace);
				else
					searchText = msg;

				if(searchText.Equals("man") && partialCommand.args.Length >= 1)
				{
					//We are doing a manpage autoComplete
					searchText = partialCommand.args[0];
					manPageAutoComplete = true;
				}

				//Find keys that are possible matches
				List<string> keys = commandDescriptors.Keys.ToList();
				keys = keys.FindAll(x => x.StartsWith(searchText));

				currentCommandDescriptors.Clear();

				//Build suggestion list
				CommandFormatDesc desc;
				int count = 0;

				for (int i = 0; i < keys.Count; i++)
				{
					if (commandDescriptors.TryGetValue(keys[i], out desc))
					{
						if (!manPageAutoComplete)
						{
							//We only care about this for manpage autocomplete
							//Skip if input has more arguments than this descriptor
							if (partialCommand.args.Length > desc.argDescs.Length)
							{
								continue;
							}
						}
						
						currentCommandDescriptors.Add(desc);
						count++;
						//Check if we already found 10 bc that's how many fit in the window
						if (count == 10)
							break;
					}
				}
				maxAutoCompleteIndex = Mathf.Max(count - 1, 0);
				if (autocompleteIndex > maxAutoCompleteIndex)
					autocompleteIndex = maxAutoCompleteIndex;


				if (manPageAutoComplete)
					currentCommand = new Command(searchText, new string[0]);

				BuildAutoCompleteText();
			}
			else
			{
				if (autocompleteRootUI.activeSelf)
					autocompleteRootUI.SetActive(false);
			}
		}

		/// <summary>
		/// Build the text to be displayed in the autocomplete window
		/// </summary>
		private void BuildAutoCompleteText()
		{
			string newText = "";
			string res;

			//newText is built backwards so that the first things added are on the bottom (that's why I do "newText = SOMETHING + newText;"
			for (int i=0; i<currentCommandDescriptors.Count; i++)
			{
				if (i > 0)
					newText = "\n" + newText;
				res = (i == autocompleteIndex) ? AUTOCOMPLETE_MARKER_STRING : "";
				res += GetFormattedSuggestionString(currentCommand, currentCommandDescriptors[i]);
				newText = res + newText;
			}

			textField.text = newText;
		}

		/// <summary>
		/// Helper method to create the coloring effect on command suggestions as you type
		/// </summary>
		/// <param name="inputCommand"></param>
		/// <param name="descriptor"></param>
		/// <returns></returns>
		private string GetFormattedSuggestionString(Command inputCommand, CommandFormatDesc descriptor)
		{
			int numArgs = inputCommand.args.Length;
			string inputText = inputCommand.msg;

			//Bail if input has more arguments than this descriptor. This shouldn't happen anymore since this is checked in UpdateAutocomplete(string)
			if (numArgs > descriptor.argDescs.Length)
			{
				return "";
			}

			string formatted = "<color=#" + MATCHED_COLOR + ">";

			if (manPageAutoComplete)
				formatted += "man ";

			if (numArgs > 0)
			{
				//We're matching numbers of arguments
				formatted += descriptor.msg;
				for (int i = 0; i < descriptor.argDescs.Length; i++)
				{
					formatted += " " + descriptor.argDescs[i].ToString();
					if (i == numArgs - 1)
						formatted += "</color>";
				}
			}
			else
			{
				//We've only matched the name
				formatted += descriptor.msg.Substring(0, inputText.Length);
				formatted += "</color>";
				formatted += descriptor.msg.Substring(inputText.Length);

				if (!manPageAutoComplete)
				{
					//Don't show argument listing when doing manpage autocomplete
					for (int i = 0; i < descriptor.argDescs.Length; i++)
					{
						formatted += " " + descriptor.argDescs[i].ToString();
					}
				}
				
				
			}

			return formatted;
		}

		public void ResetAutocompleteIndex()
		{
			autocompleteIndex = 0;
		}

		/// <summary>
		/// Return the selected autocomplete string
		/// </summary>
		/// <returns></returns>
		public string Autocomplete()
		{
			if (currentCommandDescriptors.Count == 0)
				return "";
			if (autocompleteIndex >= currentCommandDescriptors.Count)
				autocompleteIndex = currentCommandDescriptors.Count - 1;
			if (autocompleteIndex < 0)
				return "";

			//Need to prefix with "man " for manpage autocomplete
			if (manPageAutoComplete)
				return "man " + currentCommandDescriptors[autocompleteIndex].msg;

			return currentCommandDescriptors[autocompleteIndex].msg;
		}

		#endregion

		/// <summary>
		/// Get the list of commands registered with the auto-complete system
		/// </summary>
		/// <returns>the list of commands</returns>
		public List<CommandFormatDesc> GetCommandList()
		{
			return commandDescriptors.Values.ToList();
		}

		/// <summary>
		/// Get the list of commands that contain the query string
		/// </summary>
		/// <param name="query">This string must exist within the function name</param>
		/// <returns>The filtered command list</returns>
		public List<CommandFormatDesc> GetCommandList(string query)
		{
			List<CommandFormatDesc> commands = new List<CommandFormatDesc>();
			IEnumerable<string> selection = commandDescriptors.Keys.ToList().FindAll(x => x.Contains(query));
			foreach(string key in selection)
			{
				commands.Add(commandDescriptors[key]);
			}

			return commands;
		}

		/// <summary>
		/// Print the manpage of this command to the console
		/// </summary>
		/// <param name="commandName"></param>
		private void Man(string commandName)
		{
			CommandFormatDesc desc;
			if (commandDescriptors.TryGetValue(commandName, out desc))
				Console.Log(desc.GetManPage());
			else
				Console.Warn("Invalid command name");
		}

		#region Command Bus

		public void HandleCommand(Command command)
		{
			if (command.msg.Equals("man"))
			{
				if (command.args.Length == 0)
				{
					Man("man");
				}
				else
					Man(command.args[0]);
			}
		}

		public List<CommandFormatDesc> GetCommands()
		{
			List<CommandFormatDesc> commands = new List<CommandFormatDesc>();

			CommandArgDesc[] argDescs = { new CommandArgDesc("commandName", "string", "Name of command to view manpage of") };

			commands.Add(new CommandFormatDesc("man", argDescs, "Prints a detailed description about the specified command."));

			return commands;
		}

		#endregion
	}

}
