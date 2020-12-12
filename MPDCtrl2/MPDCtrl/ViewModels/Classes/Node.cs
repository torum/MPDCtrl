using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System;
using System.Collections.Generic;

namespace MPDCtrl.ViewModels.Classes
{
    /// <summary>
    /// Base class for Treeview Node and Listview Item.
    /// </summary>
    abstract public class Node : INotifyPropertyChanged
    {
        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (_name == value)
                    return;

                _name = value;

                NotifyPropertyChanged("Name");
            }
        }

        private string _pathData = "M20,18H4V8H20M20,6H12L10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6Z";
        public string PathIcon
        {
            get
            {
                return _pathData;
            }
            protected set
            {
                if (_pathData == value)
                    return;
                _pathData = value;

                NotifyPropertyChanged("PathIcon");
            }
        }

        protected Node(){}

        protected Node(string name)
        {
            Name = name;
        }

        #region == INotifyPropertyChanged ==

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        #endregion
    }

    /// <summary>
    /// Base class for Treeview Node.
    /// </summary>
    public class NodeTree : Node
    {
        public bool Selected { get; set; }

        private bool _expanded;
        public bool Expanded
        {
            get
            {
                return _expanded;
            }
            set
            {
                if (_expanded == value)
                    return;

                _expanded = value;

                NotifyPropertyChanged("Expanded");
            }
        }

        private NodeTree _parent;
        public NodeTree Parent
        {
            get
            {
                return this._parent;
            }

            set
            {
                if (_parent == value)
                    return;


                _parent = value;

                NotifyPropertyChanged("Parent");
            }
        }

        private ObservableCollection<NodeDirectory> _children = new ObservableCollection<NodeDirectory>();
        public ObservableCollection<NodeDirectory> Children
        {
            get
            {
                return _children;
            }
            set
            {
                _children = value;

                NotifyPropertyChanged("Children");
            }
        }

        protected NodeTree(){}

        protected NodeTree(string name): base(name)
        {
            BindingOperations.EnableCollectionSynchronization(_children, new object());
        }

    }

    /// <summary>
    /// Treeview Direcotry Node.
    /// </summary>
    public class NodeDirectory : NodeTree
    {
        public Uri DireUri { get; set; }

        public NodeDirectory(string name, Uri direUri) : base(name)
        {
            DireUri = direUri;
            PathIcon = "M20,18H4V8H20M20,6H12L10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6Z";
        }
    }

    /// <summary>
    /// Listview File Node.
    /// </summary>
    public class NodeEntry : Node
    {
        public Uri FileUri { get; set; }
        public string FilePath {
            get
            {
                if (FileUri != null)
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

        public NodeEntry(string name, Uri fileUri) : base(name)
        {
            FileUri = fileUri;
            PathIcon = "M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M13,13H11V18A2,2 0 0,1 9,20A2,2 0 0,1 7,18A2,2 0 0,1 9,16C9.4,16 9.7,16.1 10,16.3V11H13V13M13,9V3.5L18.5,9H13Z";
        }
    }

    /// <summary>
    /// Container class for Treeview.
    /// </summary>
    public class DirectoryTreeBuilder : NodeTree
    {
        public DirectoryTreeBuilder() { }

        public void Load(List<String> dirs)
        {
            Uri uri = new Uri(@"file:///./");
            NodeDirectory root = new NodeDirectory("/", uri);
            root.Selected = true;
            root.Expanded = true;

            root.Parent = null;
            this.Children.Add(root);

            foreach (var pathDir in dirs)
            {
                try
                {
                    string[] ValuePair = pathDir.Split('/');
                    if (ValuePair.Length > 1)
                    {
                        // set parent node
                        NodeDirectory parent = root;

                        foreach (var asdf in ValuePair)
                        {
                            if (String.IsNullOrEmpty(asdf)) continue;

                            // check if already exists.
                            bool found = false;
                            foreach (var child in parent.Children)
                            {
                                if (child.Name.ToLower() == asdf.ToLower())
                                {
                                    // set parent node
                                    parent = child;
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                            {
                                NodeDirectory hoge = new NodeDirectory(asdf.Trim(), new Uri(@"file:///" + pathDir.Trim()));
                                hoge.Selected = false;
                                hoge.Expanded = true;

                                hoge.Parent = parent;
                                parent.Children.Add(hoge);

                                // set parent node
                                parent = hoge;
                            }
                        }
                    }
                    else if (ValuePair.Length == 1)
                    {
                        NodeDirectory hoge = new NodeDirectory(ValuePair[0].Trim(), new Uri(@"file:///" + pathDir.Trim()));
                        hoge.Selected = false;
                        hoge.Expanded = true;

                        hoge.Parent = root;
                        root.Children.Add(hoge);
                    }
                }
                catch 
                { 
                    // TODO:

                }
            }

        }


    }


}
