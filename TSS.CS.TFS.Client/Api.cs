using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.TestManagement.Client.Internal;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Services.Security;

namespace TSS.CS.TFS.Client
{
    public class Api
    {
        private const string FILE_NAME = @"GetScripts {0} - {1}.sql";

        //private const string SERVER_URL = @"http://noc-teamfs01:8080/tfs/profit";
        //private const string SERVER_URL = @"http://tssultratugtf:8080/tfs/CDU";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="versionNumber"></param>
        /// <param name="url"></param>
        /// <param name="path"></param>
        /// <param name="changeSetFrom"></param>
        /// <param name="changeSetTo"></param>
        public string GetScripts(string versionNumber, string url, string path, string changeSetFrom, string changeSetTo)
        {
            try
            {
                var fileString = string.Empty;
                var server = ConnectToTfsServer(url);
                var vcs = GetVersionControlServer(server);
                var changes = GetQueryHistory(vcs, path, changeSetFrom, changeSetTo).OrderBy(c => c.ChangesetId).ToList();

                //var sortedChanges = changes.SelectMany(changeSet => changeSet.Changes).OrderBy(ch => GetOrder(ch.Item.ServerItem)).ToList();

                foreach (var w in changes.SelectMany(changeSet => changeSet.Changes))
                //foreach (var w in sortedChanges)
                {
                    fileString = string.Concat(fileString, GetSummaryString(changes.First(c => c.ChangesetId.Equals(w.Item.ChangesetId))));
                    fileString = string.Concat(fileString, GetItemFileContentAsString(w.Item));

                    Console.WriteLine("Server item: " + w.Item.ServerItem);
                    Console.WriteLine(fileString);
                }

                if (fileString != string.Empty)
                    fileString = CreateFile(fileString, versionNumber);

                return fileString;
            }
            catch(Exception ex)
            {
                throw new Exception(string.Format("Error trying to access the changes history: {0}", ex.Message));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public TfsTeamProjectCollection ConnectToTfsServer(string url)
        {
            try
            {
                if (ValidateServerUrl(url))
                    return TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(url));
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error trying to connect to server: {0}", ex.Message));
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public VersionControlServer GetVersionControlServer(TfsTeamProjectCollection server)
        {
            try
            {
                if (server != null)
                    return server.GetService<VersionControlServer>();
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error trying to connect get version control information: {0}", ex.Message));
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vcs"></param>
        /// <param name="path"></param>
        /// <param name="changeSetFrom"></param>
        /// <param name="changeSetTo"></param>
        /// <returns></returns>
        public IEnumerable<Changeset> GetQueryHistory(VersionControlServer vcs, string path, string changeSetFrom, string changeSetTo)
        {
            //"$/CDBS PROFIT/Main/Source/*.sql"
            //"C5700"
            //"C5720"
            try
            {
                return vcs.QueryHistory(
                    path,
                    VersionSpec.Latest,
                    0,
                    RecursionType.Full,
                    null,
                    VersionSpec.ParseSingleSpec(changeSetFrom, null),
                    VersionSpec.ParseSingleSpec(changeSetTo, null),
                    int.MaxValue,
                    true,
                    false) as IEnumerable<Changeset>;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error trying to get query history: {0}", ex.Message));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public string GetItemFileContentAsString(Item item)
        {
            try
            {
                using (var memoryStream = GetItemFileContentAsMemoryStream(item))
                {
                    using (var streamReader = new StreamReader(new MemoryStream(memoryStream.ToArray())))
                    {
                        var fileContent = streamReader.ReadToEnd();
                        return string.Concat(fileContent, Environment.NewLine);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error trying to get the file content as a string: {0}", ex.Message));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>/// <returns></returns>
        public MemoryStream GetItemFileContentAsMemoryStream(Item item)
        {
            try
            {
                using (var stream = item.DownloadFile())
                {
                    var memoryStream = new MemoryStream();
                    stream.CopyTo(memoryStream);
                    return memoryStream;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private bool ValidateServerUrl(string url)
        {
            Uri uriResult;
            return Uri.TryCreate(url, UriKind.Absolute, out uriResult) && uriResult.Scheme == Uri.UriSchemeHttp;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileString"></param>
        /// <param name="versionNumber"></param>
        private string CreateFile(string fileString, string versionNumber)
        {
            try
            {
                var fileName = string.Format(FILE_NAME, versionNumber, DateTime.Now.ToString("yyyyMMddhhmmssfff"));
                File.WriteAllText(fileName, fileString);

                return fileName;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error trying to create the file: {0}\n", ex.Message));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="changes"></param>
        /// <returns></returns>
        private string GetSummaryString(Changeset change)
        {
            return
                string.Format(
                    @"/*{4}Changeset: {0}{4}Comment:   {1}{4}Committer: {2}{4}Creation Date: {3}{4}*/{4}{4}",
                    change.ChangesetId,
                    change.Comment,
                    change.Committer,
                    change.CreationDate,
                    Environment.NewLine);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private int GetOrder(string path)
        {
            var sortingPaths = new List<Tuple<int, string>>();

            sortingPaths.Add(new Tuple<int, string>(1, "up"));
            sortingPaths.Add(new Tuple<int, string>(2, "functions"));
            sortingPaths.Add(new Tuple<int, string>(3, "views"));
            sortingPaths.Add(new Tuple<int, string>(4, "sprocs"));
            sortingPaths.Add(new Tuple<int, string>(5, "indexes"));
            sortingPaths.Add(new Tuple<int, string>(6, "runAfterOtherAnyTimeScripts"));

            var auxPath = path.Substring(0, path.LastIndexOf(@"/", StringComparison.Ordinal));
            var realPath = auxPath.Substring(auxPath.LastIndexOf(@"/", StringComparison.Ordinal) + 1);

            return sortingPaths.First(p => p.Item2.Equals(realPath)).Item1;
        }

        //var ws = vcs.GetWorkspace("RMATEO", "Rodrigo Matteo");
        //var version = new ChangesetVersionSpec(5704);
        //var version = new LabelVersionSpec("Label 8.1.9.1");
        //var result = ws.Get(version, GetOptions.None);

        //var projects = vcs.GetAllTeamProjects(false);

        //foreach (var teamProject in projects)
        //Console.WriteLine(teamProject.Name);

        // List all of the .xaml files.
        //var items = vcs.GetItems("$/*.sql", RecursionType.Full);
        //var items = vcs.GetItems("$/*.sql",versionspec);

        //foreach (var item in items.Items)
        //{
        //Console.Write(item.ItemType.ToString());
        //Console.Write(": ");
        //Console.WriteLine(item.ServerItem);
        //}

    }
}
