using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ClienteController : MonoBehaviour
{
    public ClientesData_SO clientes;
    public Transform puntoOrigen;
    public Transform puntoFinal;

    [HideInInspector]public bool estaSaliendo = false;
    [HideInInspector]public bool estaLlegando = true;

    // Start is called before the first frame update
    void Start()
    {
        transform.position = puntoOrigen.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (estaLlegando)
        {
            Mover(puntoFinal.position);
            if(Vector3.Distance(transform.position, puntoFinal.position) < 0.03)
            {
                estaLlegando = false;
                Debug.Log("He llegado");
            }
        }

       
            if (estaSaliendo)
            {
                Mover(puntoOrigen.position);
                if (Vector3.Distance(transform.position, puntoOrigen.position) < 0.03)
                {
                    Destroy(gameObject);
                }
            }
            
        

    }

    void Mover(Vector3 destino)
    {
        transform.position = Vector3.MoveTowards(transform.position, destino, clientes.velocidadMovimiento * Time.deltaTime);
    }

    public void Irse()
    {
        estaSaliendo = true;
        estaLlegando = false;
    }
}
