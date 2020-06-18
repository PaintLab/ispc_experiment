//MIT, 2020, WinterDev
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using BridgeBuilder.Vcx;

namespace BridgeBuilder.Ispc
{
    class IspcBuilder
    {
        //helper for Intel® Implicit SPMD Program Compiler (ISPC)
        //see more about ispc =>https://ispc.github.io/

        static string s_MsBuildPath = null;
        static string s_ispc = @"D:\projects\ispc-14-dev-windows\bin\ispc.exe";
        static bool s_checkIspc;



        public ProjectConfigKind ProjectConfigKind { get; set; } = ProjectConfigKind.Debug;
        public string IspcFilename { get; set; }
        public string IspcBridgeFunctionNamePrefix { get; set; } = "my_";
        public string AutoCsTargetFile { get; set; }

        public string[] AdditionalInputItems { get; set; }

        public void RebuildLibraryAndAPI()
        {

            UpdateMsBuildPath();
            CheckIspcBinary();

            ProjectConfigKind configKind = ProjectConfigKind;
            SimpleVcxProjGen gen = new SimpleVcxProjGen();
            //set some proprties

            //eg. IspcFilename=> simple.ispc

            string onlyProjectName = Path.GetFileNameWithoutExtension(IspcFilename);

            gen.ProjectName = onlyProjectName; //project name and output            

            string current_dir = Directory.GetCurrentDirectory();
            string tmp_dir = current_dir + "/temp";
            gen.FullProjSrcPath = current_dir;
            gen.FullProjBuildPath = tmp_dir;
            string finalProductName = gen.GetFinalProductName(configKind);

            //build ispc                 

            if (!Directory.Exists(tmp_dir))
            {
                Directory.CreateDirectory(tmp_dir);
            }

            string ispc_src = IspcFilename;

            string ispc_obj = tmp_dir + "/" + onlyProjectName + ".obj";
            string ispc_llvm_text = tmp_dir + "/" + onlyProjectName + "_ispc.llvm.txt";
            string ispc_cpp = tmp_dir + "/" + onlyProjectName + "_ispc.cpp";
            string ispc_header = tmp_dir + "/" + onlyProjectName + "_ispc.h";


            //generate header and object file in temp dir
            var procStartInfo = new System.Diagnostics.ProcessStartInfo(s_ispc,
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

            string c_interface_filename = gen.FullProjSrcPath + "/" + onlyProjectName + ".cpp";
            GenerateCBinder(ispc_header, c_interface_filename);

            string cs_method_invoke_filename = onlyProjectName + ".cs"; //save to
            GenerateCsBinder(ispc_header, cs_method_invoke_filename, Path.GetFileName(finalProductName));
            //move cs code to src folder


            if (AutoCsTargetFile != null)
            {
                MoveFileOrReplaceIfExists(cs_method_invoke_filename, AutoCsTargetFile);
            }

            //
            //at this step we have object file and header
            //build a cpp dll with msbuild  
            gen.AddObjectFile(ispc_obj);
            gen.AddIncludeFile(ispc_header);
            //add our c-interface
            gen.AddCompileFile(c_interface_filename);

            if (AdditionalInputItems != null)
            {
                foreach (string s in AdditionalInputItems)
                {
                 

                    switch (Path.GetExtension(s))
                    {
                        default: throw new NotSupportedException();
                        case ".c":
                        case ".cpp":
                            gen.AddCompileFile(s);
                            break;
                        case ".h":
                        case ".hpp":
                            gen.AddIncludeFile(s);
                            break;
                        case ".obj":
                            gen.AddObjectFile(s);
                            break;
                    }
                }
            }


            VcxProject project = gen.CreateVcxTemplate();

            XmlOutputGen xmlOutput = new XmlOutputGen();
            project.GenerateOutput(xmlOutput);

            string vxs_projOutputFilename = "ispc_autogen.xml.vcxproj";
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

            System.Diagnostics.Process proc1 = System.Diagnostics.Process.Start(s_MsBuildPath, vxs_projOutputFilename + p_config);
            proc1.WaitForExit();
            int exit_code1 = proc1.ExitCode;
            if (exit_code1 != 0)
            {
                throw new NotSupportedException();
            }

            //build pass, then copy the result dll back     
            MoveFileOrReplaceIfExists(finalProductName, Path.GetFileName(finalProductName));

        }
        static void MoveFileOrReplaceIfExists(string src, string dest)
        {
            if (File.Exists(dest))
            {
                //delete
                File.Delete(dest);
            }
            File.Move(src, dest);
        }
        static void UpdateMsBuildPath()
        {
            if (s_MsBuildPath != null) { return; }

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
                    s_MsBuildPath = msbuildPathTryList[i];
                    foundMsBuild = true;
                    break;//
                }
            }
            if (!foundMsBuild)
            {
                System.Diagnostics.Debug.Write("MSBUILD not found!");
            }
        }

