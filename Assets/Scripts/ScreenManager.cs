using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenManager : MonoBehaviour
{
	public Text screenManagerLog;

	void Start()
	{
		string message = "Displays connected: " + Display.displays.Length.ToString();
		Debug.Log(message);
		screenManagerLog.text = message;
		// Display.displays[0] is the primary, default display and is always ON.
		// Check if additional displays are available and activate each.
		if (Display.displays.Length > 1)
			Display.displays[1].Activate();
 
    }
}