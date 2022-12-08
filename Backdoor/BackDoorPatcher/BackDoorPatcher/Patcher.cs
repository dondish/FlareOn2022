using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Serialized;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.IO;
using AsmResolver.PE;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using AsmResolver.PE.File;
using static System.Net.Mime.MediaTypeNames;

namespace BackDoorPatcher
{
    internal class Patcher
    {
        private IPEImage image;
        private ModuleDefinition module;

        private IList<Tuple<byte[], Dictionary<uint, int>, string, string>> mappings = new List<Tuple<byte[], Dictionary<uint, int>, string, string>>
        {
            new Tuple<byte[], Dictionary<uint, int>, string, string>(ILConstants.pe_b, ILConstants.pe_m, "FLARE09", "flared_37"),
            new Tuple<byte[], Dictionary<uint, int>, string, string>(ILConstants.d_b, ILConstants.d_m, "FLARE12", "flared_47"),
            new Tuple<byte[], Dictionary<uint, int>, string, string>(ILConstants.gh_b, ILConstants.gh_m, "FLARE15", "flared_66"),
            new Tuple<byte[], Dictionary<uint, int>, string, string>(ILConstants.cl_b, ILConstants.cl_m, "FLARE15", "flared_67"),
            new Tuple<byte[], Dictionary<uint, int>, string, string>(ILConstants.rt_b, new Dictionary<uint, int>(), "FLARE15", "flared_68"),
            new Tuple<byte[], Dictionary<uint, int>, string, string>(ILConstants.gs_b, ILConstants.gs_m, "FLARE15", "flared_69"),
            new Tuple<byte[], Dictionary<uint, int>, string, string>(ILConstants.wl_b, ILConstants.wl_m, "FLARE15", "flared_70"),

        };

        public Patcher(IPEImage image, ModuleDefinition module)
        {
            this.image = image;
            this.module = module;
            ProcessModifiers();
        }

        private void ProcessModifiers()
        {
            foreach (var mapping in mappings)
            {
                var array = mapping.Item1;
                Console.WriteLine(array.Length);
                var modifiers = mapping.Item2;
                foreach (var modifier_item in modifiers)
                {
                    var index = modifier_item.Key;
                    var token = modifier_item.Value;
                    array[index] = (byte)token;
                    array[index + 1] = (byte)(token >> 8);
                    array[index + 2] = (byte)(token >> 16);
                    array[index + 3] = (byte)(token >> 24);
                }
            }
        }

        private byte[] GetArrayOfMethodFunction(string className, string functionName)
        {
            foreach (var mapping in mappings)
            {
                var mappingClass = mapping.Item3;
                var mappingFunction = mapping.Item4;

                if (className == mappingClass && mappingFunction == functionName)
                {
                    return mapping.Item1;
                }

            }

            return null;

        }

        public void Patch()
        {
            foreach (var type in this.module.TopLevelTypes)
            {
                foreach (var method in type.Methods)
                {
                    var cil = GetArrayOfMethodFunction(type.Name, method.Name);
                    if (!method.Name.Contains("flared")) continue;

                    if (cil == null)
                    {
                        GenericPatch(method);
                    }
                    else
                    {
                        PatchCil(method, cil);

                    }
                }
            }





        }

        private MethodDefinitionRow GetRowOfMethod(MethodDefinition method)
        {
            SerializedModuleDefinition module = (SerializedModuleDefinition)this.module;
            var tables_stream = module.ReaderContext.Metadata.GetStream<TablesStream>();
            var token = method.MetadataToken;
            var table = tables_stream.GetTable(token.Table);
            var row = table[(int)(token.Rid - 1)];
            return (MethodDefinitionRow)row;
        }

        private CilRawMethodBody GetNewMethodBody(MethodDefinition method)
        {
            var row = GetRowOfMethod(method);
            var reader = row.Body.CreateReader();
            return CilRawMethodBody.FromReader(ref reader);
        }

