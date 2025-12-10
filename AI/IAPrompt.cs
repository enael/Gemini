// Fichier: IAPrompt.cs

using UnityEngine;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Gère le System Prompt de l'IA. Utilise les TextAssets comme source de vérité persistante
/// et les méthodes d'extension pour la gestion du cache et de l'écriture.
/// </summary>
public class IAPrompt : MonoBehaviour
{
    // --------------------------------------------------------------------
    // SINGLETON (Inchangé)
    // --------------------------------------------------------------------
    public static IAPrompt _instance;
    public static IAPrompt Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<IAPrompt>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("IAPromptManager");
                    _instance = go.AddComponent<IAPrompt>();
                }
            }
            return _instance;
        }
    }

    // --------------------------------------------------------------------
    // PROPRIÉTÉS UNITY (SOURCES DE VÉRITÉ PERSISTANTES)
    // --------------------------------------------------------------------

    [Header("Prompts (Source persistante via TextAsset)")]
    [Tooltip("L'Asset principal de la personnalité de l'IA. C'est ici que les changements du SetPrompt seront écrits.")]
    public TextAsset mainPromptAsset;

    [Tooltip("Règles critiques et descriptions des fonctions (Non modifiable par SetPrompt).")]
    public TextAsset fixedRulesAsset;

    // --------------------------------------------------------------------
    // CACHE ET CALLBACKS
    // --------------------------------------------------------------------

    // Note: Le cache du mainPrompt est maintenant géré DANS TextAssetExtensions.cs
    // Nous allons utiliser une variable ici pour assembler le prompt final.
    private static string _cachedFixedRules = null; // Cache pour les règles fixes (qui ne changent jamais)

    public static List<Action> Callbacks;

    // --------------------------------------------------------------------
    // MÉTHODES PUBLIQUES DE GESTION
    // --------------------------------------------------------------------

    /// <summary>
    /// Initialise les composants et le cache des règles fixes.
    /// </summary>
    public void InitializeAIPromptFile()
    {
        if (mainPromptAsset == null || fixedRulesAsset == null)
        {
            Debug.LogError("[IAPrompt] Les TextAssets 'mainPromptAsset' ou 'fixedRulesAsset' sont manquants. Veuillez les glisser-déposer dans l'Inspecteur.");
            return;
        }

        // Utilise la méthode d'extension pour lire les règles et initialiser leur cache
        if (_cachedFixedRules == null)
        {
            _cachedFixedRules = fixedRulesAsset.GetTextWithCache();
            _cachedFixedRules = $"\n\n--- RÈGLES CRITIQUES ET OUTILS (NE PAS MODIFIER EN DESSOUS) ---\n\n{_cachedFixedRules}";

            // Déclenche une lecture du mainPrompt pour initialiser son cache dans TextAssetExtensions.
            mainPromptAsset.GetTextWithCache();
        }

        Debug.Log("[IAPrompt] Cache des TextAssets initialisé. Persistance via AssetDatabase.");
    }

    /// <summary>
    /// Met à jour le contenu du mainPromptAsset (persistance) et son cache (performance).
    /// </summary>
    public static void ChangePrompt(string newPromptContent)
    {
        if (Instance == null || Instance.mainPromptAsset == null)
        {
            Debug.LogError("[IAPrompt] Impossible de changer le prompt : Instance ou mainPromptAsset manquant.");
            return;
        }

        if (string.IsNullOrEmpty(newPromptContent))
        {
            Debug.LogError("[IAPrompt] Le nouveau contenu du prompt ne peut pas être vide.");
            return;
        }

        // 1. Appel de l'extension pour l'écriture. Ceci écrit sur le disque ET met à jour le cache dans TextAssetExtensions.
        // On écrit SEULEMENT la partie modifiable du prompt.
        _instance.mainPromptAsset.SetTextAndCache(newPromptContent);

        // 2. Déclenchement de la notification pour IAMain (la reconfiguration se fera avec GetPromptContent)
        InvokeAllCallbacks();
    }

    /// <summary>
    /// Retourne le chemin (chemin de l'Asset principal).
    /// </summary>
    public static string GetPromptPath()
    {
#if UNITY_EDITOR
        if (_instance != null && _instance.mainPromptAsset != null)
        {
            return AssetDatabase.GetAssetPath(_instance.mainPromptAsset);
        }
#endif
        return "Chemin de l'Asset non disponible en mode Runtime ou mainPromptAsset est null.";
    }

    /// <summary>
    /// Lit et retourne le contenu complet du System Prompt depuis les caches.
    /// </summary>
    public static string GetPromptContent()
    {
        if (_instance == null || _instance.mainPromptAsset == null)
        {
            return "ERREUR: mainPromptAsset non initialisé.";
        }

        if (_cachedFixedRules == null)
        {
            // Fallback si le composant n'est pas initialisé correctement, mais les assets existent
            _instance.InitializeAIPromptFile();
        }

        // 1. Récupère le contenu principal (la partie modifiable, depuis le cache TextAssetExtensions)
        string mainContent = _instance.mainPromptAsset.GetTextWithCache();

        // 2. Ajoute les règles fixes (depuis notre propre cache interne)
        string finalPrompt = mainContent + _cachedFixedRules;

        return finalPrompt;
    }

    // --------------------------------------------------------------------
    // GESTION DES CALLBACKS (Inchangée)
    // --------------------------------------------------------------------

    public static void RegisterOnPromptChange(Action callback)
    {
        if (Callbacks == null)
            Callbacks = new List<Action>();

        if (!Callbacks.Contains(callback))
            Callbacks.Add(callback);
    }

    public static void RemoveOnPromptChange(Action callback)
    {
        if (Callbacks == null)
            return;

        if (Callbacks.Contains(callback))
            Callbacks.Remove(callback);
    }

    private static void InvokeAllCallbacks()
    {
        if (Callbacks == null) return;

        var callbacksToInvoke = new List<Action>(Callbacks);

        foreach (var callback in callbacksToInvoke)
        {
            callback?.Invoke();
        }
    }
}