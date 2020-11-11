namespace JWLMergeCore.BackupFileServices.Models
{
    using System;

    public struct BibleBookChapterAndVerse : IEquatable<BibleBookChapterAndVerse>
    {
        public BibleBookChapterAndVerse(int bookNum, int chapterNum, int verseNum)
        {
            BookNumber = bookNum;
            ChapterNumber = chapterNum;
            VerseNumber = verseNum;
        }

        public int BookNumber { get; }

        public int ChapterNumber { get; }

        public int VerseNumber { get; }

        public static bool operator ==(BibleBookChapterAndVerse lhs, BibleBookChapterAndVerse rhs) => lhs.Equals(rhs);

        public static bool operator !=(BibleBookChapterAndVerse lhs, BibleBookChapterAndVerse rhs) => !lhs.Equals(rhs);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(BookNumber, ChapterNumber, VerseNumber);
        }

        public override bool Equals(object? obj) => obj is BibleBookChapterAndVerse other && Equals(other);

        public bool Equals(BibleBookChapterAndVerse other) => BookNumber == other.BookNumber && ChapterNumber == other.ChapterNumber && VerseNumber == other.VerseNumber;
    }
}