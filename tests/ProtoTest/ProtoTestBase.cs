
using Dynamo.Utilities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ProtoTest
{
    public abstract class ProtoTestBase
    {
        private AssemblyHelper assemblyHelper;
        protected ProtoCore.Core core;
        protected ProtoCore.RuntimeCore runtimeCore;
        protected TestFrameWork thisTest = new TestFrameWork();

        [SetUp]
        public virtual void Setup()
        {
            core = new ProtoCore.Core(new ProtoCore.Options());
            core.Compilers.Add(ProtoCore.Language.Associative, new ProtoAssociative.Compiler(core));
            core.Compilers.Add(ProtoCore.Language.Imperative, new ProtoImperative.Compiler(core));

            // This is set when a test is executed 
            runtimeCore = null;

            if (assemblyHelper == null)
            {
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var moduleRootFolder = Path.GetDirectoryName(assemblyPath);
                var resolutionPaths = new[]
                {
                    // These tests need "CoreNodeModels.dll" under "nodes" folder.
                    Path.Combine(moduleRootFolder, "nodes")
                };
                assemblyHelper = new AssemblyHelper(moduleRootFolder, resolutionPaths);
                AppDomain.CurrentDomain.AssemblyResolve += assemblyHelper.ResolveAssembly;
            }
        }

        private void CleanupRuntimeCore()
        {
            // If a runtimeCore was used for the test, call its Cleanup
            if (runtimeCore != null)
            {
                runtimeCore.Cleanup();
            }
        }

        [TearDown]
        public virtual void TearDown()
        {
            CleanupRuntimeCore();
            if (assemblyHelper != null)
            {
                AppDomain.CurrentDomain.AssemblyResolve -= assemblyHelper.ResolveAssembly;
            }
            thisTest.CleanUp();
        }
    }
}
