using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Joba.IBM.RPA
{
    internal class WalBuilder
    {
        private readonly ILogger logger;
        private readonly Project project;

        internal WalBuilder(ILogger logger, Project project)
        {
            this.logger = logger;
            this.project = project;
        }

        internal async Task<BuildResult> BuildAsync(WalFile wal, DirectoryInfo outputDirectory, CancellationToken cancellation)
        {
            //TODO: get installed packages
            //TODO: get all the files except 'this one' - the one we're building
            //TODO: find all package and files' references within the 'one' we're building and embed them within 1 file
            throw new NotImplementedException();
        }
    }

    public record struct BuildResult(WalFile[] Wals, TimeSpan Time);

    internal class WalAnalyzer
    {
        private readonly WalLines lines;

        internal WalAnalyzer(WalLines lines) => this.lines = lines;

        public References FindPackages(string packageName) => new(FindPackages(lines, packageName).ToList());

        private static IEnumerable<Reference> FindPackages(WalLines lines, string packageName)
        {
            var regex = PackageReferenceRegexFactory.GetOrCreate(packageName);

            foreach (var line in lines)
            {
                var match = regex.Match(line.ToString());
                if (match.Success)
                    yield return new Reference(line, regex, match);
            }
        }

        static class PackageReferenceRegexFactory
        {
            private static readonly IDictionary<string, Regex> regexes = new Dictionary<string, Regex>();

            // ^\s*executeScript+.+?(?:--name)\s+(?<name>.+?(?=\s))
            //for sub: ^(?<command>\s*executeScript+.+?(?:--name)\s+)(?<name>.+?(?=\s))
            //for sub: ^(?<command>\s*executeScript.+?(?:--name)\s+Joba_AccuWeather.+?(?:--version)\s+)(?<version>\d+)

            public static Regex GetOrCreate(string packageName)
            {
                if (!regexes.ContainsKey(packageName))
                    regexes.Add(packageName, new Regex(@$"^(?<line>\s*executeScript.+?(?:--name)\s+(?<scriptName>{packageName})(.+?(?<version>--version)\s+)?)(?<versionNumber>\d+)?", RegexOptions.IgnoreCase));

                return regexes[packageName];
            }
        }
    }

    internal class WalParser
    {
        private readonly WalContent wal;

        public WalParser(WalContent wal)
        {
            this.wal = wal;
        }

        public WalLines Parse() => new(Parse(wal).ToList());

        private static IEnumerable<WalLine> Parse(WalContent wal)
        {
            var lines = Regex.Split(wal.ToString(), "\r?\n");
            for (var index = 0; index < lines.Length; index++)
            {
                var lineNumber = index + 1;
                yield return new WalLine(lineNumber, lines[index]);
            }
        }
    }

    public class WalLines : IEnumerable<WalLine>
    {
        private readonly IEnumerable<WalLine> lines;

        internal WalLines(IEnumerable<WalLine> lines)
        {
            this.lines = lines;
        }

        public WalContent Build() =>
            new(string.Join(System.Environment.NewLine, lines.OrderBy(l => l.Number).Select(l => l.Content)));

        public IEnumerator<WalLine> GetEnumerator() => lines.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)lines).GetEnumerator();
    }

    public class WalLine
    {
        internal WalLine(int number, string content)
        {
            Number = number;
            Content = content;
        }

        public int Number { get; }
        public string Content { get; private set; }

        internal void Update(string content) => Content = content;
        public override string ToString() => Content;
    }

    public class References : IEnumerable<Reference>
    {
        private readonly IEnumerable<Reference> references;

        internal References(IEnumerable<Reference> references)
        {
            this.references = references;
        }

        public void Replace(WalVersion version)
        {
            foreach (var reference in references)
                reference.Replace(version);
        }

        public IEnumerator<Reference> GetEnumerator() => references.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)references).GetEnumerator();
    }

    public class Reference
    {
        private readonly WalLine line;
        private readonly Regex regex;

        internal Reference(WalLine line, Regex regex, Match match)
        {
            this.line = line;
            this.regex = regex;
            Version = match.Groups["versionNumber"].Success ? new Version(int.Parse(match.Groups["versionNumber"].Value), 0) : null;
            Name = match.Groups["scriptName"].Value;
        }

        internal int LineNumber => line.Number;
        internal string Name { get; }
        internal Version? Version { get; }

        internal void Replace(WalVersion version)
        {
            var replacement = Version != null ? "${line}" + version : "${line}" + $" --version {version}";
            var updatedLine = regex.Replace(line.Content, replacement);
            line.Update(updatedLine);
        }

        public override string ToString() => $"[{LineNumber}] {line}";
    }
}
