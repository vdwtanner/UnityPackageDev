using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

namespace FTF.Console
{
	public interface ISelectionInfo
	{
		string GetSelectionInfo();
	}

	public class Select : MonoBehaviour, IConsoleSubscriber
	{
		private static string HEADER_COLOR = "#F0F000";
		private static string ROW_ACCENT_COLOR = "#000000";
		private static Select sInstance;

		public GameObject selectionBackgroundRoot;
		public Text selectionTitle;
		public Text selectionInfo;
		public ScrollRect selectionScrollRect;

		private GameObject selectedGob;
		private Component selectedComponent;
		private ContentSizeFitter infoCSF;
		private ContentSizeFitter contentCSF;
		private LayoutGroup contentLG;
		private string[] namespaceListing;
		private bool showDetailedGobInfo;

		#region Unity Overrides
		void Start()
		{
			Debug.Assert(selectionBackgroundRoot != null);
			Debug.Assert(selectionTitle != null);
			Debug.Assert(selectionInfo != null);
			Debug.Assert(selectionScrollRect != null);

			infoCSF = selectionInfo.GetComponent<ContentSizeFitter>();
			contentCSF = selectionScrollRect.content.GetComponent<ContentSizeFitter>();
			contentLG = selectionScrollRect.content.GetComponent<LayoutGroup>();

			Debug.Assert(infoCSF != null);
			Debug.Assert(contentCSF != null);
			Debug.Assert(contentLG != null);

			sInstance = this;

			//Load namespaceListing
			string path = Path.Combine(Application.streamingAssetsPath, "namespace_listing.txt");
			Console.Log(path);

			if (!File.Exists(path))
			{
				Console.Warn("No namespace listing found");
			}
			else
			{
				namespaceListing = File.ReadAllLines(path);
				Console.Log("<b>Loaded namespace_listing.txt!</b>", new Color(.1f, .4f, 1.0f));
			}



			selectionBackgroundRoot.SetActive(false);
			Clear();

			Console.Subscribe(this);
			Console.SubscribeToConsoleActivationEvent(OnConsoleActive);
		}

		void Update()
		{
			UpdateSelectionDisplay();
		}

		void OnDestroy()
		{
			Console.Unsubscribe(this);
			Console.UnsubscribeFromConsoleActivationEvent(OnConsoleActive);
		}
		#endregion

		#region commands
		public List<CommandFormatDesc> GetCommands()
		{
			List<CommandFormatDesc> commands = new List<CommandFormatDesc>();
			//selection commands
			commands.Add(new CommandFormatDesc("select.cameraSelect"));
			CommandArgDesc[] argDesc = { new CommandArgDesc("name", "string") };
			commands.Add(new CommandFormatDesc("select.nameSelect", argDesc));

			//UI commands
			commands.Add(new CommandFormatDesc("select.hide"));
			commands.Add(new CommandFormatDesc("select.show"));
			commands.Add(new CommandFormatDesc("select.clear"));

			//Component selection
			argDesc = new CommandArgDesc[1];
			argDesc[0] = new CommandArgDesc("TypeName", "string");
			commands.Add(new CommandFormatDesc("selection.selectComponent", argDesc));
			argDesc = new CommandArgDesc[1];
			argDesc[0] = new CommandArgDesc("showInspector?", "bool", true);
			commands.Add(new CommandFormatDesc("selection.detailed", argDesc));
			commands.Add(new CommandFormatDesc("selection.gameObject"));

			return commands;
		}

