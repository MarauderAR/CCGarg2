using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Vida / Dinero")]
    public int dineroMax = 60;
    public int dineroActual;

    [Header("Poder Político")]
    public int poderMax = 1;
    public int poderActual;
    public int poderLimite = 10;

    void Awake()
    {
        Inicializar();
    }

    public void Inicializar()
    {
        dineroActual = dineroMax;
        poderActual = poderMax;
    }

    public void IniciarTurno()
    {
        if (poderMax < poderLimite)
            poderMax++;

        poderActual = poderMax;
        RobarCarta();
    }

    public bool PuedePagar(int costo)
    {
        return poderActual >= costo;
    }

    public void Pagar(int costo)
    {
        poderActual -= costo;
    }

    public void RecibirDaño(int cantidad)
    {
        dineroActual -= cantidad;
        if (dineroActual <= 0)
        {
            Debug.Log($"{gameObject.name} ha perdido la partida.");
        }
    }

    void RobarCarta()
    {
        Debug.Log($"{gameObject.name} roba una carta.");
        // Podés conectar esto con tu DeckManager o GameManager
    }
}
