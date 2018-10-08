using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.EventSystems;
using System.Text.RegularExpressions;

namespace FTF.Console
{
	public class Console : MonoBehaviour, IConsoleSubscriber
	{
		#region events
		public delegate void ConsoleActivationListener(bool activated);
		#endregion

		#region variables
		private const int MAX_ENTRIES = 128;
		private const float AUTO_SCROLL_ZONE = .00001f;
		private static Color ERROR_COLOR = new Color(.8f, .25f, .09f);

		private static Console sInstance;

		public Canvas consoleCanvas;
		public InputField inputField;
		public Text consoleOutputPrefab;
		public ScrollRect scrollrect;
		public ConsoleAutocomplete consoleAutocomplete;
		
		private VerticalLayoutGroup contentFrameVLG;
		private ContentSizeFitter contentFrameCSF;
		private ConsoleActivationListener consoleActivationEvent;
		private Queue<Text> consoleOutputObjects;
		

		static List<IConsoleSubscriber> subscribers = new List<IConsoleSubscriber>();
		static bool verbose = true;
		static bool logCommands = true;
		static bool forwardToUnityDebug = true;
		#endregion

		#region UnityOverrides
		private void Awake()
		{
			//pre-allocate this space
			consoleOutputObjects = new Queue<Text>(MAX_ENTRIES);

			Debug.Assert(consoleOutputPrefab != null);
			Debug.Assert(scrollrect != null);
			Debug.Assert(consoleAutocomplete != null);
			Debug.Assert(consoleCanvas != null);
			Debug.Assert(inputField != null);

			contentFrameVLG = scrollrect.content.GetComponent<VerticalLayoutGroup>();
			contentFrameCSF = scrollrect.content.GetComponent<ContentSizeFitter>();

			Debug.Assert(contentFrameVLG != null);
			Debug.Assert(contentFrameCSF != null);

			//Need to make sure the prefab has this object
			Debug.Assert(consoleOutputPrefab.GetComponent<ContentSizeFitter>() != null);
			

			sInstance = this;
			forwardToUnityDebug = true;
			scrollrect.verticalNormalizedPosition = 0;
			Log("Console online!");
			SetConsoleActive(false);
			consoleActivationEvent = null;
		}

		private void Start()
		{
			Subscribe(this);
		}

		void Update()
		{
			if (Input.GetKeyDown(KeyCode.BackQuote))
			{
				if (consoleCanvas.enabled)
				{
					SetConsoleActive(false);
				}
				else
				{
					SetConsoleActive(true);
				}
			}

			if (inputField.isFocused && inputField.text.Length > 0 && Input.GetKeyDown(KeyCode.Tab))
			{
				string autoCompleteStr = consoleAutocomplete.Autocomplete();
				int autoCompleteStrLength = autoCompleteStr.Length;
				if (autoCompleteStrLength > inputField.text.Length)
				{
					inputField.text = autoCompleteStr;
					inputField.caretPosition = autoCompleteStrLength;
				}
			}
		}
		#endregion

		private void SetConsoleActive(bool active)
		{
			if (active)
			{
				scrollrect.gameObject.SetActive(true);      //Go ahead and reactivate this fella
				consoleCanvas.enabled = true;
				scrollrect.verticalNormalizedPosition = 0;
				inputField.ActivateInputField();
				consoleAutocomplete.ResetAutocompleteIndex();
				if (consoleActivationEvent != null)
					consoleActivationEvent.Invoke(true);
			}else
			{
				scrollrect.gameObject.SetActive(false);     //Holy mother of God we can't allow this guy or it's children to update if it's not actually being shown
				consoleCanvas.enabled = false;
				inputField.text = "";
				inputField.DeactivateInputField();
				if (consoleActivationEvent != null)
					consoleActivationEvent.Invoke(false);
			}
		}

		#region UnityEvents

		public void OnInputFieldUpdated(string inputText)
		{
			Command command = ParseCommand(inputText);
			consoleAutocomplete.UpdateAutoComplete(command);
		}

		#endregion

		#region printing
		static List<ContentSizeFitter> consoleOutputCSF = new List<ContentSizeFitter>(1);

