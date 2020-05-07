﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using System.Windows.Forms;
using emulatorLauncher.Tools;
using System.Threading;

namespace emulatorLauncher
{
    class FpinballGenerator : Generator
    {
        string _bam;
        string _rom;

        public override System.Diagnostics.ProcessStartInfo Generate(string system, string emulator, string core, string rom, string playersControllers, ScreenResolution resolution)
        {
            string path = AppConfig.GetFullPath("fpinball");

            _rom = rom;

            if ("bam".Equals(emulator, StringComparison.InvariantCultureIgnoreCase) || "bam".Equals(core, StringComparison.InvariantCultureIgnoreCase))
                _bam = Path.Combine(path, "BAM", "FPLoader.exe");

            string exe = Path.Combine(path, "Future Pinball.exe");
            if (!File.Exists(exe))
            {
                exe = Path.Combine(path, "FuturePinball.exe");
                if (!File.Exists(exe))
                    return null;
            }

            if (_bam != null && File.Exists(_bam))
                SetAsAdmin(_bam);

            SetAsAdmin(exe);
            SetupOptions(resolution);

            return new ProcessStartInfo()
            {
                FileName = _bam != null && File.Exists(_bam) ? _bam : exe,
                Arguments = "/open \"" + rom + "\" /play /exit",            
            };
        }

        public override void RunAndWait(ProcessStartInfo path)
        {
            Process process = null;

            if (_bam != null && File.Exists(_bam))
            {
                Process.Start(path);

                int tickCount = Environment.TickCount;
                string fileNameWithoutExtension = "Future Pinball";

                process = Process.GetProcessesByName(fileNameWithoutExtension).FirstOrDefault<Process>();
                while (process == null && (Environment.TickCount - tickCount < 1000))
                {
                    process = Process.GetProcessesByName(fileNameWithoutExtension).FirstOrDefault<Process>();
                    if (process == null)
                        Thread.Sleep(10);
                }
            }
            else
                process = Process.Start(path);

            if (process != null)
                process.WaitForExit();
        }

        public override void Cleanup()
        {
            PerformBamCapture();
            base.Cleanup();
        }

        private void PerformBamCapture()
        {
            if (_bam == null || !File.Exists(_bam))
                return;

            string bamPng = Path.Combine(Path.GetDirectoryName(_bam), Path.ChangeExtension(Path.GetFileName(_rom), ".png"));
            if (File.Exists(bamPng))
            {
                ScreenCapture.AddImageToGameList(_rom, bamPng);

                try { File.Delete(bamPng); }
                catch { }
            }
        }

        private static void SetAsAdmin(string path)
        {
            RegistryKey regKeyc = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", true);
            if (regKeyc != null)
            {                
                regKeyc.SetValue(path, "");
                //if (regKeyc.GetValue(path) == null)
                  //  regKeyc.SetValue(path, "~ RUNASADMIN");

                regKeyc.Close();
            }
        }

