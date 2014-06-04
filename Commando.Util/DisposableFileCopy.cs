using System;
using System.IO;

namespace twomindseye.Commando.Util
{
    public sealed class DisposableFileCopy : IDisposable
    {
        bool _disposed;

        public DisposableFileCopy(string sourcePath)
        {
            SourcePath = sourcePath;
            TempCopyPath = Path.GetTempFileName();
            File.Copy(sourcePath, TempCopyPath, true);
        }

        public string SourcePath { get; private set; }

        public string TempCopyPath { get; private set; }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            File.Delete(TempCopyPath);
            _disposed = true;
        }
    }
}
