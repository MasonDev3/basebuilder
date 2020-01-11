﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Every frame, this script checks to see which tile is under the mouse
/// and then updated the GetComponent<Text>.text parameter of the 
/// object is is attached to
/// </summary>
public class MouseOverFurnitureTypeText : MonoBehaviour
{
    Text myText;
    MouseController mouseController;
    // Start is called before the first frame update
    void Start()
    {
        myText = GetComponent<Text>();

        if (myText == null)
        {
            Debug.LogError("MouseOverFurnitureTypeText: No 'Text' UI component on this object.");
            this.enabled = false;
        }
        mouseController = GameObject.FindObjectOfType<MouseController>();
        if (mouseController == null)
        {
            Debug.LogError("How do we not have an instance of mouse controller?");
        }
    }

    // Update is called once per frame
    void Update()
    {
        Tile t = mouseController.GetMouseOverTile();

        string s = "NULL";

        if (t.furniture != null)
        {
            s = t.furniture.ObjectType;
        }

        myText.text = "Furniture: " + s;
    }
}
