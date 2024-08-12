﻿using Amber.IO.Common.FileSystem;

namespace Amber.IO.FileSystem.OperatingSystem;

internal class File : IFile
{
    readonly FileInfo file;
    IFileStream? stream;

    public File(FileInfo fileInfo, IFolder parent)
    {
        file = fileInfo;
        Parent = parent;
    }

    public IFileStream Stream
    {
        get
        {
            if (stream != null)
                return stream;

            return stream = new FileStream(file);
        }
    }

    public string Name => file.Name;

    public string Path => file.FullName;

    public IFolder Parent { get; }
}
