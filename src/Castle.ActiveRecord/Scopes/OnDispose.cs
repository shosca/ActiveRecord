namespace Castle.ActiveRecord.Scopes {
    /// <summary>
    /// Governs the <see cref="TransactionScope"/> behavior 
    /// on dispose if neither <see cref="TransactionScope.VoteCommit"/>
    /// nor <see cref="TransactionScope.VoteRollBack"/> was called
    /// </summary>
    public enum OnDispose {
        /// <summary>
        /// Should commit the transaction, unless <see cref="TransactionScope.VoteRollBack"/>
        /// was called before the disposing the scope (this is the default behavior)
        /// </summary>
        Commit,

        /// <summary>
        /// Should rollback the transaction, unless <see cref="TransactionScope.VoteCommit"/>
        /// was called before the disposing the scope
        /// </summary>
        Rollback
    }
}