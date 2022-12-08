using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C2Server
{
    internal class CounterDecrypter
    {
        private string key;

        public CounterDecrypter(string key)
        {
            this.key = key;
        }

        public uint Decrypt(IEnumerable<char> enc)
        {
            var result = 0U;

            foreach (var c in enc)
            {
                result *= (uint) this.key.Length;
                result += (uint) this.key.IndexOf(c);
            }

            return result;
        }
    }
}
