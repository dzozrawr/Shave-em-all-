using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CuttingProgress : MonoBehaviour
{
    private int cuttingElements=-1, cuttingElementsMax;
    public UIController uiController;
    private GameController gameController;
    private bool didPlayAngryBaahAtHalfProgress = false;
    // Start is called before the first frame update
    void Start()
    {
        gameController = gameObject.GetComponent<GameController>(); //requires for the game object to have a GameController script
    }

    // Update is called once per frame
    void Update()
    {
        if (cuttingElements == 0)
        {
             gameController.Victory();  //also calls uiControllers showVictoryMessage() method
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

       // Debug.Log(((float)cuttingElements) / ((float)cuttingElementsMax));
        if (((float)cuttingElements) / ((float)cuttingElementsMax) < 0.5f)
        {
            if (!didPlayAngryBaahAtHalfProgress)
            {
                didPlayAngryBaahAtHalfProgress = true;
                gameController.playAngryBaah();
            }

        }
    }
}
