using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Builder;
using AsmResolver.IO;
using AsmResolver.PE;
using AsmResolver.PE.DotNet.Builder;
using AsmResolver.PE.File;

namespace BackDoorPatcher
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var image = PEImage.FromFile(@"C:\Projects\Backdoor_FlareOn\FlareOn.Backdoor.exe");
            var module = ModuleDefinition.FromImage(image);
            var patcher = new Patcher(image, module);
            var flag_exe = patcher.GetCilFromHash("5aeb2b97a34799c34e29393116e8075bfb78e90783e791e2c8186417b283fd7d");
            var data_source = new ByteArrayDataSource(flag_exe);
            PESection section = new PESection("5aeb2b97", (AsmResolver.PE.File.Headers.SectionFlags)0xC0000040, new DataSourceSegment(data_source, 0, 0x00022000, (uint)data_source.Length));


            patcher.Patch();
            ManagedPEImageBuilder builder = new ManagedPEImageBuilder();
            var result = builder.CreateImage(module);
            ManagedPEFileBuilder x = new ManagedPEFileBuilder();
            var pe_file = x.CreateFile(result.ConstructedImage);
            pe_file.Sections.Add(section);
            pe_file.Write(@"C:\Projects\Backdoor_FlareOn\FlareOn.Backdoor.patched.exe");
        }
    }
}
