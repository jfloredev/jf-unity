using UnityEngine;

// Asigna automaticamente el componente "opendoor" a todas las puertas de la escena
// (objetos cuyo nombre contiene "door" o "puerta") al iniciar el juego, sin tener
// que configurarlas una por una en el Inspector.
public static class AutoPuertas
{
    // Palabras clave que identifican a una puerta por su nombre.
    private static readonly string[] claves = { "door", "puerta" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ConfigurarPuertas()
    {
        Transform[] todos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        int contador = 0;
        foreach (Transform t in todos)
        {
            if (!EsPuerta(t.name)) continue;
            if (t.GetComponent<opendoor>() != null) continue;

            // Solo se asigna al objeto mas alto de la jerarquia: si un padre tambien
            // es puerta, lo saltamos para no mover partes anidadas por duplicado.
            if (PadreEsPuerta(t)) continue;

            opendoor puerta = t.gameObject.AddComponent<opendoor>();

            // Las puertas tipo "slider/corrediza" se deslizan; el resto son batientes.
            string n = t.name.ToLowerInvariant();
            if (n.Contains("slider") || n.Contains("corred") || n.Contains("pocket"))
                puerta.modo = opendoor.ModoPuerta.Corrediza;
            else
                puerta.modo = opendoor.ModoPuerta.Batiente;

            contador++;
            Debug.Log($"[AutoPuertas] Puerta configurada: '{t.name}' -> {puerta.modo}");
        }

        Debug.Log($"[AutoPuertas] Total de puertas configuradas: {contador}");
    }

    private static bool EsPuerta(string nombre)
    {
        string n = nombre.ToLowerInvariant();
        foreach (string clave in claves)
            if (n.Contains(clave)) return true;
        return false;
    }

    private static bool PadreEsPuerta(Transform t)
    {
        for (Transform p = t.parent; p != null; p = p.parent)
            if (EsPuerta(p.name)) return true;
        return false;
    }
}
