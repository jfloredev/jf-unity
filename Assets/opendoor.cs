using UnityEngine;

public class opendoor : MonoBehaviour
{
    public Vector3 desplazamiento = new Vector3(1.5f, 0f, 0f); // Se moverá en el eje X
    public float velocidad = 3.0f;

    private Vector3 posicionCerrada;
    private Vector3 posicionObjetivo;
    private bool estaAbierta = false;

    void Start()
    {
        // Guardamos la posición inicial exacta al arrancar el juego
        posicionCerrada = transform.localPosition;
        posicionObjetivo = posicionCerrada;
    }

    void Update()
    {
        // Al pulsar la tecla E cambiamos el destino objetivo
        if (Input.GetKeyDown(KeyCode.E))
        {
            estaAbierta = !estaAbierta;
            posicionObjetivo = estaAbierta ? posicionCerrada + desplazamiento : posicionCerrada;
        }

        // Mueve el objeto suavemente hacia la posición objetivo fijada
        transform.localPosition = Vector3.MoveTowards(transform.localPosition, posicionObjetivo, velocidad * Time.deltaTime);
    }
}