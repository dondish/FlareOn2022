using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace C2Server
{
    internal class RequestDataDecrypter
    {
        private uint Counter;
        private const string CHAR_LIST = "abcdefghijklmnopqrstuvwxyz0123456789";

        public RequestDataDecrypter(uint counter)
        {
            Counter = counter;
        }

        public string Decrypt(string request_data)
        {
            var key = generateKey();
            string text = string.Empty;
            for (int i = 0; i < request_data.Length; i++)
            {
                text += CHAR_LIST[key.IndexOf(request_data[i])].ToString();
            }
            return text;
        }
        
        private string generateKey()
        {
            string key_list = CHAR_LIST;

            int length = key_list.Length;
            string text2 = string.Empty;
            RandomMT mersenne = new RandomMT(Counter);
            for (int i = 0; i < length; i++)
            {
                int num = mersenne.NextInt32(0, key_list.Length);
                text2 += key_list[num].ToString();
                key_list = key_list.Remove(num, 1);
            }
            return text2;

        }
    }
}
