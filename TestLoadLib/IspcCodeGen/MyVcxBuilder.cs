//MIT, 2020, WinterDev
using System;
using System.Collections.Generic;
using System.IO;

namespace BridgeBuilder.Vcx
{
    class MyVcxBuilder : GeneralVcxBuilder
    {
        public MyVcxBuilder() { }
        public string ProjectName { get; set; }
        public string PrimarySource { get; set; }
        public string PrimaryHeader { get; set; }
        public void RebuildLibraryAndAPI()
        {
            UpdateMsBuildPath();


            ProjectConfigKind configKind = ProjectConfigKind;
            SimpleVcxProjGen gen = new SimpleVcxProjGen();
            //set some proprties

            //eg. IspcFilename=> simple.ispc

            string onlyProjectName = ProjectName;

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
            //-------------
            //save primary source and header
            string prim_src_filename = tmp_dir + "/main_src.cpp";
            string prim_header_filename = tmp_dir + "/main_src.h";
            File.WriteAllText(prim_src_filename, PrimarySource);
            File.WriteAllText(prim_header_filename, PrimaryHeader);
            gen.AddCompileFile(prim_src_filename);
            gen.AddIncludeFile(prim_header_filename);

            //string c_interface_filename = gen.FullProjSrcPath + "/" + onlyProjectName + ".cpp";

            //CodeCompilationUnit cu = ParseAutoGenHeaderFromFile(ispc_header);

            //GenerateCBinder(cu, ispc_header, c_interface_filename);

            //string cs_method_invoke_filename = onlyProjectName + ".cs"; //save to

            //GenerateCsBinder(cu, ispc_header, cs_method_invoke_filename, Path.GetFileName(finalProductName));

            ////move cs code to src folder
            //if (AutoCsTargetFile != null)
            //{
            //    MoveFileOrReplaceIfExists(cs_method_invoke_filename, AutoCsTargetFile);
            //}

            //-------------

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

            string vxs_projOutputFilename = "myvcx_autogen.xml.vcxproj";
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
        
    }

}