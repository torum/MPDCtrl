using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPDCtrl.Helpers;

// Sort extension method for ObservableCollection. This does not break binding or lose current selection of items because it just move items internaly.
public static class ObservableCollection
{
    public static void Sort<T>(this ObservableCollection<T> collection, Comparison<T> comparison)
    {
        var sortableList = new List<T>(collection);
        sortableList.Sort(comparison);

        for (int i = 0; i < sortableList.Count; i++)
        {
            collection.Move(collection.IndexOf(sortableList[i]), i);
        }
    }
}
