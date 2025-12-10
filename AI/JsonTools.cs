// Fichier: JsonTools.cs

using System;
using System.Text;
using UnityEngine;
using static IACoding.AITypes;

namespace IACoding
{
    public static class JsonTools
    {
        // ======================================================================
        // DÉFINITION: OUTILS DE RECHERCHE GOOGLE SEULS (Mode Recherche/Chat)
        // ======================================================================

        // Ce corps JSON contient uniquement l'activation de la recherche Google.
        public const string GOOGLE_SEARCH_TOOLS =
        @"""tools"": [
            {
                ""googleSearch"": {} 
            }
        ]";

        // ======================================================================
        // MÉTHODE PRINCIPALE DE CONSTRUCTION DU JSON (ToJson Custom)
        // ======================================================================

        public static string BuildGeminiRequestJson(ChatRequest requestBody)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");

            // 1. Contents (Historique)
            sb.Append("\"contents\":[");
            for (int i = 0; i < requestBody.contents.Length; i++)
            {
                sb.Append(SerializeTextContent(requestBody.contents[i]));
                if (i < requestBody.contents.Length - 1)
                {
                    sb.Append(",");
                }
            }
            sb.Append("]");

            // 2. System Instruction (Si non vide)
            string systemPromptText = null;
            if (requestBody.system_instruction != null &&
                requestBody.system_instruction.parts != null &&
                requestBody.system_instruction.parts.Length > 0)
            {
                systemPromptText = requestBody.system_instruction.parts[0].text;
            }

            if (!string.IsNullOrEmpty(systemPromptText))
            {
                sb.Append(",");
                sb.Append("\"system_instruction\":");
                sb.Append(SerializeSystemInstruction(systemPromptText));
            }

            // 3. Tools - Activation des outils (uniquement Google Search maintenant)
          //  if (toolsEnabled) // Si mode Recherche/Outils est ON, on active Google Search
            {
                sb.Append(",");
                sb.Append(GOOGLE_SEARCH_TOOLS);
            }

            sb.Append("}");
            return sb.ToString();
        }

        // --- Le reste des fonctions de sérialisation manuelle est inchangé ---

        private static string SerializeTextContent(TextContent content)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"role\":").Append(JsonEncode(content.role)).Append(",");
            sb.Append("\"parts\":[");

            for (int i = 0; i < content.parts.Length; i++)
            {
                sb.Append(SerializeTextPart(content.parts[i]));
                if (i < content.parts.Length - 1)
                {
                    sb.Append(",");
                }
            }
            sb.Append("]");
            sb.Append("}");
            return sb.ToString();
        }

        private static string SerializeTextPart(TextPart part)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");

            // Le Function Call est ignoré/omis ici car il ne sera jamais généré par l'IA
            // si la déclaration d'outils est absente. On se concentre sur le texte.
            sb.Append("\"text\":").Append(JsonEncode(part.text));

            sb.Append("}");
            return sb.ToString();
        }

        private static string SerializeSystemInstruction(string promptText)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"parts\":[");
            sb.Append("{\"text\":").Append(JsonEncode(promptText)).Append("}");
            sb.Append("]");
            sb.Append("}");
            return sb.ToString();
        }

        private static string JsonEncode(string s)
        {
            if (s == null) return "null";
            s = s.Replace("\\", "\\\\");
            s = s.Replace("\"", "\\\"");
            s = s.Replace("\n", "\\n");
            s = s.Replace("\r", "\\r");
            s = s.Replace("\t", "\\t");
            return $"\"{s}\"";
        }
    }
}