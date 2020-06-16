//MIT, 2020, WinterDev
using System;
using System.Collections.Generic;

using System.IO;
using System.Text;
namespace TestLoadLib
{
    enum ProjectConfigKind
    {
        Debug,
        Release,
        ReleaseWithMinSize, //TODO
        ReleaseWithDebugInfo //TODO
    }
    enum ProjectOutputKind
    {
        Application,
        DynamicLibrary,
        StaticLibrary
    }
    enum InputKind
    {
        ClInclude,
        Object,
        ClCompile
    }
    class InputItem
    {
        public InputKind InputKind { get; set; }
        public string Include { get; set; }
    }

    //WARNING:this 
    //create from visual studio 2019 vc project file.
    //not from official documentation

    class SimpleVcxProjGen
    {
        //simple create xml document for vcx proj
        string _projFileVersion = "10.0.20506.1";
        public string FullProjSrcPath { get; set; }
        public string FullProjBuildPath { get; set; }
        public string ProjectName { get; set; }
        //
        string _charset = "MultiByte";
        string _platformToolSet = "v142";
        string _initDir = "$(IntDir)";


        string _platform = "x64";
        ProjectConfigKind _proj_config;
      
        List<InputItem> _inputs = new List<InputItem>();


        public SimpleVcxProjGen()
        {

        }
        public List<string> AdditionalIncludeDirs { get; } = new List<string>();
        public void AddObjectFile(string objFile)
        {
            _inputs.Add(new InputItem()
            {
                InputKind = InputKind.Object,
                Include = objFile //from build path
            });
        }
        public void AddIncludeFile(string headerFile)
        {
            _inputs.Add(new InputItem()
            {
                InputKind = InputKind.ClInclude,
                Include = headerFile
            });
        }
        public void AddCompileFile(string sourceFile)
        {
            _inputs.Add(new InputItem()
            {
                InputKind = InputKind.ClCompile,
                Include = sourceFile
            });
        }

        public ProjectOutputKind ProjectOutputKind { get; set; } = ProjectOutputKind.DynamicLibrary;

        internal VcxProject CreateVcxTemplate()
        {
            VcxProject proj = new VcxProject();
            {
                _proj_config = ProjectConfigKind.Debug;
                 
                ConfigSetup();
                ProjectConfiguration projConfig = NewProjectConfig();
                proj.Configurations.Add(projConfig);
            }
            {
                _proj_config = ProjectConfigKind.Release;
                 
                ConfigSetup();
                ProjectConfiguration projConfig = NewProjectConfig();
                proj.Configurations.Add(projConfig);
            }

            GlobalsPropertyGroup globalsProp = proj.GlobalsPropertyGroup;
            globalsProp.ProjectGuid = "{" + Guid.NewGuid() + "}";
            globalsProp.WindowsTargetPlatformVersion = "10.0.18362.0";
            globalsProp.Keyword = "Win32Proj";
            globalsProp.Platform = _platform;
            globalsProp.ProjectName = ProjectName; //TODO:
            globalsProp.VCProjectUpgraderObjectName = "NoUpgrade";

            //
            proj.ProjectFileVersion = _projFileVersion;
            proj.CppDefaultProp = new Import { Project = @"$(VCTargetsPath)\Microsoft.Cpp.Default.props" };
            proj.CppProp = new Import { Project = @"$(VCTargetsPath)\Microsoft.Cpp.props" };
            //--------------- 

            proj.InputItems = _inputs;


            foreach (InputItem item in _inputs)
            {
                //resolve path to abs path
                if (!Path.IsPathRooted(item.Include) ||
                    !File.Exists(item.Include))
                {
                    throw new NotSupportedException();
                }
            }

            proj.PropertySheets = new PropertySheets()
            {
                Import = new Import()
                {
                    Label = "LocalAppDataPlatform",
                    Project = @"$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props",
                    Condition = @"exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')",
                }
            };

            return proj;
        }
        string _config = "";
        string _linkIncremental = "";
        string _configType = "";
        string _extension = "";
        string _outdir = "";

        string _finalProductFilename;


