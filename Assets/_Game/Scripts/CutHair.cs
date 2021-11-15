using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutHair : MonoBehaviour
{
    public CuttingProgress cuttingProgress;
    public Camera mainCamera;
    [SerializeField] private GameObject hairStrand, shaver;
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
                        objectHit.GetComponent<DestroyObject>().selfDestruct();

                        GameObject strand= Instantiate(hairStrand, hit.transform.position, Quaternion.LookRotation(Random.insideUnitSphere));
                        makeHairStrandFlyInTheShaverDirection(strand);

                        strand=Instantiate(hairStrand, hit.transform.position, Quaternion.LookRotation(Random.insideUnitSphere));
                        makeHairStrandFlyInTheShaverDirection(strand);

                    }

                }
            }
        }
    }

    private void randomizeHairStrand(GameObject strand)
    {
        strand.transform.localScale *= Random.Range(0.5f, 1.25f);
        Vector3 randDirection = Random.insideUnitSphere;
        randDirection.z = -Random.Range(0.5f, 1f);
        strand.GetComponent<Rigidbody>().AddForce(randDirection * 200,ForceMode.Impulse); 
        strand.GetComponent<Rigidbody>().AddTorque(randDirection * 400,ForceMode.Impulse); 
    }

    private void makeHairStrandFlyInTheShaverDirection(GameObject strand)
    {
        strand.transform.localScale *= Random.Range(0.5f, 1.25f);
        Debug.Log(shaver.transform.localRotation.eulerAngles.z);
        float angle = shaver.transform.localRotation.eulerAngles.z-90; 
        Vector3 lDirection = new Vector3(Mathf.Cos(Mathf.Deg2Rad * angle),Mathf.Sin(Mathf.Deg2Rad * angle), 0);
        lDirection.z = -Random.Range(0.5f, 1f);
        strand.GetComponent<Rigidbody>().AddForce(lDirection * 200, ForceMode.Impulse);
        strand.GetComponent<Rigidbody>().AddTorque(lDirection * 400, ForceMode.Impulse);
    }
}
