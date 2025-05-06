using UnityEngine;
using System.Collections; // Necesario para Coroutines

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameObject selectedCardObject = null;

    [Header("Jugadores")]
    public PlayerStats jugador1; // Asigna el objeto PlayerStats del Jugador 1
    public PlayerStats jugador2; // Asigna el objeto PlayerStats del Jugador 2 (o IA)

    [Header("Referencias")]
    [Tooltip("Arrastra aquí el GameObject que tiene el script DeckManager")]
    public DeckManager deckManager; // <-- ¡¡Variable AÑADIDA!! Necesitamos la referencia

    private bool turnoJugador1 = true;
    private bool gameSetupComplete = false; // Flag para controlar el inicio

    private void Awake()
    {
        // --- Singleton ---
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; } // Evita duplicados
        // --- Fin Singleton ---

        // Validar referencias asignadas en Inspector
        if (jugador1 == null) Debug.LogError("GameManager: Falta asignar Jugador 1!");
        // if (jugador2 == null) Debug.LogError("GameManager: Falta asignar Jugador 2!"); // Descomenta si tienes Jugador 2
        if (deckManager == null) Debug.LogError("GameManager: ¡¡Falta asignar DeckManager en el Inspector!!");

    }

    private void Start()
    {
        // Iniciar la secuencia de preparación del juego en lugar del turno directamente
        StartCoroutine(StartGameSequence());
    }

    // Coroutine para manejar la secuencia inicial del juego
    IEnumerator StartGameSequence()
    {
        Debug.Log("Iniciando secuencia del juego...");
        gameSetupComplete = false; // Marcar que el juego aún no está listo

        // --- Paso 1: Preparar Mazo y Repartir Mano Inicial ---
        if (deckManager != null)
        {
            // Llama a la coroutine del DeckManager y ESPERA a que termine
            yield return StartCoroutine(deckManager.SetupPlayerDeckAndDealInitialHand());
            // Aquí podrías añadir el reparto para el jugador 2 si es necesario
        }
        else
        {
            Debug.LogError("¡No se puede iniciar el juego sin DeckManager asignado en GameManager!");
            yield break; // Salir si no hay DeckManager
        }
        // --- Fin Paso 1 ---

        Debug.Log("Setup inicial completado. Iniciando primer turno.");
        gameSetupComplete = true; // Marcar que el setup terminó
        IniciarTurno(); // Ahora sí, iniciar el primer turno real
    }


    private void IniciarTurno()
    {
        // No hacer nada si el setup inicial aún no ha terminado
        if (!gameSetupComplete) return;

        PlayerStats jugadorActual = turnoJugador1 ? jugador1 : jugador2;

        if (jugadorActual != null)
        {
            Debug.Log($"Iniciando turno de {(turnoJugador1 ? "Jugador 1" : "Jugador 2")}");
            jugadorActual.IniciarTurno(); // Esto llamará a PlayerDrawCard en DeckManager
        }
        else {
             Debug.LogError($"¡Intento de iniciar turno para jugador nulo! (TurnoJugador1 = {turnoJugador1})");
        }
    }

    public void FinalizarTurno()
    {
         if (!gameSetupComplete) return; // No permitir finalizar turno durante setup

         Debug.Log($"Finalizando turno de {(turnoJugador1 ? "Jugador 1" : "Jugador 2")}");
        // Lógica de fin de turno si la hubiera...

        turnoJugador1 = !turnoJugador1;
        IniciarTurno();
    }
}