        void ConfigSetup()
        {
            switch (_proj_config)
            {
                default: throw new NotSupportedException();
                case ProjectConfigKind.Debug:
                    {
                        _config = "Debug";
                        _linkIncremental = "true";
                    }
                    break;
                case ProjectConfigKind.Release:
                    {
                        _config = "Release";
                        _linkIncremental = "false";
                    }
                    break;
            }
            switch (ProjectOutputKind)
            {
                default: throw new NotSupportedException();
                case ProjectOutputKind.Application:
                    _configType = "Application";
                    _extension = ".exe";
                    break;
                case ProjectOutputKind.DynamicLibrary:
                    _configType = "DynamicLibrary";
                    _extension = ".dll";
                    break;
            }
            _outdir = FullProjBuildPath + "\\" + ProjectName + "\\" + _config + "\\";
            _finalProductFilename = _outdir + "\\" + ProjectName + _extension;
        }


        public string GetFinalProductName(ProjectConfigKind configKind)
        {
            ConfigSetup();
            return _finalProductFilename;
        }

        ProjectConfiguration NewProjectConfig()
        {
            //----------------
            string combine = _config + "|" + _platform;
            var conf = new ProjectConfiguration()
            {
                Configuration = _config,
                Platform = _platform,
                Include = combine,
                Config = new ConfigurationPropertyGroup()
                {
                    ConfigurationType = _configType,
                    CharacterSet = _charset,
                    PlatformToolset = _platformToolSet,
                }
            };

            string additionalIncludeDirs = FullProjBuildPath + ";";

            if (AdditionalIncludeDirs.Count > 0)
            {
                for (int i = 0; i < AdditionalIncludeDirs.Count; ++i)
                {
                    additionalIncludeDirs += AdditionalIncludeDirs[i] + ";";
                }
            }


            ConfigurationPropertyGroup configGroup = conf.Config;
            ConditionConfig conditionConfig = new ConditionConfig();
            configGroup.ConditionConfig = conditionConfig;

            configGroup.Condition = conditionConfig.Condition = "'$(Configuration)|$(Platform)'=='" + combine + "'";

            conditionConfig.OutDir = _outdir;
            conditionConfig.IntDir = ProjectName + ".dir\\" + _config + "\\";
            conditionConfig.TargetName = ProjectName;
            conditionConfig.TargetExt = _extension;

            conditionConfig.LinkIncremental = _linkIncremental;
            conditionConfig.GenerateManifest = "true";

            //----
            ClCompile clCompile = new ClCompile();
            clCompile.AdditionalIncludeDirectories = additionalIncludeDirs + "%(AdditionalIncludeDirectories)";
            clCompile.AssemblerListingLocation = _initDir;
            clCompile.CompileAs = "CompileAsCpp";
            clCompile.ExceptionHandling = "Sync";
            clCompile.FloatingPointModel = "Fast";

            //debug



            if (_proj_config == ProjectConfigKind.Debug)
            {
                clCompile.DebugInformationFormat = "ProgramDatabase";
                clCompile.BasicRuntimeChecks = "EnableFastChecks";
                clCompile.InlineFunctionExpansion = "Disabled";
                clCompile.Optimization = "Disabled";
                clCompile.RuntimeLibrary = "MultiThreadedDebugDLL";
                clCompile.PreprocessorDefinitions = "WIN32;_WINDOWS;CMAKE_INTDIR=\"Debug\";%(PreprocessorDefinitions)";

            }
            else
            {
                clCompile.DebugInformationFormat = "";
                clCompile.InlineFunctionExpansion = "AnySuitable";
                clCompile.Optimization = "MaxSpeed";
                clCompile.RuntimeLibrary = "MultiThreadedDLL";
                clCompile.PreprocessorDefinitions = "WIN32;_WINDOWS;NDEBUG;CMAKE_INTDIR=\"Release\";%(PreprocessorDefinitions)";

            }


            clCompile.ObjectFileName = _initDir;
            clCompile.WarningLevel = "Level3";
            clCompile.UseFullPaths = "false";
            clCompile.RuntimeTypeInfo = "true";
            clCompile.PrecompiledHeader = "NotUsing";
            clCompile.IntrinsicFunctions = "true";


            //----------------------------------------------------
            ResourceCompile resCompile = new ResourceCompile();
            if (_proj_config == ProjectConfigKind.Debug)
            {
                resCompile.PreprocessorDefinitions = "WIN32;_DEBUG;_WINDOWS;CMAKE_INTDIR=\"Debug\";%(PreprocessorDefinitions)";
            }
            else
            {
                resCompile.PreprocessorDefinitions = "WIN32;_WINDOWS;NDEBUG;CMAKE_INTDIR=\"Release\";%(PreprocessorDefinitions)";
            }
            resCompile.AdditionalIncludeDirectories = additionalIncludeDirs + "%(AdditionalIncludeDirectories)";
            //----------------------------------------------------

            Midl midl = new Midl();
            midl.AdditionalIncludeDirectories = additionalIncludeDirs + "%(AdditionalIncludeDirectories)";
            midl.OutputDirectory = "$(ProjectDir)/$(IntDir)";
            midl.HeaderFileName = "%(Filename).h";
            midl.TypeLibraryName = "%(Filename).tlb";
            midl.InterfaceIdentifierFileName = "%(Filename)_i.c";
            midl.ProxyFileName = "%(Filename)_p.c";



            //----------------------------------------------------
            string link_additional_opts = "/machine:x64";
            Link link = new Link();
            link.AdditionalDependencies = "kernel32.lib;user32.lib;gdi32.lib;winspool.lib;shell32.lib;ole32.lib;oleaut32.lib;uuid.lib;comdlg32.lib;advapi32.lib";
            link.AdditionalLibraryDirectories = "%(AdditionalLibraryDirectories)";
            link.AdditionalOptions = "%(AdditionalOptions) " + link_additional_opts; //***
            link.SubSystem = "Console";

            link.ProgramDataBaseFile = _outdir + "/" + ProjectName + ".pdb";
            link.ImportLibrary = _outdir + "/" + ProjectName + ".lib";

            if (_proj_config == ProjectConfigKind.Debug)
            {
                link.GenerateDebugInformation = "true";
            }
            else
            {
                link.GenerateDebugInformation = "false";
            }

            link.IgnoreSpecificDefaultLibraries = "%(IgnoreSpecificDefaultLibraries)";

            //-----------
            ProjectReference projRef = new ProjectReference();
            projRef.LinkLibraryDependencies = "false";

            //-------------
            ItemDefinitionGroup itemDefs = new ItemDefinitionGroup();
            itemDefs.ClCompile = clCompile;
            itemDefs.ResourceCompile = resCompile;
            itemDefs.Midl = midl;
            itemDefs.Link = link;
            itemDefs.ProjectReference = projRef;

            conf.ItemDefinitionGroup = itemDefs;
            return conf;
        }

    }

