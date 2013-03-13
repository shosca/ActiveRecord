namespace Castle.ActiveRecord.Scopes {
    /// <summary>
    /// Defines the transaction scope behavior
    /// </summary>
    public enum TransactionMode {
        /// <summary>
        /// Inherits a transaction previously create on 
        /// the current context.
        /// </summary>
        Inherits,

        /// <summary>
        /// Always create an isolated transaction context.
        /// </summary>
        New
    }
}