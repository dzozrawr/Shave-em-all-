using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HairrGeneration : MonoBehaviour
{
    public GameObject wool;
    public MeshFilter meshFilter;

    public Transform localTransform;
    //public CuttingProgress cuttingProgress;
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

        //cuttingProgress.setCuttingElementNumber(mesh.vertices.Length); //optimizacija u odnosu da se doda po jedan element u svakoj iteraciji petlje
        for (int i = 0; i < mesh.vertices.Length; i++)
        {

            Vector3 pos = mesh.vertices[i];
            Vector3 normal = mesh.normals[i];

            pos = localTransform.TransformPoint(pos);

            //pos += normal * 0.5f;

            GameObject go = Instantiate(wool, pos, Quaternion.LookRotation(-Vector3.right));
            //CuttingProgress.addCuttingElement();

            //go.transform.localScale *= Random.Range(0.75f, 1.5f);

        }
    }
}