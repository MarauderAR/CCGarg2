using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic; // Necesario

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(GraphicRaycaster))]
public class BoardPlayZone : MonoBehaviour, IPointerClickHandler
{
    [Header("Configuración Carta Jugada")]
    public Transform playedCardContainer;
    public float scaleOnBoard = 0.32f;   // Escala correcta
    public float rotationOnBoard = 0f;    // Rotación correcta

    [Header("Configuración de Slots en Tablero")]
    public int maxSlots = 7;
    public float cardSpacingOnBoard = 150f; // ¡AJUSTA ESTE ESPACIADO!
    public float verticalOffsetOnBoard = 0f; // Ajusta Y si es necesario

    // Lista de cartas en esta zona
    private List<GameObject> cardsOnBoard = new List<GameObject>();

    private void Awake() { /* ... (Validaciones como antes) ... */
        Image img = GetComponent<Image>(); if (img != null) { if (!img.raycastTarget) img.raycastTarget = true; } else { Debug.LogError($"BoardPlayZone necesita Image."); }
        if (playedCardContainer == null) { playedCardContainer = transform; }
        if (GetComponent<GraphicRaycaster>() == null) { Debug.LogError($"BoardPlayZone necesita Graphic Raycaster.");}
     }

    // Calcula la posición LOCAL del slot para centrar
    Vector3 CalculateBoardSlotPosition(int cardIndex, int totalCards)
    {
        if (totalCards <= 0) return Vector3.zero;
        float totalWidth = (totalCards > 1) ? (totalCards - 1) * cardSpacingOnBoard : 0;
        float startX = -totalWidth / 2f;
        float cardX = startX + cardIndex * cardSpacingOnBoard;
        float cardY = verticalOffsetOnBoard; // Usar el offset Y
        float cardZ = -cardIndex * 0.01f;
        return new Vector3(cardX, cardY, cardZ);
    }

    // Aplica el layout a TODAS las cartas en el tablero
    void ReorganizeBoardLayout()
    {
        int cardCount = cardsOnBoard.Count;
        Debug.Log($"[BoardPlayZone] Reorganizando {cardCount} cartas en tablero.");

        Vector3 targetScale = Vector3.one * scaleOnBoard;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, rotationOnBoard);

        for (int i = 0; i < cardCount; i++)
        {
            if (cardsOnBoard[i] == null) continue; // Seguridad

            // Calcular la NUEVA posición para esta carta
            Vector3 targetLocalPosition = CalculateBoardSlotPosition(i, cardCount);

            // Mover directamente (o animar si prefieres después)
            cardsOnBoard[i].GetComponent<RectTransform>().localPosition = targetLocalPosition;
            cardsOnBoard[i].transform.localScale = targetScale;
            cardsOnBoard[i].transform.localRotation = targetRotation;
            // cardsOnBoard[i].transform.SetSiblingIndex(i); // Opcional para orden jerarquía
        }
    }

    // Método para cuando una carta es destruida/quitada del tablero
    public void NotifyCardRemovedFromBoard(GameObject cardObject) {
        if(cardsOnBoard.Remove(cardObject)) {
            Debug.Log($"[BoardPlayZone] Carta {cardObject.name} quitada. Reorganizando.");
            ReorganizeBoardLayout(); // Reorganizar al quitar
        }
    }

    // --- OnPointerClick MODIFICADO ---
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        Debug.Log("BoardPlayZone: Clic Izquierdo Detectado!");

        // 1. Verificar Selección ... (código como antes)
        if (GameManager.Instance == null || GameManager.Instance.selectedCardObject == null) return;
        GameObject selectedCardGO = GameManager.Instance.selectedCardObject;
        Card cardComponent = selectedCardGO.GetComponent<Card>();
        if (cardComponent == null || cardComponent.cardData == null) return;
        ScriptableCard cardDataToPlay = cardComponent.cardData;

        // 2. Verificar Espacio ... (código como antes)
        if (cardsOnBoard.Count >= maxSlots) { Debug.LogWarning("BoardPlayZone: No hay slots."); return; }

        // 3. Verificar Coste ... (código como antes)
        PlayerStats currentPlayerStats = GameManager.Instance.jugador1;
        if (currentPlayerStats == null || !currentPlayerStats.PuedePagar(cardDataToPlay.CostoPoderPolitico)) { /* ... manejar no poder pagar ... */ return; }

        // --- SI TODO OK, JUGAR ---
        Debug.Log("BoardPlayZone: Jugando carta...");
        currentPlayerStats.Pagar(cardDataToPlay.CostoPoderPolitico);

        // 4. Instanciar la carta JUGADA
        if (DeckManager.Instance != null && DeckManager.Instance.cardPrefab != null)
        {
            GameObject playedCard = Instantiate(DeckManager.Instance.cardPrefab, playedCardContainer);
            playedCard.name = cardDataToPlay.NombreCarta + " (Jugada)";

            // Configurar datos y estado inicial (escala/rotación)
            Card playedCardComponent = playedCard.GetComponent<Card>();
            if(playedCardComponent != null) { playedCardComponent.SetCardData(cardDataToPlay); /* ... desactivar scripts ... */ }
            playedCard.transform.localScale = Vector3.one * scaleOnBoard;
            playedCard.transform.localRotation = Quaternion.Euler(0f, 0f, rotationOnBoard);

            // --- AÑADIR A LA LISTA ---
            cardsOnBoard.Add(playedCard);
            // --- FIN AÑADIR ---

            // --- LLAMAR A REORGANIZAR ---
            // ¡Esto colocará la nueva carta y reajustará las anteriores!
            ReorganizeBoardLayout();
            // ---------------------------

            Debug.Log($"Carta {cardDataToPlay.NombreCarta} añadida al tablero. Total: {cardsOnBoard.Count}");

        } else { Debug.LogError("BoardPlayZone: No se puede instanciar!"); }

        // 5. Quitar carta ORIGINAL de la mano (Como antes)
        if (DeckManager.Instance != null) { DeckManager.Instance.RemoveCardFromHand(selectedCardGO); }

        // 6. Deseleccionar (Como antes)
        GameManager.Instance.selectedCardObject = null;
    }
}