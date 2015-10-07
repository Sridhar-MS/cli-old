using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.DotNet.ProjectModel.Impl
{
    internal static class FileSystemUtility
    {
        internal static async Task<FileStream> OpenFileStreamAsync(string filePath)
        {
            // Retry 3 times before re-throw the exception.
            // It mitigates the race condition when DTH read lock file while VS is restoring projects.

            int retry = 3;
            while (true)
            {
                try
                {
                    return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                catch (Exception)
                {
                    if (retry > 0)
                    {
                        retry--;
                        await Task.Delay(1000);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

        }

    }
}
