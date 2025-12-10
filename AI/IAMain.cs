using UnityEngine;
using System.IO;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Script principal gérant la configuration, l'assemblage du System Prompt final
/// et l'intégration avec l'éditeur Unity. Il agit comme l'orchestrateur central.
/// </summary>
namespace IACoding
{
    public class IAMain : MonoBehaviour
    {
        // --------------------------------------------------------------------
        // DEPENDANCES CRITIQUES
        // --------------------------------------------------------------------

        [Header("Dépendances")]
        [Tooltip("Le script d'API réel ou de simulation implémentant IAInterface (à glisser-déposer).")]
        public MonoBehaviour aiServiceAsset;
        private IAInterface aiService;       // Référence typée à l'interface

        [Tooltip("Le script qui exécute les commandes (Parseur).")]
        public IAParser iaParser;


        // --------------------------------------------------------------------
        // ÉVÉNEMENTS & PROPRIÉTÉS REQUIS PAR AIEditorWindow.cs
        // --------------------------------------------------------------------

        public Action<bool> onConnectionStatusChanged;
        private Action<string> editorResponseCallback;

        // Utilisation de AIMode
        private AIMode currentMode;

        // --------------------------------------------------------------------
        // INITIALISATION & DESTRUCTION (SetupForEditor est le point d'entrée)
        // --------------------------------------------------------------------

        void Awake()
        {
            // Initialisation de IAPrompt pour garantir que son Singleton est prêt.
            // On s'assure que IAPrompt (qui gère le fichier) est opérationnel.
        }

        void OnDestroy()
        {
            // Nettoyage lors de la destruction de l'objet de la scène.
            Disconnect();
        }

        // --------------------------------------------------------------------
        // GESTION DES CALLBACKS ET DU FLUX D'IA
        // --------------------------------------------------------------------

        private void HandleStatusChange(bool isConnected)
        {
            onConnectionStatusChanged?.Invoke(isConnected);
        }

        private void HandleRawAIResponse(string rawResponse)
        {
            // La logique de codage est active si le mode n'est PAS Chatting
            bool isCodingMode = (currentMode != AIMode.Chatting);

            if (iaParser != null)
            {
                iaParser.ParseAndExecute(rawResponse, isCodingMode);
            }
            else
            {
                // Si le parseur est manquant, on renvoie la réponse brute
                editorResponseCallback?.Invoke(rawResponse);
            }
        }

        private void HandleParsedResponse(string finalResult)
        {
            editorResponseCallback?.Invoke(finalResult);
        }

        /// <summary>
        /// Gère la notification de IAPrompt lorsque le fichier SystemPrompt.txt est modifié.
        /// </summary>
        private void HandlePromptChangeNotification()
        {
            // Forcer la mise à jour immédiate du service IA avec le nouveau prompt lu du cache.
            ReconfigureAIWithNewPrompt();
        }

        /// <summary>
        /// Reconfigure l'IA avec le prompt le plus à jour.
        /// (Si l'interface IA supporte Configure pour le "System Instruction" de l'API).
        /// </summary>
        private void ReconfigureAIWithNewPrompt()
        {
            if (aiService == null)
            {
                Debug.LogError("[IAMain] Ne peut pas reconfigurer, aiService est null.");
                return;
            }

            // Si Configure existe dans IAInterface:
            // aiService.Configure(BuildFinalSystemPrompt(currentMode));
        }


        // --------------------------------------------------------------------
        // MÉTHODES REQUISES PAR AIEditorWindow.cs
        // --------------------------------------------------------------------

