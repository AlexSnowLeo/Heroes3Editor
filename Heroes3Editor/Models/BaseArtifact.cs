using System.Collections.Generic;
using System.Linq;
using Heroes3Editor.Lang;

namespace Heroes3Editor.Models
{
    public abstract class BaseProp
    {
        protected Dictionary<string, string> Lang { get; set; }
        protected Dictionary<string, string> LangInverted { get; set; }
        protected Dictionary<string, string> OriginalLang { get; set; }
        protected Dictionary<string, string> HotaLang { get; set; }

        protected Dictionary<byte, string> NamesByCode { get; set; } = [];
        protected Dictionary<byte, string> HOTANamesByCode { get; set; } = [];

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

            if (OriginalLang != null && HotaLang != null)
            {
                Lang = OriginalLang.ToDictionary(key => key.Key, value => value.Value);
                foreach (var hlang in HotaLang)
                {
                    if (Lang.ContainsKey(hlang.Key))
                        Lang[hlang.Key] = hlang.Value;
                    else
                        Lang.Add(hlang.Key, hlang.Value);
                }

                LangInverted = Lang.ToDictionary(key => key.Value, value => value.Key);
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

            if (OriginalLang != null)
            {
                Lang = OriginalLang.ToDictionary(key => key.Key, value => value.Value);
            }
        }

        public void SetLang(Dictionary<string, string> langData, Dictionary<string, string> hotaLangData = null)
        {
            if (langData == null)
            {
                Lang = null;
                LangInverted = null;
                return;
            }

            Lang = langData.ToDictionary(key => key.Key, value => value.Value);
            OriginalLang = langData.ToDictionary(key => key.Key, value => value.Value);
            HotaLang = hotaLangData?.ToDictionary(key => key.Key, value => value.Value);
            LangInverted = langData.ToDictionary(key => key.Value, value => value.Key);
        }
    }
    
    public abstract class BaseArtifact : BaseProp
    {
        public const int Empty = 0xFF;
        public Dictionary<byte, string> GetArtifacts => Lang != null
            ? NamesByCode.Where(x => x.Value != "-")
                .ToDictionary(x => x.Key, x => Lang.TryGetValue(x.Value, out var value) ? value : x.Value)
            : NamesByCode.Where(x => x.Value != "-")
                .ToDictionary(x => x.Key, x => x.Value);
    }
}
