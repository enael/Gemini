// Fichier: IIAInterface.cs

using System;
using static IACoding.AITypes;

namespace IACoding
{
    public interface IAInterface
    {
        // Type de l'événement de statut (pour simplifier, on utilisera un booléen simple: true=connecté, false=déconnecté)
        // Vous pouvez le remplacer par une énumération si vous voulez plus d'états (Connecting, Error, etc.).
        void RegisterStatusCallback(Action<bool> callback); // NOUVEAU

        // se connecter au service IA
        void Connect();

        // se deconnecter du service IA
        void Disconnect();

        // pour recuperer les données envoyées par l'IA
        void RegisterResponseCallback(Action<string> callback);

        // envoye des données a l'IA
        void SendMessage(string newMessage);
    }
}