using MPDCtrl.ViewModels;
using System;

namespace MPDCtrl.Models;

public sealed partial class NodeFile : Node
{
    public NodeFile(string name, Uri fileUri, string originalFileUri) : base(name)
    {
        FileUri = fileUri;
        OriginalFileUri = originalFileUri;
        PathIcon = "M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M13,13H11V18A2,2 0 0,1 9,20A2,2 0 0,1 7,18A2,2 0 0,1 9,16C9.4,16 9.7,16.1 10,16.3V11H13V13M13,9V3.5L18.5,9H13Z";
    }

    public Uri FileUri { get; set; }

    public string OriginalFileUri { get; set; }

    public string FilePath
    {
        get
        {
            if (FileUri is not null)
            {
                string path = FileUri.LocalPath;
                return System.IO.Path.GetDirectoryName(path) ?? string.Empty;
            }
            else
            {
                return string.Empty;
            }
        }
    }

    // Workaround for WinUI3's limitation or lack of features. 
    public MainViewModel? ParentViewModel { get; set; }

}
