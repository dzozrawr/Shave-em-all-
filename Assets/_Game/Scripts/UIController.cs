using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;

//using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
    public GameObject victoryUI;
    public Slider progressBar;

    

    // Start is called before the first frame update
    void Start()
    {
        
        //Time.deltaTime
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void showVictoryMessage()
    {
        victoryUI.SetActive(true);
    }

    public void nextLevel()
    {
        Scene scene;
        SceneManager.LoadScene("Haircut");  //should be: buildIndex+1
    }

}
