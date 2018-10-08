using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FTF.Console
{
	[RequireComponent(typeof(InputField))]
	public class ConsoleInputField : MonoBehaviour
	{
		private InputField consoleInputField;
		private bool select;
		private List<string> commandHistory;

		private int historyIndex;

		private void Awake()
		{
			select = false;
			consoleInputField = GetComponent<InputField>();
			commandHistory = new List<string>();
			historyIndex = 0;
		}

		private void Update()
		{
			//reselect after submitting
			if (select && consoleInputField.isFocused == false)
			{
				select = false;
				consoleInputField.ActivateInputField();
			}
			//Shift up and down is good for highlighting, so break if either shift key is pressed
			if(!(Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift)))
			{
				if (consoleInputField.isFocused && Input.GetKeyDown(KeyCode.UpArrow) && commandHistory.Count > 0)
				{
					//go back in history
					historyIndex = Mathf.Max(historyIndex - 1, 0);
					consoleInputField.text = commandHistory[historyIndex];

					consoleInputField.caretPosition = consoleInputField.text.Length;
				}
				if (consoleInputField.isFocused && Input.GetKeyDown(KeyCode.DownArrow) && commandHistory.Count > 0)
				{
					//go forward in history
					historyIndex = Mathf.Min(historyIndex + 1, commandHistory.Count);
					if (historyIndex == commandHistory.Count)
						consoleInputField.text = "";
					else
						consoleInputField.text = commandHistory[historyIndex];

					consoleInputField.caretPosition = consoleInputField.text.Length;
				}
			}
			
		}

		private void OnGUI()
		{
			//Submit the command on user press enter
			if(consoleInputField.isFocused && consoleInputField.text != "" && (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter)))
			{
				Console.Post(consoleInputField.text);
				if(commandHistory.Count == 0 || commandHistory[commandHistory.Count-1] != consoleInputField.text)
				{
					//only add to history if it is different from previous command
					commandHistory.Add(consoleInputField.text);
				}
				historyIndex = commandHistory.Count;
				consoleInputField.text = "";
				select = true;
				consoleInputField.DeactivateInputField();
			}
		}
	}
}