        private void SetupOptions(ScreenResolution resolution)
        {
            RegistryKey regKeyc = Registry.CurrentUser.OpenSubKey(@"Software", true);
            if (regKeyc != null)
                regKeyc = regKeyc.CreateSubKey("Future Pinball").CreateSubKey("GamePlayer");

            if (regKeyc != null)
            {
                regKeyc.SetValue("FullScreen", 1); regKeyc.SetValue("FullScreen", 0);

                if (SystemConfig.isOptSet("arcademode") && SystemConfig["arcademode"] == "1")
                    regKeyc.SetValue("ArcadeMode", 1);
                else
                    regKeyc.SetValue("ArcadeMode", 0);

                if (SystemConfig.isOptSet("ratio"))
                {
                    if (SystemConfig["ratio"] == "4/3")
                        regKeyc.SetValue("AspectRatio", 43);
                    else if (SystemConfig["ratio"] == "16/9")
                        regKeyc.SetValue("AspectRatio", 169);
                }
                else
                    regKeyc.SetValue("AspectRatio", 169);

                if (resolution != null)
                {
                    regKeyc.SetValue("Width", resolution.Width);
                    regKeyc.SetValue("Height", resolution.Height);
                    regKeyc.SetValue("BitsPerPixel", resolution.BitsPerPel);
                }
                else
                {
                    regKeyc.SetValue("Height", Screen.PrimaryScreen.Bounds.Height);
                    regKeyc.SetValue("Width", Screen.PrimaryScreen.Bounds.Width);
                    regKeyc.SetValue("BitsPerPixel", Screen.PrimaryScreen.BitsPerPixel);
                }

                if (regKeyc.GetValue("DefaultCamera") == null)
                    regKeyc.SetValue("DefaultCamera", 0);

                if (regKeyc.GetValue("CameraFollowsTheBall") == null)
                    regKeyc.SetValue("CameraFollowsTheBall", 0);

                if (SystemConfig.isOptSet("preset") && SystemConfig["preset"] == "medium")
                {
                    regKeyc.SetValue("RenderGameRoom", 1);
                    regKeyc.SetValue("RenderOrnaments", 1);
                    regKeyc.SetValue("GlassOverlay", 1);
                    regKeyc.SetValue("LightFacets", 0x20);
                    regKeyc.SetValue("GlassReflections", 0);
                    regKeyc.SetValue("PlayfieldReflections", 1);
                    regKeyc.SetValue("RenderBallMirrors", 0);
                    regKeyc.SetValue("SuperNiceCrystal", 0);
                    regKeyc.SetValue("HighQualityPinballs", 1);
                    regKeyc.SetValue("BallDirt", 1);
                    regKeyc.SetValue("DisableShaders", 0);
                    regKeyc.SetValue("ModelRenderQuality", 1);
                    regKeyc.SetValue("RubberFacets", 0x10);
                    regKeyc.SetValue("RubberSides", 0x0c);
                    regKeyc.SetValue("WireGuideSides", 0x0c);
                    regKeyc.SetValue("HighQualityTextures", 1);                 
                }
                else if (SystemConfig.isOptSet("preset") && SystemConfig["preset"] == "min")
                {
                    regKeyc.SetValue("RenderGameRoom", 0);
                    regKeyc.SetValue("RenderOrnaments", 0);
                    regKeyc.SetValue("GlassOverlay", 0);
                    regKeyc.SetValue("LightFacets", 0x10);
                    regKeyc.SetValue("GlassReflections", 0);
                    regKeyc.SetValue("PlayfieldReflections", 0);
                    regKeyc.SetValue("RenderBallMirrors", 0);
                    regKeyc.SetValue("SuperNiceCrystal", 0);
                    regKeyc.SetValue("HighQualityPinballs", 0);
                    regKeyc.SetValue("BallDirt", 0);
                    regKeyc.SetValue("DisableShaders", 1);
                    regKeyc.SetValue("ModelRenderQuality", 0);
                    regKeyc.SetValue("RubberFacets", 0x08);
                    regKeyc.SetValue("RubberSides", 0x06);
                    regKeyc.SetValue("WireGuideSides", 0x06);
                    regKeyc.SetValue("HighQualityTextures", 0);
                }
                else
                {
                    regKeyc.SetValue("RenderGameRoom", 1);
                    regKeyc.SetValue("RenderOrnaments", 1);
                    regKeyc.SetValue("GlassOverlay", 1);
                    regKeyc.SetValue("LightFacets", 0x40);
                    regKeyc.SetValue("GlassReflections", 1);
                    regKeyc.SetValue("PlayfieldReflections", 1);
                    regKeyc.SetValue("RenderBallMirrors", 1);
                    regKeyc.SetValue("SuperNiceCrystal", 1);
                    regKeyc.SetValue("HighQualityPinballs", 1);
                    regKeyc.SetValue("BallDirt", 1);
                    regKeyc.SetValue("DisableShaders", 0);
                    regKeyc.SetValue("ModelRenderQuality", 2);
                    regKeyc.SetValue("RubberFacets", 0x40);
                    regKeyc.SetValue("RubberSides", 0x14);
                    regKeyc.SetValue("WireGuideSides", 0x14);
                    regKeyc.SetValue("HighQualityTextures", 1);
                }

                regKeyc.Close();
            }

            regKeyc = Registry.CurrentUser.OpenSubKey(@"Software", true);
            if (regKeyc != null)
                regKeyc = regKeyc.CreateSubKey("Future Pinball").CreateSubKey("Editor");

            if (regKeyc != null)
            {
                regKeyc.SetValue("LoadImagesIntoEditor", 0);
                regKeyc.Close();
            }
        }
    }
}