    //TODO: many properties should be enum***

    class VcxProject
    {
        public string DefaultTargets { get; set; } = "Build";
        public string ToolVersion { get; set; } = "16.0";
        public string PreferredToolArchitecture { get; set; } = "x64";
        public string ProjectFileVersion { get; set; }

        public List<ProjectConfiguration> Configurations = new List<ProjectConfiguration>();
        public GlobalsPropertyGroup GlobalsPropertyGroup = new GlobalsPropertyGroup();

        public PropertySheets PropertySheets;
        public Import CppDefaultProp { get; set; }
        public Import CppProp { get; set; }
        public List<InputItem> InputItems { get; set; }
    }


    class ProjectConfiguration
    {
        public string Include { get; set; }
        public string Configuration { get; set; }
        public string Platform { get; set; }

        public ConfigurationPropertyGroup Config { get; set; }
        public ItemDefinitionGroup ItemDefinitionGroup { get; set; }
    }


    abstract class PropertyGroup
    {

    }



    class GlobalsPropertyGroup : PropertyGroup
    {
        public string Label { get; } = "Globals";
        public string ProjectGuid { get; set; }
        public string WindowsTargetPlatformVersion { get; set; }
        public string Keyword { get; set; }
        public string Platform { get; set; }
        public string ProjectName { get; set; }
        public string VCProjectUpgraderObjectName { get; set; }
    }

    public class Import
    {
        public string Project { get; set; }
        public string Condition { get; set; }
        public string Label { get; set; }
    }


    class ConfigurationPropertyGroup : PropertyGroup
    {
        public string Condition { get; set; }
        public string ConfigurationType { get; set; }
        public string CharacterSet { get; set; }
        public string PlatformToolset { get; set; }

