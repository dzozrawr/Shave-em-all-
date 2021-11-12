using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CutHairr : MonoBehaviour
{
    //public CuttingProgress cuttingProgress;
    public Camera mainCamera;
    //private Vibrate vibrate;

    //maybe some of these should be [SerializedField] private
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
            RaycastHit[] hits = null;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            hits = Physics.RaycastAll(ray, Mathf.Infinity);
            if (hits != null)
            {
                //Debug.Log("number of hits="+hits.Length);
                foreach (RaycastHit hit in hits)
                {
                    Transform objectHit = hit.transform;
                    if (objectHit.CompareTag("Wool"))
                    {
                        //if (objectHit.GetComponent<Rigidbody>().isKinematic) cuttingProgress.removeCuttingElement();
                        objectHit.GetComponent<Rigidbody>().isKinematic = false;
                        objectHit.GetComponent<Rigidbody>().AddForce(-Vector3.right * 100);

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