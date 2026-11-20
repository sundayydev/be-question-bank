namespace BeQuestionBank.Test.Architecture;

using System.Linq;
using System.Runtime.Versioning;
using Xunit;

public class ArchitectureBootstrapTests
{
    [Fact]
    public void Solution_Assemblies_ShouldTargetNet10()
    {
        var domainAssembly = typeof(BeQuestionBank.Domain.Common.ModelBase).Assembly;
        var targetFramework = domainAssembly.GetCustomAttributes(typeof(TargetFrameworkAttribute), false)
                                            .OfType<TargetFrameworkAttribute>()
                                            .FirstOrDefault()?.FrameworkName;
        Assert.NotNull(targetFramework);
        Assert.Contains(".NETCoreApp,Version=v10.0", targetFramework);
    }
}