        public ConditionConfig ConditionConfig { get; set; }
        public ItemDefinitionGroup ItemDefinitionGroup { get; set; }

    }

    class ConditionConfig
    {
        public string Condition { get; set; }
        public string OutDir { get; set; }
        public string IntDir { get; set; }
        public string TargetName { get; set; }
        public string TargetExt { get; set; }
        public string LinkIncremental { get; set; }
        public string GenerateManifest { get; set; }
    }

    class ItemDefinitionGroup
    {
        public ClCompile ClCompile { get; set; }
        public ResourceCompile ResourceCompile { get; set; }
        public Midl Midl { get; set; }
        public Link Link { get; set; }
        public ProjectReference ProjectReference { get; set; }

    }


    class ClCompile
    {
        public string AdditionalIncludeDirectories { get; set; }
        public string AssemblerListingLocation { get; set; }
        public string BasicRuntimeChecks { get; set; }
        public string CompileAs { get; set; }
        public string DebugInformationFormat { get; set; }
        public string ExceptionHandling { get; set; }
        public string FloatingPointModel { get; set; }
        public string InlineFunctionExpansion { get; set; }
        public string IntrinsicFunctions { get; set; }
        public string Optimization { get; set; }
        public string PrecompiledHeader { get; set; }
        public string RuntimeLibrary { get; set; }
        public string RuntimeTypeInfo { get; set; }
        public string UseFullPaths { get; set; }
        public string WarningLevel { get; set; }
        public string PreprocessorDefinitions { get; set; }
        public string ObjectFileName { get; set; }
    }

    class ResourceCompile
    {
        public string PreprocessorDefinitions { get; set; }
        public string AdditionalIncludeDirectories { get; set; }
    }

    class Midl
    {
        public string AdditionalIncludeDirectories { get; set; }
        public string OutputDirectory { get; set; }
        public string HeaderFileName { get; set; }
        public string TypeLibraryName { get; set; }
        public string InterfaceIdentifierFileName { get; set; }
        public string ProxyFileName { get; set; }
    }

    class Link
    {
        public string AdditionalDependencies { get; set; }
        public string AdditionalLibraryDirectories { get; set; }
        public string AdditionalOptions { get; set; }
        public string GenerateDebugInformation { get; set; }
        public string IgnoreSpecificDefaultLibraries { get; set; }

        public string ImportLibrary { get; set; }
        public string ProgramDataBaseFile { get; set; }
        public string SubSystem { get; set; }
    }
    class ProjectReference
    {
        public string LinkLibraryDependencies { get; set; }
    }

    class PropertySheets
    {
        public Import Import { get; set; }
    }


    static class VcxProjExtensions
    {
        const string PROPERTY_GROUP = "PropertyGroup";
        const string IMPORT_GROUP = "ImportGroup";
        const string ITEM_GROUP = "ItemGroup";
        const string LABEL = "Label";

