using UnityEditor;
using UnityEngine;

// Doit être placé dans un dossier 'Editor'
public class GeminiRoutineControlWindow : EditorWindow
{
    // Ajoute une entrée dans le menu Tools > Gemini
    [MenuItem("Tools/Gemini/Contrôle de la Routine")]
    public static void ShowWindow()
    {
        // Crée ou affiche la fenêtre
        GetWindow<GeminiRoutineControlWindow>("Routine Gemini");
    }

    private void OnEnable()
    {
        // On s'abonne à update pour rafraîchir la fenêtre (le label de statut)
        EditorApplication.update += Repaint;
    }

    private void OnDisable()
    {
        // On se désabonne à la fermeture de la fenêtre
        EditorApplication.update -= Repaint;
    }

    private void OnGUI()
    {
        GUILayout.Label("Contrôle de la Routine Gemini", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // --- 1. Label de Statut ---
        // Utilise la propriété IsRunning du GeminiManager
        bool isRunning = GeminiManager.IsRunning;
        string statusText = isRunning ? "✅ ACTIF (Vérification 0.2s)" : "❌ INACTIF";
        MessageType statusType = isRunning ? MessageType.Info : MessageType.Warning;

        EditorGUILayout.HelpBox($"Statut : {statusText}", statusType);

        GUILayout.Space(20);

        // --- 2. Boutons de Contrôle ---

        // DÉMARRER
        GUI.enabled = !isRunning; // Bouton désactivé si la routine est déjà lancée
        if (GUILayout.Button("▶️ Démarrer la Routine"))
        {
            GeminiManager.StartRoutine();
        }

        // ARRÊTER
        GUI.enabled = isRunning; // Bouton désactivé si la routine est déjà arrêtée
        if (GUILayout.Button("⏹️ Arrêter la Routine"))
        {
            GeminiManager.StopRoutine();
        }

        GUI.enabled = true; // Restaure l'état du GUI
    }
}