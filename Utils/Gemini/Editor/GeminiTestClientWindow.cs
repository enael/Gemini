using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;

public class GeminiTestClientWindow : EditorWindow
{
    private string promptText = "Décrivez en une courte phrase l'utilité du EditorApplication.update pour un développeur Unity.";
    private string responseText = "En attente de la requête...";
    private Vector2 scrollPosition;
    private bool isSending = false;

    // Crée le chemin de menu : Tools > Gemini > Tester Client
    [MenuItem("Tools/Gemini/Tester Client")]
    public static void ShowWindow()
    {
        GetWindow<GeminiTestClientWindow>("Test Gemini Client");
    }

    private void OnGUI()
    {
        GUILayout.Label("Interface de Test Gemini (Communication Fichiers)", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // --- 1. Zone de Prompt ---
        GUILayout.Label("Prompt à envoyer :", EditorStyles.miniLabel);
        GUI.enabled = !isSending; // Désactive la zone de texte pendant l'envoi
        promptText = EditorGUILayout.TextArea(promptText, GUILayout.Height(60));
        GUI.enabled = true;

        GUILayout.Space(10);

        // --- 2. Bouton d'Envoi ---
        // Le bouton est actif si on n'envoie pas déjà et que le prompt n'est pas vide.
        GUI.enabled = !isSending && !string.IsNullOrEmpty(promptText);
        string buttonText = isSending ? "Envoi en cours..." : "Envoyer à Gemini (Async)";

        if (GUILayout.Button(buttonText))
        {
            // Lance la méthode asynchrone pour éviter de bloquer le thread de l'Editor
            SendPromptAsync(promptText);
        }
        GUI.enabled = true;

        // --- Indicateur de statut ---
        if (isSending)
        {
            EditorGUILayout.HelpBox("Requête en cours. La routine de fond (Manager) surveille les fichiers de réponse.", MessageType.Info);
        }

        GUILayout.Space(15);

        // --- 3. Zone de Réponse (ScrollView) ---
        GUILayout.Label("Réponse de Gemini :", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));

        // Affiche la réponse dans une zone de texte non éditable
        GUI.enabled = false;
        responseText = EditorGUILayout.TextArea(responseText, GUILayout.ExpandHeight(true));
        GUI.enabled = true;

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Gère l'appel asynchrone à la méthode SendMessage du GeminiManager.
    /// </summary>
    private async void SendPromptAsync(string prompt)
    {
        // Préparation et marquage du statut
        responseText = "Envoi...";
        isSending = true;
        Repaint(); // Force le rafraîchissement immédiat de la fenêtre

        try
        {
            // Appel de la méthode statique du Manager.
            // Le 'await' met en pause cette fonction SANS bloquer l'Editor.
            string response = await GeminiManager.SendMessage(prompt);

            // Met à jour la réponse une fois le résultat reçu par la routine de fond
            responseText = response;
        }
        catch (System.Exception ex)
        {
            responseText = $"ERREUR CRITIQUE DANS L'EDITOR: {ex.Message}";
            Debug.LogError($"Erreur lors de l'envoi du prompt: {ex}");
        }
        finally
        {
            isSending = false;
            Repaint(); // Réactive le bouton
        }
    }
}