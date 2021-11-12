using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HairGeneration : MonoBehaviour
{
    public GameObject wool;
    public MeshFilter meshFilter;

    public Transform localTransform;
    public CuttingProgress cuttingProgress;
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
        Vector3 oldPos= new Vector3();
       // cuttingProgress.setCuttingElementNumber(mesh.vertices.Length); //optimizacija u odnosu da se doda po jedan element u svakoj iteraciji petlje
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
           
            Vector3 pos = mesh.vertices[i];
            Vector3 normal = mesh.normals[i];
            
            pos = localTransform.TransformPoint(pos);

            // pos += normal*0.5f;

            //  GameObject go = Instantiate(wool, pos, Quaternion.LookRotation(Random.insideUnitSphere));
            GameObject go = Instantiate(wool, pos, Quaternion.identity);
            if (i != 0)
            {
                Vector3 inbetween = Vector3.Lerp(oldPos,pos,0.5f);
                GameObject go1 = Instantiate(wool, inbetween, Quaternion.identity);
            }
            //CuttingProgress.addCuttingElement();

            //   go.transform.localScale *= Random.Range(0.75f, 1.5f);

            //go.GetComponent<Rigidbody>().AddForce(normal*100);
            oldPos = pos;
        }
    }
}
