using System;
using System.Collections.Generic;
using System.Text;

namespace MoNeriSharp.Clases
{
    /// <summary>
    /// Builder para construir conversaciones en el formato estructurado
    /// usado por MoNeriSharp: <SYSTEM>, <USER>, <QUESTION>, <THINK>, <ASSISTANT>, <ANSWER>.
    /// Permite encadenar mensajes como en TornadoApi.
    /// </summary>
    public class ConversationBuilder
    {
        private readonly List<string> messages = new List<string>();

        public ConversationBuilder AppendSystemMessage(string text)
        {
            if (!string.IsNullOrEmpty(text))
                messages.Add($"<SYSTEM> {text}");
            return this;
        }

        public ConversationBuilder AppendUserMessage(string user, string text)
        {
            if (!string.IsNullOrEmpty(user) || !string.IsNullOrEmpty(text))
                messages.Add($"<USER> {user} <QUESTION> {text}");
            return this;
        }

        public ConversationBuilder AppendThink(string text)
        {
            if (!string.IsNullOrEmpty(text))
                messages.Add($"<THINK> {text}");
            return this;
        }

        public ConversationBuilder AppendAssistantMessage(string assistantName, string text)
        {
            if (!string.IsNullOrEmpty(assistantName) || !string.IsNullOrEmpty(text))
                messages.Add($"<ASSISTANT> {assistantName} <ANSWER> {text}");
            return this;
        }

        /// <summary>
        /// Devuelve la conversación completa como una sola cadena lista para tokenizar.
        /// </summary>
        public string Build()
        {
            return string.Join(" ", messages).Trim();
        }
    }
}