        /// <summary>
        /// Initialise la configuration et les dépendances (le point d'entrée en mode éditeur).
        /// </summary>
        public void SetupForEditor(AIMode mode, Action<string> onAIResponseReceived)
        {
            // 1. DÉPENDANCES ET INITIALISATION
            aiService = aiServiceAsset as IAInterface;

            if (aiService == null || iaParser == null)
            {
                Debug.LogError("[IAMain] Initialisation échouée : Les dépendances (IA Service ou Parser) sont manquantes.");
                return;
            }

            // 2. ABONNEMENTS ET ETAT
            currentMode = mode;
            editorResponseCallback = onAIResponseReceived;

            // S'abonne au Service IA (pour le statut et la réponse brute)
            aiService.RegisterStatusCallback(HandleStatusChange);
            aiService.RegisterResponseCallback(HandleRawAIResponse);

            // S'abonne au Parseur (pour renvoyer le résultat final à l'éditeur)
            iaParser.onParsingComplete -= HandleParsedResponse;
            iaParser.onParsingComplete += HandleParsedResponse;

            // S'abonne au changement de prompt (déclenché par SetPrompt)
            // IAPrompt.RegisterOnPromptChange(HandlePromptChangeNotification); 

            // 3. CONNEXION ET CONFIGURATION
            // NOTE: Le Configure initial est optionnel si l'interface IA n'a pas de buffer.

            aiService.Connect();

            Debug.Log($"[IAMain] Configuration pour l'éditeur initialisée. Mode AI: {mode}");
        }

        /// <summary>
        /// Gère la déconnexion et le désabonnement.
        /// </summary>
        public void Disconnect()
        {
            if (aiService != null)
            {
                aiService.Disconnect();
            }
            if (iaParser != null)
            {
                iaParser.onParsingComplete -= HandleParsedResponse;
            }

            // Désabonnement de IAPrompt
            // IAPrompt.RemoveOnPromptChange(HandlePromptChangeNotification);
        }

        /// <summary>
        /// Simule une réponse de Gemini et l'envoie directement au Parseur.
        /// </summary>
        public void SimulateGeminiResponse(string jsonResponse, AIMode mode)
        {
            if (iaParser == null) return;
            currentMode = mode; // Stocke le mode de la simulation

            // Le mode simulation utilise la logique Coding pour le parseur
            bool isCodingMode = (currentMode != AIMode.Chatting);
            iaParser.ParseAndExecute(jsonResponse, isCodingMode);
        }

        /// <summary>
        /// Envoie la requête utilisateur à l'IA via l'interface.
        /// IAMain assemble le prompt COMPLET (Règles + Séparateur + Requête Utilisateur)
        /// et l'envoie intégralement via SendMessage.
        /// </summary>
        public void SendMessageToAI(string userQuery, AIMode mode)
        {
            if (aiService == null)
            {
                Debug.LogError("[IAMain] Service IA non connecté ou non implémenté. Appelez SetupForEditor.");
                return;
            }

            currentMode = mode;

            // 1. Prépare le System Prompt de base (inclut le séparateur si mode != Chatting)
            string systemPromptWithSeparators = BuildFinalSystemPrompt(mode);

            // 2. 🎯 Assemblage final du prompt ici dans IAMain.
            // Le prompt complet est prêt à être envoyé.
            string finalPromptForService = systemPromptWithSeparators + userQuery;

            // 3. Déclenche l'envoi du prompt COMPLET
            // C'est cette chaîne complète qui doit être envoyée à l'interface (ISelenium, IAGemini, etc.)
            aiService.SendMessage(finalPromptForService);

            // Logique de Débogage
            if (mode != AIMode.Chatting)
            {
                Debug.Log("--- PROMPT FINAL ASSEMBLE (extrait) ---\n" + finalPromptForService.Substring(0, Mathf.Min(finalPromptForService.Length, 500)) + "...");
            }
            else
            {
                // En mode Chatting, finalPromptForService est juste la userQuery (ex: "hello").
                Debug.Log($"[IAMain] Mode Chatting: Envoi direct de la requête utilisateur: '{finalPromptForService}'");
            }
        }

        // --------------------------------------------------------------------
        // LOGIQUE DE PROMPT
        // --------------------------------------------------------------------

        /// <summary>
        /// Assemble le System Prompt final et ajoute le séparateur si nécessaire.
        /// </summary>
        public string BuildFinalSystemPrompt(AIMode mode)
        {
            // 1. Si Chatting, on renvoie une chaîne vide.
            if (mode == AIMode.Chatting)
            {
                return string.Empty;
            }

            // 2. Si Coding/Simulation, on lit le prompt de base (les règles).
            string finalPrompt = IAPrompt.GetPromptContent();

            // 3. AJOUT CLÉ: On ajoute le séparateur.
            finalPrompt += "\n\n--- REQUÊTE UTILISATEUR ---\n\n";

            return finalPrompt;
        }
    }
}