        public static void GenerateOutput(this VcxProject proj, XmlOutputGen output)
        {
            output.AddXmlHeader();
            output.BeginElem("Project");
            {
                //----
                output.AddAttribute(nameof(proj.DefaultTargets), proj.DefaultTargets);
                output.AddAttribute(nameof(proj.ToolVersion), proj.ToolVersion);
                output.AddAttribute("xmlns", "http://schemas.microsoft.com/developer/msbuild/2003");
                //----
                output.BeginElem(PROPERTY_GROUP);
                output.AppendSimpleElem(nameof(proj.PreferredToolArchitecture), proj.PreferredToolArchitecture);
                output.EndElem();
            }
            //---- 
            //1.
            GenPropertyConfigs(output, proj.Configurations);
            //2. global prop
            GenGlobalsProp(output, proj.GlobalsPropertyGroup);
            //3.
            GenImport(output, proj.CppDefaultProp);
            //4. gen property groups's condition config
            GenPropertyConfigs2(output, proj.Configurations);
            //5. import
            GenImport(output, proj.CppProp);
            //6.
            GenPropertySheet(output, proj.PropertySheets);
            //7.
            GenOutputConfigs(output, proj.ProjectFileVersion, proj.Configurations);
            //8.
            GenerateItemGroupDef(output, proj.Configurations);

            //9.
            output.BeginElem(ITEM_GROUP);
            {
                foreach (InputItem inputItem in proj.InputItems)
                {
                    string elem_name = "";
                    switch (inputItem.InputKind)
                    {
                        default: throw new NotSupportedException();
                        case InputKind.ClCompile:
                            elem_name = nameof(InputKind.ClCompile);
                            break;
                        case InputKind.ClInclude:
                            elem_name = nameof(InputKind.ClInclude);
                            break;
                        case InputKind.Object:
                            elem_name = nameof(InputKind.Object);
                            break;
                    }
                    output.BeginElem(elem_name);
                    output.AddAttribute(nameof(inputItem.Include), inputItem.Include);
                    output.EndElem();
                }
            }
            output.EndElem();//item group
            //-----
            //10.
            GenImport(output, new Import { Project = @"$(VCTargetsPath)\Microsoft.Cpp.targets" });
            output.BeginElem(IMPORT_GROUP);
            output.AddAttribute("Label", "ExtensionTargets");
            output.EndElem();
            //         
            //<ImportGroup Label="ExtensionTargets">

            output.EndElem(); //Project
        }
        static void GenPropertyConfigs(XmlOutputGen output, List<ProjectConfiguration> projConfigs)
        {
            output.BeginElem(ITEM_GROUP);

            output.AddAttribute(LABEL, "ProjectConfigurations");

            foreach (ProjectConfiguration projConfig in projConfigs)
            {
                output.BeginElem(nameof(ProjectConfiguration));

                output.AddAttribute(nameof(projConfig.Include), projConfig.Include);
                output.AppendSimpleElem(nameof(projConfig.Configuration), projConfig.Configuration);
                output.AppendSimpleElem(nameof(projConfig.Platform), projConfig.Platform);

                output.EndElem();
            }
            output.EndElem();
        }
        static void GenPropertyConfigs2(XmlOutputGen output, List<ProjectConfiguration> projConfigs)
        {

            foreach (ProjectConfiguration projConfig in projConfigs)
            {
                ConfigurationPropertyGroup config = projConfig.Config;

                output.BeginElem(PROPERTY_GROUP);
                output.AddAttribute(nameof(config.Condition), config.Condition);
                output.AddAttribute("Label", "Configuration");
                output.AppendSimpleElem(nameof(config.ConfigurationType), config.ConfigurationType);
                output.AppendSimpleElem(nameof(config.CharacterSet), config.CharacterSet);
                output.AppendSimpleElem(nameof(config.PlatformToolset), config.PlatformToolset);
                output.EndElem();
            }
        }


        static void GenOutputConfigs(XmlOutputGen output, string projFileVersion, List<ProjectConfiguration> projConfigs)
        {

            void GenOutputElem(string elemName, string condition, string textValue)
            {
                //NESTED method
                output.BeginElem(elemName);
                output.AddAttribute("Condition", condition);
                output.AppendTextNode(textValue);
                output.EndElem();
            }

            //7.
            output.BeginElem(PROPERTY_GROUP);
            output.AppendSimpleElem("_ProjectFileVersion", projFileVersion);
            foreach (ProjectConfiguration conf in projConfigs)
            {
                ConditionConfig condConf = conf.Config.ConditionConfig;

                GenOutputElem(nameof(condConf.OutDir), condConf.Condition, condConf.OutDir);
                GenOutputElem(nameof(condConf.IntDir), condConf.Condition, condConf.IntDir);
                GenOutputElem(nameof(condConf.TargetName), condConf.Condition, condConf.TargetName);
                GenOutputElem(nameof(condConf.TargetExt), condConf.Condition, condConf.TargetExt);
                GenOutputElem(nameof(condConf.LinkIncremental), condConf.Condition, condConf.LinkIncremental);
                GenOutputElem(nameof(condConf.GenerateManifest), condConf.Condition, condConf.GenerateManifest);

            }

            output.EndElem();//property group

        }

