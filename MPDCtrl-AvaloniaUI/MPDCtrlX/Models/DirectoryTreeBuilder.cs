using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace MPDCtrlX.Models;

public class NodeDirectory : NodeTree
{
    public Uri DireUri { get; set; }

    public NodeDirectory(string name, Uri direUri) : base(name)
    {
        DireUri = direUri;
        PathIcon = "M20,18H4V8H20M20,6H12L10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6Z";
    }
}

public class NodeFile : Node
{
    public Uri FileUri { get; set; }

    public string OriginalFileUri { get; set; }

    public string FilePath
    {
        get
        {
            if (FileUri is not null)
            {
                string path = FileUri.LocalPath;
                string filename = System.IO.Path.GetFileName(path);//System.IO.Path.GetFileName(uri.LocalPath);
                path = path.Replace(filename, "");

                return path;
            }
            else
            {
                return "";
            }
        }
    }

    public NodeFile(string name, Uri fileUri, string originalFileUri) : base(name)
    {
        FileUri = fileUri;
        OriginalFileUri = originalFileUri;
        PathIcon = "M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M13,13H11V18A2,2 0 0,1 9,20A2,2 0 0,1 7,18A2,2 0 0,1 9,16C9.4,16 9.7,16.1 10,16.3V11H13V13M13,9V3.5L18.5,9H13Z";
    }
}

public class DirectoryTreeBuilder : NodeTree
{
    public DirectoryTreeBuilder(string name) : base(name) { }

    public bool IsCanceled { get; set; }

    public async void Load(ObservableCollection<string> dirs)
    {
        if (dirs is null)
            return;

        IsCanceled = false;

        Uri uri = new(@"file:///./");
        NodeDirectory root = new("/", uri)
        {
            Selected = true,
            Expanded = true,
            Parent = null
        };

        Children.Add(root);
        
        foreach (var pathDir in dirs)
        {
            // for responsivenesss.
            await Task.Delay(1); //<- not good practice, but Avalonia's TreeView does not support virtualization..

            //await Task.Yield();

            // changed profile etc.
            if (IsCanceled)
                break;

            try
            {
                string[] ValuePair = pathDir.Split('/');
                if (ValuePair.Length > 1)
                {
                    // set parent node
                    NodeDirectory? parent = root;

                    foreach (var asdf in ValuePair)
                    {
                        if (string.IsNullOrEmpty(asdf)) continue;

                        // LINQ may be slower in this case.
                        /*
                        var fuga = parent.Children.FirstOrDefault(i => i.Name == asdf);
                        if (fuga is not null)
                        {
                            // set parent node
                            parent = fuga as NodeDirectory;
                            //break;
                        }
                        else
                        {
                            NodeDirectory hoge = new NodeDirectory(asdf.Trim(), new Uri(@"file:///" + pathDir.Trim()));
                            hoge.Selected = false;
                            hoge.Expanded = true;

                            hoge.Parent = parent;
                            parent.Children.Add(hoge);

                            // set parent node
                            parent = hoge;
                        }
                        */

                        if (parent is null)
                            continue;

                        // check if already exists.
                        bool found = false;
                        foreach (var child in parent.Children)
                        {
                            if (child.Name.ToLower() == asdf.ToLower())
                            {
                                // set parent node
                                parent = child as NodeDirectory;
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            NodeDirectory hoge = new(asdf.Trim(), new Uri(@"file:///" + pathDir.Trim()))
                            {
                                Selected = false,
                                Expanded = true,
                                Parent = parent
                            };
                            //parent.Children.Add(hoge);
                            //Application.Current.Dispatcher.Invoke(() =>
                            /*
                            Dispatcher.UIThread.Post(() =>
                            {
                                parent?.Children.Add(hoge);
                            });
                            */
                            parent?.Children.Add(hoge);
                            // set parent node
                            parent = hoge;
                        }
                        
                    }
                }
                else if (ValuePair.Length == 1)
                {
                    NodeDirectory hoge = new(ValuePair[0].Trim(), new Uri(@"file:///" + pathDir.Trim()))
                    {
                        Selected = false,
                        Expanded = true,
                        Parent = root
                    };
                    //root.Children.Add(hoge);
                    //Application.Current.Dispatcher.Invoke(() =>
                    /*
                    Dispatcher.UIThread.Post(() =>
                    {
                        root.Children.Add(hoge);
                    });
                    */
                    root.Children.Add(hoge);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error@DirectoryTreeBuilder: " + ex.Message);
            }
        }

    }

}
