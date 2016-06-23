using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Diagnostics;

namespace ETGModInstaller {
    public static class De4DotHelper {

        public static bool UnzipDe4Dot(this InstallerWindow ins) {
            string pathGame = ins.MainMod.Dir.FullName;

            if (File.Exists(Path.Combine(pathGame, "de4dot.exe"))) {
                ins.LogLine("de4dot already existing, not extracting.");
                return false;
            }

            ins.LogLine("Extracting de4dot...");

            using (Stream zs = Assembly.GetExecutingAssembly().GetManifestResourceStream("ETGMod.Installer.Assets.de4dot.zip")) {
                using (ZipArchive zip = new ZipArchive(zs, ZipArchiveMode.Read)) {
                    ins.InitProgress("Extracting de4dot...", zip.Entries.Count);
                    for (int i = 0; i < zip.Entries.Count; i++) {
                        ins.SetProgress(i);
                        ZipArchiveEntry entry = zip.Entries[i];
                        ins.SetProgress(i);
                        string path = Path.Combine(pathGame, entry.FullName.Replace('/', Path.DirectorySeparatorChar));
                        ins.Log("Extracting: ").Log(entry.FullName).Log(" (").Log(entry.Length.ToString()).Log(" bytes) -> ").LogLine(path);
                        if (entry.Length == 0 && entry.CompressedLength == 0) {
                            Directory.CreateDirectory(path);
                        } else {
                            Directory.GetParent(path).Create();
                            entry.ExtractToFile(path, true);
                        }
                    }
                    ins.EndProgress("Extracted ZIP.");
                }
            }

            return true;
        }


        public static void Deobfuscate(this InstallerWindow ins, string file) {
            ins.Log("Deobfuscating ").Log(file).LogLine(" with de4dot");
            ins.InitProgress("Deobfuscating...", 1);

            string de4dotPath = Path.Combine(ins.MainMod.Dir.FullName, "de4dot" + (IntPtr.Size == 4 ? ".exe" : "-x64.exe"));
            string fileIn = Path.Combine(ins.MainMod.Dir.FullName, file);
            string fileOut = fileIn + ".deobfuscated";

            if (File.Exists(fileOut)) {
                File.Delete(fileOut);
            }

            Process de4dot = new Process();
            de4dot.StartInfo.FileName = de4dotPath;
            de4dot.StartInfo.Arguments = "-f \"" + fileIn + "\" -o \"" + fileOut + "\"";
            de4dot.StartInfo.CreateNoWindow = true;
            de4dot.StartInfo.RedirectStandardOutput = true;
            de4dot.StartInfo.UseShellExecute = false;
            de4dot.EnableRaisingEvents = true;

            string os = ETGFinder.GetPlatform().ToString().ToLower();
            if (!os.Contains("win")) {
                de4dot.StartInfo.Arguments = "\"" + de4dot.StartInfo.FileName + "\" " + de4dot.StartInfo.Arguments;
                de4dot.StartInfo.FileName = "mono";
            }

            de4dot.OutputDataReceived += new DataReceivedEventHandler(
                delegate(object sender, DataReceivedEventArgs e) {
                    ins.LogLine(e.Data);
                }
            );
            //ins.Log(de4dot.StartInfo.FileName).Log(" ").LogLine(de4dot.StartInfo.Arguments);
            de4dot.Start();
            de4dot.BeginOutputReadLine();
            de4dot.WaitForExit();
            de4dot.CancelOutputRead();

            File.Delete(fileIn);
            File.Move(fileOut, fileIn);
            ins.EndProgress("Deobfuscated.");
        }

    }
}
