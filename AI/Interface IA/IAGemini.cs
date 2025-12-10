using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
// Assurez-vous que AITypes est correctement défini dans un autre fichier (IACoding/AITypes.cs)
using static IACoding.AITypes;

namespace IACoding
{
    // L'énumération AICodingMode DOIT être définie soit ici, soit dans AITypes.cs
    // Pour cet exemple, je suppose que l'accès est correct.

    public class IAGemini : MonoBehaviour, IAInterface
    {
        public enum GeminiModel
        {
            Gemini_2_5_Flash,
            Gemini_2_5_Pro,
        }

        [Header("1. Configuration API")]
        public string apiKey = "VOTRE_CLE_API_ICI";
        public GeminiModel selectedModel = GeminiModel.Gemini_2_5_Flash;

        // État : Cette variable stockera le prompt COMPLET assemblé par IAMain.
        // Elle inclut le System Prompt, les séparateurs ET la Requête Utilisateur.
        private string finalPromptContent = "";

        private string modelId;
        private string apiEndpoint;

        // Callbacks
        private Action<string> responseCallback;
        private Action<bool> statusCallback;

        // 🎯 SUPPRESSION DE L'HISTORIQUE ET DE systemInstruction :
        // Ces variables ne sont plus nécessaires car IAMain nous envoie le prompt complet
        // dans 'systemInstructions' (maintenant 'finalPromptContent').

        // --- Interface ---

        public void RegisterStatusCallback(Action<bool> callback) { this.statusCallback = callback; }

        public void Connect()
        {
            this.modelId = GetModelId(selectedModel.ToString());
            this.apiEndpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{modelId}:generateContent?key={apiKey}";
            statusCallback?.Invoke(true);
            Debug.Log($"[IAGemini] Prêt. Modèle: {modelId}");
        }

        public void Disconnect()
        {
            this.finalPromptContent = "";
            statusCallback?.Invoke(false);
        }

        public void RegisterResponseCallback(Action<string> callback) { this.responseCallback = callback; }

        /// <summary>
        /// Déclenche l'envoi. Le paramètre userMessage est ignoré car le prompt complet 
        /// est déjà dans 'finalPromptContent'.
        /// </summary>
        public void SendMessage(string userMessage)
        {
            // Nous appelons PostRequest en utilisant le prompt final assemblé.
            StartCoroutine(PostRequest(this.finalPromptContent));
        }

        // --- Logique API ---

        /// <summary>
        /// Construit et envoie la requête API en utilisant le prompt complet fourni.
        /// </summary>
        private IEnumerator PostRequest(string promptToSend)
        {
            // --- 1. CONSTRUIRE LE JSON MINIMAL POUR L'ENVOI ---

            // On crée un seul contenu avec le rôle 'user' contenant le prompt complet.
            // Le modèle traitera l'ensemble comme un message simple.
            TextContent content = new TextContent
            {
                role = "user",
                parts = new TextPart[] { new TextPart { text = promptToSend } }
            };

            // Le JSON final est juste l'objet de requête avec ce contenu unique.
            ChatRequest requestBody = new ChatRequest
            {
                // On envoie le prompt complet comme le seul élément 'contents'.
                // L'IA le traitera comme l'entrée initiale.
                contents = new TextContent[] { content },
                // On retire system_instruction de la requête car son contenu est inclus dans le promptToSend.
                // system_instruction = null (si le sérialiseur le gère)
            };

            // 🎯 Utilise l'outil de sérialisation pour construire le JSON.
            // NOTE : Vous devrez vérifier si votre JsonTools.BuildGeminiRequestJson gère
            // l'absence de system_instruction et de tools.
            string json = JsonTools.BuildGeminiRequestJson(requestBody);

            // Debug.Log($"[IAGemini] JSON Envoyé (minimal):\n{json}");

            // --- 2. Envoi ---
            using (UnityWebRequest www = new UnityWebRequest(apiEndpoint, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[IAGemini] Erreur : {www.error}\nRéponse: {www.downloadHandler.text}");
                    statusCallback?.Invoke(false);
                    responseCallback?.Invoke($"Erreur API ({www.responseCode}): {www.error}");
                }
                else
                {
                    statusCallback?.Invoke(true);
                    HandleApiResponse(www.downloadHandler.text);
                }
            }
        }

        private void HandleApiResponse(string jsonResponse)
        {
            // NOTE : Je ne modifie pas HandleApiResponse car la réponse de l'API reste la même
            // (texte ou fonction), que l'on ait envoyé un prompt simple ou complexe.

            TextResponse response = JsonUtility.FromJson<TextResponse>(jsonResponse);

            if (response == null || response.candidates == null || response.candidates.Length == 0)
            {
                responseCallback?.Invoke("Erreur: Réponse de l'IA bloquée ou vide (vérifier le safety setting).");
                return;
            }

            TextContent modelContent = response.candidates[0].content;

            // --- VÉRIFICATION DE LA RÉPONSE TEXTE ---
            if (modelContent.parts != null && modelContent.parts.Length > 0)
            {
                if (modelContent.parts[0].text != null && modelContent.parts[0].text != "")
                {
                    string textReply = modelContent.parts[0].text;
                    // L'historique n'est pas géré ici (car IAMain gère le prompt complet)
                    responseCallback?.Invoke(textReply);
                    return; // Fin du traitement pour une réponse textuelle
                }
            }

            // Si le code atteint ce point, c'est une réponse vide ou non gérée.
            responseCallback?.Invoke("Erreur: Réponse reçue mais contenu non interprétable (ni texte, ni fonction).");
        }

        private string GetModelId(string selectedModelName)
        {
            switch (selectedModelName)
            {
                case "Gemini_2_5_Pro": return "gemini-2.5-pro";
                case "Gemini_2_5_Flash": default: return "gemini-2.5-flash";
            }
        }
    }
}