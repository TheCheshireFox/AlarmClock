using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DynamicData;

namespace AlarmClock.Extensions;

public static class ObservableCollectionExtension
{
    public static void Replace<T>(this ObservableCollection<T> collection, IEnumerable<T> newItems, IEqualityComparer<T>? comparer = null)
    {
        var newItemsList = newItems.ToList();
        
        if (collection.SequenceEqual(newItemsList, comparer))
            return;
        
        collection.Clear();
        collection.AddRange(newItemsList);
    }
}