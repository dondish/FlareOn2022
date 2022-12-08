using C2Server;
using DNS.Server;
using System.Net;
using System.Net.Sockets;

var server = new DnsServer(new C2Resolver());
// Log errors
server.Errored += (sender, e) => Console.WriteLine(e.Exception.Message);
await server.Listen(53, IPAddress.Parse("192.168.41.1"));