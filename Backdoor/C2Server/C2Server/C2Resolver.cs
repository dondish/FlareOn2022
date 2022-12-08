using DNS.Client;
using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace C2Server
{
    internal class C2Resolver : IRequestResolver
    {
        private DnsClient proxyClient = new DnsClient("8.8.8.8");
        private CounterDecrypter counterDecrypter = new CounterDecrypter("amsjl6zci20dbt35guhw7n1fqvx4k8y9rpoe");
        private C2Handler c2Handler = new C2Handler();

        public async Task<IResponse> Resolve(IRequest request, CancellationToken cancellationToken = default)
        {
            IResponse response = Response.FromRequest(request);

            foreach (Question question in response.Questions)
            {
                if (question.Name.ToString().EndsWith(".flare-on.com"))
                {
                    Console.WriteLine(question.Name);
                    response.AnswerRecords.Add(await handleC2Request(question));
                } else
                {
                    response.AnswerRecords.Add(await proxyRequest(question));
                }

            }

            return response;
        }

        public async Task<IResourceRecord> proxyRequest(Question question)
        {
            var response = await proxyClient.Resolve(question.Name, question.Type);
            return response.AnswerRecords[0];
        }

        public Task<IResourceRecord> handleC2Request(Question question)
        {
            var request = question.Name.ToString().SkipLast(".flare-on.com".Length);
            var enc_counter = request.TakeLast(3);
            var counter = counterDecrypter.Decrypt(enc_counter);
            Console.WriteLine("Counter: " + counter.ToString());
            var enc_request_data = request.SkipLast(3);
            var request_decrypter = new RequestDataDecrypter(counter);
            var request_data = request_decrypter.Decrypt(String.Join("", enc_request_data));
            string response;
            if (request_data == "aflareon")
            {
                response = "192.168.0.0";
                c2Handler = new C2Handler();
            }
            else
            {
                response = c2Handler.Handle(request_data);
            }
            return Task.FromResult<IResourceRecord>(new IPAddressResourceRecord(question.Name, IPAddress.Parse(response)));
        }
    }
}