        public void GenericPatch(MethodDefinition method)
        {
            var hash = GetHashOfMethod(method);
            try
            {
                var cil = GetCilFromHash(hash);
                cil = DecryptCil(cil, new byte[] { 18, 120, 171, 223 });
                AddCilModifiers(cil);
                PatchCil(method, cil);
            }
            catch (KeyNotFoundException)
            {
                var raw_body = GetNewMethodBody(method);
                var number_of_nop = raw_body.Code.GetPhysicalSize() - 1;
                var cil = new List<byte>();
                for (int i = 0; i < number_of_nop; i++)
                {
                    cil.Add(0);
                }
                cil.Add(0x2a);
                PatchCil(method, cil.ToArray());
            }
        }

        public string GetTypeName(TypeSignature type)
        {
            if (type.ElementType == ElementType.GenericInst)
            {
                return type.FullName.Replace("<", "[").Replace(">", "]");
            }
            return type.FullName;
        }

        public string GetHashOfMethod(MethodDefinition method)
        {
            string text = "";
            string text2 = "";
            var signature = method.Signature;
            var attributes = (System.Reflection.MethodAttributes)method.Attributes;
            byte[] bytes = Encoding.ASCII.GetBytes(attributes.ToString());
            byte[] bytes2 = Encoding.ASCII.GetBytes(GetTypeName(signature.ReturnType));
            byte[] bytes3 = Encoding.ASCII.GetBytes("Standard");
            foreach (var parameterType in signature.ParameterTypes)
            {
                string text3 = text2;
                text2 = text3 + GetTypeName(parameterType);
            }
            var raw_body = GetNewMethodBody(method);
            var fat_raw_body = raw_body as CilRawFatMethodBody;
            var max_stack_size = raw_body.IsFat ? fat_raw_body.MaxStack : 8;
            byte[] bytes4 = Encoding.ASCII.GetBytes(max_stack_size.ToString());
            var il_length = raw_body.Code.GetPhysicalSize();
            byte[] bytes5 = BitConverter.GetBytes(il_length);
            if (raw_body.IsFat)
            {
                var localVarSigTok = fat_raw_body.LocalVarSigToken;
                if (localVarSigTok != MetadataToken.Zero
                    && module.TryLookupMember(localVarSigTok, out var member))
                {
                    var standaloneSig = member as StandAloneSignature;
                    if (standaloneSig == null) return null;
                    var variableTypes = (standaloneSig.Signature as LocalVariablesSignature).VariableTypes;
                    for (int i = 0; i < variableTypes.Count; i++)
                    {
                        var type = variableTypes[i];
                        string text4 = text;
                        text = text4 + GetTypeName(type);
                    }
                }

            }
            byte[] bytes6 = Encoding.ASCII.GetBytes(text);
            byte[] bytes7 = Encoding.ASCII.GetBytes(text2);
            IncrementalHash incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            incrementalHash.AppendData(bytes5);
            incrementalHash.AppendData(bytes);
            incrementalHash.AppendData(bytes2);
            incrementalHash.AppendData(bytes4);
            incrementalHash.AppendData(bytes6);
            incrementalHash.AppendData(bytes7);
            incrementalHash.AppendData(bytes3);
            byte[] hashAndReset = incrementalHash.GetHashAndReset();
            StringBuilder stringBuilder = new StringBuilder(hashAndReset.Length * 2);
            for (int j = 0; j < hashAndReset.Length; j++)
            {
                stringBuilder.Append(hashAndReset[j].ToString("x2"));
            }
            return stringBuilder.ToString();

        }

        public byte[] GetCilFromHash(string hash)
        {
            var file = PEFile.FromFile(image.FilePath);
            foreach (var section in file.Sections)
            {
                if (hash.StartsWith(section.Name))
                {
                    byte[] data = new byte[section.GetVirtualSize()];
                    section.CreateReader().ReadBytes(data, 0, (int)section.GetVirtualSize());
                    return data;
                }
            }
            throw new KeyNotFoundException("Section hash not found");
        }

