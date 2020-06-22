# ispc_experiment


Experiment about C# tool chain for Intel® ISPC

see more about ISPC at https://ispc.github.io/ispc.html

Concept:
 
This C# tool chain will ... ( [see the code](https://github.com/PaintLab/ispc_experiment/blob/master/BridgeBuilder/6_Ispc/IspcBuilder.cs) )
 
 
1. this C# toolchain sends **input** .ispc code to  _ispc.exe_ => **output** c-header and object file

2. then the toolchain reads **input** c-header (and parse it)=> **output** c-code binding ("extern C {" code for a new native dll)
   
     AND C# extern code ([DllImport]) for binding with the new dll.   

3. then toolchain gathers **input** c-header and autogen c code  and create => **output** .vcxproj project on the fly.

4. the toolchain sends **input** .vcxproj  project to MSBuild.exe => **output** native dll 

5. the rest is loading that dll, and the C# code will access native func dll from extern code that are generated on step 2


----

Some outputs from the examples...

![test_mandelbrot](https://user-images.githubusercontent.com/7447159/85295983-eb29cc80-b4ca-11ea-9919-0aae955d83fb.png)

_mandelbrot example_


![ao](https://user-images.githubusercontent.com/7447159/85295991-eebd5380-b4ca-11ea-9110-d7c95b6b2d83.png)

_ao example_

Just Experiment!, Not complete



LICENSE: MIT


