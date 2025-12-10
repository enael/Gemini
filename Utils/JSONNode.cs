// Assets/Scripts/Utils/SimpleJSON.cs
// Version complète et corrigée — copie-colle direct

using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace SimpleJSON
{
    public enum JSONNodeType
    {
        Array = 1,
        Object = 2,
        String = 3,
        Number = 4,
        NullValue = 5,
        Boolean = 6,
        None = 7,
    }

    public abstract class JSONNode
    {
        public virtual JSONNode this[int aIndex] { get { return null; } set { } }
        public virtual JSONNode this[string aKey] { get { return null; } set { } }
        public virtual string Value { get { return ""; } set { } }
        public virtual int Count { get { return 0; } }

        public virtual void Add(string aKey, JSONNode aItem) { }
        public virtual void Add(JSONNode aItem) { Add("", aItem); }

        public virtual JSONNode Remove(string aKey) { return null; }
        public virtual JSONNode Remove(int aIndex) { return null; }
        public virtual JSONNode Remove(JSONNode aNode) { return aNode; }

        public virtual IEnumerable<JSONNode> Children { get { yield break; } }

        public static JSONNode Parse(string aJSON)
        {
            return new Parser(aJSON).Parse();
        }

        public override string ToString()
        {
            return "JSONNode";
        }
    }

    public class JSONArray : JSONNode
    {
        private List<JSONNode> m_List = new List<JSONNode>();
        public override JSONNode this[int aIndex]
        {
            get
            {
                if (aIndex < 0 || aIndex >= m_List.Count)
                    return null;
                return m_List[aIndex];
            }
            set
            {
                if (value == null)
                    value = JSONNull.CreateOrGet();
                if (aIndex < 0 || aIndex >= m_List.Count)
                    m_List.Add(value);
                else
                    m_List[aIndex] = value;
            }
        }

        public override int Count => m_List.Count;

        public override void Add(string aKey, JSONNode aItem)
        {
            m_List.Add(aItem);
        }

        public override JSONNode Remove(int aIndex)
        {
            if (aIndex < 0 || aIndex >= m_List.Count)
                return null;
            JSONNode tmp = m_List[aIndex];
            m_List.RemoveAt(aIndex);
            return tmp;
        }

        public override IEnumerable<JSONNode> Children
        {
            get
            {
                foreach (JSONNode N in m_List)
                    yield return N;
            }
        }
    }

    public class JSONObject : JSONNode
    {
        private Dictionary<string, JSONNode> m_Dict = new Dictionary<string, JSONNode>();

        public override JSONNode this[string aKey]
        {
            get
            {
                if (m_Dict.ContainsKey(aKey))
                    return m_Dict[aKey];
                return null;
            }
            set
            {
                if (value == null)
                    value = JSONNull.CreateOrGet();
                if (m_Dict.ContainsKey(aKey))
                    m_Dict[aKey] = value;
                else
                    m_Dict.Add(aKey, value);
            }
        }

        public override void Add(string aKey, JSONNode aItem)
        {
            if (aItem == null)
                aItem = JSONNull.CreateOrGet();

            if (!string.IsNullOrEmpty(aKey))
            {
                if (m_Dict.ContainsKey(aKey))
                    m_Dict[aKey] = aItem;
                else
                    m_Dict.Add(aKey, aItem);
            }
            else
                m_Dict.Add(Guid.NewGuid().ToString(), aItem);
        }

        public override int Count => m_Dict.Count;

        public override IEnumerable<JSONNode> Children
        {
            get
            {
                foreach (KeyValuePair<string, JSONNode> N in m_Dict)
                    yield return N.Value;
            }
        }
    }

    public class JSONString : JSONNode
    {
        private string m_Data;
        public JSONString(string aData) => m_Data = aData;
        public override string Value { get => m_Data; set => m_Data = value; }
        public override string ToString() => "\"" + m_Data + "\"";
    }

    public class JSONNumber : JSONNode
    {
        private double m_Data;
        public JSONNumber(double aData) => m_Data = aData;
        public override string Value { get => m_Data.ToString(); set => double.TryParse(value, out m_Data); }
    }

    public class JSONBool : JSONNode
    {
        private bool m_Data;
        public JSONBool(bool aData) => m_Data = aData;
        public override string Value { get => m_Data.ToString(); set => bool.TryParse(value, out m_Data); }
    }

    public class JSONNull : JSONNode
    {
        static JSONNull m_StaticInstance = new JSONNull();
        public static JSONNull CreateOrGet() => m_StaticInstance;
        private JSONNull() { }
        public override string ToString() => "null";
        public override string Value { get => "null"; set { } }
    }

    internal class Parser
    {
        const string WHITE_SPACE = " \t\n\r";
        const string WORD_BREAK = " \t\n\r{}[],:\"";

        private string json;
        private int index;

        public Parser(string jsonString) => json = jsonString;

        public JSONNode Parse()
        {
            index = 0;
            return ParseValue();
        }

        private void EatWhitespace()
        {
            while (index < json.Length && WHITE_SPACE.IndexOf(json[index]) != -1)
                index++;
        }

        private JSONNode ParseValue()
        {
            EatWhitespace();
            if (index >= json.Length) return null;

            char c = json[index];
            if (c == '"') return ParseString();
            if (c == '{') return ParseObject();
            if (c == '[') return ParseArray();
            if (json.Substring(index, 4) == "true") { index += 4; return new JSONBool(true); }
            if (json.Substring(index, 5) == "false") { index += 5; return new JSONBool(false); }
            if (json.Substring(index, 4) == "null") { index += 4; return JSONNull.CreateOrGet(); }

            return ParseNumber();
        }

        private JSONString ParseString()
        {
            StringBuilder s = new StringBuilder();
            index++; // skip opening quote
            while (index < json.Length)
            {
                char c = json[index++];
                if (c == '"') return new JSONString(s.ToString());
                if (c == '\\' && index < json.Length)
                {
                    c = json[index++];
                    if (c == 'n') s.Append('\n');
                    else if (c == 't') s.Append('\t');
                    else if (c == 'r') s.Append('\r');
                    else if (c == '\\') s.Append('\\');
                    else if (c == '"') s.Append('"');
                    else if (c == 'u') s.Append((char)int.Parse(json.Substring(index, 4), NumberStyles.HexNumber));
                    index += 4;
                }
                else s.Append(c);
            }
            throw new Exception("Unterminated string");
        }

        private JSONObject ParseObject()
        {
            JSONObject table = new JSONObject();
            index++; // skip {
            while (index < json.Length)
            {
                EatWhitespace();
                if (json[index] == '}') { index++; return table; }

                string key = ParseString().Value;
                EatWhitespace();
                if (json[index] != ':') throw new Exception("Expected colon");
                index++;
                table[key] = ParseValue();
                EatWhitespace();
                if (json[index] == ',') index++;
            }
            throw new Exception("Unclosed object");
        }

        private JSONArray ParseArray()
        {
            JSONArray arr = new JSONArray();
            index++; // skip [
            while (index < json.Length)
            {
                EatWhitespace();
                if (json[index] == ']') { index++; return arr; }
                arr.Add(ParseValue());
                EatWhitespace();
                if (json[index] == ',') index++;
            }
            throw new Exception("Unclosed array");
        }

        private JSONNumber ParseNumber()
        {
            int start = index;
            while (index < json.Length && "0123456789+-.eE".IndexOf(json[index]) != -1) index++;
            string number = json.Substring(start, index - start);
            return new JSONNumber(double.Parse(number, CultureInfo.InvariantCulture));
        }
    }
}