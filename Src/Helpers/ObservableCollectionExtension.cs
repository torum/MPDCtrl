using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MPDCtrl.Helpers;

// Too slow. Not Used anymore.
//Queue.Sort((a, b) => { return a.Index.CompareTo(b.Index); }); // TOO Slow.

// Sort extension method for ObservableCollection. This does not break binding or lose current selection of items because it just move items internaly.
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public static class ObservableCollectionExtension
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
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
    /*
    extension<T>(ObservableCollection<T> collection)
    {
        public void Sort(Comparison<T> comparison)
        {
            var sortableList = new List<T>(collection);
            sortableList.Sort(comparison);
            
            for (int i = 0; i < sortableList.Count; i++)
            {
                collection.Move(collection.IndexOf(sortableList[i]), i);
            }
        }
    }
    */
}
