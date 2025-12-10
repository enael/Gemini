// Fichier: IAsonModels.cs
// Contient toutes les structures de données utilisées par JsonUtility pour la communication IA/Parseur.

using System;
using UnityEngine;

namespace IACoding
{
    // --------------------------------------------------------------------
    // 1. Structures de Commande (Requête de l'IA)
    // --------------------------------------------------------------------

    [Serializable]
    public class CommandStructure
    {
        public string functionName;
        public string arguments;
    }

    [Serializable]
    public class CommandListWrapper
    {
        public CommandStructure[] commands;
    }

    // --------------------------------------------------------------------
    // 2. Structures de Données pour les Arguments Spécifiques
    // --------------------------------------------------------------------

    [Serializable]
    public class CreateDirectoryArgs
    {
        public string directoryPath;
    }

    [Serializable]
    public class ListContentsArgs
    {
        public string directoryPath;
    }

    [Serializable]
    public class DeletePathArgs
    {
        public string pathToDelete;
    }

    [Serializable]
    public class CreateFileArgs
    {
        public string filePath;
        public string fileContent;
    }

    [Serializable]
    public class ReadFileArgs
    {
        public string filePath;
    }

    // MODIFIÉ : Arguments pour la mise à jour du System Prompt - Plus de chemin ici
    [Serializable]
    public class SetPromptArgs
    {
        public string promptContent;   // Le nouveau contenu du prompt
    }

    // --------------------------------------------------------------------
    // 3. Structures de DONNÉES DE RETOUR (Feedback au format JSON)
    // --------------------------------------------------------------------

    [Serializable]
    public class DirectoryEntry
    {
        public string name;
        public string type; // "file" ou "directory"
    }

    [Serializable]
    public class DirectoryListing
    {
        public string path;
        public DirectoryEntry[] contents;
        public DirectoryListing[] subdirectories;
    }

    [Serializable]
    public class FunctionResult
    {
        public string commandName;
        public string status;      // "SUCCESS" ou "ERROR"
        public string message;     // Message d'information (log)

        public string fileContent; // Contenu si la commande était ReadFile
        public DirectoryListing directoryListing; // Arborescence si la commande était ListContents
    }

    [Serializable]
    public class ResultListWrapper
    {
        public FunctionResult[] results;
    }
}