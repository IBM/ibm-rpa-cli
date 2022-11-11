using System.Reflection;
using System.Text.RegularExpressions;

namespace Joba.IBM.RPA
{
    internal interface ISnippetFactory
    {
        Task<ISnippet?> GetAsync(string snippetName, CancellationToken cancellation);
    }

    internal class CurrentAssemblySnippetFactory : ISnippetFactory
    {
        private readonly Assembly assembly;

        internal CurrentAssemblySnippetFactory()
            : this(Assembly.GetExecutingAssembly()) { }

        private CurrentAssemblySnippetFactory(Assembly assembly)
        {
            this.assembly = assembly;
        }

        async Task<ISnippet?> ISnippetFactory.GetAsync(string snippetName, CancellationToken cancellation)
        {
            var definitionsStream = assembly.GetManifestResourceStream(@$"Joba.IBM.RPA.Snippets.{snippetName}.definitions.snippet");
            var mainStream = assembly.GetManifestResourceStream(@$"Joba.IBM.RPA.Snippets.{snippetName}.main.snippet");
            var routineStream = assembly.GetManifestResourceStream(@$"Joba.IBM.RPA.Snippets.{snippetName}.routine.snippet");
            if (definitionsStream == null && mainStream == null && routineStream == null)
                return null;

            string? definitions = null, main = null, routine = null;
            if (definitionsStream != null)
            {
                using var reader = new StreamReader(definitionsStream);
                definitions = await reader.ReadToEndAsync(cancellation);
            }
            if (mainStream != null)
            {
                using var reader = new StreamReader(mainStream);
                main = await reader.ReadToEndAsync(cancellation);
            }
            if (routineStream != null)
            {
                using var reader = new StreamReader(routineStream);
                routine = await reader.ReadToEndAsync(cancellation);
            }

            return new Snippet(definitions, main, routine);
        }
    }

    internal interface ISnippet
    {
        void Configure(string key, string value);
        void Apply(WalFile wal);
    }

    internal class Snippet : ISnippet
    {
        private readonly IDictionary<string, string> replacements = new Dictionary<string, string>();
        private readonly SnippetSection? definitions, main, routine;

        internal Snippet(string? definitions, string? main, string? routine)
        {
            if (main != null)
                this.main = new SnippetSection(main);
            if (definitions != null)
                this.definitions = new SnippetSection(definitions);
            if (routine != null)
                this.routine = new SnippetSection(routine);
        }

        void ISnippet.Configure(string key, string value)
        {
            replacements.Add(key, value);
        }

        void ISnippet.Apply(WalFile wal)
        {
            var lines = new List<string>();
            var analyzer = new WalAnalyzer(wal);
            if (definitions != null)
                lines.AddRange(definitions.Build(replacements));

            int? lastDefinitionIndex = null;
            for (var index = 0; index < analyzer.Lines.Count; index++)
            {
                var line = analyzer.Lines[index];
                if (line is DefineVariableLine)
                    lastDefinitionIndex = index;

                var isAfterAllDefinitions = lastDefinitionIndex == index - 1;
                if (isAfterAllDefinitions && main != null)
                    lines.AddRange(main.Build(replacements));

                lines.Add(line.Content);
            }

            if (routine != null)
                lines.AddRange(routine.Build(replacements));

            wal.Overwrite(WalContent.Build(lines));
        }
    }

    internal class SnippetSection
    {
        private string contents;

        internal SnippetSection(string contents)
        {
            this.contents = contents;
        }

        internal IEnumerable<string> Build(IDictionary<string, string> replacements)
        {
            contents = Regex.Replace(contents, @"@{(\w+)}",
                match => replacements.TryGetValue(match.Groups[1].Value, out var replacement) ? replacement : match.Value);

            return Regex.Split(contents, "\r?\n");
        }

        public override string ToString() => contents;
    }
}