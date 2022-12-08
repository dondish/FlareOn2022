using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.IO;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace BackDoorPatcher
{
    internal class HeaderReader
    {
        private BinaryStreamReader reader;
        public byte headerSize;
        public bool isFat;
        public ushort flags;
        public ushort maxStack;
        public uint codeSize;
        public uint localVarSigTok;
        public ulong startOfHeader;


        public HeaderReader(BinaryStreamReader reader)
        {
            this.reader = reader;
        }   

        public void ParseHeader()
        {
            startOfHeader = reader.Offset;
            byte b = reader.ReadByte();
            switch (b & 7)
            {
                case 2:
                case 6:
                    // Tiny header. [7:2] = code size, max stack is 8, no locals or exception handlers
                    flags = 2;
                    maxStack = 8;
                    codeSize = (uint)(b >> 2);
                    localVarSigTok = 0;
                    headerSize = 1;
                    isFat = false;
                    break;

                case 3:
                    // Fat header. Can have locals and exception handlers
                    flags = (ushort)((reader.ReadByte() << 8) | b);
                    headerSize = (byte)(flags >> 12);
                    maxStack = reader.ReadUInt16();
                    codeSize = reader.ReadUInt32();
                    localVarSigTok = reader.ReadUInt32();

                    // The CLR allows the code to start inside the method header. But if it does,
                    // the CLR doesn't read any exceptions.
                    reader.Offset = reader.Offset - 12 + headerSize * 4U;
                    if (headerSize < 3)
                        flags &= 0xFFF7;
                    headerSize *= 4;
                    isFat = true;
                    break;
            }
        }

        public IList<CilLocalVariable> ReadLocals()
        {
            var result = new List<CilLocalVariable>();
            //var module = method_body.Owner.Module;
            //if (localVarSigTok != MetadataToken.Zero
            //   && module.TryLookupMember(localVarSigTok, out var member))
            //{
            //    var standaloneSig = member as StandAloneSignature;
            //    if (standaloneSig == null) return null;
            //    var variableTypes = (standaloneSig.Signature as LocalVariablesSignature).VariableTypes;
            //    for (int i = 0; i < variableTypes.Count; i++)
            //        result.Add(new CilLocalVariable(variableTypes[i]));
            //}
            return result;
        }
    }
}