        static void GenerateItemGroupDef(XmlOutputGen output, List<ProjectConfiguration> projConfigs)
        {
            foreach (ProjectConfiguration conf in projConfigs)
            {
                ItemDefinitionGroup itemDefGroup = conf.ItemDefinitionGroup;

                output.BeginElem(nameof(ItemDefinitionGroup));
                {
                    output.AddAttribute("Condition", conf.Config.Condition);
                    ClCompile clcompile = itemDefGroup.ClCompile;
                    output.BeginElem(nameof(ClCompile));
                    {
                        output.AppendSimpleElem(nameof(clcompile.AdditionalIncludeDirectories), clcompile.AdditionalIncludeDirectories);
                        output.AppendSimpleElem(nameof(clcompile.AssemblerListingLocation), clcompile.AssemblerListingLocation);
                        output.AppendSimpleElem(nameof(clcompile.BasicRuntimeChecks), clcompile.BasicRuntimeChecks);
                        output.AppendSimpleElem(nameof(clcompile.CompileAs), clcompile.CompileAs);
                        output.AppendSimpleElem(nameof(clcompile.DebugInformationFormat), clcompile.DebugInformationFormat);
                        output.AppendSimpleElem(nameof(clcompile.ExceptionHandling), clcompile.ExceptionHandling);
                        output.AppendSimpleElem(nameof(clcompile.FloatingPointModel), clcompile.FloatingPointModel);
                        output.AppendSimpleElem(nameof(clcompile.InlineFunctionExpansion), clcompile.InlineFunctionExpansion);
                        output.AppendSimpleElem(nameof(clcompile.IntrinsicFunctions), clcompile.IntrinsicFunctions);
                        output.AppendSimpleElem(nameof(clcompile.Optimization), clcompile.Optimization);
                        output.AppendSimpleElem(nameof(clcompile.PrecompiledHeader), clcompile.PrecompiledHeader);
                        output.AppendSimpleElem(nameof(clcompile.RuntimeLibrary), clcompile.RuntimeLibrary);
                        output.AppendSimpleElem(nameof(clcompile.RuntimeTypeInfo), clcompile.RuntimeTypeInfo);
                        output.AppendSimpleElem(nameof(clcompile.UseFullPaths), clcompile.UseFullPaths);
                        output.AppendSimpleElem(nameof(clcompile.WarningLevel), clcompile.WarningLevel);
                        output.AppendSimpleElem(nameof(clcompile.PreprocessorDefinitions), clcompile.PreprocessorDefinitions);
                        output.AppendSimpleElem(nameof(clcompile.ObjectFileName), clcompile.ObjectFileName);
                    }
                    output.EndElem();
                    //-----
                    ResourceCompile resCompile = itemDefGroup.ResourceCompile;
                    output.BeginElem(nameof(ResourceCompile));
                    {
                        output.AppendSimpleElem(nameof(resCompile.PreprocessorDefinitions), resCompile.PreprocessorDefinitions);
                        output.AppendSimpleElem(nameof(resCompile.AdditionalIncludeDirectories), resCompile.AdditionalIncludeDirectories);
                    }
                    output.EndElem();

                    //-----
                    Midl midl = itemDefGroup.Midl;
                    output.BeginElem(nameof(Midl));
                    {
                        output.AppendSimpleElem(nameof(midl.AdditionalIncludeDirectories), midl.AdditionalIncludeDirectories);
                        output.AppendSimpleElem(nameof(midl.OutputDirectory), midl.OutputDirectory);
                        output.AppendSimpleElem(nameof(midl.HeaderFileName), midl.HeaderFileName);
                        output.AppendSimpleElem(nameof(midl.TypeLibraryName), midl.TypeLibraryName);
                        output.AppendSimpleElem(nameof(midl.InterfaceIdentifierFileName), midl.InterfaceIdentifierFileName);
                        output.AppendSimpleElem(nameof(midl.ProxyFileName), midl.ProxyFileName);
                    }
                    output.EndElem();
                    //-----

                    Link link = itemDefGroup.Link;
                    output.BeginElem(nameof(Link));
                    {
                        output.AppendSimpleElem(nameof(link.AdditionalDependencies), link.AdditionalDependencies);
                        output.AppendSimpleElem(nameof(link.AdditionalLibraryDirectories), link.AdditionalLibraryDirectories);
                        output.AppendSimpleElem(nameof(link.AdditionalOptions), link.AdditionalOptions);
                        output.AppendSimpleElem(nameof(link.GenerateDebugInformation), link.GenerateDebugInformation);
                        output.AppendSimpleElem(nameof(link.IgnoreSpecificDefaultLibraries), link.IgnoreSpecificDefaultLibraries);
                        output.AppendSimpleElem(nameof(link.ImportLibrary), link.ImportLibrary);
                        output.AppendSimpleElem(nameof(link.ProgramDataBaseFile), link.ProgramDataBaseFile);
                        output.AppendSimpleElem(nameof(link.SubSystem), link.SubSystem);
                    }
                    output.EndElem();
                    //-----
                    output.BeginElem(nameof(ProjectReference));
                    {
                        ProjectReference projRef = itemDefGroup.ProjectReference;
                        output.AppendSimpleElem(nameof(projRef.LinkLibraryDependencies), projRef.LinkLibraryDependencies);
                    }
                    output.EndElem();


                }
                output.EndElem();
            }

        }
        static void GenGlobalsProp(XmlOutputGen output, GlobalsPropertyGroup globalsProp)
        {
            output.BeginElem(PROPERTY_GROUP);
            output.AddAttribute(LABEL, "Globals");
            output.AppendSimpleElem(nameof(globalsProp.ProjectGuid), "{6481C558-B7F1-3FA1-BA6E-AD764749B15C}");
            output.AppendSimpleElem(nameof(globalsProp.WindowsTargetPlatformVersion), globalsProp.WindowsTargetPlatformVersion);
            output.AppendSimpleElem(nameof(globalsProp.Keyword), globalsProp.Keyword);
            output.AppendSimpleElem(nameof(globalsProp.Platform), globalsProp.Platform);
            output.AppendSimpleElem(nameof(globalsProp.ProjectName), globalsProp.ProjectName);
            output.AppendSimpleElem(nameof(globalsProp.VCProjectUpgraderObjectName), globalsProp.VCProjectUpgraderObjectName);

            output.EndElem();
        }
        static void GenImport(XmlOutputGen output, Import import)
        {
            output.BeginElem("Import");
            output.AddAttribute(nameof(import.Project), import.Project);
            if (import.Label != null)
            {
                output.AddAttribute(nameof(import.Label), import.Label);
            }
            if (!string.IsNullOrEmpty(import.Condition))
            {
                output.AddAttribute(nameof(import.Condition), import.Condition);
            }
            output.EndElem();
        }
        static void GenPropertySheet(XmlOutputGen output, PropertySheets propertySheet)
        {
            output.BeginElem(IMPORT_GROUP);
            output.AddAttribute("Label", nameof(PropertySheets));
            GenImport(output, propertySheet.Import);
            output.EndElem();
        }
    }
    class XmlOutputGen
    {
        StringBuilder _sb = new StringBuilder();
        Stack<string> _elemStack = new Stack<string>();

