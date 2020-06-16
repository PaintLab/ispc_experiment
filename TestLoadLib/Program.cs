//MIT, 2020, WinterDev
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;

using BridgeBuilder;

namespace TestLoadLib
{
    class Program
    {
        static string MSBUILD_PATH = "";
        static void UpdateMsBuildPath()
        {
            string[] msbuildPathTryList = new string[]
            {
                @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
                @"C:\Program Files (x86)\Microsoft Visual Studio\2019\MSBuild\15.0\Bin\MSBuild.exe"
            };

            bool foundMsBuild = false;
            for (int i = msbuildPathTryList.Length - 1; i >= 0; --i)
            {
                if (File.Exists(msbuildPathTryList[i]))
                {
                    MSBUILD_PATH = msbuildPathTryList[i];
                    foundMsBuild = true;
                    break;//
                }
            }
            if (!foundMsBuild)
            {
                System.Diagnostics.Debug.Write("MSBUILD not found!");
            }
        }

        static CodeCompilationUnit ParseAutoGenHeader(string content)
        {
            //at this version, use a simple parser
            //very specific to this header
            List<string> lines = new List<string>();
            bool startCollecting = false;

            using (StringReader reader = new StringReader(content))
            {
                string line = reader.ReadLine();
                while (line != null)
                {
                    if (!startCollecting)
                    {
                        if (line.StartsWith("extern \"C\" {"))
                        {
                            startCollecting = true;
                        }
                    }
                    else
                    {
                        if (!line.StartsWith("#"))
                        {

                            if (line == "} /* end extern C */")
                            {
                                break;
                            }
                            lines.Add(line);
                        }
                    }
                    line = reader.ReadLine();
                }
            }

            //
            HeaderParser headerParser = new HeaderParser();
            headerParser.Parse("virtual_filename", lines);
            return headerParser.Result;
        }


