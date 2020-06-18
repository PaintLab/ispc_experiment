/*x14*///AUTOGEN,2020-06-18T17:39:07
/*x15*///Tools: ispc and BridgeBuilder
/*x16*///Src: D:\projects\ispc_experiment\TestLoadLib\bin\Debug/temp/mandelbrot_ispc.h
/*x17*/
/*x18*/using System;
/*x19*/using System.Runtime.InteropServices;
/*x20*/
/*x21*/using int32_t = System.Int32;
/*x22*/using uint32_t = System.UInt32;
/*x23*/
/*x24*/namespace mandelbrot_ispc{
/*x25*/public static unsafe class NativeMethods{
/*x26*/const string LIB_NAME="mandelbrot.dll";
/*x27*/[DllImport(LIB_NAME,EntryPoint ="my_mandelbrot_ispc")]
public static extern void mandelbrot_ispc(float x0,float y0,float x1,float y1,int32_t width,int32_t height,int32_t maxIterations,int32_t* output)/*x28*/;
/*x29*/}
/*x30*/}
