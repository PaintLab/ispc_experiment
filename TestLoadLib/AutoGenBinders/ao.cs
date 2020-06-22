/*x20*///AUTOGEN,2020-06-22T20:32:20
/*x21*///Tools: ispc and BridgeBuilder
/*x22*///Src: D:\projects\ispc_experiment\TestLoadLib\bin\Debug/temp/ao_ispc.h
/*x23*/
/*x24*/using System;
/*x25*/using System.Runtime.InteropServices;
/*x26*/
/*x27*/
using uint8_t = System.Byte;
using int8_t = System.SByte;
using uint16_t = System.UInt16;
using int16_t = System.Int16;
using int32_t = System.Int32;
using uint32_t = System.UInt32;

/*x28*/namespace ao_ispc{
/*x29*/public static unsafe class NativeMethods{
/*x30*/const string LIB_NAME="ao.dll";
/*x31*/[DllImport(LIB_NAME,EntryPoint ="my_ao_ispc")]
public static extern void ao_ispc(int32_t w,int32_t h,int32_t nsubsamples,float* image)/*x32*/;
/*x33*/[DllImport(LIB_NAME,EntryPoint ="my_ao_ispc_tasks")]
public static extern void ao_ispc_tasks(int32_t w,int32_t h,int32_t nsubsamples,float* image)/*x34*/;
/*x35*/}
/*x36*/}
