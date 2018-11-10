using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
	void Start ()
	{
	    var r = GetComponentInChildren<MeshRenderer>();
        Debug.Log(r.material.color);
        foreach (var material in r.materials)
        {
            material.shader = Shader.Find("Standard (Flat Lighting)");
        }
	}
}