        public byte[] DecryptCil(byte[] cil, byte[] key)
        {
            int[] array = new int[256];
            int[] array2 = new int[256];
            byte[] array3 = new byte[cil.Length];
            int i;
            for (i = 0; i < 256; i++)
            {
                array[i] = (int)key[i % key.Length];
                array2[i] = i;
            }
            int num;
            for (i = (num = 0); i < 256; i++)
            {
                num = (num + array2[i] + array[i]) % 256;
                int num2 = array2[i];
                array2[i] = array2[num];
                array2[num] = num2;
            }
            int num3;
            num = (num3 = (i = 0));
            while (i < cil.Length)
            {
                num3++;
                num3 %= 256;
                num += array2[num3];
                num %= 256;
                int num2 = array2[num3];
                array2[num3] = array2[num];
                array2[num] = num2;
                int num4 = array2[(array2[num3] + array2[num]) % 256];
                array3[i] = (byte)((int)cil[i] ^ num4);
                i++;
            }
            return array3;
        }

        public void AddCilModifiers(byte[] cil)
        {
            int j = 0;
            while (j < cil.Length)
            {
                bool flag = cil[j] == 254;
                uint num;
                if (flag)
                {
                    num = 65024U + (uint)cil[j + 1];
                    j++;
                }
                else
                {
                    num = (uint)cil[j];
                }
                ILConstants.OT ot = ILConstants.cil_modifiers[num];
                j++;
                switch (ot)
                {
                    case ILConstants.OT.B:
                        {
                            uint num2 = (uint)GetCilLocalHash(cil, j);
                            num2 ^= 2727913149U;
                            cil[j] = (byte)num2;
                            cil[j + 1] = (byte)(num2 >> 8);
                            cil[j + 2] = (byte)(num2 >> 16);
                            cil[j + 3] = (byte)(num2 >> 24);
                            j += 4;
                            break;
                        }
                    case ILConstants.OT.C:
                    case ILConstants.OT.E:
                        j++;
                        break;
                    case ILConstants.OT.D:
                    case ILConstants.OT.G:
                        j += 4;
                        break;
                    case ILConstants.OT.F:
                        j += 2;
                        break;
                    case ILConstants.OT.H:
                        j += 8;
                        break;
                    case ILConstants.OT.I:
                        j += 4 + GetCilLocalHash(cil, j) * 4;
                        break;
                }
            }
        }

        private int GetCilLocalHash(byte[] cil, int index)
        {
            int num = (int)cil[index + 3] * 16777216;
            num += (int)cil[index + 2] * 65536;
            num += (int)cil[index + 1] * 256;
            return num + (int)cil[index];

        }

