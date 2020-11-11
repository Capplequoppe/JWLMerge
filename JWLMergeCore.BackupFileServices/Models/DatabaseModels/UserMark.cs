﻿namespace JWLMergeCore.BackupFileServices.Models.DatabaseModels
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("UserMark")]
    public class UserMark
    {
        public UserMark()
        {
            UserMarkGuid = Guid.NewGuid().ToString();
        }
        /// <summary>
        /// The user mark identifier.
        /// </summary>
        public int UserMarkId { get; set; }

        /// <summary>
        /// The index of the marking (highlight) color.
        /// </summary>
        public int ColorIndex { get; set; }

        /// <summary>
        /// The location identifier.
        /// Refers to Location.LocationId
        /// </summary>
        public int LocationId { get; set; }

        public virtual Location? Location { get; set; }
        /// <summary>
        /// The style index (unused?)
        /// </summary>
        public int StyleIndex { get; set; }

        /// <summary>
        /// The guid. Useful in merging!
        /// </summary>
        public string UserMarkGuid { get; set; }

        /// <summary>
        /// The highlight version. Semantics unknown!
        /// </summary>
        public int Version { get; set; }

        public UserMark Clone()
        {
            return (UserMark)MemberwiseClone();
        }
    }
}