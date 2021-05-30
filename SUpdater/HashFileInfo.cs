using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SUpdater
{
    class HashFileInfo
    {
        public string FullName { get => _fileInfo.FullName; }
        public string Name { get => _fileInfo.Name; }
        public string Extention { get => _fileInfo.Extension; }
        public DirectoryInfo Directory { get => _fileInfo.Directory; }
        public DateTime CreationTime { get => _fileInfo.CreationTime; }
        public DateTime LastWriteTime { get => _fileInfo.LastWriteTime; }
        public string Hash { get => _fileInfo.Exists ? _hash : ""; }

        public bool Exists { get => _fileInfo.Exists; }

        public bool IsLocal = false;

        FileInfo _fileInfo;
        string _hash;

        public HashFileInfo(string filePath)
        {
            _fileInfo = new FileInfo(filePath);

            if (_fileInfo.Exists)
            {
                _hash = SHA256CheckSum(filePath);
            }
        }
        static string SHA256CheckSum(string filePath)
        {
            using (SHA256 SHA256 = SHA256.Create())
            {
                using (FileStream fileStream = File.OpenRead(filePath))
                    return Convert.ToBase64String(SHA256.ComputeHash(fileStream));
            }
        }
    }
}