        public void PatchCil(MethodDefinition method, byte[] cil)
        {

            SerializedModuleDefinition module = (SerializedModuleDefinition)this.module;
            var raw_body = GetNewMethodBody(method);
            raw_body.Code = new DataSegment(cil);
            CilMethodBody cilMethodBody = CilMethodBody.FromRawMethodBody(module.ReaderContext, method, raw_body);
            method.CilMethodBody = cilMethodBody;
            cilMethodBody.ComputeMaxStackOnBuild = false;

            //var data_source = new ByteArrayDataSource(cil);
            //var reader = new BinaryStreamReader(data_source, 0, 0, (uint) cil.Length);
            //var operand_resolver = new PhysicalCilOperandResolver(this.module, method_body);
            //var dissassembler = new CilDisassembler(reader, operand_resolver);
            //var disassembly = dissassembler.ReadInstructions();
            //Console.WriteLine(module.Name + "." + method_body.Owner.Name); 
            //foreach (var instruction in disassembly)
            //{
            //    Console.WriteLine(instruction.ToString());
            //}
            //method_body.Instructions.Clear();
            //method_body.Instructions.AddRange(disassembly);
        }
        private void PatchWl(CilMethodBody method_body)
        {
            method_body.Instructions.Clear();

            method_body.Instructions.Add(CilOpCodes.Nop);
            method_body.Instructions.Add(CilOpCodes.Ldarg_0);
            method_body.Instructions.Add(CilOpCodes.Newobj, new MetadataToken(0xa0000e5));  // StackTrace
            method_body.Instructions.Add(CilOpCodes.Stloc_0);
            method_body.Instructions.Add(CilOpCodes.Ldloc_0);
            method_body.Instructions.Add(CilOpCodes.Ldc_I4_0);
            method_body.Instructions.Add(CilOpCodes.Callvirt, new MetadataToken(0xa0000a3)); // GetFrame(0)
            method_body.Instructions.Add(CilOpCodes.Callvirt, new MetadataToken(0xa0000a4)); // .GetMethod()
            method_body.Instructions.Add(CilOpCodes.Callvirt, new MetadataToken(0xa0000e6)); // MetadataToken
            method_body.Instructions.Add(CilOpCodes.Stloc_1); // metadata_token = new StackTrace(exception).GetFrame(0).GetMethod().MetadataToken;
            method_body.Instructions.Add(CilOpCodes.Ldloc_1);
            method_body.Instructions.Add(CilOpCodes.Call, new MetadataToken(0x60000b3));
            method_body.Instructions.Add(CilOpCodes.Stloc_2); // // var res1 = FLARE15.flare_66(metadata_token); // gh
            method_body.Instructions.Add(CilOpCodes.Ldloc_2);
            method_body.Instructions.Add(CilOpCodes.Call, new MetadataToken(0x60000b9));
            method_body.Instructions.Add(CilOpCodes.Stloc_3); // var res2 =  FLARE16.flare_69(res1); // gs
            method_body.Instructions.Add(CilOpCodes.Ldc_I4_4);
            method_body.Instructions.Add(CilOpCodes.Newarr, new MetadataToken(0x100002f)); // array<byte>
            method_body.Instructions.Add(CilOpCodes.Dup);
            method_body.Instructions.Add(CilOpCodes.Ldtoken, new MetadataToken(0x4000140)); // C91849C78D4D52D51AE27BD136F927AE1418705C0A2BC9066D6F38125967F602 
            method_body.Instructions.Add(CilOpCodes.Call, new MetadataToken(0xa000092)); // new Array
            method_body.Instructions.Add(CilOpCodes.Ldloc_3); // new array<byte>()
            method_body.Instructions.Add(CilOpCodes.Call, new MetadataToken(0x6000080)); // Flare12.flare_46
            method_body.Instructions.Add(CilOpCodes.Stloc_S, method_body.LocalVariables[4]); // var res3 = flare_46
            method_body.Instructions.Add(CilOpCodes.Ldloc_S, method_body.LocalVariables[4]);
            method_body.Instructions.Add(CilOpCodes.Ldloc_1);
            method_body.Instructions.Add(CilOpCodes.Ldarg_1);
            method_body.Instructions.Add(CilOpCodes.Call, new MetadataToken(0x60000b5)); // Flare15.flare_67
            method_body.Instructions.Add(CilOpCodes.Stloc_S, method_body.LocalVariables[5]);
            method_body.Instructions.Add(CilOpCodes.Ldloc_S, method_body.LocalVariables[5]);
            method_body.Instructions.Add(CilOpCodes.Stloc_S, method_body.LocalVariables[6]);
            //method_body.Instructions.Add(CilOpCodes.Br_S, (byte)0);
            method_body.Instructions.Add(CilOpCodes.Ldloc_S, method_body.LocalVariables[6]);
            method_body.Instructions.Add(CilOpCodes.Ret);
        }
    }
}
