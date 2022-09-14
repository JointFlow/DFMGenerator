using System;
using System.IO;
using NUnit.Framework;
using ReferenceResolver;
using Slb.Ocean.Petrel.Testing;

namespace DFNGenerator_Ocean
{
    [SetUpFixture]
    public class PluginTestSetup
    {
        private const string PetrelInstallationPath = @"C:\Program Files\Schlumberger\Petrel 2020";

        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            SafeRun(() =>
            {
                var resolver = new PetrelAssembliesResolver(PetrelInstallationPath);
                AppDomain.CurrentDomain.SetData("APPBASE", PetrelInstallationPath);
                Directory.SetCurrentDirectory(PetrelInstallationPath);

                InitializePetrel();
            });
        }

        private void SafeRun(Action code)
        {
            try
            {
                code();
            }
            catch (BadImageFormatException)
            {
                Assert.Fail("The current tests configuration does not match the selected Petrel. Please select default processor architecture properly and then run tests again.");
            }
        }

        private void InitializePetrel()
        {
            Assert.IsFalse(IsDebuggerAttached(),
                "Petrel is being initialized in a debug mode. Debug mode can only be used after the end of initialization.");
            PetrelEngine.Instance.Initialize("-licensePackage Package1");
        }

        [OneTimeTearDown]
        public void RunAfterAnyTests()
        {
            SafeRun(() =>
            {
                Assert.IsFalse(IsDebuggerAttached(),
                    "Petrel is being destroyed in a debug mode. Detach is recommended before the destroy method is called.");
                PetrelEngine.Instance.Teardown();
            });
        }

        public static bool IsDebuggerAttached()
        {
            return System.Diagnostics.Debugger.IsAttached;
        }
    }
}
