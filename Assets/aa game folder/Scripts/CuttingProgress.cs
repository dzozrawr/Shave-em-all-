using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CuttingProgress : MonoBehaviour
{
    private int cuttingElements=-1, cuttingElementsMax;
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

    public void setCuttingElementNumber(int n)
    {
        cuttingElements = n;
        cuttingElementsMax = n;
        uiController.progressBar.maxValue = n;
    }

    public  void addCuttingElement()
    {
        if (cuttingElements == -1) cuttingElements = 1; else cuttingElements++;
        
    }

    public  void removeCuttingElement()
    {
        cuttingElements--;
        uiController.progressBar.value = cuttingElementsMax - cuttingElements;
    }
}
