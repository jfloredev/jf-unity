using UnityEngine;

// Abre la puerta como una puerta batiente real: gira sobre una bisagra en su borde
// y se abre hacia el lado contrario al jugador, con animacion suave.
// Funciona aunque el pivot del objeto este en el origen del mundo (modelos Revit),
// porque la rotacion se hace alrededor de un punto de bisagra calculado en el borde
// real de la malla, no alrededor del pivot.
public class opendoor : MonoBehaviour
{
    [Header("Apertura")]
    [Tooltip("Angulo maximo de apertura de la puerta, en grados.")]
    public float anguloApertura = 90f;

    [Tooltip("Duracion aproximada de la animacion en segundos. Mas alto = mas lento y suave.")]
    public float duracionApertura = 1.0f;

    [Tooltip("Invierte el sentido de giro si la puerta abre hacia el lado equivocado.")]
    public bool invertirGiro = false;

    [Header("Deteccion del jugador")]
    [Tooltip("Distancia (en unidades) a la que la puerta empieza a abrirse.")]
    public float distanciaApertura = 3.0f;

    [Tooltip("Transform del jugador. Si se deja vacio se busca la camara automaticamente.")]
    public Transform jugador;

    [Header("Diagnostico")]
    public bool debug = false;

    private Renderer[] renderers;
    private Vector3 centroReferencia; // centro de la puerta cerrada (para medir distancia)
    private Vector3 puntoBisagra;     // punto del mundo sobre el que gira la puerta
    private Vector3 bordeLibre;       // borde opuesto a la bisagra (para decidir el sentido)
    private Vector3 normalPared;      // eje que atraviesa el hueco de la puerta

    private bool estaAbierta = false;
    private bool sentidoCalculado = false;
    private float signo = 1f;         // sentido de giro (+/-)

    private float apertura = 0f;      // 0 = cerrada, 1 = abierta
    private float aperturaVel = 0f;   // velocidad interna para SmoothDamp
    private float anguloActual = 0f;  // grados ya aplicados a la puerta

    void Start()
    {
        // Mallas de la puerta (pueden estar en objetos hijos).
        renderers = GetComponentsInChildren<Renderer>(true);

        // Bounds reales de la puerta en el mundo.
        Bounds b = CalcularBounds();
        centroReferencia = b.center;

        // El lado mas ancho (en horizontal) es el ancho de la puerta; la bisagra va
        // en un extremo de ese ancho, y el eje perpendicular atraviesa el hueco.
        Vector3 ejeAncho;
        float mitadAncho;
        if (b.size.x >= b.size.z)
        {
            ejeAncho = Vector3.right;
            mitadAncho = b.extents.x;
            normalPared = Vector3.forward;
        }
        else
        {
            ejeAncho = Vector3.forward;
            mitadAncho = b.extents.z;
            normalPared = Vector3.right;
        }

        puntoBisagra = b.center - ejeAncho * mitadAncho; // un borde vertical de la puerta
        bordeLibre = b.center + ejeAncho * mitadAncho;    // el borde opuesto

        if (jugador == null)
            jugador = BuscarJugador();

        if (debug)
            Debug.Log($"[opendoor] '{name}': mallas={renderers.Length}, " +
                      $"jugador={(jugador != null ? jugador.name : "NULL")}, bisagra={puntoBisagra}");
    }

    void Update()
    {
        if (jugador == null)
        {
            jugador = BuscarJugador();
            if (jugador == null) return;
        }

        // Calcula una sola vez hacia que lado debe abrir (alejandose del jugador).
        if (!sentidoCalculado)
            CalcularSentido();

        // La puerta se abre si el jugador esta dentro del radio, medido desde el
        // centro de la puerta CERRADA (fijo, para que no oscile al abrirse).
        float distancia = Vector3.Distance(centroReferencia, jugador.position);
        bool antes = estaAbierta;
        estaAbierta = distancia <= distanciaApertura;

        if (debug && (antes != estaAbierta || Time.frameCount % 30 == 0))
            Debug.Log($"[opendoor] '{name}': distancia={distancia:F2} umbral={distanciaApertura} abierta={estaAbierta}");

        // Progreso suave 0..1 con aceleracion y desaceleracion naturales.
        float objetivo = estaAbierta ? 1f : 0f;
        apertura = Mathf.SmoothDamp(apertura, objetivo, ref aperturaVel, duracionApertura);

        // Aplica solo la diferencia de angulo de este frame, girando sobre la bisagra.
        float anguloDeseado = signo * anguloApertura * apertura;
        float delta = anguloDeseado - anguloActual;
        if (Mathf.Abs(delta) > 0.0001f)
        {
            transform.RotateAround(puntoBisagra, Vector3.up, delta);
            anguloActual = anguloDeseado;
        }
    }

    // Decide el sentido de giro para que el borde libre se aleje del jugador.
    private void CalcularSentido()
    {
        Vector3 rotado = RotarPunto(bordeLibre, puntoBisagra, Vector3.up, anguloApertura);
        float mueveHaciaNormal = Vector3.Dot(rotado - bordeLibre, normalPared);
        float jugadorEnNormal = Vector3.Dot(jugador.position - centroReferencia, normalPared);

        // Si al girar +angulo el borde libre iria hacia el mismo lado que el jugador,
        // invertimos para que abra hacia el lado contrario.
        signo = (Mathf.Sign(mueveHaciaNormal) == Mathf.Sign(jugadorEnNormal)) ? -1f : 1f;
        if (invertirGiro) signo = -signo;
        sentidoCalculado = true;
    }

    private static Vector3 RotarPunto(Vector3 punto, Vector3 pivote, Vector3 eje, float grados)
    {
        return pivote + Quaternion.AngleAxis(grados, eje) * (punto - pivote);
    }

    private Bounds CalcularBounds()
    {
        if (renderers == null || renderers.Length == 0)
            return new Bounds(transform.position, Vector3.one);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);
        return b;
    }

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