		/// <summary>
		/// Print colored string to console
		/// </summary>
		/// <param name="str">The string to print</param>
		/// <param name="color">The color for the text</param>
		private static void printToConsole(string str, Color color)
		{
			if(sInstance != null)
			{
				bool autoScroll = sInstance.scrollrect.verticalNormalizedPosition <= AUTO_SCROLL_ZONE;

				//build formatted text string
				string newText = "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">";  //prepend with color tag
				newText += str;
				newText += "</color>";

				Text textToWriteTo;
				if(sInstance.consoleOutputObjects.Count < MAX_ENTRIES)
				{
					//We aren't at capacity yet, continue to add
					textToWriteTo = Instantiate(sInstance.consoleOutputPrefab, sInstance.scrollrect.content);
				}else
				{
					//We are at capacity, start to recycle
					//Dequeue to get the oldest message
					textToWriteTo = sInstance.consoleOutputObjects.Dequeue();
				}

				//Assign text to console output
				textToWriteTo.text = newText;

				//We are using a vertical layout group and want this guy to be at the end of the list
				textToWriteTo.transform.SetAsLastSibling();

				//Add to queue
				sInstance.consoleOutputObjects.Enqueue(textToWriteTo);
				

				if (autoScroll && sInstance.consoleCanvas.enabled)
				{
					textToWriteTo.GetComponents(consoleOutputCSF);	//This will always be present

					Canvas.ForceUpdateCanvases();

					//update console output rect
					consoleOutputCSF[0].SetLayoutVertical();
					consoleOutputCSF[0].SetLayoutHorizontal();

					//Update content rect
					sInstance.contentFrameCSF.SetLayoutVertical();
					sInstance.contentFrameCSF.SetLayoutHorizontal();

					//update scrollrect
					sInstance.scrollrect.CalculateLayoutInputVertical();

					//now we can move the scroll bar to the bottom
					sInstance.scrollrect.verticalNormalizedPosition = 0;
				}
				
			}
		}

		/// <summary>
		/// Print uncolored string to console
		/// </summary>
		/// <param name="str">String to print</param>
		private static void printToConsole(string str)
		{
			if (sInstance != null)
			{
				bool autoScroll = sInstance.scrollrect.verticalNormalizedPosition <= AUTO_SCROLL_ZONE;

				//build formatted text string
				string newText = str;

				Text textToWriteTo;
				if (sInstance.consoleOutputObjects.Count < MAX_ENTRIES)
				{
					//We aren't at capacity yet, continue to add
					textToWriteTo = Instantiate(sInstance.consoleOutputPrefab, sInstance.scrollrect.content);
				}
				else
				{
					//We are at capacity, start to recycle
					//Dequeue to get the oldest message
					textToWriteTo = sInstance.consoleOutputObjects.Dequeue();
				}

				//Assign text to console output
				textToWriteTo.text = newText;

				//We are using a vertical layout group and want this guy to be at the end of the list
				textToWriteTo.transform.SetAsLastSibling();

				//Add to queue
				sInstance.consoleOutputObjects.Enqueue(textToWriteTo);


				if (autoScroll && sInstance.consoleCanvas.enabled)
				{
					textToWriteTo.GetComponents(consoleOutputCSF);  //This will always be present

					Canvas.ForceUpdateCanvases();

					//update console output rect
					consoleOutputCSF[0].SetLayoutVertical();
					consoleOutputCSF[0].SetLayoutHorizontal();

					//Update content rect
					sInstance.contentFrameCSF.SetLayoutVertical();
					sInstance.contentFrameCSF.SetLayoutHorizontal();

					//update scrollrect
					sInstance.scrollrect.CalculateLayoutInputVertical();

					//now we can move the scroll bar to the bottom
					sInstance.scrollrect.verticalNormalizedPosition = 0;
				}
			}
		}
		#endregion

		#region logging
		/// <summary>
		/// Logs a message to console and also forwards to Debug.Log()
		/// </summary>
		/// <param name="obj"></param>
		public static void Log(object obj)
		{
			if(forwardToUnityDebug)
				Debug.Log(obj);
			printToConsole(obj.ToString());
		}

		/// <summary>
		/// Logs a message to console and also forwards to Debug.Log()
		/// </summary>
		/// <param name="obj">String to log</param>
		/// <param name="color">Color to log the string in</param>
		public static void Log(object obj, Color color)
		{
			if(forwardToUnityDebug)
				Debug.Log(obj);
			printToConsole(obj.ToString(), color);
		}

