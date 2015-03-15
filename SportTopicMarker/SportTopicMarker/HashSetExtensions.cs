using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SportTopicMarker
{
    public static class HashSetExtensions
    {
        public static void Save(this HashSet<string> data, string path)
        {
            var builder = new StringBuilder();
            foreach (string s in data)
            {
                builder.AppendLine(s);
            }
            File.WriteAllText(path, builder.ToString());
        }

        public static void Load(this HashSet<string> data, string path)
        {
            string[] lines = File.ReadAllLines(path);
            foreach (string line in lines)
            {
                data.Add(line);
            }
        }
    }
}
