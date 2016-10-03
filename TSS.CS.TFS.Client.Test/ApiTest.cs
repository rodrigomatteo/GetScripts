using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TSS.CS.TFS.Client.Test
{
    [TestClass]
    public class ApiTest
    {
        private Api clientApi = null;
        const string url = @"http://noc-teamfs01:8080/tfs/profit";

        [TestInitialize]
        public void TestStartUp()
        {
            clientApi = new Api();
        }

        [TestMethod]
        public void VerifyApiCanConnectUrl()
        {
            var server = clientApi.ConnectToTfsServer(url);
            Assert.IsNotNull(server);
        }

        [TestMethod]
        public void VerifyApiCanGetService()
        {
            var server = clientApi.ConnectToTfsServer(url);
            var service = clientApi.GetVersionControlServer(server);
            Assert.IsNotNull(service);
        }

        [TestMethod]
        public void VerifyApiCanQueryHistory()
        {
            var server = clientApi.ConnectToTfsServer(url);
            var service = clientApi.GetVersionControlServer(server);
            var changeSet = clientApi.GetQueryHistory(service, "$/CDBS PROFIT/Main/Source/*.sql", "5730", "5750");
            
            Assert.IsNotNull(changeSet);
        }
    }
}