		/// <summary>
		/// Logs a message to console (colored yellow) and also forwards to Debug.LogWarning()
		/// </summary>
		/// <param name="obj"></param>
		public static void Warn(object obj)
		{
			if(forwardToUnityDebug)
				Debug.LogWarning(obj);
			printToConsole(obj.ToString(), Color.yellow);
		}

		/// <summary>
		/// Logs a message to console (colored red) and also forwards to Debug.LogError()
		/// </summary>
		/// <param name="obj"></param>
		public static void Error(object obj)
		{
			//always forward errors
			Debug.LogError(obj);
			printToConsole(obj.ToString(), ERROR_COLOR);
		}

		/// <summary>
		/// Only logs if console.verbose is true
		/// </summary>
		/// <param name="obj"></param>
		public static void Verbose(object obj)
		{
			if (verbose)
				Log(obj);
		}

		/// <summary>
		/// Only logs if console.verbose is true
		/// </summary>
		/// <param name="obj">String to log</param>
		/// <param name="color">Color to log the string in</param>
		public static void Verbose(object obj, Color color)
		{
			if (verbose)
				Log(obj, color);
		}

		#endregion

		#region MessageBus
		public static void PostFromConsoleInput(string commandString)
		{
			Post(commandString);
			if(sInstance != null)
			{
				sInstance.consoleAutocomplete.ResetAutocompleteIndex();
			}
		}

		/// <summary>
		/// Post a command that is parsed from the input string.
		/// The command string will always be logged.
		/// Generally speaking you should use <see cref="PostCommand(Command)"/> instead.
		/// </summary>
		/// <param name="commandString">The string to parse a command from</param>
		public static void Post(string commandString)
		{
			Log(commandString);

			Command command = ParseCommand(commandString);

			PostCommand(command);
		}

		public static void PostCommand(Command command)
		{
			if (logCommands)
				Log(command);

			//Broadcast the command to all subscribers (effectively a message bus)
			for(int i = 0; i< subscribers.Count; i++)
			{
				subscribers[i].HandleCommand(command);
			}
		}

		/// <summary>
		/// Subscribe to the command bus
		/// </summary>
		/// <param name="subscriber"></param>
		public static void Subscribe(IConsoleSubscriber subscriber)
		{
			subscribers.Add(subscriber);
			if(sInstance != null)
			{
				sInstance.consoleAutocomplete.AddDescriptors(subscriber.GetCommands());
			}
		}

		/// <summary>
		/// Unsubscribe from the command bus
		/// </summary>
		/// <param name="unsub"></param>
		public static void Unsubscribe(IConsoleSubscriber unsub)
		{
			subscribers.Remove(unsub);
		}
		#endregion

		#region Command Listing
		public void HandleCommand(Command command)
		{
			if (command.msg.Equals("console.verbose"))
			{
				if (command.args.Length >= 1)
				{
					verbose = bool.Parse(command.args[0]);
				}
				else
					verbose = !verbose;

				Log("console.verbose: " + verbose, Color.cyan);
			}
			else if (command.msg.Equals("console.logCommands"))
			{
				if (command.args.Length >= 1)
				{
					logCommands = bool.Parse(command.args[0]);
				}
				else
					logCommands = !logCommands;

				Log("console.logCommands: " + logCommands, Color.cyan);
			}
			else if (command.msg.Equals("console.subscribers"))
			{
				Log("<b>======SUBSCRIBERS======</b>", Color.blue);
				foreach (IConsoleSubscriber sub in subscribers)
				{
					Log(sub);
				}
			}
			else if (command.msg.Equals("console.clear"))
			{
				Warn("Not implemented!");
				/*Canvas.ForceUpdateCanvases();

				//update console output rect
				sInstance.consoleOutputCSF.SetLayoutVertical();
				sInstance.consoleOutputCSF.SetLayoutHorizontal();

				//Update content rect
				sInstance.contentFrameCSF.SetLayoutVertical();
				sInstance.contentFrameCSF.SetLayoutHorizontal();

				//update scrollrect
				sInstance.scrollrect.CalculateLayoutInputVertical();*/
			}
			else if (command.msg.Equals("help"))
			{
				System.Text.StringBuilder builder = new System.Text.StringBuilder();
				builder.AppendLine("<color=#0080FF><b>===== COMMAND LISTING =====</b></color>");
				if (command.args.Length == 0)
				{
					foreach(CommandFormatDesc desc in consoleAutocomplete.GetCommandList())
					{
						builder.AppendLine(desc.ToString());
					}
					
				}
				else
				{
					foreach (CommandFormatDesc desc in consoleAutocomplete.GetCommandList(command.args[0]))
					{
						
						builder.AppendLine(desc.ToString());
					}
				}
				//Use printToConsole so that we don't flood Unity debug console
				printToConsole(builder.ToString());
			}
			else if (command.msg.Equals("console.logToUnity"))
			{
				if(command.args.Length >= 1)
					forwardToUnityDebug = bool.Parse(command.args[0]);
				else
					forwardToUnityDebug = !forwardToUnityDebug;
				
				Log(forwardToUnityDebug, Color.blue);
			}
		}

