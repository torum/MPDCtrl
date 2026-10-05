using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MPDCtrl.Models;

public sealed partial class DirectoryTreeBuilder(string name) : NodeTree(name)
{
    public bool IsCanceled { get; set; }

    public async Task Load(ObservableCollection<string> dirs)
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
            //await Task.Delay(1); //<- not good practice, but Avalonia's TreeView does not support virtualization..

            await Task.Yield();

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
                            //if (child.Name.ToLower() == asdf.ToLower())
                            if (string.Equals(child.Name, asdf, StringComparison.OrdinalIgnoreCase))
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
                                Expanded = false,
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
                        Expanded = false,
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
