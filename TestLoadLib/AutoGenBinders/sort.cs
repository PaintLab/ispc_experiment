/*x14*///AUTOGEN,2020-06-18T16:58:03
/*x15*///Tools: ispc and BridgeBuilder
/*x16*///Src: D:\projects\ispc_experiment\TestLoadLib\bin\Debug/temp/sort_ispc.h
/*x17*/
/*x18*/using System;
/*x19*/using System.Runtime.InteropServices;
/*x20*/
/*x21*/using int32_t = System.Int32;
/*x22*/using uint32_t = System.UInt32;
/*x23*/
/*x24*/namespace sort_ispc{
/*x25*/public static unsafe class NativeMethods{
/*x26*/const string LIB_NAME="sort.dll";
/*x27*/[DllImport(LIB_NAME,EntryPoint ="my_sort_ispc")]
public static extern void sort_ispc(int32_t n,uint32_t* code,int32_t* order,int32_t ntasks)/*x28*/;
/*x29*/}
/*x30*/}
