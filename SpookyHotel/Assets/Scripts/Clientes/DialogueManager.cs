using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public TMP_Text dialogoText;

    public ClientesData_SO clienteActual;
    public ClienteController clienteController;

    private int indiceDialogo = 0;

    // Start is called before the first frame update
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        if(clienteController.estaLlegando == true)
        {
            IniciarDialogo(clienteActual);
        }
    }

    void IniciarDialogo(ClientesData_SO cliente)
    {
        clienteActual = cliente;
        indiceDialogo = 0;
        MostrarDialogoActual();

    }

    public void SiguienteDialogo()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (indiceDialogo < clienteActual.dialogosIniciales.Length)
            {
                MostrarDialogoActual();
            }
            else
            {
                TerminarDialogo();

            }

            indiceDialogo++;
        }      

    }

    void MostrarDialogoActual()
    {
        dialogoText.text = clienteActual.dialogosIniciales[indiceDialogo];
    }

    void TerminarDialogo()
    {
        Debug.Log("He terminado");
        dialogoText.text = "";
    }
}
