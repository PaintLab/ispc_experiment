//MIT, 2020, WinterDev
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace TestLoadLib.OldCode
{
    class ExportFunc
    {
        public string OriginalName { get; private set; }
        public string ProperCsName { get; private set; }
        public ExportFunc(string orgName)
        {
            OriginalName = orgName;
            //then parse
            int pos = orgName.IndexOf("___");
            if (pos < 0)
            {
                //found
                ProperCsName = orgName;
            }
            else
            {
                ProperCsName = orgName.Substring(0, pos);
            }
        }
        public override string ToString()
        {
            return ProperCsName + " : " + OriginalName;
        }

        public IntPtr NaitveFuncPtr { get; set; }
    }


    static class OldCodeBuilder
    {
        //need delegate name preserved
        //export void swap_rb(uniform int v_in[], uniform int count){
        delegate void swap_rb(IntPtr inputArr, int len);

        //export void flipY_and_swap(uniform int v_in[],
        //                   uniform int v_out[],
        //                   uniform int width,
        //                   uniform int height){
        delegate void flipY_and_swap(IntPtr inputArr, IntPtr outputArr, int width, int height);

        //export void clear(uniform int v_in[],uniform int newValue,uniform int count)
        delegate void clear(IntPtr inputArr, int newValue, int count);


        public static void OldCode()
        {
            //test build dll from obj file 
            string msvc_sdk = Resolver.GetMsvcSdkPath();

            if (msvc_sdk == null)
            {
                throw new NotSupportedException();
            }

            //---------             
            {
                var procStartInfo = new System.Diagnostics.ProcessStartInfo(msvc_sdk + "\\link.exe", "/DLL /out:simple.dll /NOENTRY *.obj");
                procStartInfo.UseShellExecute = false;
                System.Diagnostics.Process proc = System.Diagnostics.Process.Start(procStartInfo);
                proc.WaitForExit();

                int exit_code = proc.ExitCode;
            }
            //---------
            ExportFunc[] exportFuncs = null;
            {
                //dump all functions from lib
                var procStartInfo = new System.Diagnostics.ProcessStartInfo(msvc_sdk + "\\dumpbin.exe",
                   "/EXPORTS simple.dll");
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                System.Diagnostics.Process proc = System.Diagnostics.Process.Start(procStartInfo);
                StreamReader r = proc.StandardOutput;
                List<string> lines = new List<string>();
                string line = r.ReadLine();
                while (line != null)
                {
                    lines.Add(line);
                    line = r.ReadLine();
                }
                proc.WaitForExit();
                DumpBinResultParser parser = new DumpBinResultParser();
                parser.Parse(lines);

                string[] results = parser.GetAllExportFuncNames();

                exportFuncs = new ExportFunc[results.Length];
                //then parse output
                if (results != null)
                {
                    //convert func 
                    //to more proper form
                    for (int i = 0; i < results.Length; ++i)
                    {
                        exportFuncs[i] = new ExportFunc(results[i]);
                    }
                }
            }

            //Resolve(dllPtr, exportFuncs); 
            //convert arr to dic, then we will resolve it
            Dictionary<string, ExportFunc> funcDic = Resolver.ConvertToFuncDic(exportFuncs);

            //ResolveFuncs(funcDic, out swap_rb swap_rb_);
            //ResolveFuncs(funcDic, out flipY_and_swap flipY_and_swap_);
            //ResolveFuncs(funcDic, out clear clear_);
        }
    }
  
    static class Resolver
    {
        [DllImport("Kernel32.dll")]
        static extern IntPtr LoadLibrary(string libraryName);
        [DllImport("Kernel32.dll")]
        static extern bool FreeLibrary(IntPtr hModule);
        [DllImport("Kernel32.dll")]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);


        public static string GetMsvcSdkPath()
        {
            string msvcPath = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Tools\MSVC";
            string[] folders = Directory.GetDirectories(msvcPath);

            foreach (string f in folders)
            {
                string msvc_sdk = f + @"\bin\Hostx64\x64";
                if (File.Exists(msvc_sdk + "\\link.exe"))
                {
                    return msvc_sdk;
                }
            }
            return null;
        }

        public static void Resolve(IntPtr modulePtr, ExportFunc[] exportFuncs)
        {
            for (int i = 0; i < exportFuncs.Length; ++i)
            {
                ExportFunc exportFunc = exportFuncs[i];
                exportFunc.NaitveFuncPtr = GetProcAddress(modulePtr, exportFunc.OriginalName);
            }
        }
        public static void ResolveFuncs<T>(string csFuncName, Dictionary<string, ExportFunc> exportDic, out T delOutput)
        {
            if (exportDic.TryGetValue(csFuncName, out ExportFunc found))
            {
                delOutput = (T)(object)Marshal.GetDelegateForFunctionPointer(found.NaitveFuncPtr, typeof(T));
            }
            else
            {
                delOutput = default(T);
            }
        }
        public static void ResolveFuncs<T>(Dictionary<string, ExportFunc> exportDic, out T delOutput)
        {
            string csFuncName = typeof(T).Name;
            if (exportDic.TryGetValue(csFuncName, out ExportFunc found))
            {
                delOutput = (T)(object)Marshal.GetDelegateForFunctionPointer(found.NaitveFuncPtr, typeof(T));
            }
            else
            {
                delOutput = default(T);
            }
        }
        public static Dictionary<string, ExportFunc> ConvertToFuncDic(ExportFunc[] exportFuncs)
        {
            Dictionary<string, ExportFunc> dic = new Dictionary<string, ExportFunc>();
            for (int i = 0; i < exportFuncs.Length; ++i)
            {
                ExportFunc exportFunc = exportFuncs[i];
                if (dic.ContainsKey(exportFunc.ProperCsName))
                {
                    throw new NotSupportedException();
                }
                else
                {
                    dic.Add(exportFunc.ProperCsName, exportFunc);
                }
            }
            return dic;
        }


    }
}