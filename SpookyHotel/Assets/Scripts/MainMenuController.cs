using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public GameObject creditosPanel;

    private void Start()
    {
       creditosPanel.SetActive(false);
    }

    public void Jugar()
    {
        SceneManager.LoadScene("Hotel");
    }

    public void Creditos()
    {
        creditosPanel.SetActive(true);
    }

    public void CerrarCreditos()
    {
        creditosPanel.SetActive(false);
    }

    public void Salir()
    {
        Application.Quit();
    }

}
