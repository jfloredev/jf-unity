using UnityEngine;

// Asigna automaticamente el componente "opendoor" a cada PUERTA INDIVIDUAL de la
// escena al iniciar el juego. En modelos Revit las puertas se agrupan por categoria
// ("Puertas (16)") y por familia ("Entrance door (2)"); esos son contenedores, no
// puertas reales. Las puertas reales son las instancias, que tienen un id entre
// corchetes en el nombre (p. ej. "Entrance door [423107]"). Por eso solo tomamos los
// objetos cuyo nombre contiene "[" ademas de una palabra clave de puerta.
public static class AutoPuertas
{
    private static readonly string[] claves = { "door", "puerta", "slider", "pocket", "flush" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ConfigurarPuertas()
    {
        Transform[] todos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        int contador = 0;
        foreach (Transform t in todos)
        {
            string n = t.name.ToLowerInvariant();

            if (!EsPuerta(n)) continue;
            if (!n.Contains("[")) continue;              // solo instancias, no grupos de Revit
            if (t.GetComponent<opendoor>() != null) continue;

            opendoor puerta = t.gameObject.AddComponent<opendoor>();

            // Las tipo "slider/pocket/corrediza" se deslizan; el resto son batientes.
            if (n.Contains("slider") || n.Contains("pocket") || n.Contains("corred"))
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
        foreach (string clave in claves)
            if (nombre.Contains(clave)) return true;
        return false;
    }
}
