//MIT, 2020, WinterDev
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;

using BridgeBuilder;
using BridgeBuilder.Ispc;
using System.Runtime.CompilerServices;

namespace TestLoadLib
{
    class Program
    {

        static void Main(string[] args)
        {

            //----------
            //more easier
            //----------
            //Ispc_SimpleExample();
            //Ispc_SortExample();
            Ispc_MandelbrotExample();
        }


        static void Ispc_SimpleExample()
        {
            //from: ispc-14-dev-windows\examples\simple
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

        static void Ispc_SortExample()
        {
            //from: ispc-14-dev-windows\examples\sort

            string dllName = "sort.dll";
            //TODO: check if we need to rebuild or not
            bool rebuild = true;
            if (rebuild)
            {
                IspcBuilder ispcBuilder = new IspcBuilder();
                ispcBuilder.ProjectConfigKind = BridgeBuilder.Vcx.ProjectConfigKind.Debug;
                ispcBuilder.IspcFilename = "sort.ispc";
                ispcBuilder.AutoCsTargetFile = "..\\..\\AutoGenBinders\\sort.cs";

                string currentDir = Directory.GetCurrentDirectory();
                ispcBuilder.AdditionalInputItems = new string[]
                {
                    currentDir + "\\tasksys.cpp"
                };
                ispcBuilder.RebuildLibraryAndAPI();

            }

            IntPtr dllPtr = LoadLibrary(dllName);

            if (dllPtr == IntPtr.Zero) { throw new NotSupportedException(); }


            int m_round = 20;
            int elem_count = 10000;
            Random rand = new Random(20);
            uint[] code = new uint[elem_count];
            for (int round = 0; round < m_round; round++)
            {
                for (int i = 0; i < elem_count; i++)
                {
                    code[i] = (uint)rand.Next(0, elem_count);
                }
                unsafe
                {
                    int[] ordered_output = new int[elem_count];
                    fixed (uint* code_ptr = &code[0])
                    fixed (int* ordered_output_ptr = &ordered_output[0])
                    {
                        sort_ispc.NativeMethods.sort_ispc(elem_count, code_ptr, ordered_output_ptr, 0);
                    }
                }
            }

        }

        static void Ispc_MandelbrotExample()
        {
            //from: ispc-14-dev-windows\examples\mandelbrot
            string dllName = "mandelbrot.dll";
            //TODO: check if we need to rebuild or not
            bool rebuild = false;
            if (rebuild)
            {
                IspcBuilder ispcBuilder = new IspcBuilder();
                ispcBuilder.ProjectConfigKind = BridgeBuilder.Vcx.ProjectConfigKind.Debug;
                ispcBuilder.IspcFilename = "mandelbrot.ispc";
                ispcBuilder.AutoCsTargetFile = "..\\..\\AutoGenBinders\\mandelbrot.cs";

                string currentDir = Directory.GetCurrentDirectory();
                ispcBuilder.AdditionalInputItems = new string[]
                {
                    currentDir + "\\tasksys.cpp"
                };
                ispcBuilder.RebuildLibraryAndAPI();
            }

            IntPtr dllPtr = LoadLibrary(dllName);

            if (dllPtr == IntPtr.Zero) { throw new NotSupportedException(); }

            int width = 768;
            int height = 512;
            //
            float x0 = -2;
            float x1 = 1;
            float y0 = -1;
            float y1 = 1;

            int maxIterations = 256;
            int[] buffer = new int[width * height];
            unsafe
            {
                fixed (int* output_h = &buffer[0])
                {
                    mandelbrot_ispc.NativeMethods.mandelbrot_ispc(x0, y0, x1, y1, width, height, maxIterations, output_h);
                }
            }
            //convert to grayscale image

            using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                System.Drawing.Imaging.BitmapData bmpdata = bmp.LockBits(new System.Drawing.Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);

                IntPtr scan0 = bmpdata.Scan0;
                unsafe
                {
                    int* output_ptr = (int*)scan0;
                    int index = 0;
                    for (int y = 0; y < height; ++y)
                    {
                        for (int x = 0; x < width; ++x)
                        {
                            int data = buffer[index];
                            byte output = ((data & 0x1) != 0) ? (byte)240 : (byte)20;
                            *output_ptr = (255 << 24) | (output << 16) | (output << 8) | (output << 0);

                            output_ptr++;
                            index++;
                        }
                    }
                }

                bmp.UnlockBits(bmpdata);
                bmp.Save("test_mandelbrot.png");
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