		static List<ISelectionInfo> extraInfo = new List<ISelectionInfo>();
		public void HandleCommand(Command command)
		{
			if (command.msg.StartsWith("selection."))
			{
				string msg = command.msg.Substring(10);
				if (msg.Equals("selectComponent"))
				{
					if(selectedGob == null)
					{
						Console.Error("<b>Command requires valid selection</b>");
						return;
					}
					if(command.args.Length == 1)
					{
						SelectComponentByTypeName(command.args[0]);
					}
					else
					{
						Console.Error("Requires 1 argument: component type name.");
						return;
					}
				}
				else if (msg.Equals("detailed"))
				{
					if(selectedComponent != null)
					{
						SelectGameObject(selectedComponent.gameObject);
					}
					if (command.args.Length > 0)
						showDetailedGobInfo = bool.Parse(command.args[0]);
					else
						showDetailedGobInfo = !showDetailedGobInfo;
				}
				else if (msg.Equals("gameObject"))
				{
					if(selectedComponent == null)
					{
						Console.Error("Must have a component selected!");
						return;
					}
					SelectGameObject(selectedComponent.gameObject);
				}
			}
			else if (command.msg.StartsWith("select."))
			{
				if (command.msg.Equals("select.cameraSelect"))
				{
					Transform t = Camera.main.transform;
					RaycastHit hit;
					if (Physics.Raycast(t.position, t.forward, out hit))
					{
						SelectGameObject(hit.collider.gameObject);
					}
					else
					{
						Clear();
						if (selectionBackgroundRoot.activeSelf)
							selectionBackgroundRoot.SetActive(false);
						Console.Warn("No object selected.");
					}
				}
				else if (command.msg.Equals("select.nameSelect") && command.args.Length > 0)
				{
					GameObject gob = GameObject.Find(command.args[0]);
					if (gob == null)
					{
						Console.Warn(command.args[0] + " not found.");
					}
					else
					{
						SelectGameObject(gob);
					}
				}
				else if (command.msg.Equals("select.hide"))
				{
					if (selectionBackgroundRoot.activeSelf)
						selectionBackgroundRoot.SetActive(false);
					enabled = false;
				}
				else if (command.msg.Equals("select.show"))
				{
					if (!selectionBackgroundRoot.activeSelf)
						selectionBackgroundRoot.SetActive(true);
					if (selectedGob != null)
						enabled = true;
				}
				else if (command.msg.Equals("select.clear"))
				{
					Clear();
				}
			}
			
		}
		#endregion commands

		#region helper methods
		void OnConsoleActive(bool newActive)
		{
			if (newActive && selectedGob != null)
				enabled = true;
			else
				enabled = false;
		}

		private void SelectComponentByTypeName(string name)
		{
			System.Type componentType = System.Type.GetType(name, false, true);
			if(componentType == null)
			{
				//Try to find a valid type from our namespace listing...
				if(namespaceListing != null)
				{
					string tempName;
					for (int i = 0; i < namespaceListing.Length; i++)
					{
						tempName = namespaceListing[i] + "." + name;
						Console.Log("Checking " + tempName + "...");
						componentType = System.Type.GetType(tempName, false, true);
						if (componentType != null)
						{
							Console.Log("Found valid type!", new Color(0, .8f, .15f));
							break;
						}
							
					}
				}
				
				if(componentType == null)
				{
					//Still couldn't find a valid type :(
					Console.Error("<b>Invalid Type</b>\nNo selection made.");
					return;
				}
			}

			Console.Log("Searching for type: " + componentType.ToString());

			selectedComponent = selectedGob.GetComponent(componentType);

			//selected component == selected component, else select in parent
			selectedComponent = selectedComponent ?? selectedGob.GetComponentInParent(componentType);

			if(selectedComponent == null)
			{
				Console.Warn("Component not found in self or parent.");
				return;
			}
		}

		private void Clear()
		{
			enabled = false;
			selectedGob = null;
			selectedComponent = null;
			selectionTitle.text = "<i>None</i>";
			selectionInfo.text = "";

			Canvas.ForceUpdateCanvases();

			infoCSF.SetLayoutVertical();
			contentLG.CalculateLayoutInputVertical();
			contentCSF.SetLayoutVertical();
		}

		private void SelectGameObject(GameObject gob)
		{
			Console.Verbose("Selected " + gob.name);

			selectedGob = gob;
			selectedComponent = null;
			enabled = true;

#if UNITY_EDITOR
			UnityEditor.Selection.activeGameObject = gob;
#endif
		}
		#endregion

