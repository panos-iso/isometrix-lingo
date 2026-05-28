using System;
using System.Collections.Generic;
using System.Linq;

namespace IsometrixLingo.Services;

/// <summary>
/// Service for calculating minimal differentiating directory paths for display purposes
/// </summary>
public class PathDisplayService
{
    /// <summary>
    /// Calculates minimal unique directory paths for a collection of items
    /// </summary>
    /// <typeparam name="T">Type of items</typeparam>
    /// <param name="items">Items with Name and DirectoryPath properties</param>
    /// <param name="nameSelector">Function to extract the name from an item</param>
    /// <param name="pathSelector">Function to extract the directory path from an item</param>
    /// <returns>Dictionary mapping each item to its minimal display path (null/empty for unique names)</returns>
    public Dictionary<T, string?> CalculateMinimalPaths<T>(
        IEnumerable<T> items,
        Func<T, string> nameSelector,
        Func<T, string?> pathSelector) where T : notnull
    {
        var result = new Dictionary<T, string?>();
        var itemList = items.ToList();
        
        // Group items by name to find duplicates
        var itemsByName = itemList.GroupBy(nameSelector).ToList();

        foreach (var group in itemsByName)
        {
            var itemsWithPaths = group.Where(item => !string.IsNullOrEmpty(pathSelector(item))).ToList();
            var itemsWithoutPaths = group.Where(item => string.IsNullOrEmpty(pathSelector(item))).ToList();

            // If only one item with this name, no need to show directory path
            if (group.Count() == 1)
            {
                foreach (var item in group)
                {
                    result[item] = null;
                }
                continue;
            }

            // Multiple items with the same name - calculate minimal differentiating paths
            foreach (var item in itemsWithPaths)
            {
                var path = pathSelector(item);
                if (string.IsNullOrEmpty(path))
                {
                    result[item] = null;
                    continue;
                }

                // Split the directory path into segments
                var segments = path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                
                // Find the minimal unique suffix by comparing with other items with the same name
                var otherItems = itemsWithPaths.Where(i => !i.Equals(item)).ToList();
                var minimalPath = path;

                // Try increasingly shorter paths (from end) until we find one that's unique
                for (int depth = 1; depth <= segments.Length; depth++)
                {
                    var candidatePath = string.Join("/", segments.TakeLast(depth));
                    
                    // Check if this path is unique among items with the same name
                    var isUnique = !otherItems.Any(other =>
                    {
                        var otherPath = pathSelector(other);
                        if (string.IsNullOrEmpty(otherPath)) return false;
                        
                        var otherSegments = otherPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                        var otherCandidate = string.Join("/", otherSegments.TakeLast(depth));
                        return string.Equals(candidatePath, otherCandidate, StringComparison.OrdinalIgnoreCase);
                    });

                    if (isUnique)
                    {
                        minimalPath = candidatePath;
                        break;
                    }
                }

                result[item] = minimalPath;
            }

            // Items without directory paths in a group with duplicates
            foreach (var item in itemsWithoutPaths)
            {
                result[item] = null;
            }
        }

        return result;
    }
}
