using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FTF.Console
{
	[RequireComponent(typeof(Scrollbar))]
	public class SelectScrollbarMover : MonoBehaviour
	{
		private const float SCROLL_SCALAR = .5f;
		private Scrollbar scrollBar;
		// Use this for initialization
		void Start()
		{
			scrollBar = GetComponent<Scrollbar>();
		}

		// Update is called once per frame
		void Update()
		{
			if((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl))){
				if (Input.GetKeyDown(KeyCode.PageDown))
				{
					scrollBar.value -= scrollBar.size * SCROLL_SCALAR;
				}
				else if (Input.GetKeyDown(KeyCode.PageUp))
				{
					scrollBar.value += scrollBar.size * SCROLL_SCALAR;
				}
			}
			
		}
	}
}

