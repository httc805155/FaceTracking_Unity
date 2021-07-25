using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class TextReader : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        ReadString("Assets/Resources/model.txt");
    }

    static void ReadString(string path)
    {
        //Read the text from directly from the test.txt file
        StreamReader reader = new StreamReader(path);
        var list = reader.ReadToEnd().Split('\n').Select(Convert.ToSingle).ToList();

        Debug.Log(list);
        reader.Close();
    }
}
