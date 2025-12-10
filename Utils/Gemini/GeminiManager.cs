using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

// La classe statique est chargée au démarrage de l'Editor, assurant la routine de fond.
[InitializeOnLoad]
public static class GeminiManager
{
    // 🔑 CONFIGURATION : VÉRIFIEZ CES CHEMINS ET NOMS
    private const string MessageDirectory = @"E:\Projets VS\Selenium\Messages";
    private const string LauncherPath = @"E:\Projets VS\Selenium\LANCER_TOUT.bat";
    private const string SeleniumProcessName = "Selenium";

    // Modèles de fichiers
    private const string MessageFileNameTemplate = "message_{0}.txt";
    private const string ResponseFilePattern = "reponse_*.txt";

    // Variables pour la gestion des requêtes asynchrones
    private static int _currentId = 0;
    private static Dictionary<int, TaskCompletionSource<string>> _pendingRequests = new Dictionary<int, TaskCompletionSource<string>>();

    // Logique de surveillance de fond (Thread)
    private static Thread _monitorThread;
    private static bool _isMonitoring = false;
    private static readonly Queue<Action> _mainThreadActions = new Queue<Action>();
    private static readonly object _queueLock = new object();

    // Propriété publique pour le GUI
    public static bool IsRunning => _isMonitoring;

    // ===============================================
    // 1. INITIALISATION ET CONTRÔLE DE LA ROUTINE
    // ===============================================

    static GeminiManager()
    {
        CleanupOrphanedFiles();
        EditorApplication.update += Update;
        EditorApplication.quitting += StopRoutine;
    }

    /// <summary>
    /// Démarre la routine de surveillance des fichiers sur un thread séparé.
    /// </summary>
    public static void StartRoutine()
    {
        if (_isMonitoring) return;

        if (!Directory.Exists(MessageDirectory))
        {
            Log($"ERREUR: Le répertoire des messages '{MessageDirectory}' n'existe pas. Veuillez le créer.");
            return;
        }

        _isMonitoring = true;
        _monitorThread = new Thread(MonitorResponsesLoop);
        _monitorThread.IsBackground = true;
        _monitorThread.Start();
        Log("Surveillance des réponses DÉMARRÉE.");
    }

    /// <summary>
    /// Arrête le thread de surveillance.
    /// </summary>
    public static void StopRoutine()
    {
        if (!_isMonitoring) return;

        _isMonitoring = false;
        if (_monitorThread != null && _monitorThread.IsAlive)
        {
            _monitorThread.Join(500);
        }
        Log("Surveillance ARRÊTÉE.");
    }

    // ===============================================
    // 2. MÉTHODE D'ENVOI PRINCIPALE (ASYNCHRONE)
    // ===============================================

    /// <summary>
    /// Envoie un prompt, lance le client si nécessaire, et attend la réponse.
    /// </summary>
    public static async Task<string> SendMessage(string message)
    {
        // ⭐ NOUVEAU : DÉMARRE LA ROUTINE SI ELLE NE TOURNE PAS
        StartRoutine();

        // VÉRIFICATION ET LANCEMENT DU CLIENT SI NÉCESSAIRE
        LaunchClientIfNecessary();

        int id = Interlocked.Increment(ref _currentId);
        string idString = id.ToString();
        string messagePath = Path.Combine(MessageDirectory, string.Format(MessageFileNameTemplate, idString));

        Log($"Envoi de la requête ID {id}. Création du fichier: {Path.GetFileName(messagePath)}");

        try
        {
            var tcs = new TaskCompletionSource<string>();

            lock (_pendingRequests)
            {
                _pendingRequests.Add(id, tcs);
            }

            File.WriteAllText(messagePath, message, Encoding.UTF8);

            string response = await tcs.Task;

            return response;
        }
        catch (Exception ex)
        {
            Log($"❌ Échec de la requête ID {id}: {ex.Message.Split('\n')[0]}");
            return $"ERREUR_CLIENT_UNITY: {ex.Message.Split('\n')[0]}";
        }
        finally
        {
            lock (_pendingRequests)
            {
                if (_pendingRequests.ContainsKey(id))
                {
                    _pendingRequests.Remove(id);
                }
            }
        }
    }

