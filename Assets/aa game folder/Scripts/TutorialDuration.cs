using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialDuration : MonoBehaviour
{
    [SerializeField] private float duration=3;
    private float timePassed=0;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!transform.gameObject.activeSelf) return;
        timePassed += Time.deltaTime;
        if(timePassed> duration)
        {
            transform.gameObject.SetActive(false);
        }
    }
}
