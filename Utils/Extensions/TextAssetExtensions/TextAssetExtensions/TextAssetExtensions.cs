// Fichier: TextAssetExtensions.cs (Doit être dans un dossier 'Editor' ou utiliser #if UNITY_EDITOR)

using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEditor;

public static class TextAssetExtensions
{
    // Dictionnaire statique pour stocker le contenu de l'asset en cache.
    // Clé: Instance ID de l'Asset (identifiant unique pour chaque TextAsset)
    // Valeur: Le contenu textuel mis en cache.
    private static readonly Dictionary<int, string> AssetCache = new Dictionary<int, string>();

    // --- MÉTHODE D'EXTENSION DE LECTURE (GET) ---
    /// <summary>
    /// Récupère le contenu textuel de l'Asset. Utilise le cache si disponible.
    /// </summary>
    public static string GetTextWithCache(this TextAsset asset)
    {
        if (asset == null)
        {
            return string.Empty;
        }

        int assetId = asset.GetInstanceID();

        // 1. Tente de lire depuis le cache
        if (AssetCache.TryGetValue(assetId, out string cachedContent))
        {
            return cachedContent;
        }

        // 2. Si non présent (première lecture), lit la propriété interne
        string content = asset.text;

        // 3. Met en cache pour les prochaines lectures
        AssetCache[assetId] = content;

        return content;
    }

    // --- MÉTHODE D'EXTENSION D'ÉCRITURE (SET) ---
    // Agit sur le fichier disque et met à jour le cache.
    /// <summary>
    /// Modifie le contenu du fichier physique correspondant à cet Asset et met à jour le cache.
    /// Doit être appelée uniquement en mode Éditeur.
    /// </summary>
#if UNITY_EDITOR
    public static void SetTextAndCache(this TextAsset asset, string text)
    {
        if (asset == null)
        {
            Debug.LogError("TextAsset est null. Impossible de mettre à jour le contenu.");
            return;
        }

        // 1. Mise à jour du cache en mémoire
        AssetCache[asset.GetInstanceID()] = text;

        // 2. Obtention du chemin et écriture sur le disque (I/O)
        string assetPath = AssetDatabase.GetAssetPath(asset);

        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogError($"Impossible de trouver le chemin de l'Asset pour {asset.name}.");
            return;
        }

        try
        {
            File.WriteAllText(assetPath, text);

            // 3. Forcer Unity à recharger l'Asset pour mettre à jour l'AssetDatabase.
            AssetDatabase.ImportAsset(assetPath);
            Debug.Log($"[TextAssetExtension] Contenu mis à jour sur disque et en cache pour : {assetPath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Erreur lors de l'écriture du fichier {assetPath}: {ex.Message}");
        }
    }
#endif
}