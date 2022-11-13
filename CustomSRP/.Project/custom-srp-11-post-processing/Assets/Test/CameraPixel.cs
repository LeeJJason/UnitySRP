using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPixel : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Screen.SetResolution(960, 640, false);
        Debug.LogError($"pixel : {Camera.main.pixelWidth}, {Camera.main.pixelHeight}");
        Debug.LogError($"scale pixel : {Camera.main.scaledPixelWidth}, {Camera.main.scaledPixelHeight}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
