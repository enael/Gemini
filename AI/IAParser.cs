// Fichier: IAParser.cs

using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace IACoding
{
    // --------------------------------------------------------------------
    // ATTENTION : Toutes les structures CommandStructure et *Args doivent 
    // être définies UNE SEULE FOIS dans le fichier IAsonModels.cs
    // --------------------------------------------------------------------

    /// <summary>
    /// Le Parseur reçoit la réponse brute de l'IA (texte ou JSON), l'analyse, 
    /// exécute les commandes via IAFiles, et renvoie le résultat à IAMain.
    /// Il n'est PAS responsable de l'initialisation des dépendances ni des fichiers de prompt.
    /// </summary>
    public class IAParser : MonoBehaviour
    {
        // Événement déclenché lorsque le parsing et l'exécution sont terminés. 
        // Le résultat est envoyé à IAMain.
        public Action<string> onParsingComplete;

        public void ParseAndExecute(string response, bool isCodingMode)
        {
            if (string.IsNullOrEmpty(response))
            {
                onParsingComplete?.Invoke("{\"results\": [{\"commandName\": \"Parse\", \"status\": \"ERROR\", \"message\": \"Réponse vide reçue.\"}]}");
                return;
            }

            // Si le mode codage n'est pas actif, on renvoie la réponse brute.
            if (!isCodingMode)
            {
                onParsingComplete?.Invoke(response);
                return;
            }

            // Tente d'extraire le JSON de commande
            string jsonContent = ExtractJson(response);

            // Si aucun JSON valide n'est trouvé en mode codage, l'IA a parlé sans outil.
            if (string.IsNullOrEmpty(jsonContent))
            {
                onParsingComplete?.Invoke(response);
                return;
            }

            CommandListWrapper commandWrapper;
            try
            {
                // Désérialisation de la liste des commandes
                commandWrapper = JsonUtility.FromJson<CommandListWrapper>(jsonContent);
            }
            catch (Exception e)
            {
                onParsingComplete?.Invoke($"[Parser] ❌ ERREUR: JSON malformé ou structure incorrecte (attendu : {{ \"commands\": [...] }}). Erreur: {e.Message}");
                return;
            }

            if (commandWrapper == null || commandWrapper.commands == null || commandWrapper.commands.Length == 0)
            {
                onParsingComplete?.Invoke("[Parser] ⚠️ AVERTISSEMENT: Le JSON était valide, mais le tableau 'commands' est vide.");
                return;
            }

            // Liste pour stocker les résultats structurés (FunctionResult)
            List<FunctionResult> results = new List<FunctionResult>();

            // BOUCLE D'EXÉCUTION DES COMMANDES
            foreach (var command in commandWrapper.commands)
            {
                FunctionResult executionResult = ExecuteFunction(command.functionName, command.arguments);
                results.Add(executionResult);
            }

            // Encapsuler tous les résultats dans le wrapper final
            ResultListWrapper finalWrapper = new ResultListWrapper { results = results.ToArray() };

            // Sérialiser le wrapper en JSON pour le retour à l'IA
            string finalJsonResult = JsonUtility.ToJson(finalWrapper, true);

            // Envoi du résultat JSON final à IAMain.
            onParsingComplete?.Invoke(finalJsonResult);
        }

        // Tente d'extraire le premier bloc JSON valide de la réponse brute de l'IA
        private string ExtractJson(string response)
        {
            int start = response.IndexOf('{');
            int end = response.LastIndexOf('}');

            if (start >= 0 && end > start)
            {
                return response.Substring(start, end - start + 1);
            }
            return null;
        }

        // Exécute la fonction de gestion de fichiers correspondante
        public FunctionResult ExecuteFunction(string functionName, string argumentsJson)
        {
            switch (functionName)
            {
                case "CreateDirectory":
                    try
                    {
                        var args = JsonUtility.FromJson<CreateDirectoryArgs>(argumentsJson);
                        return IAFiles.CreateDirectory(args.directoryPath);
                    }
                    catch (Exception e)
                    {
                        return new FunctionResult { commandName = functionName, status = "ERROR", message = $"Problème de désérialisation: {e.Message}" };
                    }

                case "ListContents":
                    try
                    {
                        var args = JsonUtility.FromJson<ListContentsArgs>(argumentsJson);
                        return IAFiles.ListContents(args.directoryPath);
                    }
                    catch (Exception e)
                    {
                        return new FunctionResult { commandName = functionName, status = "ERROR", message = $"Problème de désérialisation: {e.Message}" };
                    }

                case "DeletePath":
                    try
                    {
                        var args = JsonUtility.FromJson<DeletePathArgs>(argumentsJson);
                        return IAFiles.DeletePath(args.pathToDelete);
                    }
                    catch (Exception e)
                    {
                        return new FunctionResult { commandName = functionName, status = "ERROR", message = $"Problème de désérialisation: {e.Message}" };
                    }

                case "CreateFile":
                    try
                    {
                        var args = JsonUtility.FromJson<CreateFileArgs>(argumentsJson);
                        return IAFiles.CreateFile(args.filePath, args.fileContent);
                    }
                    catch (Exception e)
                    {
                        return new FunctionResult { commandName = functionName, status = "ERROR", message = $"Problème de désérialisation: {e.Message}" };
                    }

                case "ReadFile":
                    try
                    {
                        var args = JsonUtility.FromJson<ReadFileArgs>(argumentsJson);
                        return IAFiles.ReadFile(args.filePath);
                    }
                    catch (Exception e)
                    {
                        return new FunctionResult { commandName = functionName, status = "ERROR", message = $"Problème de désérialisation: {e.Message}" };
                    }

                // SetPrompt utilise le chemin initialisé dans Awake()
                case "SetPrompt":
                    try
                    {
                        // 1. Désérialisation des arguments (le contenu du nouveau prompt)
                        var args = JsonUtility.FromJson<SetPromptArgs>(argumentsJson);

                        if (args == null || string.IsNullOrEmpty(args.promptContent))
                        {
                            return new FunctionResult
                            {
                                commandName = "SetPrompt",
                                status = "ERROR",
                                message = "Le contenu du prompt est manquant ou invalide."
                            };
                        }

                        // 2. Appel de la classe IAPrompt pour modifier le fichier sur le disque.
                        // Cette fonction écrit le fichier et déclenche l'événement qui notifiera IAMain.
                        IAPrompt.ChangePrompt(args.promptContent);

                        // 3. Retour d'un succès au client
                        // (Le IAPrompt.ChangePrompt() déclenche la reconfiguration de l'IA en arrière-plan)
                        return new FunctionResult
                        {
                            commandName = "SetPrompt",
                            status = "SUCCESS",
                            message = "Le System Prompt a été mis à jour avec succès. L'IA sera reconfigurée pour la prochaine étape."
                        };
                    }
                    catch (Exception e)
                    {
                        return new FunctionResult
                        {
                            commandName = "SetPrompt",
                            status = "ERROR",
                            message = $"Erreur interne lors de la modification du prompt: {e.Message}"
                        };
                    }

                default:
                    return new FunctionResult { commandName = functionName, status = "ERROR", message = $"Fonction '{functionName}' inconnue." };
            }
        }
    }
}