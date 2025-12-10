// Fichier: IAFiles.cs

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace IACoding
{
    public static class IAFiles
    {
        // Fonction utilitaire pour nettoyer et standardiser les chemins (commencent par Assets/)
        private static string CleanPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            // Assure l'utilisation des slashs Unix
            path = path.Trim().Replace("\\", "/");

            // Si le chemin ne commence pas par Assets/, l'ajoute
            if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) &&
                !path.Equals("Assets", StringComparison.OrdinalIgnoreCase))
            {
                path = Path.Combine("Assets", path).Replace("\\", "/");
            }

            // Supprime les doubles slashs accidentels
            path = path.Replace("//", "/");

            return path;
        }

        // --------------------------------------------------------------------
        // Fonction utilitaire récursive pour obtenir la liste complète (Arborescence)
        // --------------------------------------------------------------------
        private static DirectoryListing GetDirectoryListingRecursive(string relativePath)
        {
            string fullPath = Path.GetFullPath(relativePath);

            // S'assurer que le chemin est correct pour Unity
            relativePath = CleanPath(relativePath);

            List<DirectoryEntry> entries = new List<DirectoryEntry>();
            List<DirectoryListing> subdirectories = new List<DirectoryListing>();

            if (Directory.Exists(fullPath))
            {
                try
                {
                    // 1. Récupérer et traiter les sous-répertoires
                    string[] directories = Directory.GetDirectories(fullPath);

                    foreach (string dir in directories)
                    {
                        string dirName = Path.GetFileName(dir);
                        string relativeSubPath = Path.Combine(relativePath, dirName).Replace("\\", "/");

                        // Ajout au contenu du répertoire actuel (directory)
                        entries.Add(new DirectoryEntry { name = dirName, type = "directory" });

                        // Appel récursif pour les sous-répertoires, pour remplir le tableau subdirectories
                        subdirectories.Add(GetDirectoryListingRecursive(relativeSubPath));
                    }

                    // 2. Récupérer et traiter les fichiers (en ignorant les .meta)
                    string[] files = Directory.GetFiles(fullPath)
                        .Where(f => !f.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                        .ToArray();

                    foreach (string file in files)
                    {
                        // Ajout au contenu du répertoire actuel (file)
                        entries.Add(new DirectoryEntry { name = Path.GetFileName(file), type = "file" });
                    }

                }
                catch (Exception ex)
                {
                    Debug.LogError($"Erreur de listage récursif pour {relativePath}: {ex.Message}");
                }
            }

            return new DirectoryListing
            {
                path = relativePath,
                contents = entries.ToArray(),
                subdirectories = subdirectories.ToArray()
            };
        }


        // --------------------------------------------------------------------
        // FONCTIONS DE GESTION DE FICHIERS (Retournent FunctionResult)
        // --------------------------------------------------------------------

        public static FunctionResult CreateDirectory(string path)
        {
            const string name = "CreateDirectory";
            if (string.IsNullOrEmpty(path))
            {
                return new FunctionResult { commandName = name, status = "ERROR", message = "Le chemin du répertoire ne peut pas être vide." };
            }

            path = CleanPath(path);

            if (Directory.Exists(path))
            {
                return new FunctionResult { commandName = name, status = "SUCCESS", message = $"Le répertoire existe déjà à: {path}" };
            }

            try
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
                return new FunctionResult { commandName = name, status = "SUCCESS", message = $"Répertoire créé à: {path}" };
            }
            catch (Exception e)
            {
                return new FunctionResult { commandName = name, status = "ERROR", message = $"Erreur système: Impossible de créer le répertoire. Erreur: {e.Message}" };
            }
        }

        public static FunctionResult DeletePath(string path)
        {
            const string name = "DeletePath";
            if (string.IsNullOrEmpty(path))
            {
                return new FunctionResult { commandName = name, status = "ERROR", message = "Le chemin à supprimer ne peut pas être vide." };
            }

            path = CleanPath(path);

            if (!AssetDatabase.IsValidFolder(path) && AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) == null)
            {
                return new FunctionResult { commandName = name, status = "ERROR", message = $"Le chemin n'existe pas ou n'est pas un Asset valide : {path}" };
            }

            bool deleted = AssetDatabase.DeleteAsset(path);

            if (deleted)
            {
                return new FunctionResult { commandName = name, status = "SUCCESS", message = $"Chemin supprimé : {path}" };
            }
            else
            {
                return new FunctionResult { commandName = name, status = "ERROR", message = $"Erreur système: Impossible de supprimer le chemin {path}. (Peut-être utilisé?)" };
            }
        }

        public static FunctionResult CreateFile(string path, string content)
        {
            const string name = "CreateFile";
            if (string.IsNullOrEmpty(path))
            {
                return new FunctionResult { commandName = name, status = "ERROR", message = "Le chemin du fichier ne peut pas être vide." };
            }

            path = CleanPath(path);

            try
            {
                bool exists = File.Exists(path);

                string directoryPath = Path.GetDirectoryName(path);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                File.WriteAllText(path, content);
                AssetDatabase.Refresh();

                string action = exists ? "mis à jour" : "créé";

                return new FunctionResult { commandName = name, status = "SUCCESS", message = $"Fichier {action} à: {path} (Taille: {content.Length} caractères)" };
            }
            catch (Exception e)
            {
                return new FunctionResult { commandName = name, status = "ERROR", message = $"Erreur système: Impossible de créer/écrire le fichier. Erreur: {e.Message}" };
            }
        }

        public static FunctionResult ReadFile(string path)
        {
            const string name = "ReadFile";
            if (string.IsNullOrEmpty(path))
            {
                return new FunctionResult { commandName = name, status = "ERROR", message = "Le chemin du fichier ne peut pas être vide." };
            }

            path = CleanPath(path);

            if (Directory.Exists(path))
            {
                return new FunctionResult { commandName = name, status = "ERROR", message = $"Le chemin est un répertoire, utilisez ListContents : {path}" };
            }

            if (!File.Exists(path))
            {
                return new FunctionResult { commandName = name, status = "ERROR", message = $"Le fichier n'existe pas à : {path}" };
            }

            try
            {
                string content = File.ReadAllText(path);

                return new FunctionResult
                {
                    commandName = name,
                    status = "SUCCESS",
                    message = $"Fichier lu avec succès : {path}",
                    fileContent = content
                };
            }
            catch (Exception e)
            {
                return new FunctionResult { commandName = name, status = "ERROR", message = $"Erreur système: Impossible de lire le fichier. Erreur: {e.Message}" };
            }
        }

        public static FunctionResult ListContents(string path)
        {
            const string name = "ListContents";
            path = CleanPath(path);

            if (!Directory.Exists(path))
            {
                return new FunctionResult { commandName = name, status = "ERROR", message = $"Le répertoire n'existe pas à : {path}" };
            }

            try
            {
                // Appel de la fonction récursive pour obtenir la liste complète
                DirectoryListing listing = GetDirectoryListingRecursive(path);

                return new FunctionResult
                {
                    commandName = name,
                    status = "SUCCESS",
                    message = $"Arborescence du répertoire listée avec succès : {path}",
                    directoryListing = listing
                };
            }
            catch (Exception e)
            {
                return new FunctionResult { commandName = name, status = "ERROR", message = $"Erreur système: Impossible de lister le contenu. Erreur: {e.Message}" };
            }
        }

        // --------------------------------------------------------------------
        // NOUVEAU : MISE À JOUR DU PROMPT SYSTÈME (SetPrompt)
        // --------------------------------------------------------------------

        // ATTENTION : Cette fonction attend le chemin réel du fichier, qui sera fourni par IAParser.
        public static FunctionResult SetPrompt(string actualFilePath, string content)
        {
            const string name = "SetPrompt";
            if (string.IsNullOrEmpty(actualFilePath) || string.IsNullOrEmpty(content))
            {
                return new FunctionResult { commandName = name, status = "ERROR", message = "Le chemin du fichier de prompt (interne) et le contenu ne peuvent pas être vides." };
            }

            string filePath = actualFilePath;

            try
            {
                // Assure que le répertoire existe (doit être géré principalement par IAParser, 
                // mais le fait ici par sécurité).
                string directoryPath = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    AssetDatabase.Refresh();
                }

                // Écrit le contenu dans le fichier
                File.WriteAllText(filePath, content);
                AssetDatabase.Refresh();

                // Afficher le fichier dans l'éditeur (pour confirmation visuelle)
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
                if (asset != null)
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }

                return new FunctionResult { commandName = name, status = "SUCCESS", message = $"System Prompt mis à jour avec succès dans le fichier : {filePath}" };
            }
            catch (Exception e)
            {
                return new FunctionResult { commandName = name, status = "ERROR", message = $"Erreur système: Impossible d'écrire le nouveau prompt. Erreur: {e.Message}" };
            }
        }
    }
}