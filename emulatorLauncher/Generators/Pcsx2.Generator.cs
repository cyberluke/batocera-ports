﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.Management;

namespace emulatorLauncher
{
    class Pcsx2Generator : Generator
    {

        public Pcsx2Generator()
        {
            DependsOnDesktopResolution = true;
        }

        public override int RunAndWait(ProcessStartInfo path)
        {
            FakeBezelFrm bezel = null;

            if (_bezelFileInfo != null)
                bezel = _bezelFileInfo.ShowFakeBezel(_resolution);

            int ret = base.RunAndWait(path);

            if (bezel != null)
                bezel.Dispose();

            return ret;
        }
      
        private string _path;
        private BezelFiles _bezelFileInfo;
        private ScreenResolution _resolution;

        private bool _isPcsx17;

        public override System.Diagnostics.ProcessStartInfo Generate(string system, string emulator, string core, string rom, string playersControllers, ScreenResolution resolution)
        {
            string folderName = emulator;

            _path = AppConfig.GetFullPath(folderName);
            if (string.IsNullOrEmpty(_path))
                _path = AppConfig.GetFullPath("pcsx2");

            if (string.IsNullOrEmpty(_path))
                _path = AppConfig.GetFullPath("pcsx2-16");
            
            string exe = Path.Combine(_path, "pcsx2x64.exe"); // v1.7 filename
            if (!File.Exists(exe))
                exe = Path.Combine(_path, "pcsx2.exe"); // v1.6 filename            

            // v1.7.0 ???
            Version version = new Version();
            if (Version.TryParse(FileVersionInfo.GetVersionInfo(exe).ProductVersion, out version))
                _isPcsx17 = version >= new Version(1, 7, 0, 0);

            // Select avx2 build
            if (_isPcsx17 && (core == "pcsx2-avx2" || core == "avx2"))
            {
                string avx2 = Path.Combine(_path, "pcsx2x64-avx2.exe");

                if (!File.Exists(avx2))
                    avx2 = Path.Combine(_path, "pcsx2-avx2.exe");

                if (File.Exists(avx2))
                {
                    exe = avx2;

                    if (Version.TryParse(FileVersionInfo.GetVersionInfo(exe).ProductVersion, out version))
                        _isPcsx17 = version >= new Version(1, 7, 0, 0);
                }
            }

            if (!File.Exists(exe))
                return null;

            SetupPaths(emulator, core);
            SetupVM();
            SetupLilyPad();
            SetupGSDx(resolution);

            File.WriteAllText(Path.Combine(_path, "portable.ini"), "RunWizard=0");

            if (!SystemConfig.isOptSet("ratio") || SystemConfig["ratio"] == "4:3")
                _bezelFileInfo = BezelFiles.GetBezelFiles(system, rom, resolution);

            _resolution = resolution;

            List<string> commandArray = new List<string>();
            commandArray.Add("--portable");
            commandArray.Add("--fullscreen");
            commandArray.Add("--nogui");

            if (SystemConfig.isOptSet("fullboot") && SystemConfig.getOptBoolean("fullboot"))
                commandArray.Add("--fullboot");

            string args = string.Join(" ", commandArray);

            return new ProcessStartInfo()
            {
                FileName = exe,
                WorkingDirectory = _path,
                Arguments = args + " \"" + rom + "\"",
            };
        }
        private void SetupPaths(string emulator, string core)
        {
            var biosList = new string[] { 
                            "SCPH30004R.bin", "SCPH30004R.MEC", "scph39001.bin", "scph39001.MEC", 
                            "SCPH-39004_BIOS_V7_EUR_160.BIN", "SCPH-39001_BIOS_V7_USA_160.BIN", "SCPH-70000_BIOS_V12_JAP_200.BIN" };

            string iniFile = Path.Combine(_path, "inis", "PCSX2_ui.ini");

            try
            {
                using (var ini = new IniFile(iniFile))
                {
                    Uri relRoot = new Uri(_path, UriKind.Absolute);

                    string biosPath = AppConfig.GetFullPath("bios");
                    if (!string.IsNullOrEmpty(biosPath))
                    {
                        ini.WriteValue("Folders", "UseDefaultBios", "disabled");

                        if (biosList.Any(b => File.Exists(Path.Combine(biosPath, "pcsx2", "bios", b))))
                            ini.WriteValue("Folders", "Bios", Path.Combine(biosPath, "pcsx2", "bios").Replace("\\", "\\\\"));
                        else
                            ini.WriteValue("Folders", "Bios", biosPath.Replace("\\", "\\\\"));
                        
                        ini.WriteValue("Folders", "UseDefaultCheats", "disabled");
                        ini.WriteValue("Folders", "Cheats", Path.Combine(biosPath, "pcsx2", "cheats").Replace("\\", "\\\\"));
                        ini.WriteValue("Folders", "UseDefaultCheatsWS", "disabled");
                        ini.WriteValue("Folders", "CheatsWS", Path.Combine(biosPath, "pcsx2", "cheats_ws").Replace("\\", "\\\\"));
                    }

                    string savesPath = AppConfig.GetFullPath("saves");
                    if (!string.IsNullOrEmpty(savesPath))
                    {
                        savesPath = Path.Combine(savesPath, "ps2", Path.GetFileName(_path));

                        if (!Directory.Exists(savesPath))
                            try { Directory.CreateDirectory(savesPath); }
                            catch { }

                        ini.WriteValue("Folders", "UseDefaultSavestates", "disabled");
                        ini.WriteValue("Folders", "UseDefaultMemoryCards", "disabled");
                        ini.WriteValue("Folders", "Savestates", savesPath.Replace("\\", "\\\\") + "\\\\" + "sstates");
                        ini.WriteValue("Folders", "MemoryCards", savesPath.Replace("\\", "\\\\") + "\\\\" + "memcards");
                    }

                    string screenShotsPath = AppConfig.GetFullPath("screenshots");
                    if (!string.IsNullOrEmpty(screenShotsPath))
                    {

                        ini.WriteValue("Folders", "UseDefaultSnapshots", "disabled");
                        ini.WriteValue("Folders", "Snapshots", screenShotsPath.Replace("\\", "\\\\") + "\\\\" + "pcsx2");
                    }

                    if (SystemConfig.isOptSet("ratio") && !string.IsNullOrEmpty(SystemConfig["ratio"]))
                        ini.WriteValue("GSWindow", "AspectRatio", SystemConfig["ratio"]);
                    else
                        ini.WriteValue("GSWindow", "AspectRatio", "4:3");

                    if (SystemConfig.isOptSet("fmv_ratio") && !string.IsNullOrEmpty(SystemConfig["fmv_ratio"]))
                        ini.WriteValue("GSWindow", "FMVAspectRatioSwitch", SystemConfig["fmv_ratio"]);
                    else if (Features.IsSupported("fmv_ratio"))
                        ini.WriteValue("GSWindow", "FMVAspectRatioSwitch", "Off");

                    ini.WriteValue("ProgramLog", "Visible", "disabled");
                    ini.WriteValue("GSWindow", "IsFullscreen", "enabled");

                    if (Features.IsSupported("negdivhack") && SystemConfig.isOptSet("negdivhack") && SystemConfig.getOptBoolean("negdivhack"))
                        ini.WriteValue(null, "EnablePresets", "disabled");

                    if (!_isPcsx17)
                    {
                        ini.WriteValue("Filenames", "PAD", "LilyPad.dll");

                        // Enabled for <= 1.6.0
                        if (Features.IsSupported("gs_plugin"))
                        {
                            if (SystemConfig.isOptSet("gs_plugin") && !string.IsNullOrEmpty(SystemConfig["gs_plugin"]))
                                ini.WriteValue("Filenames", "GS", SystemConfig["gs_plugin"]);
                            else
                                ini.WriteValue("Filenames", "GS", "GSdx32-SSE2.dll");
                        }

                        foreach (var key in new string[] { "SPU2", "CDVD", "USB", "FW", "DEV9", "BIOS"})
                        {
                            string value = ini.GetValue("Filenames", key);
                            if (value == null || value == "Please Configure")
                            {
                                switch (key)
                                {
                                    case "SPU2":
                                        value = "Spu2-X.dll";
                                        break;
                                    case "CDVD":
                                        value = "cdvdGigaherz.dll";
                                        break;
                                    case "USB":
                                        value = "USBnull.dll";
                                        break;
                                    case "FW":
                                        value = "FWnull.dll";
                                        break;
                                    case "DEV9":
                                        value = "DEV9null.dll";
                                        break;
                                    case "BIOS":

                                        var biosFile = biosList.FirstOrDefault(b => File.Exists(Path.Combine(biosPath, "pcsx2", "bios", b)));
                                        if (!string.IsNullOrEmpty(biosFile))
                                            value = biosFile;
                                        else
                                            value = "SCPH30004R.bin";

                                        break;
                                }

                                ini.WriteValue("Filenames", key, value);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void SetupLilyPad()
        {
            if (_isPcsx17)
                return; // Keyboard Mode 1 is not supported anymore

            string iniFile = Path.Combine(_path, "inis", _isPcsx17 ? "PAD.ini" : "LilyPad.ini");

            try
            {
                using (var ini = new IniFile(iniFile))
                    ini.WriteValue("General Settings", "Keyboard Mode", "1");
            }
            catch { }
        }

        private void SetupVM()
        {
            string iniFile = Path.Combine(_path, "inis", "PCSX2_vm.ini");

            try
            {
                using (var ini = new IniFile(iniFile))
                {
                    if (_isPcsx17)
                        ini.WriteValue("EmuCore", "EnableRecordingTools", "disabled");

                    if (!string.IsNullOrEmpty(SystemConfig["VSync"]))
                        ini.WriteValue("EmuCore/GS", "VsyncEnable", SystemConfig["VSync"]);
                    else
                        ini.WriteValue("EmuCore/GS", "VsyncEnable", "1");

                    if (Features.IsSupported("negdivhack"))
                    {
                        string negdivhack = SystemConfig.isOptSet("negdivhack") && SystemConfig.getOptBoolean("negdivhack") ? "enabled" : "disabled";

                        ini.WriteValue("EmuCore/Speedhacks", "vuThread", negdivhack);

                        ini.WriteValue("EmuCore/CPU/Recompiler", "vuExtraOverflow", negdivhack);
                        ini.WriteValue("EmuCore/CPU/Recompiler", "vuSignOverflow", negdivhack);
                        ini.WriteValue("EmuCore/CPU/Recompiler", "fpuExtraOverflow", negdivhack);
                        ini.WriteValue("EmuCore/CPU/Recompiler", "fpuFullMode", negdivhack);

                        ini.WriteValue("EmuCore/Gamefixes", "VuClipFlagHack", negdivhack);
                        ini.WriteValue("EmuCore/Gamefixes", "FpuNegDivHack", negdivhack);
                    }
                }
            }
            catch { }
        }

        private void SetupGSDx(ScreenResolution resolution)
        {
            string iniFile = Path.Combine(_path, "inis", _isPcsx17 ? "GS.ini" : "GSdx.ini");

            try
            {
                using (var ini = new IniFile(iniFile))
                {
                    
                    if (!_isPcsx17)
                        ini.WriteValue("Settings", "UserHacks", "1");
                    
                    if ((_isPcsx17) && (SystemConfig.isOptSet("UserHacks") && !string.IsNullOrEmpty(SystemConfig["UserHacks"])))
                        ini.WriteValue("Settings", "UserHacks", SystemConfig["UserHacks"]);
                    else if (_isPcsx17)
                        ini.WriteValue("Settings", "UserHacks", "0");


                    if (!string.IsNullOrEmpty(SystemConfig["internalresolution"]))
                        ini.WriteValue("Settings", "upscale_multiplier", SystemConfig["internalresolution"]);
                    else
                        ini.WriteValue("Settings", "upscale_multiplier", "1");

                    if (string.IsNullOrEmpty(SystemConfig["internalresolution"]) || SystemConfig["internalresolution"] == "0")
                    {
                        if (resolution != null)
                        {
                            ini.WriteValue("Settings", "resx", resolution.Width.ToString());
                            ini.WriteValue("Settings", "resy", (resolution.Height * 2).ToString());
                        }
                        else
                        {
                            ini.WriteValue("Settings", "resx", Screen.PrimaryScreen.Bounds.Width.ToString());
                            ini.WriteValue("Settings", "resy", (Screen.PrimaryScreen.Bounds.Height * 2).ToString());
                        }
                    }

                    ini.WriteValue("Settings", "shaderfx", "1");

                    if (SystemConfig.isOptSet("TVShader") && !string.IsNullOrEmpty(SystemConfig["TVShader"]))
                        ini.WriteValue("Settings", "TVShader", SystemConfig["TVShader"]);
                    else if (Features.IsSupported("TVShader"))
                        ini.WriteValue("Settings", "TVShader", "0");

                    if (SystemConfig.isOptSet("Offset") && !string.IsNullOrEmpty(SystemConfig["Offset"]))
                        ini.WriteValue("Settings", "UserHacks_WildHack", SystemConfig["Offset"]);
                    else if (Features.IsSupported("Offset"))
                        ini.WriteValue("Settings", "UserHacks_WildHack", "0");

                    if (SystemConfig.isOptSet("bilinear_filtering") && !string.IsNullOrEmpty(SystemConfig["bilinear_filtering"]))
                        ini.WriteValue("Settings", "linear_present", SystemConfig["bilinear_filtering"]);
                    else if (Features.IsSupported("bilinear_filtering"))
                        ini.WriteValue("Settings", "linear_present", "0");

                    if (SystemConfig.isOptSet("fxaa") && !string.IsNullOrEmpty(SystemConfig["fxaa"]))
                        ini.WriteValue("Settings", "fxaa", SystemConfig["fxaa"]);
                    else if (Features.IsSupported("fxaa"))
                        ini.WriteValue("Settings", "fxaa", "0");

                    if (SystemConfig.isOptSet("renderer") && !string.IsNullOrEmpty(SystemConfig["renderer"]))
                        ini.WriteValue("Settings", "Renderer", SystemConfig["renderer"]);
                    else if (Features.IsSupported("renderer"))
                        ini.WriteValue("Settings", "Renderer", "12");

                    if (SystemConfig.isOptSet("interlace") && !string.IsNullOrEmpty(SystemConfig["interlace"]))
                        ini.WriteValue("Settings", "interlace", SystemConfig["interlace"]);
                    else if (Features.IsSupported("interlace"))
                        ini.WriteValue("Settings", "interlace", "7");

                    if (SystemConfig.isOptSet("anisotropic_filtering") && !string.IsNullOrEmpty(SystemConfig["anisotropic_filtering"]))
                        ini.WriteValue("Settings", "MaxAnisotropy", SystemConfig["anisotropic_filtering"]);
                    else if (Features.IsSupported("anisotropic_filtering"))
                        ini.WriteValue("Settings", "MaxAnisotropy", "0");

                    if (SystemConfig.isOptSet("align_sprite") && !string.IsNullOrEmpty(SystemConfig["align_sprite"]))
                        ini.WriteValue("Settings", "UserHacks_align_sprite_X", SystemConfig["align_sprite"]);
                    else if (Features.IsSupported("align_sprite"))
                        ini.WriteValue("Settings", "UserHacks_align_sprite_X", "0");

                    if (SystemConfig.isOptSet("skipdraw") && !string.IsNullOrEmpty(SystemConfig["skipdraw"]))
                        ini.WriteValue("Settings", "UserHacks_SkipDraw", SystemConfig["skipdraw"]);
                    else if (Features.IsSupported("skipdraw"))
                        ini.WriteValue("Settings", "UserHacks_SkipDraw", "0");

                    if (!_isPcsx17)
                    {
                        if (SystemConfig.isOptSet("DrawFramerate") && SystemConfig.getOptBoolean("DrawFramerate"))
                        {
                            ini.WriteValue("Settings", "osd_monitor_enabled", "1");
                            ini.WriteValue("Settings", "osd_indicator_enabled", "1");
                        }
                        else
                        {
                            ini.WriteValue("Settings", "osd_monitor_enabled", "0");
                            ini.WriteValue("Settings", "osd_indicator_enabled", "0");
                        }

                        if (SystemConfig.isOptSet("Notifications") && SystemConfig.getOptBoolean("Notifications"))
                            ini.WriteValue("Settings", "osd_log_enabled", "1");
                        else
                            ini.WriteValue("Settings", "osd_log_enabled", "0");

                    }
                    else
                    {
                        if (SystemConfig.isOptSet("DrawFramerate") && SystemConfig.getOptBoolean("DrawFramerate"))
                        {
                            ini.WriteValue("Settings", "OsdShowCPU", "1");
                            ini.WriteValue("Settings", "OsdShowFPS", "1");
                            ini.WriteValue("Settings", "OsdShowGPU", "1");                            
                            ini.WriteValue("Settings", "OsdShowResolution", "1");
                            ini.WriteValue("Settings", "OsdShowSpeed", "1");
                        }
                        else
                        {
                            ini.WriteValue("Settings", "OsdShowCPU", "0");
                            ini.WriteValue("Settings", "OsdShowFPS", "0");
                            ini.WriteValue("Settings", "OsdShowGPU", "0");
                            ini.WriteValue("Settings", "OsdShowResolution", "0");
                            ini.WriteValue("Settings", "OsdShowSpeed", "0");
                        }

                        if (SystemConfig.isOptSet("Notifications") && SystemConfig.getOptBoolean("Notifications"))
                            ini.WriteValue("Settings", "OsdShowMessages", "1");
                        else
                            ini.WriteValue("Settings", "OsdShowMessages", "0");
                    }

                }

            }
            catch { }
        }
    }
}
