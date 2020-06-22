//MIT, 2020, WinterDev
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;

using BridgeBuilder;
using BridgeBuilder.Ispc;
using BridgeBuilder.Vcx;

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
            //Ispc_MandelbrotExample();
            //Ispc_MandlebrotTaskExample();
            //Ispc_DeferredShading();
            //Ispc_TestCallback();
            //TestGenerateVcxBuilder();

            Ispc_AoBench();
#if DEBUG
            // dbugParseHeader(@"deferred\kernels_ispc.h");
#endif
        }
#if DEBUG

        static void dbugParseHeader(string filename)
        {
            IspcBuilder builder = new IspcBuilder();
            builder.ParseAutoGenHeaderFromFile(filename);

        }
#endif

        static void TestGenerateVcxBuilder()
        {
            //from: ispc-14-dev-windows\examples\simple
            string module = "mysimple_dll1";

            //TODO: check if we need to rebuild or not

            MyVcxBuilder myVcxBuilder = new MyVcxBuilder();
            myVcxBuilder.ProjectName = "mysimple_dll1";
            myVcxBuilder.ProjectConfigKind = BridgeBuilder.Vcx.ProjectConfigKind.Debug;

            myVcxBuilder.PrimaryHeader = @"";
            myVcxBuilder.PrimarySource = @"#ifdef _WIN32 
#define MY_DLL_EXPORT __declspec(dllexport)
#else
#define MY_DLL_EXPORT
#endif
            extern ""C"" { 
            typedef void(__cdecl * managed_callback)(int data);
            managed_callback myext_mcallback;
            MY_DLL_EXPORT void set_managed_callback(managed_callback m_callback)
            {
                myext_mcallback = m_callback;
            }
            }";
            myVcxBuilder.RebuildLibraryAndAPI();

            string dllName = module + ".dll";
            IntPtr dllPtr = LoadLibrary(dllName);

            if (dllPtr == IntPtr.Zero)
            {
                throw new NotSupportedException();
            }

            GetManagedDelegate(dllPtr, "set_managed_callback", out s_setManagedCallback);
            if (s_setManagedCallback == null) { throw new NotSupportedException(); }
        }



        static void Ispc_SimpleExample()
        {
            //from: ispc-14-dev-windows\examples\simple
            string module = "simple";

            //TODO: check if we need to rebuild or not
            bool rebuild = NeedRebuildIspc(module);
            if (rebuild)
            {
                IspcBuilder ispcBuilder = new IspcBuilder();
                ispcBuilder.ProjectConfigKind = BridgeBuilder.Vcx.ProjectConfigKind.Debug;
                ispcBuilder.IspcFilename = module + ".ispc";
                ispcBuilder.AutoCsTargetFile = $"..\\..\\AutoGenBinders\\{module}.cs";
                ispcBuilder.RebuildLibraryAndAPI();

            }

            string dllName = module + ".dll";
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

            string module = "sort";
            //TODO: check if we need to rebuild or not
            bool rebuild = NeedRebuildIspc(module);
            if (rebuild)
            {
                IspcBuilder ispcBuilder = new IspcBuilder();
                ispcBuilder.ProjectConfigKind = BridgeBuilder.Vcx.ProjectConfigKind.Debug;
                ispcBuilder.IspcFilename = module + ".ispc";
                ispcBuilder.AutoCsTargetFile = $"..\\..\\AutoGenBinders\\{module}.cs";

                string currentDir = Directory.GetCurrentDirectory();
                ispcBuilder.AdditionalInputItems = new string[]
                {
                    currentDir + "\\tasksys.cpp"
                };
                ispcBuilder.RebuildLibraryAndAPI();

            }

            string dllName = module + ".dll";
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
            string module = "mandelbrot.dll";
            //TODO: check if we need to rebuild or not
            bool rebuild = NeedRebuildIspc(module);
            if (rebuild)
            {
                IspcBuilder ispcBuilder = new IspcBuilder();
                ispcBuilder.ProjectConfigKind = BridgeBuilder.Vcx.ProjectConfigKind.Debug;
                ispcBuilder.IspcFilename = module + ".ispc";
                ispcBuilder.AutoCsTargetFile = $"..\\..\\AutoGenBinders\\{module}.cs";

                string currentDir = Directory.GetCurrentDirectory();
                ispcBuilder.AdditionalInputItems = new string[]
                {
                    currentDir + "\\tasksys.cpp"
                };
                ispcBuilder.RebuildLibraryAndAPI();
            }

            string dllName = module + ".dll";
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
            SaveManelbrotImage(buffer, width, height, "test_mandelbrot.png");
        }

        static void Ispc_MandlebrotTaskExample()
        {
            //from: ispc-14-dev-windows\examples\mandelbrot

            //TODO: check if we need to rebuild or not
            string module = "mandelbrot_task";
            bool rebuild = NeedRebuildIspc(module);
            if (rebuild)
            {
                IspcBuilder ispcBuilder = new IspcBuilder();
                ispcBuilder.ProjectConfigKind = BridgeBuilder.Vcx.ProjectConfigKind.Debug;
                ispcBuilder.IspcFilename = module + ".ispc";
                ispcBuilder.AutoCsTargetFile = $"..\\..\\AutoGenBinders\\{module}.cs";

                string currentDir = Directory.GetCurrentDirectory();
                ispcBuilder.AdditionalInputItems = new string[]
                {
                    currentDir + "\\tasksys.cpp"
                };
                ispcBuilder.RebuildLibraryAndAPI();
            }

            string dllName = module + ".dll";
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
                    mandelbrot_task_ispc.NativeMethods.mandelbrot_ispc(x0, y0, x1, y1, width, height, maxIterations, output_h);
                }
            }

            SaveManelbrotImage(buffer, width, height, "test_mandelbrot_task.png");
        }

        static void Ispc_DeferredShading()
        {
            //from ispc-14-dev-windows\examples\deferred\kernels.ispc

            //TODO: check if we need to rebuild or not
            string module = "kernels";
            bool rebuild = NeedRebuildIspc(module);
            if (rebuild)
            {
                IspcBuilder ispcBuilder = new IspcBuilder();
                ispcBuilder.ProjectConfigKind = BridgeBuilder.Vcx.ProjectConfigKind.Debug;
                ispcBuilder.IspcFilename = module + ".ispc";
                ispcBuilder.AutoCsTargetFile = $"..\\..\\AutoGenBinders\\{module}.cs";

                string currentDir = Directory.GetCurrentDirectory();
                ispcBuilder.AdditionalInputItems = new string[]
                {
                    currentDir + "\\tasksys.cpp",
                    currentDir + "\\deferred.h"
                };
                ispcBuilder.RebuildLibraryAndAPI();
            }

            string dllName = module + ".dll";
            IntPtr dllPtr = LoadLibrary(dllName);
            if (dllPtr == IntPtr.Zero) { throw new NotSupportedException(); }

            unsafe
            {
                byte[] data_file = File.ReadAllBytes("Data/pp1280x720.bin");
                kernels_ispc.NativeMethods.InputDataArrays array = new kernels_ispc.NativeMethods.InputDataArrays();
                //TODO:
                //port ispc-14-dev-windows\examples\deferred\main.cpp
            }
        }



        delegate void ManagedCallback(int data);
        delegate void SetManagedCallback(IntPtr m);

        static SetManagedCallback s_setManagedCallback;

        static ManagedCallback m_callback;
        static IntPtr m_callback_ptr;


        static bool NeedRebuildIspc(string moduleName) => IspcBuilder.NeedRebuildIspc(moduleName);

        static void Ispc_TestCallback()
        {
            //read more about callback from ispc
            //on  https://ispc.github.io/ispc.html => section "Interoperability with The Application" 

            string module = "callback_test";
            bool rebuild = NeedRebuildIspc(module);
            if (rebuild)
            {
                IspcBuilder ispcBuilder = new IspcBuilder();
                ispcBuilder.ProjectConfigKind = BridgeBuilder.Vcx.ProjectConfigKind.Debug;
                ispcBuilder.IspcFilename = module + ".ispc";
                ispcBuilder.AutoCsTargetFile = $"..\\..\\AutoGenBinders\\{module}.cs";

                string currentDir = Directory.GetCurrentDirectory();
                ispcBuilder.AdditionalInputItems = new string[]
                {
                     currentDir + "\\callback_test1.cpp"
                };
                ispcBuilder.RebuildLibraryAndAPI();
            }

            string dllName = module + ".dll";
            IntPtr dllPtr = LoadLibrary(dllName);
            if (dllPtr == IntPtr.Zero) { throw new NotSupportedException(); }

            GetManagedDelegate(dllPtr, "set_managed_callback", out s_setManagedCallback);
            if (s_setManagedCallback == null) { throw new NotSupportedException(); }

            //-----------
            m_callback = (int a) =>
            {
#if DEBUG
                //System.Diagnostics.Debugger.Break();
                System.Diagnostics.Debug.WriteLine("callback from ispc");
#endif
            };
            m_callback_ptr = Marshal.GetFunctionPointerForDelegate(m_callback);
            s_setManagedCallback(m_callback_ptr);

            //-----------
            //test call to ispc
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
                    callback_test_ispc.NativeMethods.clear(h, 0, inputData.Length);
                }
            }
        }


        static void Ispc_AoBench()
        {
            string module = "ao";
            bool rebuild = NeedRebuildIspc(module);
            if (rebuild)
            {
                IspcBuilder ispcBuilder = new IspcBuilder();
                ispcBuilder.ProjectConfigKind = BridgeBuilder.Vcx.ProjectConfigKind.Debug;
                ispcBuilder.IspcFilename = module + ".ispc";
                ispcBuilder.AutoCsTargetFile = $"..\\..\\AutoGenBinders\\{module}.cs";

                string currentDir = Directory.GetCurrentDirectory();
                ispcBuilder.AdditionalInputItems = new string[]
                {
                    currentDir + "\\tasksys.cpp"
                };

                ispcBuilder.RebuildLibraryAndAPI();
            }

            string dllName = module + ".dll";
            IntPtr dllPtr = LoadLibrary(dllName);
            if (dllPtr == IntPtr.Zero) { throw new NotSupportedException(); }

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            unsafe
            {
                sw.Reset();
                sw.Start();
                int w = 800;
                int h = 600;
                float[] imgBuffer = new float[w * h * 3];//3 channel
                fixed (float* img_h = &imgBuffer[0])
                {
                    ao_ispc.NativeMethods.ao_ispc(w, h, 2, img_h);
                }
                sw.Stop();
                System.Diagnostics.Debug.WriteLine("ao_ms:" + sw.ElapsedMilliseconds.ToString());

                //conver float[] to img                
                ConvertToBitmapAndSave(imgBuffer, w, h, "ao1.png");
              
                
            }
            unsafe
            {
                sw.Reset();
                sw.Start();
                int w = 800;
                int h = 600;
                float[] imgBuffer = new float[w * h * 3];//3 channel
                fixed (float* img_h = &imgBuffer[0])
                {
                    ao_ispc.NativeMethods.ao_ispc_tasks(w, h, 2, img_h);
                }
                sw.Stop();
                System.Diagnostics.Debug.WriteLine("ao_task_ms:" + sw.ElapsedMilliseconds.ToString());
                //conver float[] to img
                ConvertToBitmapAndSave(imgBuffer, w, h, "ao2.png"); 
            }
        }

        static void ConvertToBitmapAndSave(float[] floatRGB, int width, int height, string filename)
        {

            byte Clamp(float f)
            {
                //NEST
                int i = (int)(f * 255.5);

                if (i < 0)
                    i = 0;
                if (i > 255)
                    i = 255;
                return (byte)i;
            }

            using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                System.Drawing.Imaging.BitmapData bmpdata = bmp.LockBits(
                    new System.Drawing.Rectangle(0, 0, width, height),
                    System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);

                IntPtr scan0 = bmpdata.Scan0;
                unsafe
                {
                    int* output_ptr = (int*)scan0;
                    int index = 0;

                    for (int y = 0; y < height; ++y)
                    {
                        for (int x = 0; x < width; ++x)
                        {
                            byte r = Clamp(floatRGB[index]);
                            byte g = Clamp(floatRGB[index + 1]);
                            byte b = Clamp(floatRGB[index + 2]);

                            *output_ptr = (255 << 24) | (r << 16) | (b << 8) | (b << 0);

                            output_ptr++;

                            index += 3;
                        }
                    }
                }

                bmp.UnlockBits(bmpdata);
                bmp.Save(filename);
            }


        }
        static void GetManagedDelegate<T>(IntPtr modulePtr, string funcName, out T delOutput)
        {
            IntPtr ptr = GetProcAddress(modulePtr, funcName);
            if (ptr == IntPtr.Zero)
            {
                delOutput = default;
            }
            else
            {
                delOutput = (T)(object)Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
            }
        }
        static void GetManagedDelegate<T>(IntPtr ptr, out T delOutput)
        {
            delOutput = (T)(object)Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
        }

        static void SaveManelbrotImage(int[] buffer, int width, int height, string filename)
        {
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
                bmp.Save(filename);
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
