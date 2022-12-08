# C2 Server
The C2 Server implementation for the Backdoor binary.

This implementation is non-production code, and is intended to be used with a single backdoor client.

## Communication Protocol
The Backdoor binary exfliterates data using DNS requests, the request data is stored in the domain, and the result is stored in the IPv4 bytes.

### Encryption
The request is encrypted in a special way:

The last 3 characters of the host name is the counter, it is encrypted in a rotation based encryption (CounterDecrypter.cs).
After the counter is decrypted it is used as the key to decrypt the rest of the request.

The counter is used as the seed for a Mersenne Twister, which results in the key, for a different rotation based encryption (RequestDataDecrypter.cs).

### Request Type and command offset Encoding
The request type and the command offset are encoded in a format similar to base32, where the letters themselves are amsjl6zci20dbt35guhw7n1fqvx4k8y9rpoe


### Result Encoding
The result data is encoded in Base32.

### Request Types:
#### Request Agent Id
Request Data: aflareon

Response Data: The least significant byte is the agent id.

#### Request Command
Request Data: Agent Id

Response Data: The 3 least significant bytes are the length.

#### Download Command
Request Data: the current offset of the command (in case the command wasn't fully sent)

Response Data: 4 bytes of the command


#### Send Result
Request Data: 7 bytes of the result

Response Data: The same as Request Command

#### Download Command And Send Result
Request Data: 7 bytes of the result

Response Data: The same as Download Command

