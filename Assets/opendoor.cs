using UnityEngine;

// Abre la puerta automaticamente cuando el jugador se acerca y la cierra al alejarse,
// con una animacion suave (aceleracion/desaceleracion).
// Se puede arrastrar este script sobre cualquier puerta en el Inspector, o dejar que
// AutoPuertas.cs lo asigne solo a todas las puertas de la escena.
public class opendoor : MonoBehaviour
{
    [Header("Movimiento de apertura")]
    [Tooltip("Cuanto y en que direccion se desplaza la puerta al abrirse (coordenadas locales).")]
    public Vector3 desplazamiento = new Vector3(1.5f, 0f, 0f); // Se mueve en el eje X

    [Tooltip("Duracion aproximada de la animacion de apertura/cierre en segundos. Mas alto = mas lento y suave.")]
    public float duracionApertura = 1.0f;

    [Header("Deteccion del jugador")]
    [Tooltip("Distancia (en unidades) a la que la puerta empieza a abrirse.")]
    public float distanciaApertura = 3.0f;

    [Tooltip("Transform del jugador. Si se deja vacio se busca la camara automaticamente.")]
    public Transform jugador;

    [Header("Diagnostico")]
    [Tooltip("Muestra en la Consola la distancia al jugador y el estado de la puerta.")]
    public bool debug = false;

    private Vector3 posicionCerrada;
    private Vector3 posicionAbierta;
    private bool estaAbierta = false;
    private Renderer[] renderers;

    private float apertura = 0f;     // 0 = cerrada, 1 = abierta
    private float aperturaVel = 0f;  // velocidad interna para SmoothDamp

    void Start()
    {
        // Guardamos la posicion inicial exacta al arrancar el juego.
        posicionCerrada = transform.localPosition;
        posicionAbierta = posicionCerrada + desplazamiento;

        // Cacheamos las mallas (pueden estar en objetos hijos) para calcular el
        // centro REAL de la puerta, ya que en modelos Revit el pivot suele estar
        // en el origen del mundo y no donde se ve la puerta.
        renderers = GetComponentsInChildren<Renderer>(true);

        if (jugador == null)
            jugador = BuscarJugador();

        if (debug)
            Debug.Log($"[opendoor] '{name}': mallas encontradas={renderers.Length}, " +
                      $"jugador={(jugador != null ? jugador.name : "NULL")}");
    }

    void Update()
    {
        // Si aun no tenemos referencia al jugador, intentamos localizarlo de nuevo.
        if (jugador == null)
        {
            jugador = BuscarJugador();
            if (jugador == null) return;
        }

        // Distancia desde el centro visible de la puerta hasta el jugador.
        float distancia = Vector3.Distance(CentroPuerta(), jugador.position);
        bool antes = estaAbierta;
        estaAbierta = distancia <= distanciaApertura;

        if (debug && (antes != estaAbierta || Time.frameCount % 30 == 0))
            Debug.Log($"[opendoor] '{name}': distancia={distancia:F2} umbral={distanciaApertura} abierta={estaAbierta}");

        // Animacion suave: 'apertura' avanza hacia 1 (abierta) o 0 (cerrada) con
        // aceleracion y desaceleracion naturales gracias a SmoothDamp.
        float objetivo = estaAbierta ? 1f : 0f;
        apertura = Mathf.SmoothDamp(apertura, objetivo, ref aperturaVel, duracionApertura);

        transform.localPosition = Vector3.Lerp(posicionCerrada, posicionAbierta, apertura);
    }

    // Centro real de la puerta segun las mallas visibles (no el pivot del objeto).
    private Vector3 CentroPuerta()
    {
        if (renderers == null || renderers.Length == 0)
            return transform.position;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);
        return b.center;
    }

    // Localiza al jugador: camara principal (si esta etiquetada MainCamera), si no
    // cualquier camara de la escena (el ojo central del rig VR), y por ultimo un
    // objeto con la etiqueta "Player".
    private Transform BuscarJugador()
    {
        if (Camera.main != null)
            return Camera.main.transform;

        Camera cam = FindAnyObjectByType<Camera>();
        if (cam != null)
            return cam.transform;

        GameObject porTag = GameObject.FindGameObjectWithTag("Player");
        if (porTag != null)
            return porTag.transform;

        return null;
    }
}
