namespace LUC.VectorClock
{
    public enum Ordering
    {
        /// <summary>
        ///     VectorClock is younger.
        /// </summary>
        After,

        /// <summary>
        ///     VectorClock is older.
        /// </summary>
        Before,

        /// <summary>
        ///     VectorClock are same age.
        /// </summary>
        Same,

        /// <summary>
        ///     VectorClock both contain concurrent mutations and must be merged.
        /// </summary>
        Concurrent,

        //TODO: Ideally this would be private, change to override of compare?
        /// <summary>
        ///     TBD
        /// </summary>
        FullOrder
    }
}