using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowMouse : MonoBehaviour
{   //perhaps the script should be renamed, because the sound plays here as well
    private Vector3 mouseWorldPosition;
    private float mouseZPlaneCameraOffset;
    // Start is called before the first frame update
    void Start()
    {
        mouseZPlaneCameraOffset = gameObject.transform.position.z- Camera.main.transform.position.z;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            mouseWorldPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, mouseZPlaneCameraOffset));

            Vector3 currentOffset = gameObject.transform.position;
            Vector3 desiredOffset = mouseWorldPosition;

            // mouseWorldPosition = new Vector3(mouseWorldPosition.x, mouseWorldPosition.y, gameObject.transform.position.z);
            //var direction = (mouseWorldPosition - gameObject.transform.position).normalized;
            //gameObject.transform.up = direction;

            gameObject.transform.position = mouseWorldPosition;


            currentOffset = new Vector3(currentOffset.x, currentOffset.y, 0);
            desiredOffset = new Vector3(desiredOffset.x, desiredOffset.y, 0);

            currentOffset = new Vector3(currentOffset.x, currentOffset.y, 0);
            desiredOffset = new Vector3(desiredOffset.x, desiredOffset.y, 0);

            gameObject.transform.localRotation *= Quaternion.FromToRotation(currentOffset, desiredOffset);  //rotation could be improved to be more responsive
        }
    }
}
