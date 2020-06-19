/*x31*///AUTOGEN,2020-06-19T09:29:43
/*x32*///Tools: ispc and BridgeBuilder
/*x33*///Src: D:\projects\ispc_experiment\TestLoadLib\bin\Debug/temp/kernels_ispc.h
/*x34*/
/*x35*/using System;
/*x36*/using System.Runtime.InteropServices;
/*x37*/
/*x38*/
using uint8_t = System.Byte;
using int8_t = System.SByte;
using uint16_t = System.UInt16;
using int16_t = System.Int16;
using int32_t = System.Int32;
using uint32_t = System.UInt32;

/*x39*/namespace kernels_ispc{
/*x40*/public static unsafe class NativeMethods{
/*x41*/const string LIB_NAME="kernels.dll";
/*x42*/[StructLayout(LayoutKind.Sequential)]
/*x43*/public struct InputHeader{
public fixed float cameraProj[16];public float cameraNear/*x44*/;
public float cameraFar/*x45*/;
public int32_t framebufferWidth/*x46*/;
public int32_t framebufferHeight/*x47*/;
public int32_t numLights/*x48*/;
public int32_t inputDataChunkSize/*x49*/;
public fixed int32_t inputDataArrayOffsets[16];/*x50*/}
/*x51*/[StructLayout(LayoutKind.Sequential)]
/*x52*/public struct InputDataArrays{
public float* zBuffer/*x53*/;
public uint16_t* normalEncoded_x/*x54*/;
public uint16_t* normalEncoded_y/*x55*/;
public uint16_t* specularAmount/*x56*/;
public uint16_t* specularPower/*x57*/;
public uint8_t* albedo_x/*x58*/;
public uint8_t* albedo_y/*x59*/;
public uint8_t* albedo_z/*x60*/;
public float* lightPositionView_x/*x61*/;
public float* lightPositionView_y/*x62*/;
public float* lightPositionView_z/*x63*/;
public float* lightAttenuationBegin/*x64*/;
public float* lightColor_x/*x65*/;
public float* lightColor_y/*x66*/;
public float* lightColor_z/*x67*/;
public float* lightAttenuationEnd/*x68*/;
/*x69*/}
/*x70*/[DllImport(LIB_NAME,EntryPoint ="my_ComputeZBoundsRow")]
public static extern void ComputeZBoundsRow(int32_t tileY,int32_t tileWidth,int32_t tileHeight,int32_t numTilesX,int32_t numTilesY,float* zBuffer,int32_t gBufferWidth,float cameraProj_33,float cameraProj_43,float cameraNear,float cameraFar,float* minZArray,float* maxZArray)/*x71*/;
/*x72*/[DllImport(LIB_NAME,EntryPoint ="my_IntersectLightsWithTileMinMax")]
public static extern int32_t IntersectLightsWithTileMinMax(int32_t tileStartX,int32_t tileEndX,int32_t tileStartY,int32_t tileEndY,float minZ,float maxZ,int32_t gBufferWidth,int32_t gBufferHeight,float cameraProj_11,float cameraProj_22,int32_t numLights,float* light_positionView_x_array,float* light_positionView_y_array,float* light_positionView_z_array,float* light_attenuationEnd_array,int32_t* tileLightIndices)/*x73*/;
/*x74*/[DllImport(LIB_NAME,EntryPoint ="my_RenderStatic")]
public static extern void RenderStatic(InputHeader* inputHeader,InputDataArrays* inputData,int32_t visualizeLightCount,uint8_t* framebuffer_r,uint8_t* framebuffer_g,uint8_t* framebuffer_b)/*x75*/;
/*x76*/[DllImport(LIB_NAME,EntryPoint ="my_ShadeTile")]
public static extern void ShadeTile(int32_t tileStartX,int32_t tileEndX,int32_t tileStartY,int32_t tileEndY,int32_t gBufferWidth,int32_t gBufferHeight,InputDataArrays* inputData,float cameraProj_11,float cameraProj_22,float cameraProj_33,float cameraProj_43,int32_t* tileLightIndices,int32_t tileNumLights,bool visualizeLightCount,uint8_t* framebuffer_r,uint8_t* framebuffer_g,uint8_t* framebuffer_b)/*x77*/;
/*x78*/[DllImport(LIB_NAME,EntryPoint ="my_SplitTileMinMax")]
public static extern void SplitTileMinMax(int32_t tileMidX,int32_t tileMidY,float* subtileMinZ,float* subtileMaxZ,int32_t gBufferWidth,int32_t gBufferHeight,float cameraProj_11,float cameraProj_22,int32_t* lightIndices,int32_t numLights,float* light_positionView_x_array,float* light_positionView_y_array,float* light_positionView_z_array,float* light_attenuationEnd_array,int32_t* subtileIndices,int32_t subtileIndicesPitch,int32_t* subtileNumLights)/*x79*/;
/*x80*/}
/*x81*/}
