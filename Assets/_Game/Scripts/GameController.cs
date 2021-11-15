using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] private UIController uiController;
    [SerializeField] private Animator shavedObjectAnimator = null;

    private bool isGameOver = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Victory()
    {
        if (!isGameOver)
        {
            shavedObjectAnimator.SetTrigger("Victory");
            uiController.showVictoryMessage();
            isGameOver = true;
        }
    }
}
