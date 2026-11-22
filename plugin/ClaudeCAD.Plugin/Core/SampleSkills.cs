using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ClaudeCAD.Plugin.Core
{
    /// <summary>
    /// Brings the shipped sample skills up to date after an update, without touching a skill the user edited.
    ///
    /// Setup.exe, install.ps1 and the in-plugin updater all copy Contents\skills "only if the file does not exist",
    /// so a machine that already has the samples never received a newer version of them. The bundle therefore also
    /// carries a pristine copy (Contents\skills-mau, mirrored like any program file); at start the plugin replaces a
    /// sample in the skill folder with the pristine one when the installed file is provably an untouched older
    /// sample: its content matches a sample shipped up to v0.9.5 (<see cref="Legacy"/>) or the one this code
    /// recorded when it last looked (<see cref="RecordName"/>). Anything else is the user's and stays.
    /// A sample the user removed is not put back here (the installers add missing files).
    /// </summary>
    public static class SampleSkills
    {
        /// <summary>Folder next to the skill folder holding the pristine samples of the installed version.</summary>
        public const string PristineFolderName = "skills-mau";

        /// <summary>In the skill folder: "relative/path|sha256" of every sample that is still as shipped.</summary>
        public const string RecordName = ".claudecad-mau.txt";

        /// <summary>
        /// Samples shipped up to v0.9.5 (before the record existed): "relative/path|sha256", content hashed
        /// without BOM and carriage returns. Never remove an entry: an old machine may still have that file.
        /// </summary>
        public static readonly string[] Legacy =
        {
            "README.md|104c605314364b77030305ad786d44fcd33ce07a0cf775f486825951392d6f70",
            "README.md|817c6adc589e5e53a29526397092d1422de803f43d3ce6593559b9d45875ed62",
            "README.md|a8ff2970b559df9c3ae85785c955ec1a2c391a87c7dab0e71e34486b7c4c0c59",
            "README.md|6005a411de74d5d2f587ffa2d767d5fb21f4ba39d451da1c921048357bc68b50",
            "ve-ha-tang/SKILL.md|6dc1cc430a59689a89d43f8fad19fec3e501d0fd968f0a4a5178dbf83632c647",
            "ve-kien-truc/SKILL.md|524cff7011f13b7c20c9a836db1925db3b66647a789496739faf6a5d6732eee4",
            "ve-mep/SKILL.md|78dda1dc22f29276a9c23e2b97cfc8c2be0ec62b2d3800fe492ef69a8dfda031",
        };

        public sealed class Result
        {
            /// <summary>Samples replaced by the newer shipped version (relative paths).</summary>
            public readonly List<string> Updated = new List<string>();
            /// <summary>Samples that differ from the shipped version and were left alone: the user's edits.</summary>
            public readonly List<string> Kept = new List<string>();
        }

        /// <summary>
        /// SHA-256 of the text content: a UTF-8 BOM and carriage returns are ignored, so the same sample hashes
        /// the same whether Git checked it out with LF or CRLF.
        /// </summary>
        public static string Hash(byte[] content)
        {
            if (content == null) content = new byte[0];
            int start = content.Length >= 3 && content[0] == 0xEF && content[1] == 0xBB && content[2] == 0xBF ? 3 : 0;
            var clean = new byte[content.Length - start];
            int n = 0;
            for (int i = start; i < content.Length; i++)
                if (content[i] != (byte)'\r') clean[n++] = content[i];
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(clean, 0, n);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        /// <summary>
        /// Replaces untouched older samples in <paramref name="skillsDir"/> with the ones in
        /// <paramref name="pristineDir"/> and rewrites the record. Does nothing when either folder is missing.
        /// </summary>
        public static Result Refresh(string pristineDir, string skillsDir, IEnumerable<string> legacy = null)
        {
            var result = new Result();
            if (string.IsNullOrEmpty(pristineDir) || string.IsNullOrEmpty(skillsDir)) return result;
            if (!Directory.Exists(pristineDir) || !Directory.Exists(skillsDir)) return result;

            var known = new HashSet<string>(legacy ?? Legacy, StringComparer.OrdinalIgnoreCase);
            string recordFile = Path.Combine(skillsDir, RecordName);
            var oldRecord = ReadRecord(recordFile);
            foreach (string line in oldRecord) known.Add(line);

            var newRecord = new List<string>();
            string root = Path.GetFullPath(pristineDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            foreach (string source in Directory.GetFiles(root, "*", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                string rel = source.Substring(root.Length + 1).Replace('\\', '/');
                if (string.Equals(rel, RecordName, StringComparison.OrdinalIgnoreCase)) continue;
                string target = Path.Combine(skillsDir, rel.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(target)) continue; // removed by the user, or about to be added by the installer

                byte[] shipped = File.ReadAllBytes(source);
                string shippedHash = Hash(shipped);
                string installedHash = Hash(File.ReadAllBytes(target));
                if (installedHash == shippedHash)
                {
                    newRecord.Add(rel + "|" + shippedHash);
                }
                else if (known.Contains(rel + "|" + installedHash))
                {
                    File.WriteAllBytes(target, shipped);
                    newRecord.Add(rel + "|" + shippedHash);
                    result.Updated.Add(rel);
                }
                else
                {
                    result.Kept.Add(rel);
                }
            }

            if (!newRecord.SequenceEqual(oldRecord, StringComparer.Ordinal))
                File.WriteAllLines(recordFile, newRecord.ToArray(), new UTF8Encoding(false));
            return result;
        }

        /// <summary>
        /// True when the installed sample <paramref name="rel"/> differs from the shipped one and is not a known
        /// older sample (which Refresh updates): the user edited it.
        /// </summary>
        public static bool IsEdited(string pristineDir, string skillsDir, string rel, IEnumerable<string> legacy = null)
        {
            string source = Path.Combine(pristineDir ?? "", rel.Replace('/', Path.DirectorySeparatorChar));
            string target = Path.Combine(skillsDir ?? "", rel.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(source) || !File.Exists(target)) return false;
            string installed = Hash(File.ReadAllBytes(target));
            if (installed == Hash(File.ReadAllBytes(source))) return false;
            var known = new HashSet<string>(legacy ?? Legacy, StringComparer.OrdinalIgnoreCase);
            foreach (string line in ReadRecord(Path.Combine(skillsDir, RecordName))) known.Add(line);
            return !known.Contains(rel + "|" + installed);
        }

        private static List<string> ReadRecord(string file)
        {
            var lines = new List<string>();
            try
            {
                if (!File.Exists(file)) return lines;
                foreach (string raw in File.ReadAllLines(file))
                {
                    string line = raw.Trim();
                    int bar = line.LastIndexOf('|');
                    if (bar > 0 && line.Length - bar - 1 == 64) lines.Add(line);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return lines;
        }
    }
}
