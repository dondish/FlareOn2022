# BackdoorPatcher
Deobfuscates the backdoor binary. After deobfuscation, it is possible to use it with dnSpy, but it is necessary to run the original exe to solve the solution.

The code here is not production code as it was done in a blitz for the CTF itself, it contains a lot of dead code from my experimentations and I am not going to work on it.

## How is the file obfuscated

There are two steps of obfuscation, both use the same concept.

The functions that have the prefix "flared" are dynamic, And their actual CIL body is garbage. The code itself is encrypted inside its own PE section.

### First Step
The functions needed to load the rest of the functions need to be extracted, the code and relocations are stored in the binary, they were extracted into ILConstants.cs.
In order to view the functions and related data, you can refer to the mappings property of Patcher.

### Second Step
The rest of the functions are encrypted and stored in their own PE Section.
The PE Section name is the first 8 character of the hex representation of the SHA256 hash of:
* The method body length
* Parameter types' names
* Parameter names
* Function name
* Local variable names
* Return type
* Function type

The section is encrypted in a custom weak encryption, the implementation is in DecryptCil in Patcher.

