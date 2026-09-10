using UnityEngine;

// Abre la puerta automaticamente cuando el jugador se acerca, con animacion suave.
// Soporta dos tipos de movimiento realista:
//   - Batiente: gira sobre una bisagra en su borde (como puerta de entrada).
//   - Corrediza: se desliza hacia un costado (como puerta tipo "pocket slider").
// Funciona aunque el pivot del objeto este en el origen del mundo (modelos Revit),
// porque tanto el giro como el deslizamiento se calculan con los bounds reales de la
// malla y se aplican en espacio de mundo, no respecto al pivot.
public class opendoor : MonoBehaviour
{
    public enum ModoPuerta { Batiente, Corrediza }

    [Header("Tipo de movimiento")]
    public ModoPuerta modo = ModoPuerta.Batiente;

    [Tooltip("Duracion aproximada de la animacion en segundos. Mas alto = mas lento y suave.")]
    public float duracionApertura = 1.0f;

    [Header("Batiente (giro)")]
    [Tooltip("Angulo maximo de apertura en grados.")]
    public float anguloApertura = 90f;

    [Header("Corrediza (deslizar)")]
    [Tooltip("Cuanto se desliza, como fraccion del ancho de la puerta (1 = un ancho completo).")]
    public float factorDeslizamiento = 0.95f;

    [Header("Ajuste manual")]
    [Tooltip("Voltea el lado de la bisagra (batiente) o el sentido del deslizamiento (corrediza).")]
    public bool invertir = false;

    [Tooltip("Desactiva la colision de la puerta mientras esta abierta para poder cruzar.")]
    public bool atravesarAlAbrir = true;

    [Header("Deteccion del jugador")]
    [Tooltip("Distancia (en unidades) a la que la puerta empieza a abrirse.")]
    public float distanciaApertura = 3.0f;

    [Tooltip("Transform del jugador. Si se deja vacio se busca la camara automaticamente.")]
    public Transform jugador;

    [Header("Diagnostico")]
    public bool debug = true;

    private Renderer[] renderers;
    private Collider[] colliders;     // colisiones de la puerta (se apagan al abrir)
    private bool colisionActiva = true;
    private Vector3 centroReferencia; // centro de la puerta cerrada (para medir distancia)
    private Vector3 ejeAncho;         // eje horizontal a lo largo del ancho de la puerta
    private Vector3 normalPared;      // eje que atraviesa el hueco de la puerta
    private float anchoPuerta;        // ancho de la puerta (unidades de mundo)

    private Vector3 puntoBisagra;     // punto del mundo sobre el que gira (batiente)
    private Vector3 bordeLibre;       // borde opuesto a la bisagra (batiente)

    private bool estaAbierta = false;
    private float signo = 1f;         // sentido de giro/deslizamiento, se recalcula al abrir

    private float apertura = 0f;      // 0 = cerrada, 1 = abierta
    private float aperturaVel = 0f;   // velocidad interna para SmoothDamp
    private float anguloActual = 0f;  // grados ya aplicados (batiente)
    private float distActual = 0f;    // desplazamiento ya aplicado (corrediza)

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);

        Bounds b = CalcularBounds();
        centroReferencia = b.center;

        // El lado horizontal mas ancho es el ancho de la puerta.
        if (b.size.x >= b.size.z)
        {
            ejeAncho = Vector3.right;
            anchoPuerta = b.size.x;
            normalPared = Vector3.forward;
        }
        else
        {
            ejeAncho = Vector3.forward;
            anchoPuerta = b.size.z;
            normalPared = Vector3.right;
        }

        float mitad = anchoPuerta * 0.5f;
        Vector3 bordeA = b.center - ejeAncho * mitad;
        Vector3 bordeB = b.center + ejeAncho * mitad;
        puntoBisagra = invertir ? bordeB : bordeA;
        bordeLibre = invertir ? bordeA : bordeB;

        if (jugador == null)
            jugador = BuscarJugador();

        if (debug)
            Debug.Log($"[opendoor] '{name}': modo={modo}, mallas={renderers.Length}, " +
                      $"colliders={colliders.Length}, ancho={anchoPuerta:F2}, " +
                      $"jugador={(jugador != null ? jugador.name : "NULL")}");
    }

    void Update()
    {
        if (jugador == null)
        {
            jugador = BuscarJugador();
            if (jugador == null) return;
        }

        // Distancia medida desde el centro de la puerta CERRADA (fijo).
        float distancia = Vector3.Distance(centroReferencia, jugador.position);
        bool antes = estaAbierta;
        estaAbierta = distancia <= distanciaApertura;

        // Al empezar a abrir (puerta casi cerrada) recalculamos el sentido para que
        // SIEMPRE se aleje del jugador, venga por donde venga.
        if (!antes && estaAbierta && apertura < 0.05f)
            CalcularSentido();

        if (debug && antes != estaAbierta)
            Debug.Log($"[opendoor] '{name}': distancia={distancia:F2} abierta={estaAbierta} signo={signo}");

        // Progreso suave 0..1 (acelera al abrir, frena al llegar).
        float objetivo = estaAbierta ? 1f : 0f;
        apertura = Mathf.SmoothDamp(apertura, objetivo, ref aperturaVel, duracionApertura);

        if (modo == ModoPuerta.Batiente)
            AplicarGiro();
        else
            AplicarDeslizamiento();

        // La puerta bloquea el paso solo cuando esta practicamente cerrada.
        if (atravesarAlAbrir)
            ActualizarColision(apertura < 0.1f);
    }

    private void ActualizarColision(bool bloquear)
    {
        if (bloquear == colisionActiva) return;
        colisionActiva = bloquear;
        foreach (Collider c in colliders)
            if (c != null) c.enabled = bloquear;
    }

    private void AplicarGiro()
    {
        float anguloDeseado = signo * anguloApertura * apertura;
        float delta = anguloDeseado - anguloActual;
        if (Mathf.Abs(delta) > 0.0001f)
        {
            transform.RotateAround(puntoBisagra, Vector3.up, delta);
            anguloActual = anguloDeseado;
        }
    }

    private void AplicarDeslizamiento()
    {
        float objetivoDist = signo * anchoPuerta * factorDeslizamiento * apertura;
        float delta = objetivoDist - distActual;
        if (Mathf.Abs(delta) > 0.0001f)
        {
            transform.Translate(ejeAncho * delta, Space.World);
            distActual = objetivoDist;
        }
    }

    // Sentido de apertura para alejarse del jugador.
    private void CalcularSentido()
    {
        float jugadorEnNormal = Vector3.Dot(jugador.position - centroReferencia, normalPared);

        if (modo == ModoPuerta.Batiente)
        {
            Vector3 rotado = RotarPunto(bordeLibre, puntoBisagra, Vector3.up, anguloApertura);
            float mueve = Vector3.Dot(rotado - bordeLibre, normalPared);
            signo = (Mathf.Sign(mueve) == Mathf.Sign(jugadorEnNormal)) ? -1f : 1f;
        }
        else
        {
            // La corrediza se mueve a lo largo de la pared; el sentido no afecta al
            // jugador, asi que se respeta el ajuste manual ('invertir').
            signo = invertir ? -1f : 1f;
        }
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
