using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "ClientesData", menuName = "Cliente")]
public class ClientesData_SO : ScriptableObject
{
    public string nombre;
    public int numeroNoches;
    public int id;
    public int velocidadMovimiento;
    public string[] dialogosIniciales;
    
    public enum Raza
    {
        Vampiro,
        HombreLobo,
        Yokai,
        Zombie,
        Slime
    }

    public enum TipoHabitación
    {
        Normal,
        Suite,
        MegaSuite
    }
}
