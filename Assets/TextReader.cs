using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class TextReader : MonoBehaviour
{
    [SerializeField]
    private GameObject indicator_red;

    [SerializeField]
    private GameObject indicator_blue;

    // Start is called before the first frame update
    void Start()
    {
        var headmodel = ReadString("Assets/Resources/model.txt");

        foreach (var point in headmodel)
        {
            var go = GameObject.Instantiate(indicator_red, new Vector3(point.x, -point.y, point.z), Quaternion.identity);
            go.SetActive(true);
        }

        var facemodel = new List<Vector3> 
        {
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(0.0f, -330f, -65.0f),
            new Vector3(-225.0f, 170.0f, -135.0f),     // Left eye left corner
            new Vector3(225.0f, 170.0f, -135.0f),      // Right eye right corner
            new Vector3(-150.0f, -150.0f, -125.0f),    // Mouth left corner
            new Vector3(150.0f, -150.0f, -125.0f)
        };

        foreach (var point in facemodel)
        {
            var go = GameObject.Instantiate(indicator_blue, point / 4.5f, Quaternion.identity);
            go.SetActive(true);
        }
    }

    private List<Vector3> ReadString(string path)
    {
        List<Vector3> headModel = new List<Vector3>();

        //Read the text from directly from the test.txt file
        StreamReader reader = new StreamReader(path);
        var list = reader.ReadToEnd().Split('\n').Select(Convert.ToSingle).ToList();


        var step = list.Count / 3;
        for (var i = 0; i < step; i++)
        {
            var vector = new Vector3(list[i], list[i + step*1], list[i + step*2]);
            headModel.Add(vector);
        }
        reader.Close();
        return headModel;
    }
}
