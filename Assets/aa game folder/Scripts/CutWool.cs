using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CutWool : MonoBehaviour
{
    public Camera camera;
    private Vibrate vibrate;
    // Start is called before the first frame update
    void Start()
    {
       // vibrate = new Vibrate();
    }

    // Update is called once per frame
    void Update()
    {
        
        if (Input.GetKey(KeyCode.Escape))
        {
            SceneManager.LoadScene(0);
        }
        if (Input.GetMouseButton(0))
        {
            RaycastHit[] hits=null;
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            hits= Physics.RaycastAll(ray, Mathf.Infinity);
            if (hits != null)
            {
                //Debug.Log("number of hits="+hits.Length);
                foreach (RaycastHit hit in hits)
                {
                    Transform objectHit = hit.transform;
                    if (objectHit.CompareTag("Wool"))
                    {
                        if(objectHit.GetComponent<Rigidbody>().isKinematic) CuttingProgress.removeCuttingElement();
                        objectHit.GetComponent<Rigidbody>().isKinematic = false;
                        
                        // Handheld.Vibrate();
                        // if(vibrate.hasVibrator()) vibrate.vibrate(200);

                    }

                }
            }
        }
        else
        {
            //vibrate.cancel();
        }
    }


}
