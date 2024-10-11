using System.Linq;
using System.Windows;

namespace Heroes3Editor
{
    internal static class LangHepler
    {
        public static string Get(string key)
        {
            var r = Application.Current.Resources.MergedDictionaries.First();
            return (string)r[key] ?? key;
        }
    }
}