        static void CheckIspcBinary()
        {
            if (s_checkIspc)
            {
                return;
            }
            if (File.Exists(s_ispc))
            {
                s_checkIspc = true;
            }
            else
            {
                throw new NotSupportedException();
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

        /// <summary>
        /// generate C binder
        /// </summary>
        /// <param name="ispc_headerFilename"></param>
        /// <param name="outputC_filename"></param>
        void GenerateCBinder(string ispc_headerFilename, string outputC_filename)
        {
            string header_content = File.ReadAllText(ispc_headerFilename);

            CodeCompilationUnit cu = ParseAutoGenHeader(header_content);
            //from cu=> we generate interface c 
            CodeStringBuilder sb = new CodeStringBuilder();
            sb.AppendLine("//AUTOGEN," + DateTime.Now.ToString("s"));
            sb.AppendLine("//Tools: ispc and BridgeBuilder");
            sb.AppendLine("//Src: " + ispc_headerFilename);

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

                    sb.AppendLine("__declspec(dllexport) "); //WINDOWS

                    sb.Append(met.ToString(IspcBridgeFunctionNamePrefix));

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

        /// <summary>
        /// generate CS binder
        /// </summary>
        /// <param name="ispc_headerFilename"></param>
        /// <param name="outputCs_filename"></param>
        /// <param name="nativeLibName"></param>
        void GenerateCsBinder(string ispc_headerFilename, string outputCs_filename, string nativeLibName)
        {
            string header_content = File.ReadAllText(ispc_headerFilename);

            CodeCompilationUnit cu = ParseAutoGenHeader(header_content);
            //from cu=> we generate interface c 
            CodeStringBuilder sb = new CodeStringBuilder();
            sb.AppendLine("//AUTOGEN," + DateTime.Now.ToString("s"));
            sb.AppendLine("//Tools: ispc and BridgeBuilder");
            sb.AppendLine("//Src: " + ispc_headerFilename);
            sb.AppendLine();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Runtime.InteropServices;");

            sb.AppendLine();
            sb.AppendLine("using int32_t = System.Int32;");
            sb.AppendLine("using uint32_t = System.UInt32;");
            sb.AppendLine();

            //
            sb.AppendLine("namespace " + Path.GetFileNameWithoutExtension(Path.GetFileName(ispc_headerFilename)) + "{");
            sb.AppendLine("public static unsafe class NativeMethods{");
            sb.AppendLine($"const string LIB_NAME=\"{nativeLibName}\";");

            foreach (CodeMemberDeclaration mb in cu.GlobalTypeDecl.GetMemberIter())
            {
                if (mb is CodeMethodDeclaration met)
                {
                    string retType = met.ReturnType.ToString();
                    sb.AppendLine($"[DllImport(LIB_NAME,EntryPoint =\"{IspcBridgeFunctionNamePrefix + met.Name}\")]");
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
    }
}