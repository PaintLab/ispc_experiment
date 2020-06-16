//MIT, 2020, WinterDev
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;

using BridgeBuilder;
using BridgeBuilder.Ispc;

namespace TestLoadLib
{
    class Program
    {

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
                IspcBuilder ispcBuilder = new IspcBuilder();
                ispcBuilder.ProjectConfigKind = BridgeBuilder.Vcx.ProjectConfigKind.Debug;
                ispcBuilder.IspcFilename = "simple.ispc";
                ispcBuilder.AutoCsTargetFile = "..\\..\\AutoGenBinders\\simple.cs";
                ispcBuilder.RebuildLibraryAndAPI();

            }

            IntPtr dllPtr = LoadLibrary(dllName);

            if (dllPtr == IntPtr.Zero) { throw new NotSupportedException(); }

            IntPtr funct = GetProcAddress(dllPtr, "my_simple"); //test with raw name
            if (funct == IntPtr.Zero) { throw new NotSupportedException(); }


            //test1
            int[] inputData = new int[]
            {
                1<<16,  2<<16, 3<<16,  4<<16,
                5<<16,  6<<16, 7<<16,  8<<16,
                9<<16, 10<<16, 11<<16,  12<<16,
            };
            //test2
            int[] outputData = new int[inputData.Length];
            unsafe
            {
                fixed (int* output_h = &outputData[0])
                fixed (int* h = &inputData[0])
                {
                    simple_ispc.NativeMethods.flipY_and_swap(h, output_h, 4, 3);
                }
            }

            unsafe
            {
                fixed (int* h = &inputData[0])
                {
                    simple_ispc.NativeMethods.clear(h, 0, inputData.Length);
                }
            }


        }


        [DllImport("Kernel32.dll")]
        static extern IntPtr LoadLibrary(string libraryName);
        [DllImport("Kernel32.dll")]
        static extern bool FreeLibrary(IntPtr hModule);
        [DllImport("Kernel32.dll")]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);


    }
}
