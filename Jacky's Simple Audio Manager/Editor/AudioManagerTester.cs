using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using JSAM;
using JSAM.JSAMEditor;

public class AudioManagerTester : MonoBehaviour
{
    public string testPath;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    [ContextMenu("TEST")]
    void Test()
    {
        JSAMEditorHelper.GenerateFolderStructure(testPath);
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    SceneLoader.ReloadScene();
        //}
    }
}