		#region selection display
		void UpdateSelectionDisplay()
		{
			if (!selectionBackgroundRoot.activeSelf)
				selectionBackgroundRoot.SetActive(true);

			if (selectedComponent != null)
				ComponentDisplay();
			else if (showDetailedGobInfo)
				DetailedDisplay();
			else
				DefaultDisplay();
		}

		void DefaultDisplay()
		{
			selectionTitle.text = selectedGob.name;

			//Display some basic information
			string newSelectionInfo = "<b>Pos:</b>\t" + selectedGob.transform.position;
			newSelectionInfo += "\n<b>Rot:</b>\t" + selectedGob.transform.eulerAngles;

			//Get extra selection info from components
			extraInfo.Clear();
			selectedGob.GetComponents(extraInfo);
			string colorString = "<color=" + HEADER_COLOR + "> ";
			for (int i = 0; i < extraInfo.Count; i++)
			{
				newSelectionInfo += "\n========================================\n";
				newSelectionInfo += "<b>" + colorString + extraInfo[i].GetType() + "</color></b>\n";
				newSelectionInfo += extraInfo[i].GetSelectionInfo();
			}

			selectionInfo.text = newSelectionInfo;

			Canvas.ForceUpdateCanvases();

			infoCSF.SetLayoutVertical();
			contentLG.CalculateLayoutInputVertical();
			contentCSF.SetLayoutVertical();
		}

		List<MonoBehaviour> detailedInfoList = new List<MonoBehaviour>();
		/// <summary>
		/// Builds a list of inspectors for each monobehavior on the selected GameObject
		/// </summary>
		void DetailedDisplay()
		{
			selectionTitle.text = selectedGob.name;

			//Display some basic information
			string newSelectionInfo = "<b>Pos:</b> " + selectedGob.transform.position;
			newSelectionInfo += "\n<b>Rot:</b> " + selectedGob.transform.eulerAngles;

			//Get extra selection info from components
			extraInfo.Clear();
			selectedGob.GetComponents(detailedInfoList);
			string colorString = "<color=" + HEADER_COLOR + "> ";
			for (int i = 0; i < detailedInfoList.Count; i++)
			{
				newSelectionInfo += "\n========================================\n";
				newSelectionInfo += "<b>" + colorString + detailedInfoList[i].GetType().Name + "</color></b>\n";
				newSelectionInfo += Util.GenerateReflectionInspector(detailedInfoList[i], HEADER_COLOR, ROW_ACCENT_COLOR);
			}
			selectionInfo.text = newSelectionInfo;

			Canvas.ForceUpdateCanvases();

			infoCSF.SetLayoutVertical();
			contentLG.CalculateLayoutInputVertical();
			contentCSF.SetLayoutVertical();
		}

		private void ComponentDisplay()
		{
			selectionTitle.text = selectedComponent.GetType().ToString();

			selectionInfo.text = Util.GenerateReflectionInspector(selectedComponent, HEADER_COLOR, ROW_ACCENT_COLOR);

			Canvas.ForceUpdateCanvases();

			infoCSF.SetLayoutVertical();
			contentLG.CalculateLayoutInputVertical();
			contentCSF.SetLayoutVertical();
		}
		#endregion

		#region Static Functions

		/// <summary>
		/// Helper function for command bus selections
		/// 
		/// WARN: will do nothing if the Select object is not present in scene!
		/// 
		/// NOTE: only call this as a way to add functionality to command bus things!
		/// This is not meant to support general scripting.
		/// </summary>
		/// <param name="gob">GameObject to select</param>
		public static void SelectGob(GameObject gob)
		{
			if(sInstance != null)
			{
				sInstance.SelectGameObject(gob);
			}
		}

		/// <summary>
		/// Helper function for command bus selections
		/// 
		/// WARN: will do nothing if the Select object is not present in scene!
		/// 
		/// NOTE: only call this as a way to add functionality to command bus things!
		/// This is not meant to support general scripting.
		/// 
		/// </summary>
		/// <param name="component">The component you wish to select</param>
		public static void SelectComponent(Component component)
		{
			if (sInstance != null)
				sInstance.selectedComponent = component;
		}

		#endregion


	}
}

