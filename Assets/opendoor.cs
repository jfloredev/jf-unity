using UnityEngine;

// Abre la puerta automaticamente cuando el jugador se acerca y la cierra al alejarse.
// Se puede arrastrar este script sobre cualquier puerta en el Inspector, o dejar que
// AutoPuertas.cs lo asigne solo a todas las puertas de la escena.
public class opendoor : MonoBehaviour
{
    [Header("Movimiento de apertura")]
    [Tooltip("Cuanto y en que direccion se desplaza la puerta al abrirse (coordenadas locales).")]
    public Vector3 desplazamiento = new Vector3(1.5f, 0f, 0f); // Se mueve en el eje X

    [Tooltip("Velocidad de apertura/cierre en unidades por segundo.")]
    public float velocidad = 3.0f;

    [Header("Deteccion del jugador")]
    [Tooltip("Distancia (en metros) a la que la puerta empieza a abrirse.")]
    public float distanciaApertura = 3.0f;

    [Tooltip("Transform del jugador. Si se deja vacio se usa la camara principal (VR) automaticamente.")]
    public Transform jugador;

    private Vector3 posicionCerrada;
    private Vector3 posicionAbierta;
    private bool estaAbierta = false;

    void Start()
    {
        // Guardamos la posicion inicial exacta al arrancar el juego.
        posicionCerrada = transform.localPosition;
        posicionAbierta = posicionCerrada + desplazamiento;

        if (jugador == null)
            jugador = BuscarJugador();
    }

    void Update()
    {
        // Si aun no tenemos referencia al jugador, intentamos localizarlo de nuevo.
        if (jugador == null)
        {
            jugador = BuscarJugador();
            if (jugador == null) return;
        }

        // La puerta se abre si el jugador esta dentro del radio, y se cierra si se aleja.
        float distancia = Vector3.Distance(transform.position, jugador.position);
        estaAbierta = distancia <= distanciaApertura;

        Vector3 objetivo = estaAbierta ? posicionAbierta : posicionCerrada;

        // Movimiento suave hacia la posicion objetivo (abierta o cerrada).
        transform.localPosition = Vector3.MoveTowards(transform.localPosition, objetivo, velocidad * Time.deltaTime);
    }

    // Localiza al jugador: primero la camara principal (CenterEyeAnchor del OVR),
    // y si no existe, cualquier objeto con la etiqueta "Player".
    private Transform BuscarJugador()
    {
        if (Camera.main != null)
            return Camera.main.transform;

        GameObject porTag = GameObject.FindGameObjectWithTag("Player");
        if (porTag != null)
            return porTag.transform;

        return null;
    }
}
