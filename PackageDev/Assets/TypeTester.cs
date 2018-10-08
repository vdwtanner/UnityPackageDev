using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TypeTester : MonoBehaviour {

	public string testA = "ha-hi!";
	public int intTest = 123456;
	public float floatTest = 1.23564f;
	public Vector3 vec3Test = Vector3.zero;
	public GameObject gob;
	public int prop1 { get; set; }

	private int ImPrivate = 69;
	private int m_uglyIntName = 25;
	private string h_weirdStr = "I'm a strange string";
	protected string prop2 { get; private set; }

	Transform tran;

	private void Start()
	{
		tran = GetComponent<Transform>();
	}
}
