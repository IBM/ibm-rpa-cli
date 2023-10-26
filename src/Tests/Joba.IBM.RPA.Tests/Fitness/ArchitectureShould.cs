using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Joba.IBM.RPA.Tests
{
    public class ArchitectureShould
    {
        private static readonly System.Reflection.Assembly[] assemblies = new System.Reflection.Assembly[] { typeof(IProject).Assembly };
        private static readonly Architecture architecture = new ArchLoader().LoadAssemblies(assemblies).Build();

        [Fact]
        public void DisallowUsingHttpClient()
        {
            var rule = Types().Should().NotDependOnAny(typeof(HttpClient))
                .Because($"'{string.Join(", ", assemblies.Select(a => a.GetName().Name))}' require(s) using abstractions '{nameof(IRpaClient)} or {nameof(IRpaClientFactory)}'");
            rule.Check(architecture);
        }

        [Fact]
        public void EnsureSameNamespace()
        {
            var rule = Types().That().ArePublic().Should().ResideInNamespace(@"Joba.IBM.RPA(\.Server)?", useRegularExpressions: true)
                .Because("exposing many namespaces is a bad practice.");
            rule.Check(architecture);
        }

        //[Fact]
        //public void DisallowReferencingSystemCommandLinePackage()
        //{
        //    //ArchUnitTEST does not support
        //    //See: https://github.com/TNG/ArchUnitNET/issues/130
        //    //See: https://gaevoy.com/2022/05/19/review-dependencies-on-every-commit.html
        //}
    }
}
