// Fichier: AIEditorWindow.cs

using UnityEngine;
using UnityEditor;
using System.Collections;
using System;
using IACoding;
// 🎯 AJOUT: Importation de IAMain pour l'énumération AIMode
using static IACoding.IAMain;

public class AIEditorWindow : EditorWindow
{
    private static IAMain iaMain;
    private string userInput = "{\n  \"commands\": [\n    {\n      \"functionName\": \"CreateDirectory\",\n      \"arguments\": \"{\\\"directoryPath\\\": \\\"Test/DemoIA\\\"}\"\n    }\n  ]\n}"; // JSON d'exemple
    private string aiResponse = "En attente de connexion...";

    // Position de défilement pour la zone d'entrée
    private Vector2 inputScrollPosition;
    // Position de défilement pour la zone de réponse
    private Vector2 responseScrollPosition;

    // 🎯 REMPLACEMENT: Utilisation de AIMode au lieu de booléens séparés
    private AIMode currentMode = AIMode.Coding;

    private bool isConnected = false;

    // Styles
    private GUIStyle statusStyle;
    private const float STATUS_SIZE = 14f;

    // Contrôle de la police de caractère
    private readonly int[] possibleFontSizes = { 12, 13, 14, 15, 16, 17, 18, 19, 20 };
    private int currentFontSize = 15;
    private GUIStyle inputTextStyle;
    private GUIStyle responseTextStyle;

    [MenuItem("Tools/IA/AI Gemini Assistant")]
    public static void ShowWindow()
    {
        GetWindow<AIEditorWindow>("Gemini Assistant");
    }

    private void OnEnable()
    {
        UpdateConnection();
        EditorApplication.update += EditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= EditorUpdate;
        DisconnectService();
    }

    private void EditorUpdate()
    {
        // Pas de logique de mise à jour constante nécessaire ici
    }

    private void InitStyles()
    {
        if (statusStyle == null)
        {
            statusStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fixedWidth = STATUS_SIZE,
                fixedHeight = STATUS_SIZE,
                alignment = TextAnchor.MiddleCenter
            };
        }

        // Style pour la zone d'entrée
        if (inputTextStyle == null || inputTextStyle.fontSize != currentFontSize)
        {
            inputTextStyle = new GUIStyle(EditorStyles.textArea)
            {
                fontSize = currentFontSize,
                wordWrap = true
            };
        }

