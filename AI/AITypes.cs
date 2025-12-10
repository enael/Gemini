// Fichier: AITypes.cs
using System;
using System.Collections.Generic;

namespace IACoding
{
    public enum AIMode
    {
        Chatting,
        Coding,
        Simulation
    }

    public static class AITypes
    {
        // --- WRAPPERS POUR JSONUTILITY (Unity ne gère pas les tableaux nus) ---
        [Serializable]
        public class ChatRequest
        {
            public Content system_instruction;
            public List<Content> contents = new List<Content>();
            public ToolsWrapper[] tools; // Optionnel : pour le mode Coding
        }

        [Serializable]
        public class Content
        {
            public string role; // "user" ou "model"
            public List<Part> parts = new List<Part>();
        }

        [Serializable]
        public class Part
        {
            public string text;
            // Pour le futur (Function Calling)
            // public FunctionCall functionCall; 
            // public FunctionResponse functionResponse;
        }

        // --- OUTILS (Structure JSON pour Gemini) ---
        [Serializable]
        public class ToolsWrapper
        {
            public FunctionDeclaration[] function_declarations;
        }

        [Serializable]
        public class FunctionDeclaration
        {
            public string name;
            public string description;
            // parameters est un objet complexe, souvent traité comme string ou objet générique
        }

        // --- REPONSE (Output) ---
        [Serializable]
        public class GeminiResponse
        {
            public Candidate[] candidates;
        }

        [Serializable]
        public class Candidate
        {
            public Content content;
            public string finishReason;
        }
    }
}