using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Necesario si usas Image en ToggleHighlight como fallback

public class Card : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("Asigna aquí el ScriptableObject con los datos de esta carta.")]
    public ScriptableCard cardData;
    private CardUI cardUI;
    private CardZoomManager zoomManager;

    // --- VARIABLE NUEVA PARA EL BORDE ---
    [Header("Feedback Visual")]
    [Tooltip("Arrastra aquí el GameObject HIJO llamado 'HighlightBorder' desde el Prefab")]
    public GameObject highlightBorderObject; // Referencia al objeto del borde
    // ------------------------------------

    // Propiedad para acceder a los datos (Opcional, alternativa a GetCardData)
    public ScriptableCard CardData => cardData;

    private void Awake()
    {
        cardUI = GetComponent<CardUI>();
        if (cardUI == null) cardUI = GetComponentInChildren<CardUI>();
        if (cardUI == null) Debug.LogError($"Error en '{gameObject.name}': No se encontró CardUI.");

        // Desactivar el borde al inicio por si acaso se quedó activo en el editor
        if (highlightBorderObject != null)
        {
            highlightBorderObject.SetActive(false);
        }
        else
        {
             Debug.LogWarning($"Carta '{gameObject.name}': No se asignó 'highlightBorderObject' en el Inspector. El resaltado no funcionará con borde.");
        }
    }

    private void Start()
    {
        zoomManager = FindFirstObjectByType<CardZoomManager>();
        if (zoomManager == null) Debug.LogError("¡ERROR CRÍTICO! No se encontró CardZoomManager.");
        UpdateCardUI();
    }

    // Método para establecer datos
     public void SetCardData(ScriptableCard newCardData)
    {
        cardData = newCardData;
        UpdateCardUI();
    }

    // Método para actualizar UI
    private void UpdateCardUI() {
         if (cardData != null && cardUI != null) {
             cardUI.Setup(cardData);
         }
    }

    // --- OnPointerClick (Como en la respuesta #77, diferencia Izq/Der) ---
    public void OnPointerClick(PointerEventData eventData)
    {
        // --- SI ES CLIC IZQUIERDO: Seleccionar / Deseleccionar ---
        if (eventData.button == PointerEventData.InputButton.Left)
        {

            if (transform.parent.name != "HandArea") return;
            Debug.Log($"Clic Izquierdo en: {cardData?.NombreCarta}");
            if (GameManager.Instance == null) { Debug.LogError("GameManager.Instance es NULL!"); return; }

            if (GameManager.Instance.selectedCardObject == this.gameObject) {
                GameManager.Instance.selectedCardObject = null;
                ToggleHighlight(false); // Apagar resaltado
                Debug.Log($"Carta deseleccionada: {cardData?.NombreCarta}");
            } else {
                if (GameManager.Instance.selectedCardObject != null) {
                    Card previouslySelectedCard = GameManager.Instance.selectedCardObject.GetComponent<Card>();
                    if (previouslySelectedCard != null) {
                        previouslySelectedCard.ToggleHighlight(false);
                    }
                }
                GameManager.Instance.selectedCardObject = this.gameObject;
                ToggleHighlight(true); // Encender resaltado
                Debug.Log($"Carta seleccionada: {cardData?.NombreCarta}");

                if (transform.parent.name != "HandArea") // O el nombre real de tu zona de mano
{
    Debug.Log("Carta ya jugada, no puede seleccionarse.");
    return;
}
            }
        }
        // --- SI ES CLIC DERECHO: Mostrar Zoom ---
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            Debug.Log($"Clic Derecho en: {cardData?.NombreCarta}. Intentando Zoom...");
            if (zoomManager != null && cardData != null) {
                 zoomManager.ShowCard(cardData);
            } else { /* ... Logs de error ... */ }
        }
    }

    // --- MÉTODO ToggleHighlight MODIFICADO ---
    // Ahora activa/desactiva el GameObject 'HighlightBorder'
    public void ToggleHighlight(bool highlightOn)
    {
        if (highlightBorderObject != null)
        {
            // Activa o desactiva el GameObject del borde
            highlightBorderObject.SetActive(highlightOn);
            // Debug.Log($"Highlight {highlightOn} para {cardData?.NombreCarta}"); // Log opcional
        }
        else
        {
            // Si no hay borde asignado, no hace nada o puedes poner un fallback
             Debug.LogWarning($"Intentando activar/desactivar highlight, pero 'highlightBorderObject' no está asignado en {gameObject.name}");
            // Fallback: Cambiar color de imagen raíz (si lo prefieres)
            // var image = GetComponent<Image>();
            // if (image != null) { image.color = highlightOn ? Color.yellow : Color.white; }
        }
    }
}