        // Style pour la zone de réponse - Doit ressembler à un TextArea pour la sélection
        if (responseTextStyle == null || responseTextStyle.fontSize != currentFontSize)
        {
            // Utiliser EditorStyles.textArea pour la réponse pour garantir la sélection et le look
            responseTextStyle = new GUIStyle(EditorStyles.textArea)
            {
                fontSize = currentFontSize,
                wordWrap = true
            };
        }
    }

    private void OnGUI()
    {
        InitStyles();

        // --- 1. Titre et Rond de Statut ---
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("🤖 Assistant IA Unity", EditorStyles.boldLabel);

            DrawConnectionStatus();

            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.Separator();

        // --- Contrôle de la taille de police ---
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Taille Police :", EditorStyles.miniLabel, GUILayout.Width(80));

            int newSize = EditorGUILayout.IntPopup(
                currentFontSize,
                Array.ConvertAll(possibleFontSizes, x => x.ToString()),
                possibleFontSizes,
                GUILayout.Width(50)
            );

            if (newSize != currentFontSize)
            {
                currentFontSize = newSize;
                InitStyles();
                Repaint();
            }
        }

        EditorGUILayout.Separator();

        // ------------------------------------------------------------------
        // 2. Sélection du Mode (Chatting, Coding, Simulation)
        // ------------------------------------------------------------------

        DrawModeSelection();

        EditorGUILayout.Separator();

        // ------------------------------------------------------------------
        // 3. Contrôles de Connexion (CONDITIONNEL)
        // ------------------------------------------------------------------

        // DÉSACTIVE les contrôles d'API si nous sommes en mode simulation
        GUI.enabled = (currentMode != AIMode.Simulation);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Connecter / Reconfigurer"))
            {
                // Reconnexion ou reconfiguration (le mode est pris en compte ici)
                UpdateConnection();
            }
            if (GUILayout.Button("Déconnecter"))
            {
                DisconnectService();
            }
        }

        // Rétablir GUI.enabled
        GUI.enabled = true;

        EditorGUILayout.Separator();

        // --- 4. Zone d'Entrée Utilisateur (AVEC SCROLLVIEW) ---

        GUILayout.Label("Entrée Utilisateur (Requête) :", EditorStyles.boldLabel);

        // Début de la ScrollView pour l'entrée utilisateur
        inputScrollPosition = EditorGUILayout.BeginScrollView(inputScrollPosition, GUILayout.Height(150));

        userInput = EditorGUILayout.TextArea(
            userInput,
            inputTextStyle,
            GUILayout.ExpandHeight(true)
        );

        // Fin de la ScrollView pour l'entrée utilisateur
        EditorGUILayout.EndScrollView();

        // --- 5. Bouton d'Envoi ---

        // Le bouton est activé si connecté OU en mode simulation
        GUI.enabled = (iaMain != null && isConnected) || (currentMode == AIMode.Simulation);

        string buttonText;
        if (currentMode == AIMode.Simulation)
        {
            buttonText = "➡️ Envoyer au Parseur (Simulation)";
        }
        else if (!isConnected)
        {
            buttonText = "➡️ Envoyer (Non Connecté)";
            GUI.enabled = false;
        }
        else
        {
            buttonText = "➡️ Envoyer à l'IA (Requête API)";
        }

        if (GUILayout.Button(buttonText))
        {
            if (currentMode == AIMode.Simulation)
            {
                SendToParserInSimulation();
            }
            else
            {
                SendRequestInEditor();
            }
        }
        GUI.enabled = true;

        EditorGUILayout.Separator();

        // --- 6. Zone de Réponse de l'IA (AVEC SÉLECTION, SCROLLVIEW et BOUTON COPIER) ---

        GUILayout.Label("Réponse de l'IA / Logs :", EditorStyles.boldLabel);

        // Début du ScrollView pour la zone de réponse
        responseScrollPosition = EditorGUILayout.BeginScrollView(responseScrollPosition, GUILayout.Height(350));

        // UTILISATION DE TEXTAREA pour permettre la sélection (lecture seule)
        EditorGUILayout.TextArea(
            aiResponse,
            responseTextStyle,
            GUILayout.ExpandHeight(true)
        );

        // Fin du ScrollView pour la zone de réponse
        EditorGUILayout.EndScrollView();

        // BOUTON COPIER AJOUTÉ
        if (GUILayout.Button("📋 Copier la Réponse (Clipboard)", GUILayout.Height(25)))
        {
            GUIUtility.systemCopyBuffer = aiResponse;
        }

        EditorGUILayout.Space(5);
    }

    // Dessine le sélecteur de mode
    private void DrawModeSelection()
    {
        EditorGUILayout.LabelField("Choix du Mode IA :", EditorStyles.boldLabel);

        // Création des labels pour les boutons
        GUIContent[] modeContents = new GUIContent[]
        {
            new GUIContent("💬 Chatting (Sans Outils)"),
            new GUIContent("🧑‍💻 Coding (Outils JSON)"),
            new GUIContent("🧪 Simulation (Local)")
        };

        // Conversion du mode actuel en index pour l'affichage (0, 1, 2)
        int selectedIndex = (int)currentMode;

        // Affichage des boutons de sélection de mode (toolbar)
        int newIndex = GUILayout.Toolbar(selectedIndex, modeContents, EditorStyles.toolbarButton);

        // Si le mode a changé
        if (newIndex != selectedIndex)
        {
            currentMode = (AIMode)newIndex;

            // Si on passe en mode Simulation, ou en sort, on reconfigure si connecté
            if (isConnected)
            {
                UpdateConnection();
            }

            // Mise à jour de la réponse pour clarifier le mode
            aiResponse = $"Mode changé: {currentMode}. Cliquez sur 'Connecter / Reconfigurer' si vous êtes connecté pour appliquer le mode.";

            Repaint();
        }
    }

    // Dessine l'indicateur de statut (carré coloré)
    private void DrawConnectionStatus()
    {
        Color color = isConnected ? Color.green : Color.red;
        string statusText = isConnected ? "Connecté à l'API Gemini." : "Déconnecté ou Erreur de connexion.";

        Rect rect = GUILayoutUtility.GetRect(STATUS_SIZE, STATUS_SIZE, GUILayout.Width(STATUS_SIZE), GUILayout.Height(STATUS_SIZE));

        EditorGUI.DrawRect(rect, color);

        EditorGUI.LabelField(rect, new GUIContent("", statusText));
    }

    // -----------------------------------------------------------

    // 6. LOGIQUE D'INITIALISATION ET D'ENVOI

    // -----------------------------------------------------------


    private void UpdateConnection()
    {
        if (iaMain == null)
        {
            iaMain = FindObjectOfType<IAMain>();
        }

        if (iaMain == null)
        {
            aiResponse = "ERREUR: Le composant IAMain n'est pas présent dans la scène. Veuillez l'ajouter à un GameObject.";
            isConnected = false;
            return;
        }

        iaMain.onConnectionStatusChanged = OnConnectionStatusChanged;

        // 🎯 MODIFICATION: Passe le AIMode au lieu du booléen
        iaMain.SetupForEditor(
            currentMode,
            OnAIResponseReceived
        );

        aiResponse = $"Service IAGemini tenté de connexion ou de reconfiguration en mode {currentMode}. Vérification en cours...";
    }

    // NOUVELLE MÉTHODE : Injection du message dans le pipeline (Simulation)
    private void SendToParserInSimulation()
    {
        if (iaMain == null)
        {
            aiResponse = "Erreur: Le contrôleur IAMain n'est pas initialisé.";
            return;
        }

        string simulationResponse = userInput;

        aiResponse = $"➡️ **SIMULATION EN COURS** ({currentMode}): Envoi de '{userInput}' directement au Parseur...";
        Repaint();

        // 🎯 MODIFICATION: Passe le AIMode au lieu du booléen
        iaMain.SimulateGeminiResponse(simulationResponse, currentMode);
    }

    private void OnConnectionStatusChanged(bool status)
    {
        isConnected = status;

        Repaint();

        string statusText = isConnected ? "✅ Connexion établie à Gemini." : "❌ Déconnecté.";
        aiResponse = statusText;
    }

    private void DisconnectService()
    {
        if (iaMain != null)
        {
            iaMain.Disconnect();
        }
        aiResponse = "Déconnexion demandée.";
        isConnected = false;
        Repaint();
    }

    private void SendRequestInEditor()
    {
        if (iaMain == null)
        {
            aiResponse = "Erreur: Le contrôleur IAMain n'est pas initialisé.";
            return;
        }

        if (!isConnected)
        {
            aiResponse = "Erreur: Non connecté. Veuillez cliquer sur 'Connecter/Reconfigurer'.";
            return;
        }

        aiResponse = $"➡️ **Envoi de la requête en cours ({currentMode})...** (Veuillez patienter)";

        // 🎯 MODIFICATION: Passe le AIMode au lieu du booléen
        iaMain.SendMessageToAI(userInput, currentMode);

        Repaint();
    }

    private void OnAIResponseReceived(string response)
    {
        aiResponse = response;

        if (!EditorApplication.isPlaying)
        {
            // Assure la mise à jour de l'interface en dehors du mode Play
            EditorApplication.delayCall += Repaint;
        }
    }
}