using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClienteManager : MonoBehaviour
{
    public ClienteController[] prefabsClientes;

    public Transform puntoOrigen;
    public Transform puntoFinal;

    public ClienteController clienteActual;
    private int indiceActual = 0;
    private bool todosAtendidos = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (clienteActual == null && !todosAtendidos)
        {
            InvocarCliente();
        }

        if (Input.GetKeyDown(KeyCode.Space) && clienteActual != null && !clienteActual.estaSaliendo && !clienteActual.estaLlegando)
        {
            clienteActual.Irse();
            Invoke(nameof(InvocarCliente), 2f);
        }
    }

    void InvocarCliente()
    {
        if(indiceActual >= prefabsClientes.Length)
        {
            todosAtendidos = true;
            return;
        }

        clienteActual = Instantiate(prefabsClientes[indiceActual]);
        clienteActual.puntoOrigen = puntoOrigen;
        clienteActual.puntoFinal = puntoFinal;

        indiceActual++;
    }
}
