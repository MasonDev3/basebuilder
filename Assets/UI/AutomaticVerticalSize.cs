using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class AutomaticVerticalSize : MonoBehaviour
{
    public float childHeight = 35.0f;
    // Start is called before the first frame update
    void Start()
    {
        AdjustSize();
    }
    void Update()
    {
        AdjustSize();
    }

    public void AdjustSize()
    {
        Vector2 size = this.GetComponent<RectTransform>().sizeDelta;
        size.y = this.transform.childCount * childHeight;
        this.GetComponent<RectTransform>().sizeDelta = size;
    }
}
