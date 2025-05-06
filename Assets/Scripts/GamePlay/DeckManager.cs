using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// --- Clases/Structs de Ejemplo (Asegúrate de tener las tuyas) ---
// [System.Serializable] public class CardCollection : ScriptableObject { public List<ScriptableCard> CardsInCollection; }
// [System.Serializable] public class ScriptableCard : ScriptableObject { public string NombreCarta; public int CostoPoderPolitico; /* ... etc */ public Sprite Ilustracion;}
// public class Card : MonoBehaviour {
//    public ScriptableCard cardData;
//    public void SetCardData(ScriptableCard data) { this.cardData = data; /* ... */ }
//}
// --------------------------------------------------------------

public class DeckManager : MonoBehaviour
{
    // --- Singleton ---
    public static DeckManager Instance { get; private set; }

    [Header("Mazo Base")]
    [Tooltip("El asset ScriptableObject que contiene la lista de cartas del mazo inicial.")]
    public CardCollection mazoBase; // ¡Asignar en Inspector!

    [Header("Configuración del Juego")]
    [Tooltip("Cuántas cartas se reparten al inicio.")]
    public int startingHandSize = 5;
    [Tooltip("Cuánto tarda (en segundos) la animación de una carta al repartirse.")]
    public float dealAnimationDuration = 0.4f; // ¡Ajusta para velocidad! (0.4 - 0.5)

    [Header("Referencias Visuales")]
    [Tooltip("El prefab de la carta a instanciar.")]
    public GameObject cardPrefab;        // ¡Asignar Prefab en Inspector!
    [Tooltip("El objeto Transform que actúa como contenedor y centro de la mano.")]
    public Transform playerHandArea;     // ¡Asignar Contenedor de Mano ('My Hands') en Inspector!
    [Tooltip("El objeto Transform que marca de dónde salen las cartas al repartir.")]
    public Transform deckPositionTransform; // ¡Asignar Posición del Mazo en Inspector!

    [Header("Hand Layout Manual")]
    [Tooltip("Escala final de las cartas en la mano (e.g., 0.8 = 80%)")]
    public float cardScaleInHand = 0.8f; // ¡Ajusta para tamaño!
    [Tooltip("Espacio horizontal entre los centros de las cartas en la mano.")]
    public float cardSpacingOnHand = 300f; // ¡AJUSTA ESTE VALOR! (Prueba 200, 300, 350...)

    // --- Listas Internas ---
    // Usaremos mazoJugador para la lista lógica en juego, como en tu script base
    [HideInInspector] public List<ScriptableCard> mazoJugador = new List<ScriptableCard>();
    // Listas adicionales necesarias
    private List<ScriptableCard> playerHand = new List<ScriptableCard>(); // Mano lógica
    private List<GameObject> handCardObjects = new List<GameObject>(); // Referencia a GameObjects en mano
    // private List<ScriptableCard> playerDiscard = new List<ScriptableCard>(); // Para el futuro

