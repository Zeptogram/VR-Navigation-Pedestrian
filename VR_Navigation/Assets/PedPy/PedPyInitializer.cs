using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PedPyInitializer : MonoBehaviour
{
    void Start()
    {
        Capture();
        string pedPyPath = Application.dataPath + "/PedPy";
        File.Delete(pedPyPath + "/PedPyStats.txt");
        File.Delete(pedPyPath + "/UserStats.txt");
    }

    // Capture an image that creates a map of the scene that can be used as a background for PedPy
    void Capture()
    {
        Camera Cam = GetComponent<Camera>();

        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = Cam.targetTexture;

        Cam.Render();

        Texture2D Image = new Texture2D(Cam.targetTexture.width, Cam.targetTexture.height);
        Image.ReadPixels(new Rect(0, 0, Cam.targetTexture.width, Cam.targetTexture.height), 0, 0);
        Image.Apply();
        RenderTexture.active = currentRT;

        var Bytes = Image.EncodeToPNG();
        Destroy(Image);

        File.WriteAllBytes(Application.dataPath + "/PedPy/BackgroundMap.png", Bytes);    }
}
