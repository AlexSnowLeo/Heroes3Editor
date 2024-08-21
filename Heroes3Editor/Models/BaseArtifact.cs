using System.Collections.Generic;
using System.Linq;

namespace Heroes3Editor.Models
{
    public abstract class BaseProp
    {
        protected Dictionary<string, string> Lang { get; set; }
        protected Dictionary<string, string> LangInverted { get; set; }
        
        protected Dictionary<byte, string> NamesByCode { get; set; } = new();
        protected Dictionary<byte, string> HOTANamesByCode { get; set; } = new();

        internal Dictionary<string, byte> CodesByName => NamesByCode?.ToDictionary(i => i.Value, i => i.Key);

        public string[] Names => Lang != null
            ? NamesByCode?.Values
                .Select(x => Lang.TryGetValue(x, out var value) ? value : x)
                .ToArray()
            : NamesByCode?.Values.ToArray();

        public string[] OriginalNames => NamesByCode?.Values.ToArray();
        
        public string this[byte key] => Lang != null && Lang.ContainsKey(NamesByCode[key])
            ? Lang[NamesByCode[key]]
            : NamesByCode[key];

        public byte this[string key] => LangInverted != null && LangInverted.ContainsKey(key) 
            ? CodesByName[LangInverted[key]]
            : CodesByName[key];

        public string GetLangValue(string key) => Lang != null && Lang.ContainsKey(key)
            ? Lang[key]
            : key;

        public string GetOriginalValue(string key) => LangInverted != null && LangInverted.ContainsKey(key)
            ? LangInverted[key]
            : key;

        public void LoadHotaReferenceCodes()
        {
            foreach (var code in HOTANamesByCode)
            {
                if (NamesByCode.ContainsKey(code.Key))
                {
                    continue;
                }
                NamesByCode.Add(code.Key, code.Value);
            }
        }

        public void RemoveHotaReferenceCodes()
        {
            foreach (var kvp in HOTANamesByCode)
            {
                if (NamesByCode.ContainsKey(kvp.Key))
                {
                    NamesByCode.Remove(kvp.Key);
                }
            }
        }

        public void SetLang(Dictionary<string, string> langData)
        {
            if (langData == null)
            {
                Lang = null;
                LangInverted = null;
                return;
            }

            Lang = langData;
            LangInverted = langData.ToDictionary(key => key.Value, value => value.Key);
        }
    }
    
    public abstract class BaseArtifact : BaseProp
    {
        public Dictionary<byte, string> GetArtifacts => Lang != null
            ? NamesByCode.Where(x => x.Value != "-")
                .ToDictionary(x => x.Key, x => Lang.TryGetValue(x.Value, out var value) ? value : x.Value)
            : NamesByCode.Where(x => x.Value != "-")
                .ToDictionary(x => x.Key, x => x.Value);
    }
}