        static void GenerateCBinder(string ispc_headerFilename, string outputC_filename)
        {
            string header_content = File.ReadAllText(ispc_headerFilename);

            CodeCompilationUnit cu = ParseAutoGenHeader(header_content);
            //from cu=> we generate interface c 
            CodeStringBuilder sb = new CodeStringBuilder();
            sb.AppendLine("//AUTOGEN," + DateTime.Now.ToString("s"));
            sb.AppendLine("#include <stdio.h>");
            sb.AppendLine("#include <stdlib.h>");
            sb.AppendLine("//Include the header file that the ispc compiler generates");
            sb.AppendLine($"#include \"{ Path.GetFileName(ispc_headerFilename) }\"");

            //c_inf.AppendLine("using namespace ispc;");
            sb.AppendLine("extern \"C\"{");

            foreach (CodeMemberDeclaration mb in cu.GlobalTypeDecl.GetMemberIter())
            {
                if (mb is CodeMethodDeclaration met)
                {
                    sb.AppendLine("__declspec(dllexport) ");
                    sb.Append(met.ToString("my_"));
                    sb.AppendLine("{");
                    string ret = met.ReturnType.ToString();
                    if (ret != "void")
                    {
                        sb.Append("return ");
                    }
                    sb.Append("ispc::" + met.Name + "(");

                    int par_i = 0;
                    foreach (CodeMethodParameter par in met.Parameters)
                    {
                        if (par_i > 0) { sb.Append(","); }
                        sb.Append(par.ParameterName);
                        par_i++;
                    }
                    sb.AppendLine(");");
                    sb.AppendLine("}");
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            sb.AppendLine("}");//close extern

            string file_content = sb.ToString();
            File.WriteAllText(outputC_filename, file_content);
        }

        static void GenerateCsBinder(string ispc_headerFilename, string outputCs_filename, string nativeLibName)
        {
            string header_content = File.ReadAllText(ispc_headerFilename);

            CodeCompilationUnit cu = ParseAutoGenHeader(header_content);
            //from cu=> we generate interface c 
            CodeStringBuilder sb = new CodeStringBuilder();
            sb.AppendLine("//AUTOGEN," + DateTime.Now.ToString("s"));
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Runtime.InteropServices;");

            sb.AppendLine("using int32_t = System.Int32;");
            sb.AppendLine("namespace " + Path.GetFileNameWithoutExtension(Path.GetFileName(ispc_headerFilename)) + "{");
            sb.AppendLine("public static unsafe class NativeMethods{");

            foreach (CodeMemberDeclaration mb in cu.GlobalTypeDecl.GetMemberIter())
            {
                if (mb is CodeMethodDeclaration met)
                {
                    string retType = met.ReturnType.ToString();
                    sb.AppendLine($"[DllImport(\"{nativeLibName}\",EntryPoint =\"{"my_" + met.Name}\")]");
                    sb.Append("public static extern ");
                    sb.Append(retType);
                    sb.Append(" ");
                    sb.Append(met.Name);
                    sb.Append("(");

                    for (int i = 0; i < met.Parameters.Count; ++i)
                    {
                        CodeMethodParameter par = met.Parameters[i];
                        if (i > 0) { sb.Append(","); }
                        sb.Append(par.ParameterType.ToString());
                        sb.Append(" ");
                        sb.Append(par.ParameterName);
                    }

                    sb.Append(")");
                    sb.AppendLine(";");
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            sb.AppendLine("}");//static class NativeMethods
            sb.AppendLine("}");//namespace

            string file_content = sb.ToString();
            File.WriteAllText(outputCs_filename, file_content);
        }

        static void RebuildLibraryAndAPI()
        {

            UpdateMsBuildPath();

            ProjectConfigKind configKind = ProjectConfigKind.Debug;
            SimpleVcxProjGen gen = new SimpleVcxProjGen();
            //set some proprties
            gen.ProjectName = "simple"; //project name and output            

            string current_dir = Directory.GetCurrentDirectory();
            string tmp_dir = current_dir + "/temp";
            gen.FullProjSrcPath = current_dir;
            gen.FullProjBuildPath = tmp_dir;
            string finalProductName = gen.GetFinalProductName(configKind);

            //build ispc                 
            string ispc = @"D:\projects\ispc-14-dev-windows\bin\ispc.exe";

            if (!Directory.Exists(tmp_dir))
            {
                Directory.CreateDirectory(tmp_dir);
            }

            string ispc_src = "simple.ispc";
            string ispc_obj = tmp_dir + "/simple_ispc.obj";
            string ispc_llvm_text = tmp_dir + "/simple_ispc.llvm.txt";
            string ispc_cpp = tmp_dir + "/simple_ispc.cpp";
            string ispc_header = tmp_dir + "/simple_ispc.h";

            //generate header and object file in temp dir
            var procStartInfo = new System.Diagnostics.ProcessStartInfo(ispc,
             $"{ispc_src} -O2 -o {ispc_obj} -h {ispc_header}");

            //$"{ispc_src} --emit-c++ -o {ispc_cpp}");
            //$"{ispc_src} --emit-llvm-text -o {ispc_llvm_text} -h {ispc_header}");

            procStartInfo.UseShellExecute = false;
            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.RedirectStandardError = true;
            System.Diagnostics.Process proc = System.Diagnostics.Process.Start(procStartInfo);

            var errReader = proc.StandardError;
            {
                string line = errReader.ReadLine();

                while (line != null)
                {
                    line = errReader.ReadLine();
                }
            }
            var outputStrmReader = proc.StandardOutput;
            {
                string line = outputStrmReader.ReadLine();
                while (line != null)
                {
                    line = outputStrmReader.ReadLine();
                }
            }

            proc.WaitForExit();
            int exit_code2 = proc.ExitCode;
            if (exit_code2 != 0)
            {
                throw new NotSupportedException();
            }
            //----------
            //now read auto-gen header

            string c_interface_filename = gen.FullProjSrcPath + "/simple/simple.cpp";
            GenerateCBinder(ispc_header, c_interface_filename);

            string cs_method_invoke_filename = "simple.cs";
            GenerateCsBinder(ispc_header, cs_method_invoke_filename, Path.GetFileName(finalProductName));
            //move cs code to src folder


            //
            //at this step we have object file and header
            //build a cpp dll with msbuild  
            gen.AddObjectFile(ispc_obj);
            gen.AddIncludeFile(ispc_header);
            //add our c-interface
            gen.AddCompileFile(c_interface_filename);

            VcxProject project = gen.CreateVcxTemplate();

            XmlOutputGen xmlOutput = new XmlOutputGen();
            project.GenerateOutput(xmlOutput);

            string vxs_projOutputFilename = "test2_1.xml.vcxproj";
            File.WriteAllText(vxs_projOutputFilename, xmlOutput.Output.ToString());

            //debug build or release build

            string p_config = "";
            switch (configKind)
            {
                default: throw new NotSupportedException();
                case ProjectConfigKind.Debug:
                    p_config = " /p:configuration=debug";
                    break;
                case ProjectConfigKind.Release:
                    p_config = " /p:configuration=release";
                    break;
            }

            System.Diagnostics.Process proc1 = System.Diagnostics.Process.Start(MSBUILD_PATH, vxs_projOutputFilename + p_config);
            proc1.WaitForExit();
            int exit_code1 = proc1.ExitCode;
            if (exit_code1 != 0)
            {
                throw new NotSupportedException();
            }

            //build pass, then copy the result dll back     

            File.Move(finalProductName, Path.GetFileName(finalProductName));
        }
        static void Main(string[] args)
        {

            //----------
            //more easier
            //----------

            string dllName = "simple.dll";


            //TODO: check if we need to rebuild or not
            bool rebuild = true;
            if (rebuild)
            {
                RebuildLibraryAndAPI();
            }

            IntPtr dllPtr = LoadLibrary(dllName);

            if (dllPtr == IntPtr.Zero) { throw new NotSupportedException(); }

            IntPtr funct = GetProcAddress(dllPtr, "my_simple"); //test
            if (funct == IntPtr.Zero) { throw new NotSupportedException(); }


            //test1
            int[] inputData = new int[]
            {
                1<<16,  2<<16, 3<<16,  4<<16,
                5<<16,  6<<16, 7<<16,  8<<16,
                9<<16, 10<<16, 11<<16,  12<<16,
            };
            unsafe
            {
                fixed (int* h = &inputData[0])
                {
                    simple_ispc.NativeMethods.clear(h, 0, inputData.Length);
                }
            }

            //test2
            //int[] outputData = new int[inputData.Length];
            //unsafe
            //{
            //    fixed (int* output_h = &outputData[0])
            //    fixed (int* h = &inputData[0])
            //    {
            //        flipY_and_swap_((IntPtr)h, (IntPtr)output_h, 4, 3);
            //    }
            //}
        }


        [DllImport("Kernel32.dll")]
        static extern IntPtr LoadLibrary(string libraryName);
        [DllImport("Kernel32.dll")]
        static extern bool FreeLibrary(IntPtr hModule);
        [DllImport("Kernel32.dll")]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);


    }
}
