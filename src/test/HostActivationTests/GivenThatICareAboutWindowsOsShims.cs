using System;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.WindowsOsShims
{
    public class GivenThatICareAboutWindowsOsShims : IClassFixture<GivenThatICareAboutWindowsOsShims.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public GivenThatICareAboutWindowsOsShims(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void MuxerRunsPortableAppWithoutWindowsOsShims()
        {
            TestProjectFixture portableAppFixture = sharedTestState.PortableTestWindowsOsShimsAppFixture.Copy();

            portableAppFixture.BuiltDotnet.Exec(portableAppFixture.TestProject.AppDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Reported OS version is newer or equal to the true OS version - no shims.");
        }

        public class SharedTestState : IDisposable
        {
            private static RepoDirectoriesProvider RepoDirectories { get; set; }

            public TestProjectFixture PortableTestWindowsOsShimsAppFixture { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                PortableTestWindowsOsShimsAppFixture = new TestProjectFixture("TestWindowsOsShimsApp", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();
            }

            public void Dispose()
            {
                //PortableTestWindowsOsShimsAppFixture.Dispose();
            }
        }
    }
}
