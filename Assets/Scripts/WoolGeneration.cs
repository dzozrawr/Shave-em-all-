﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WoolGeneration : MonoBehaviour
{
    public GameObject wool;
    public MeshFilter meshFilter;
    // Start is called before the first frame update
    void Start()
    {
        generateWool();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void generateWool()
    {
        var mesh = meshFilter.mesh;
        Vector3[] normals = mesh.normals;
        // mesh.vertices;
        for (int i = 0; i < mesh.vertices.Length; i++)
        {

            Vector3 pos = mesh.vertices[i];
            Vector3 normal = mesh.normals[i];
            pos = transform.TransformPoint(pos + normal * 0.05f);
           // pos = transform.TransformPoint(pos);
            
            GameObject go = Instantiate(wool, pos, Quaternion.LookRotation(Random.insideUnitSphere));
            go.transform.localScale *= Random.Range(0.5f, 1.5f);
            go.GetComponent<Rigidbody>().AddForce(normal*100);
        }
    }
}
