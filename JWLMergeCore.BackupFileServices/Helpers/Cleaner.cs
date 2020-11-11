namespace JWLMergeCore.BackupFileServices.Helpers
{
    using Microsoft.Extensions.Logging;
    using System.Collections.Generic;
    using System.Linq;
    using Models.DatabaseModels;

    /// <summary>
    /// Cleans jwlibrary files by removing redundant or anomalous database rows.
    /// </summary>
    internal class Cleaner
    {
        private readonly Database _database;
        private readonly ILogger? _logger;

        public Cleaner(Database database, ILogger? logger = null)
        {
            _logger = logger;
            _database = database;
        }

        /// <summary>
        /// Cleans the data, removing unused rows.
        /// </summary>
        /// <returns>Number of rows removed.</returns>
        public int Clean() => CleanBlockRanges() + CleanLocations();

        private HashSet<int> GetUserMarkIdsInUse()
        {
            var result = new HashSet<int>();
            
            foreach (var userMark in _database.UserMarks)
            {
                result.Add(userMark.UserMarkId);
            }

            return result;
        }

        private HashSet<int> GetLocationIdsInUse()
        {
            var result = new HashSet<int>();

            foreach (var bookmark in _database.Bookmarks)
            {
                result.Add(bookmark.LocationId);
                result.Add(bookmark.PublicationLocationId);
            }
            
            foreach (var note in _database.Notes.Where(note => note.LocationId != null))
            {
                result.Add(note.LocationId!.Value);
            }

            foreach (var userMark in _database.UserMarks)
            {
                result.Add(userMark.LocationId);
            }

            foreach (var tagMap in _database.TagMaps.Where(tagMap => tagMap.LocationId != null))
            {
                result.Add(tagMap.LocationId!.Value);
            }

            _logger.LogDebug($"Found {result.Count} location Ids in use");
            
            return result;
        }

        /// <summary>
        /// Cleans the locations.
        /// </summary>
        /// <returns>Number of location rows removed.</returns>
        private int CleanLocations()
        {
            var removed = 0;
            
            var locations = _database.Locations;
            if (!locations.Any()) return removed;

            var locationIds = GetLocationIdsInUse();

            foreach (var location in Enumerable.Reverse(locations))
            {
                if (!locationIds.Contains(location.LocationId))
                {
                    _logger?.LogDebug($"Removing redundant location id: {location.LocationId}");
                    locations.Remove(location);
                    ++removed;
                }
            }

            return removed;
        }

        /// <summary>
        /// Cleans the block ranges.
        /// </summary>
        /// <returns>Number of ranges removed.</returns>
        private int CleanBlockRanges()
        {
            var removed = 0;

            var userMarkIdsFound = new HashSet<int>();
            
            var ranges = _database.BlockRanges;
            if (!ranges.Any()) return removed;

            var userMarkIds = GetUserMarkIdsInUse();
                
            foreach (var range in Enumerable.Reverse(ranges))
            {
                if (!userMarkIds.Contains(range.UserMarkId))
                {
                    _logger?.LogDebug($"Removing redundant range: {range.BlockRangeId}");
                    ranges.Remove(range);
                    ++removed;
                }
                else
                {
                    if (userMarkIdsFound.Contains(range.UserMarkId))
                    {
                        // don't know how to handle this situation - we are expecting 
                        // a unique constraint on the UserMarkId column but have found 
                        // occasional duplication!
                        _logger?.LogDebug($"Removing redundant range (duplicate UserMarkId): {range.BlockRangeId}");
                        ranges.Remove(range);
                        ++removed;
                    }
                    else
                    {
                        userMarkIdsFound.Add(range.UserMarkId);
                    }
                }
            }

            return removed;
        }
    }
}