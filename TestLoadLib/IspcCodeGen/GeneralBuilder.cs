//MIT, 2020, WinterDev
using System;
using System.Collections.Generic;
using System.IO;

namespace BridgeBuilder.Vcx
{
    abstract class GeneralVcxBuilder
    {
        protected static string s_MsBuildPath = null;
        public string[] AdditionalInputItems { get; set; }
        public ProjectConfigKind ProjectConfigKind { get; set; } = ProjectConfigKind.Debug;
        protected static void UpdateMsBuildPath()
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
        protected static void MoveFileOrReplaceIfExists(string src, string dest)
        {
            if (File.Exists(dest))
            {
                //delete
                File.Delete(dest);
            }
            File.Move(src, dest);
        }
    }



}