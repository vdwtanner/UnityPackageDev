using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FTF.Console.Samples
{
	public class ExtraSelectableInfo : MonoBehaviour, ISelectionInfo
	{
		public int val1;
		public float val2;
		public string val3;

		public string GetSelectionInfo()
		{
			string output = "";
			output += "<b>Val1:</b> " + val1;
			output += "\n<b>Val2:</b> " + val2;
			output += "\n<b>Val3:</b> " + val3;


			return output;
		}
	}
}

