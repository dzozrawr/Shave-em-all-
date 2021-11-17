using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyCuttingElem : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnBecameInvisible()
    {
        if (gameObject.transform.parent != null) {
            gameObject.transform.parent.GetComponent<DestroyObject>().selfDestruct();
            return;
        }

        Destroy(gameObject);
    }
}
