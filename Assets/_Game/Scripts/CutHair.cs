using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutHair : MonoBehaviour
{
    public CuttingProgress cuttingProgress;
    public Camera mainCamera;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            RaycastHit[] hits = null;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            hits = Physics.RaycastAll(ray, Mathf.Infinity);
            if (hits != null)
            {
                //Debug.Log("number of hits="+hits.Length);
                foreach (RaycastHit hit in hits)
                {
                    Transform objectHit = hit.transform;
                    if (objectHit.CompareTag("Hair"))
                    {
                        Destroy(objectHit);
                        

                    }

                }
            }
        }
    }
}
