using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CuttingProgress : MonoBehaviour
{
    private static int cuttingElements=-1;
    public UIController uiController;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (cuttingElements == 0)
        {
            uiController.showVictoryMessage();
            cuttingElements = -1;
        }
    }

    public static void addCuttingElement()
    {
        if (cuttingElements == -1) cuttingElements = 1; else cuttingElements++;
        
    }

    public static void removeCuttingElement()
    {
        cuttingElements--;
        
    }
}
