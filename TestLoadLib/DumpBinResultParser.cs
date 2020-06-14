//{Microsoft(R) COFF/PE Dumper Version 14.25.28614.0
//Copyright(C) Microsoft Corporation.All rights reserved.



//Dump of file simple.dll

//File Type: DLL

// Section contains the following exports for simple.dll

//    00000000 characteristics
//   FFFFFFFF time date stamp
//        0.00 version
//           1 ordinal base
//           5 number of functions
//           5 number of names

//   ordinal hint RVA      name

//          1    0 000013A0 clear___un_3C_uni_3E_uniuni
//          2    1 00001450 flipY_and_swap___un_3C_uni_3E_un_3C_uni_3E_uniuni
//          3    2 00001190 simple2___un_3C_uni_3E_un_3C_uni_3E_uni
//          4    3 00001000 simple___un_3C_unf_3E_un_3C_unf_3E_uni
//          5    4 00001230 swap_rb___un_3C_uni_3E_uni

// Summary

//        1000 .rdata
//        1000 .text
//}

using System;
using System.Collections.Generic;

namespace TestLoadLib
{

    public class DumpBinResultParser
    {
        List<string> _exportFuncs;
        static readonly char[] s_seps = new char[] { ' ' };
        public string[] GetAllExportFuncNames()
        {
            if (_exportFuncs == null)
            {
                return null;
            }
            else
            {
                return _exportFuncs.ToArray();
            }
        }
        public void Parse(List<string> lines)
        {
            _exportFuncs = new List<string>();

            int startAt = 0;
            int j = lines.Count;
            int state = 0;
            for (int i = startAt; i < j; ++i)
            {
                string s = lines[i].Trim();
                switch (state)
                {
                    case 0:
                        {

                            if (s == "ordinal hint RVA      name")
                            {
                                state = 1;
                            }
                        }
                        break;
                    case 1:
                        {

                            if (s.Length == 0)
                            {
                                state = 2;
                            }
                            else
                            {
                                throw new NotSupportedException();
                            }
                        }
                        break;
                    case 2:
                        {
                            if (s.Length == 0)
                            {
                                //stop here 
                                state = 3;
                                i = j + 1; //force exit
                            }
                            else
                            {
                                //read export symbol
                                string[] splitLines = s.Split(s_seps, StringSplitOptions.RemoveEmptyEntries);
                                _exportFuncs.Add(splitLines[splitLines.Length - 1]);
                            }
                        }
                        break;
                }

            }
        }
    }
}