		public List<CommandFormatDesc> GetCommands()
		{
			List<CommandFormatDesc> commands = new List<CommandFormatDesc>();
			//verbose logging command
			CommandArgDesc[] argDesc = { new CommandArgDesc("useVerbose?", "bool", true) };
			commands.Add(new CommandFormatDesc("console.verbose", argDesc));

			//Log commands command
			argDesc = new CommandArgDesc[1];
			argDesc[0] = new CommandArgDesc("logCommands?", "bool", true);
			commands.Add(new CommandFormatDesc("console.logCommands", argDesc));

			//forwardToUnity command
			argDesc = new CommandArgDesc[1];
			argDesc[0] = new CommandArgDesc("forwardMsgsToUnity?", "bool", true);
			commands.Add(new CommandFormatDesc("console.logToUnity", argDesc));

			//List subscribers command
			commands.Add(new CommandFormatDesc("console.subscribers"));

			//clear console
			commands.Add(new CommandFormatDesc("console.clear"));

			argDesc = new CommandArgDesc[1];
			argDesc[0] = new CommandArgDesc("filter", "string", true);
			commands.Add(new CommandFormatDesc("help", argDesc));

			return commands;
		}
		#endregion

		/// <summary>
		/// Instantiates the console UI in the scene if it isn't already in scene.
		/// The console is then marked as DontDestroyOnLoad.
		/// 
		/// NOTE: Be sure to call <see cref="EnsureEventSystemPresent()"/> in order to have input
		/// </summary>
		public static void TryInstantiate()
		{
			if(sInstance == null)
			{
				DontDestroyOnLoad(Instantiate(Resources.Load("FTF_Console")));
			}
			
		}

		/// <summary>
		/// Checks the scene for an Event system. If one is not present it will create a default one.
		/// This is needed to allow for Input to the console
		/// </summary>
		public static void EnsureEventSystemPresent()
		{
			EventSystem es = FindObjectOfType<EventSystem>();
			if (es == null)
			{
				Instantiate(Resources.Load("FTF_Console_DefaultEventSystem"));
			}
		}

		public static void SubscribeToConsoleActivationEvent(ConsoleActivationListener listener)
		{
			if(sInstance != null)
			{
				sInstance.consoleActivationEvent += listener;
			}
		}

		public static void UnsubscribeFromConsoleActivationEvent(ConsoleActivationListener listener)
		{
			if(sInstance != null)
			{
				sInstance.consoleActivationEvent -= listener;
			}
		}

		/// <summary>
		/// Parses a command from a given string
		/// </summary>
		/// <param name="commandString"></param>
		/// <returns></returns>
		private static Command ParseCommand(string commandString)
		{
			commandString = commandString.Trim();
			int msgEnd = commandString.IndexOf(' ');
			Command command = new Command();
			if (msgEnd > 0)
			{
				//This message has parameters
				command.msg = commandString.Substring(0, msgEnd);
				commandString = commandString.Substring(msgEnd);

				var parts = Regex.Matches(commandString, @"[\""].+?[\""]|[^ ]+")
					.Cast<Match>()
					.Select(m => m.Value)
					.ToArray();

				for(int i=0; i<parts.Length; i++)
				{
					parts[i] = parts[i].Replace("\"", "");
				}

				command.args = parts;

				//char[] splitter = { ' ' };
				//command.args = commandString.Split(splitter, System.StringSplitOptions.RemoveEmptyEntries);
			}
			else
			{
				command.msg = commandString;
				command.args = new string[0];
			}

			return command;
		}
	}
}

