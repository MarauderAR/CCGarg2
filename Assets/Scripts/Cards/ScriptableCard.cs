using UnityEngine;

[CreateAssetMenu(fileName = "Nueva Carta", menuName = "Cards/Nueva Carta")]
public class ScriptableCard : ScriptableObject
{
    [Header("Identificación")]
    public string IdUnico;
    public string NombreCarta;
    
    [Header("Estadísticas")]
    public int CostoPoderPolitico;
    public int Influencia;
    public int Resistencia;
    
    [Header("Características")]
    public string Faccion;
    public string TipoCarta;
    public string Descripcion;
    public string Rareza;
    
    [Header("Estados")]
    public bool TieneFueros;
    public bool AccionInmediata;
    
    [Header("Visual")]
    public Sprite Ilustracion;
}