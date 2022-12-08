using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C2Server
{
    internal class RequestDecoder
    {
        private string key = "amsjl6zci20dbt35guhw7n1fqvx4k8y9rpoe";

        public uint Decode(IEnumerable<char> x)
        {
            uint result = 0;
            
            foreach (var c in x.Reverse())
            {
                result *= (uint)key.Length;
                result += (uint)key.IndexOf(c);
            }
            return result;


        }
    }
}
