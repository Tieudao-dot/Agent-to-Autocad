using System;
using System.IO;
using System.Linq;
using System.Text;
using ClaudeCAD.Plugin.Core;
using Xunit;

namespace ClaudeCAD.Core.Tests
{
    public class SampleSkillsTests : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "claudecad-samples-" + Guid.NewGuid().ToString("N"));
        private string Pristine => Path.Combine(_root, "skills-mau");
        private string Skills => Path.Combine(_root, "skills");

        public SampleSkillsTests()
        {
            Directory.CreateDirectory(Pristine);
            Directory.CreateDirectory(Skills);
        }

        public void Dispose()
        {
            try { Directory.Delete(_root, true); } catch { }
        }

        private static byte[] B(string s) => Encoding.UTF8.GetBytes(s);

        private void Put(string dir, string rel, string text)
        {
            string f = Path.Combine(dir, rel.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(f));
            File.WriteAllBytes(f, B(text));
        }

        private string Read(string rel) => File.ReadAllText(Path.Combine(Skills, rel.Replace('/', Path.DirectorySeparatorChar)));

        private static string[] LegacyOf(string rel, string text) => new[] { rel + "|" + SampleSkills.Hash(B(text)) };

        [Fact]
        public void HashIgnoresBomAndCarriageReturns()
        {
            string lf = SampleSkills.Hash(B("# Vẽ\nbước 1\n"));
            Assert.Equal(lf, SampleSkills.Hash(B("# Vẽ\r\nbước 1\r\n")));
            Assert.Equal(lf, SampleSkills.Hash(new byte[] { 0xEF, 0xBB, 0xBF }.Concat(B("# Vẽ\nbước 1\n")).ToArray()));
            Assert.NotEqual(lf, SampleSkills.Hash(B("# Vẽ\nbước 2\n")));
            Assert.Equal(64, lf.Length);
        }

        [Fact]
        public void UntouchedOldSampleIsReplaced()
        {
            Put(Pristine, "ve-mep/SKILL.md", "# MEP v2\n");
            Put(Skills, "ve-mep/SKILL.md", "# MEP v1\r\n"); // as checked out on Windows
            var r = SampleSkills.Refresh(Pristine, Skills, LegacyOf("ve-mep/SKILL.md", "# MEP v1\n"));
            Assert.Equal(new[] { "ve-mep/SKILL.md" }, r.Updated);
            Assert.Empty(r.Kept);
            Assert.Equal("# MEP v2\n", Read("ve-mep/SKILL.md"));
        }

        [Fact]
        public void EditedSampleIsKept()
        {
            Put(Pristine, "ve-mep/SKILL.md", "# MEP v2\n");
            Put(Skills, "ve-mep/SKILL.md", "# MEP v1\nquy định riêng của công ty\n");
            var r = SampleSkills.Refresh(Pristine, Skills, LegacyOf("ve-mep/SKILL.md", "# MEP v1\n"));
            Assert.Empty(r.Updated);
            Assert.Equal(new[] { "ve-mep/SKILL.md" }, r.Kept);
            Assert.Equal("# MEP v1\nquy định riêng của công ty\n", Read("ve-mep/SKILL.md"));
            // an edited file is not recorded as a sample, so a later version does not replace it either
            Put(Pristine, "ve-mep/SKILL.md", "# MEP v3\n");
            r = SampleSkills.Refresh(Pristine, Skills, LegacyOf("ve-mep/SKILL.md", "# MEP v1\n"));
            Assert.Empty(r.Updated);
            Assert.Equal("# MEP v1\nquy định riêng của công ty\n", Read("ve-mep/SKILL.md"));
        }

        [Fact]
        public void RecordLetsTheNextVersionReplaceAnUntouchedSample()
        {
            var noLegacy = new string[0];
            Put(Pristine, "ve-mep/SKILL.md", "# MEP v2\n");
            Put(Skills, "ve-mep/SKILL.md", "# MEP v2\n");
            var r = SampleSkills.Refresh(Pristine, Skills, noLegacy);
            Assert.Empty(r.Updated);
            Assert.True(File.Exists(Path.Combine(Skills, SampleSkills.RecordName)));

            Put(Pristine, "ve-mep/SKILL.md", "# MEP v3\n"); // the next update
            r = SampleSkills.Refresh(Pristine, Skills, noLegacy);
            Assert.Equal(new[] { "ve-mep/SKILL.md" }, r.Updated);
            Assert.Equal("# MEP v3\n", Read("ve-mep/SKILL.md"));

            Put(Skills, "ve-mep/SKILL.md", "# MEP v3\nsửa tay\n"); // the user edits it afterwards
            Put(Pristine, "ve-mep/SKILL.md", "# MEP v4\n");
            r = SampleSkills.Refresh(Pristine, Skills, noLegacy);
            Assert.Empty(r.Updated);
            Assert.Equal("# MEP v3\nsửa tay\n", Read("ve-mep/SKILL.md"));
        }

        [Fact]
        public void UnknownOlderContentWithoutRecordIsKept()
        {
            Put(Pristine, "ve-mep/SKILL.md", "# MEP v2\n");
            Put(Skills, "ve-mep/SKILL.md", "# không rõ nguồn\n");
            var r = SampleSkills.Refresh(Pristine, Skills, new string[0]);
            Assert.Empty(r.Updated);
            Assert.Equal("# không rõ nguồn\n", Read("ve-mep/SKILL.md"));
        }

        [Fact]
        public void RemovedSampleAndUserSkillsAreLeftAlone()
        {
            Put(Pristine, "ve-mep/SKILL.md", "# MEP v2\n");
            Put(Skills, "cua-toi/SKILL.md", "# Skill của tôi\n");
            var r = SampleSkills.Refresh(Pristine, Skills, LegacyOf("ve-mep/SKILL.md", "# MEP v1\n"));
            Assert.Empty(r.Updated);
            Assert.Empty(r.Kept);
            Assert.False(File.Exists(Path.Combine(Skills, "ve-mep", "SKILL.md")));
            Assert.Equal("# Skill của tôi\n", Read("cua-toi/SKILL.md"));
        }

        [Fact]
        public void LegacyHashOfAnotherFileDoesNotCount()
        {
            Put(Pristine, "ve-mep/SKILL.md", "# MEP v2\n");
            Put(Skills, "ve-mep/SKILL.md", "# Hạ tầng v1\n");
            var r = SampleSkills.Refresh(Pristine, Skills, LegacyOf("ve-ha-tang/SKILL.md", "# Hạ tầng v1\n"));
            Assert.Empty(r.Updated);
            Assert.Equal("# Hạ tầng v1\n", Read("ve-mep/SKILL.md"));
        }

        [Fact]
        public void MissingFoldersDoNothing()
        {
            var r = SampleSkills.Refresh(Path.Combine(_root, "none"), Skills);
            Assert.Empty(r.Updated);
            r = SampleSkills.Refresh(Pristine, Path.Combine(_root, "none"));
            Assert.Empty(r.Updated);
            r = SampleSkills.Refresh(null, null);
            Assert.Empty(r.Updated);
        }

        [Fact]
        public void LegacyListIsWellFormedAndCoversTheThreeSamples()
        {
            foreach (string entry in SampleSkills.Legacy)
            {
                int bar = entry.LastIndexOf('|');
                Assert.True(bar > 0, entry);
                Assert.Matches("^[0-9a-f]{64}$", entry.Substring(bar + 1));
                Assert.DoesNotContain("\\", entry);
            }
            foreach (string name in new[] { "ve-kien-truc", "ve-mep", "ve-ha-tang" })
                Assert.Contains(SampleSkills.Legacy, e => e.StartsWith(name + "/SKILL.md|", StringComparison.Ordinal));
        }
    }
}
