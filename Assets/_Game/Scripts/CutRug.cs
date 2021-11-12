using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CutRug : MonoBehaviour
{
    public CuttingProgress cuttingProgress;
    public Camera mainCamera;
    private Vibrate vibrate;

    //maybe some of these should be [SerializedField] private
    // Start is called before the first frame update
    void Start()
    {
        // vibrate = new Vibrate();
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
                    if (objectHit.CompareTag("Wool"))   //promeni  tag, ili nadji drugi nacin da detektujes
                    {
                        if (objectHit.GetComponent<Rigidbody>().isKinematic) cuttingProgress.removeCuttingElement();
                        objectHit.GetComponent<Rigidbody>().isKinematic = false;
                        Vector3 randDirection = Random.insideUnitSphere;
                        randDirection.z = -Random.Range(0f,1f);
                        Debug.Log(randDirection);
                        objectHit.GetComponent<Rigidbody>().AddForce(randDirection * 200);
                        objectHit.GetComponent<Rigidbody>().AddTorque(randDirection * 400);

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