    private System.Random rng = new System.Random();

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }
        if (mazoBase == null) Debug.LogError("¡DeckManager: Falta Mazo Base!");
        if (cardPrefab == null) Debug.LogError("¡DeckManager: Falta Card Prefab!");
        if (playerHandArea == null) Debug.LogError("¡DeckManager: Falta Player Hand Area!");
        if (deckPositionTransform == null) Debug.LogError("¡DeckManager: Falta Deck Position Transform!");
        CrearMazos(); // Crear las listas lógicas al inicio
    }

    // --- Métodos Públicos ---
    public IEnumerator SetupPlayerDeckAndDealInitialHand()
    {
        if (mazoJugador == null || mazoJugador.Count == 0) {
            Debug.LogError("SetupPlayerDeck: mazoJugador vacío."); yield break;
        }
        ShuffleList(mazoJugador);
        Debug.Log($"Mazo del jugador barajado: {mazoJugador.Count} cartas.");
        yield return StartCoroutine(DealInitialHand(startingHandSize));
        Debug.Log("Setup del jugador completo.");
    }

    public void PlayerDrawCard() { StartCoroutine(DrawCardAndArrange()); }
    public void UpdateHandLayout() { ArrangeHandVisuals(true); }

    // --- Métodos Internos ---
    public void CrearMazos() { /* ... (Tu método original para llenar mazoJugador) ... */
         mazoJugador.Clear(); playerHand.Clear(); handCardObjects.Clear(); // Limpiar todo
         if (mazoBase == null || mazoBase.CardsInCollection == null || mazoBase.CardsInCollection.Count == 0) {
             Debug.LogError("❌ El mazo base es nulo o está vacío."); return;
         }
         mazoJugador = new List<ScriptableCard>(mazoBase.CardsInCollection);
         Debug.Log($"✅ Mazo lógico del jugador creado con {mazoJugador.Count} cartas.");
    }
    void ShuffleList<T>(List<T> list) { /* ... (Algoritmo Fisher-Yates) ... */
        int n = list.Count;
        while (n > 1) {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    IEnumerator DealInitialHand(int amount)
    {
        Debug.Log($"Repartiendo mano inicial de {amount} cartas...");
        handCardObjects.Clear(); playerHand.Clear();
        int cardsToDeal = Mathf.Min(amount, mazoJugador.Count);
        List<Coroutine> dealingCoroutines = new List<Coroutine>();
        for (int i = 0; i < cardsToDeal; i++) {
           dealingCoroutines.Add(StartCoroutine(DrawCardCoroutine(i, cardsToDeal)));
           yield return new WaitForSeconds(0.1f); // Pausa entre inicios
        }
        Debug.Log("Esperando fin de animaciones de reparto...");
        foreach(var coro in dealingCoroutines) { yield return coro; }
        Debug.Log("Animaciones de reparto terminadas.");
        ArrangeHandVisuals(false); // Ajuste final SIN animación
        Debug.Log($"Mano inicial repartida con {playerHand.Count} cartas.");
    }

    private IEnumerator DrawCardCoroutine(int cardIndexInHand, int totalCardsInHand)
    {
        if (mazoJugador.Count == 0) { Debug.LogWarning("No quedan cartas."); yield break; }
        ScriptableCard cardData = mazoJugador[0];
        mazoJugador.RemoveAt(0);
        playerHand.Add(cardData);
        if (cardPrefab == null || playerHandArea == null || deckPositionTransform == null) yield break;

        GameObject newCardObject = Instantiate(cardPrefab, deckPositionTransform.position, deckPositionTransform.rotation);
        if (cardData != null) newCardObject.name = cardData.NombreCarta ?? "CartaSinNombre";

        Card cardComponent = newCardObject.GetComponent<Card>();
        if (cardComponent != null) cardComponent.SetCardData(cardData);
        else Debug.LogError($"Prefab '{cardPrefab.name}' no tiene script 'Card'!");

        newCardObject.transform.SetParent(playerHandArea, true); // Mantener pos mundial inicial
        newCardObject.transform.localScale = Vector3.one;      // Escala inicial del prefab
        handCardObjects.Add(newCardObject);

        Vector3 targetPosition = CalculateCardPositionInHand(cardIndexInHand, totalCardsInHand);
        Quaternion targetRotation = CalculateCardRotationInHand(cardIndexInHand, totalCardsInHand);
        Vector3 targetScale = Vector3.one * cardScaleInHand;

        yield return StartCoroutine(AnimateCardMovement(newCardObject, targetPosition, targetRotation, targetScale, dealAnimationDuration));
    }

     private IEnumerator DrawCardAndArrange() {
         int currentIndex = playerHand.Count;
         int finalCount = currentIndex + 1;
         yield return StartCoroutine(DrawCardCoroutine(currentIndex, finalCount));
         ArrangeHandVisuals(true);
     }

    IEnumerator AnimateCardMovement(GameObject cardObject, Vector3 targetPosition, Quaternion targetRotation, Vector3 targetScale, float duration)
    {
        // ... (Código de AnimateCardMovement - SIN CAMBIOS) ...
        if (cardObject == null || duration <= 0) { /*...*/ yield break; }
        Vector3 startPosition = cardObject.transform.position; Quaternion startRotation = cardObject.transform.rotation; Vector3 startScale = cardObject.transform.localScale; float elapsedTime = 0f;
        while (elapsedTime < duration) {
            if (cardObject == null) yield break; float t = elapsedTime / duration; t = t*t*(3f-2f*t);
            cardObject.transform.position = Vector3.LerpUnclamped(startPosition, targetPosition, t);
            cardObject.transform.rotation = Quaternion.LerpUnclamped(startRotation, targetRotation, t);
            cardObject.transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, t);
            elapsedTime += Time.deltaTime; yield return null;
        }
        if (cardObject != null) { cardObject.transform.position = targetPosition; cardObject.transform.rotation = targetRotation; cardObject.transform.localScale = targetScale;}
    }

    // Calcula la posición MUNDIAL para línea recta
    Vector3 CalculateCardPositionInHand(int cardIndex, int totalCards)
    {
         if (playerHandArea == null) return Vector3.zero;
         if (totalCards == 0) return playerHandArea.position;
         float totalWidth = (totalCards > 1) ? (totalCards - 1) * cardSpacingOnHand : 0;
         float startX = -totalWidth / 2f;
         float cardX = startX + cardIndex * cardSpacingOnHand;
         float cardY = 0f; float cardZ = -cardIndex * 0.01f; // Z-offset ligero
         Vector3 localPos = new Vector3(cardX, cardY, cardZ);
         return playerHandArea.TransformPoint(localPos); // Convertir a mundo
    }

     // Calcula rotación LOCAL (recta)
     Quaternion CalculateCardRotationInHand(int cardIndex, int totalCards) {
         return Quaternion.identity;
     }

    // Reorganiza TODAS las cartas visualmente
    public void ArrangeHandVisuals(bool animate = true)
    {
        int cardCount = handCardObjects.Count;
        Debug.Log($"ArrangeHandVisuals: Reorganizando {cardCount} cartas. Animar: {animate}");
        Vector3 targetScale = Vector3.one * cardScaleInHand;
        for (int i = 0; i < cardCount; i++) {
            GameObject cardObj = handCardObjects[i];
            if (cardObj == null) { /*...*/ continue; }
            Vector3 targetPosition = CalculateCardPositionInHand(i, cardCount);
            Quaternion targetRotation = CalculateCardRotationInHand(i, cardCount);
             if(cardObj.transform.parent == playerHandArea && cardObj.transform.GetSiblingIndex() != i) {
                cardObj.transform.SetSiblingIndex(i); // Asegurar orden jerarquía
             }
            if (animate) { StartCoroutine(AnimateCardMovement(cardObj, targetPosition, targetRotation, targetScale, 0.2f)); }
            else { cardObj.transform.position = targetPosition; cardObj.transform.localRotation = targetRotation; cardObj.transform.localScale = targetScale; }
        }
    }

     // Método para quitar carta (llamado por BoardPlayZone)
     public void RemoveCardFromHand(GameObject cardObjectToRemove) { /* ... (Código de la Respuesta #85) ... */
        if (cardObjectToRemove == null) return;
        Debug.Log($"Intentando eliminar '{cardObjectToRemove.name}' de la mano.");
        bool cardRemoved = false;
        if (handCardObjects.Remove(cardObjectToRemove)) { Debug.Log($"'{cardObjectToRemove.name}' eliminado de handCardObjects."); cardRemoved = true; }
        else { Debug.LogWarning($"'{cardObjectToRemove.name}' no se encontró en handCardObjects."); }
        Card cardComponent = cardObjectToRemove.GetComponent<Card>();
        if (cardComponent != null && cardComponent.cardData != null) { if (playerHand.Remove(cardComponent.cardData)) { Debug.Log($"'{cardComponent.cardData.NombreCarta}' eliminado de playerHand."); cardRemoved = true; }
            else { Debug.LogWarning($"'{cardComponent.cardData.NombreCarta}' no se encontró en playerHand."); } }
        else { Debug.LogWarning($"No se pudo obtener cardData desde '{cardObjectToRemove.name}'."); }
        if (cardRemoved) { Debug.Log($"Destruyendo GameObject '{cardObjectToRemove.name}'..."); Destroy(cardObjectToRemove); ArrangeHandVisuals(true); } // Llamar a Arrange después
        else { Debug.LogWarning($"No se pudo eliminar completamente '{cardObjectToRemove.name}'."); }
     }


    // --- Getters ---
    public List<ScriptableCard> GetPlayerHandData() => playerHand;
    public List<GameObject> GetPlayerHandObjects() => handCardObjects;

    // SyncLogicalHandOrder NO es necesario si no hay drag & drop
}