// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;

namespace Friflo.Json.Fliox.Hub.Utils
{
    internal interface IFileHandler {
        Task<byte[]>    ReadFile(string path);
        string[]        GetFiles(string folder);
    }
    
    internal class  FileHandler : IFileHandler {
        private readonly string         rootFolder;
        
        internal FileHandler (string rootFolder) {
            this.rootFolder = rootFolder;
        } 
        
        public async Task<byte[]> ReadFile(string path) {
            var filePath = rootFolder + path;
            using (var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: false)) {
                var memoryStream = new MemoryStream();
                byte[] buffer = new byte[0x1000];
                int numRead;
                while ((numRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) != 0) {
                    memoryStream.Write(buffer, 0, numRead);
                }
                return memoryStream.ToArray();
            }
        }
        
        public string[] GetFiles(string folder) {
            var path = rootFolder + folder;
            if (!Directory.Exists(path)) {
                return null;
            }
            string[] fileNames = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
            for (int n = 0; n < fileNames.Length; n++) {
                fileNames[n] = fileNames[n].Substring(rootFolder.Length).Replace('\\', '/');
            }
            return fileNames;
        }
    }
}