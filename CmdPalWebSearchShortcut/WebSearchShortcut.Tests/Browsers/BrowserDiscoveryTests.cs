using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebSearchShortcut.Browser;

namespace WebSearchShortcut.Tests.Helpers
{
    [TestClass]
    public class BrowserProgIdFinderTests
    {
        [TestMethod]
        public void FindUniqueHttpUrlAssociationProgIdsShouldPrintResults()
        {
            var browserInfos = BrowsersDiscovery.GetAllInstalledBrowsers();

            foreach (var browserInfo in browserInfos)
            {
                Console.WriteLine($"Found browser: {browserInfo.Name}({browserInfo.Id}) - {browserInfo.Path} {browserInfo.ArgumentsPattern}");
            }

            Console.WriteLine($"Total browsers: {browserInfos.Count}");
        }
    }
}
