using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FTF.Console.Sample
{
	public class ConsoleActivationListener : MonoBehaviour
	{

		private void Start()
		{
			Console.SubscribeToConsoleActivationEvent(OnConsoleActivation);
		}

		public void OnConsoleActivation(bool activation)
		{
			if (activation)
				Console.Log("Console activated!", Color.blue);
			else
				Console.Log("Console deactivated!", Color.green);
		}

		private void OnDestroy()
		{
			Console.UnsubscribeFromConsoleActivationEvent(OnConsoleActivation);
		}
	}
}
