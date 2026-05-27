using System.Collections;
using System.Diagnostics;
using DependencyInjector.Core;
using DependencyInjector.Tests.BaseClasses;
using NUnit.Framework;
using UnityEngine.Profiling;
using UnityEngine.TestTools;

namespace DependencyInjector.PlayMode
{
    public class DependencyInjectorPerformanceTests
    {
        private const int Iterations = 1000;
        private const long MaxMilliseconds = 500;

        [UnityTest]
        public IEnumerator Performance_CoreInjectionPipeline_1000Iterations_CompletesWithinThreshold()
        {
            FieldsReflectionInjector reflectionInjector = new FieldsReflectionInjector();
            IReflectionInjector[] reflectionInjectors = { reflectionInjector };

            // Warm up JIT and prime the reflection cache
            RunInjectionCycle(reflectionInjectors);

            // Act
            Profiler.BeginSample("DI_CoreInjectionPipeline");
            Stopwatch stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
                RunInjectionCycle(reflectionInjectors);

            stopwatch.Stop();
            Profiler.EndSample();

            // Assert
            Assert.Less(
                stopwatch.ElapsedMilliseconds,
                MaxMilliseconds,
                $"Core injection pipeline took {stopwatch.ElapsedMilliseconds}ms over {Iterations} iterations (threshold: {MaxMilliseconds}ms)."
            );

            yield return null;
        }

        private void RunInjectionCycle(IReflectionInjector[] reflectionInjectors)
        {
            IInstaller[] installers = { new InjectThisInstaller(), new InjectionTestInstaller() };
            IDIContainer diContainer = new DIContainer();
            new Injector(installers, diContainer, reflectionInjectors, null).InjectAll();
        }

        private class InjectThisInstaller : IInstaller
        {
            public bool HasToForceUseGlobalInstaller => false;
            public bool HasToSkipInstallation() => false;
            public void Install(IDIContainer diContainer) => diContainer.RegisterAsSingle<IInjectThis>(new InjectThis());

#if UNITY_EDITOR
            private bool _isInstalled;
            public bool IsInstalled => _isInstalled;
            public void SetAsInstalled() => _isInstalled = true;
#endif
        }

        private class InjectionTestInstaller : IInstaller
        {
            [Inject] private IInjectThis _injectThis;

            public bool HasToForceUseGlobalInstaller => false;
            public bool HasToSkipInstallation() => false;
            public void Install(IDIContainer diContainer) => diContainer.RegisterAsSingle(new InjectionTest(_injectThis));

#if UNITY_EDITOR
            private bool _isInstalled;
            public bool IsInstalled => _isInstalled;
            public void SetAsInstalled() => _isInstalled = true;
#endif
        }
    }
}