        bool _closeLatestOpenTag = true;

        public StringBuilder Output => _sb;

        public void AddXmlHeader()
        {
            _sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        }

        public void AppendSimpleElem(string elemName, string textContent)
        {
            if (textContent == null) return;

            if (!_closeLatestOpenTag)
            {
                _sb.Append(">");
                _closeLatestOpenTag = true;
            }
            _sb.Append("<" + elemName + ">" + textContent + "</" + elemName + ">");
        }
        public void AppendTextNode(string textContent)
        {
            if (!_closeLatestOpenTag)
            {
                _sb.Append(">");
                _closeLatestOpenTag = true;
            }
            _sb.Append(textContent);
        }

        public void BeginElem(string elemName)
        {
            if (!_closeLatestOpenTag)
            {
                _sb.Append(">");
            }
            _elemStack.Push(elemName);
            _sb.Append("<" + elemName);
            _closeLatestOpenTag = false;
        }
        public void AddAttribute(string attr, string attrValue)
        {
            _sb.Append(" " + attr + "=\"" + attrValue + "\"");
        }
        public void EndElem()
        {
            if (!_closeLatestOpenTag)
            {
                _sb.Append(">");
                _closeLatestOpenTag = true;
            }
            string latestElem = _elemStack.Pop();
            _sb.Append("</" + latestElem + ">");
        }
    }


}