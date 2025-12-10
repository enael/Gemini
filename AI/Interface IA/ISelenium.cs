// Fichier: ISelenium.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using static IACoding.AITypes;
using System.Threading.Tasks;
// Ajout de l'importation de IAMain pour l'énumération AIMode si nécessaire dans d'autres logiques
using static IACoding.IAMain;

namespace IACoding
{
    /// <summary>
    /// Implémentation de IAInterface utilisant le pattern File Polling
    /// et le gestionnaire GeminiManager (processus C# externe) pour simuler la communication.
    /// </summary>
    public class ISelenium : MonoBehaviour, IAInterface
    {
        // ===============================================
        // 1. DÉCLARATION
        // ===============================================

        // Stocke les callbacks enregistrés par d'autres scripts.
        private Action<string> _responseCallback;
        private Action<bool> _statusCallback;
        private bool _isConnected = false;

        // ===============================================
        // 2. IMPLÉMENTATION DE L'INTERFACE IAInterface
        // ===============================================

        public void RegisterStatusCallback(Action<bool> callback)
        {
            _statusCallback = callback;
            _statusCallback?.Invoke(_isConnected);
        }

        public void RegisterResponseCallback(Action<string> callback)
        {
            _responseCallback = callback;
        }

        public void Connect()
        {
            // Tente de démarrer le processus C# externe (GeminiManager)
            GeminiManager.StartRoutine();
            if (GeminiManager.IsRunning)
            {
                _isConnected = true;
                _statusCallback?.Invoke(true);
                UnityEngine.Debug.Log("[ISelenium] Connecté au gestionnaire de fichiers.");
            }
            else
            {
                _statusCallback?.Invoke(false);
                UnityEngine.Debug.LogError("[ISelenium] Échec de la connexion/démarrage du GeminiManager.");
            }
        }

        public void Disconnect()
        {
            // Arrête le processus C# externe
            GeminiManager.StopRoutine();
            _isConnected = false;
            _statusCallback?.Invoke(false);
            UnityEngine.Debug.Log("[ISelenium] Déconnecté du gestionnaire.");
        }

        public void SendMessage(string userQuery)
        {
            if (!_isConnected)
            {
                UnityEngine.Debug.LogError("[ISelenium] Impossible d'envoyer un message : non connecté.");
                _responseCallback?.Invoke("ERREUR: NON CONNECTÉ");
                return;
            }

            // 2. Envoi du prompt complet
            HandleSendMessageAsync(userQuery);
        }

        // ===============================================
        // 3. GESTION ASYNCHRONE (LECTURE/ÉCRITURE FICHIER)
        // ===============================================

        private async void HandleSendMessageAsync(string fullPrompt)
        {
            try
            {
                // Le GeminiManager.SendMessage écrit le contenu complet dans le fichier de requête
                // et attend que le processus C# écrive la réponse dans le fichier de réponse.
                string response = await GeminiManager.SendMessage(fullPrompt);

                if (response.StartsWith("ERREUR_CLIENT_UNITY:"))
                {
                    _responseCallback?.Invoke($"ERREUR_COMMUNICATION: {response}");
                }
                else
                {
                    // La réponse est le texte brut (JSON ou Chat) lu du fichier de sortie.
                    _responseCallback?.Invoke(response);
                }
            }
            catch (Exception ex)
            {
                _responseCallback?.Invoke($"ERREUR_INATTENDUE: {ex.Message}");
            }
        }

        // ===============================================
        // 4. NETTOYAGE
        // ===============================================

        private void OnDestroy()
        {
            Disconnect();
        }
    }
}