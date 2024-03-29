using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Octopus.RoslynAnalyzers
{
    public static class Descriptors
    {
        static DiagnosticDescriptor GetTestAnalyzerDescriptor(string id, string title, string? description = null)
        {
            return new DiagnosticDescriptor(id,
                title,
                title,
                "Octopus.Testing",
                DiagnosticSeverity.Warning,
                false, // Testing analysers are disabled by default, these will only be enabled for test projects
                description);
        }

        static readonly string noBaseClassesDescription = "Creating base classes for tests is a really good way to make tests neat, and 1 or 2 levels " +
            "of inheritance is generally pretty easy to reason with. From past experience, these hierarchies quickly " +
            "get out of control and it gets really hard to find where different things set up in the chain." +
            Environment.NewLine +
            "If you need to share logic and resources (that are expensive to set up) between multiple tests, xUnits " +
            "IClassFixture and IAssemblyFixture might be your answer: https://xunit.net/docs/shared-context" +
            Environment.NewLine +
            "If you just need to share some logic, setup, or helpers between these tests please make use of builders " +
            "or feel free to write a custom helper or context class." +
            Environment.NewLine +
            "And finally, if you want to run a bunch of the same tests with slightly different configuration, make use of " +
            "xUnit's Theories with InlineData/ClassData/MemberData. These won't run in parallel though, so please use sparingly " +
            "in integration test land." +
            Environment.NewLine +
            "Reach out to @team-core-blue if you need any help with strategies to test efficiently without base classes.";

        public static DiagnosticDescriptor Oct2001NoIntegrationTestBaseClasses => GetTestAnalyzerDescriptor(
            "OCT2001",
            "Integration test classes should inherit directly from IntegrationTest",
            noBaseClassesDescription
        );

        public static DiagnosticDescriptor Oct2002NoUnitTestBaseClasses => GetTestAnalyzerDescriptor(
            "OCT2002",
            "Unit test classes should inherit directly from UnitTest",
            noBaseClassesDescription
        );

        public static DiagnosticDescriptor Oct2004IntegrationTestContainerClassMustBeStatic => GetTestAnalyzerDescriptor(
            "OCT2004",
            "Integration test container class should be static",
            "Integration test container classes should be static. They're containers for organisation only " +
            "and should never be instantiated (also Rider/ReSharper gets upset if they're not static)."
        );

        public static DiagnosticDescriptor Oct2005DoNotNestIntegrationTestContainerClasses => GetTestAnalyzerDescriptor(
            "OCT2005",
            "Do not nest integration test container classes",
            "A single level of nesting should be enough for organising tests in a file. Creating separate files is a" +
            "good way to break things up. This convention is in place to stop files getting to big, and to stop tests being " +
            "added at multiple levels of the class hierarchy in a given file."
        );

        public static DiagnosticDescriptor Oct2006IntegrationTestContainersMustOnlyContainTypesAndMethods => GetTestAnalyzerDescriptor(
            "OCT2006",
            "Integration test container classes should only contain nested types and methods",
            "Integration test container classes should only contain integration test classes and methods, any other complex logic or state " +
            "should be in builders, class/assembly fixtures, or some other generic helper."
        );
        
        public static DiagnosticDescriptor Oct2007IntegrationTestContainersMethodsMustBePrivate => GetTestAnalyzerDescriptor(
            "OCT2007",
            "Methods in integration test container classes should be private",
            "Integration test container classes should only be used to organise tests (because we must have 1 test per class for maximum parallel goodness,"
            + " any logic that you want to share across multiple tests should be in builders, class/assembly fixtures, or some other generic helper."
        );
    }
}