    // ===============================================
    // 3. LA ROUTINE PERMANENTE (MonitorResponsesLoop)
    // ===============================================

    private static void MonitorResponsesLoop()
    {
        while (_isMonitoring)
        {
            try
            {
                string[] responseFiles = Directory.GetFiles(MessageDirectory, ResponseFilePattern);

                foreach (string fileToProcess in responseFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(fileToProcess);
                    string idString = fileName.Replace("reponse_", "");
                    if (!int.TryParse(idString, out int id)) continue;

                    string responseContent = ReadResponseFile(fileToProcess, id);

                    if (responseContent != null)
                    {
                        if (_pendingRequests.TryGetValue(id, out TaskCompletionSource<string> tcs))
                        {
                            Log($"📝 Réponse reçue pour ID {id}.");
                            TryDeleteFile(fileToProcess, id);
                            tcs.SetResult(responseContent);
                        }
                        else
                        {
                            TryDeleteFile(fileToProcess, id);
                            Log($"⚠️ Fichier de réponse orphelin trouvé et supprimé (ID {id}).");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Erreur lors de la surveillance des réponses : {ex.Message.Split('\n')[0]}");
            }

            Thread.Sleep(500); // Intervalle de vérification
        }
    }

    // ===============================================
    // 4. MÉTHODES UTILITAIRES DE GESTION
    // ===============================================

    private static void Log(string message)
    {
        lock (_queueLock)
        {
            _mainThreadActions.Enqueue(() => UnityEngine.Debug.Log("[Gemini Manager] " + message));
        }
    }

    private static void Update()
    {
        lock (_queueLock)
        {
            while (_mainThreadActions.Count > 0)
            {
                _mainThreadActions.Dequeue().Invoke();
            }
        }
    }

    private static bool IsSeleniumRunning() { return Process.GetProcessesByName(SeleniumProcessName).Length > 0; }

    private static void LaunchClientIfNecessary()
    {
        if (IsSeleniumRunning()) return;

        if (!File.Exists(LauncherPath))
        {
            Log($"ERREUR FATALE: Fichier de lancement batch introuvable : {LauncherPath}");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(LauncherPath)
            {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            });
            Log("Fichier de lancement exécuté.");
        }
        catch (Exception ex)
        {
            Log($"ERREUR lors du lancement du batch : {ex.Message}");
        }
    }

    private static void CleanupOrphanedFiles()
    {
        try
        {
            var orphanedMessages = Directory.GetFiles(MessageDirectory, MessageFileNameTemplate.Replace("{0}", "*"));
            var orphanedResponses = Directory.GetFiles(MessageDirectory, ResponseFilePattern);

            foreach (var file in orphanedMessages.Concat(orphanedResponses))
            {
                File.Delete(file);
            }
            Log($"Nettoyage effectué : {orphanedMessages.Length + orphanedResponses.Length} fichiers orphelins supprimés.");
        }
        catch (Exception ex)
        {
            Log($"❌ Avertissement : Échec du nettoyage initial : {ex.Message.Split('\n')[0]}");
        }
    }

    private static string ReadResponseFile(string filePath, int id)
    {
        int maxAttempts = 5;
        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                return File.ReadAllText(filePath, Encoding.UTF8);
            }
            catch (IOException)
            {
                Thread.Sleep(50);
            }
            catch (Exception ex)
            {
                Log($"[{id}] ❌ Erreur critique de lecture : {ex.Message.Split('\n')[0]}");
                break;
            }
        }
        return null;
    }

    private static void TryDeleteFile(string filePath, int id)
    {
        int maxAttempts = 5;
        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                File.Delete(filePath);
                return;
            }
            catch (IOException)
            {
                Thread.Sleep(50);
            }
            catch (Exception ex)
            {
                Log($"[{id}] ❌ Erreur critique à la suppression: {ex.Message.Split('\n')[0]}");
                return;
            }
        }
        Log($"[{id}] ❌ Impossible de supprimer le fichier de réponse après {maxAttempts} tentatives.");
    }
}