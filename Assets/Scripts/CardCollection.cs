using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NuevaColeccionDeCartas", menuName = "Cartas/Colección")]
public class CardCollection : ScriptableObject
{
    [SerializeField]
    private List<ScriptableCard> cartas = new List<ScriptableCard>();
    
    // Propiedad pública para acceder a las cartas
    public List<ScriptableCard> CardsInCollection
    {
        get { return cartas; }
        set { cartas = value; }
    }
}