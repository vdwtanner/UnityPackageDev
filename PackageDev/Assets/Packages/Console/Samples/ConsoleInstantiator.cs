using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FTF.Console.Sample
{
	public class ConsoleInstantiator : MonoBehaviour
	{
		void Awake()
		{
			Console.TryInstantiate();
			Console.EnsureEventSystemPresent();
		}
	}
}

