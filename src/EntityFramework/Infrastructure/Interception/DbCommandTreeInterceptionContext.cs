// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure.Interception
{
    using System.ComponentModel;
    using System.Data.Entity.Core.Common.CommandTrees;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Utilities;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Represents contextual information associated with calls into <see cref="IDbCommandTreeInterceptor" />
    /// implementations.
    /// </summary>
    /// <remarks>
    /// Instances of this class are publicly immutable for contextual information. To add
    /// contextual information use one of the With... or As... methods to create a new
    /// interception context containing the new information.
    /// </remarks>
    public class DbCommandTreeInterceptionContext :
        DbInterceptionContext,
        IDbMutableInterceptionContext<DbCommandTree>
    {
        private readonly InterceptionContextMutableData<DbCommandTree> _mutableData
            = new InterceptionContextMutableData<DbCommandTree>();

        /// <summary>
        /// Constructs a new <see cref="DbCommandTreeInterceptionContext" /> with no state.
        /// </summary>
        public DbCommandTreeInterceptionContext()
        {
        }

        /// <summary>
        /// Creates a new <see cref="DbCommandTreeInterceptionContext" /> by copying state from the given
        /// interception context. Also see <see cref="DbCommandTreeInterceptionContext.Clone" />
        /// </summary>
        /// <param name="copyFrom">The context from which to copy state.</param>
        public DbCommandTreeInterceptionContext(DbInterceptionContext copyFrom)
            : base(copyFrom)
        {
            Check.NotNull(copyFrom, "copyFrom");
        }

        internal InterceptionContextMutableData<DbCommandTree> MutableData
        {
            get { return _mutableData; }
        }

        InterceptionContextMutableData<DbCommandTree> IDbMutableInterceptionContext<DbCommandTree>.MutableData
        {
            get { return _mutableData; }
        }

        InterceptionContextMutableData IDbMutableInterceptionContext.MutableData
        {
            get { return _mutableData; }
        }

        /// <summary>
        /// The original tree created by Entity Framework. Interceptors can change the
        /// <see cref="Result" /> property to change the tree that will be used, but the
        /// <see cref="OriginalResult" /> will always be the tree created by Entity Framework.
        /// </summary>
        public DbCommandTree OriginalResult
        {
            get { return _mutableData.OriginalResult; }
        }

        /// <summary>
        /// The command tree that will be used by Entity Framework. This starts as the tree contained in the 
        /// the <see cref="OriginalResult"/> property but can be set by interceptors to change
        /// the tree that will be used by Entity Framework.
        /// </summary>
        public DbCommandTree Result
        {
            get { return _mutableData.Result; }
            set { _mutableData.Result = value; }
        }

        /// <inheritdoc />
        protected override DbInterceptionContext Clone()
        {
            return new DbCommandTreeInterceptionContext(this);
        }

        /// <summary>
        /// Creates a new <see cref="DbCommandTreeInterceptionContext" /> that contains all the contextual information in this
        /// interception context with the addition of the given <see cref="DbContext" />.
        /// </summary>
        /// <param name="context">The context to associate.</param>
        /// <returns>A new interception context associated with the given context.</returns>
        public new DbCommandTreeInterceptionContext WithDbContext(DbContext context)
        {
            Check.NotNull(context, "context");

            return (DbCommandTreeInterceptionContext)base.WithDbContext(context);
        }

        /// <summary>
        /// Creates a new <see cref="DbCommandTreeInterceptionContext" /> that contains all the contextual information in this
        /// interception context with the addition of the given <see cref="ObjectContext" />.
        /// </summary>
        /// <param name="context">The context to associate.</param>
        /// <returns>A new interception context associated with the given context.</returns>
        public new DbCommandTreeInterceptionContext WithObjectContext(ObjectContext context)
        {
            Check.NotNull(context, "context");

            return (DbCommandTreeInterceptionContext)base.WithObjectContext(context);
        }

        /// <summary>
        /// Creates a new <see cref="DbCommandTreeInterceptionContext" /> that contains all the contextual information in this
        /// interception context the <see cref="DbInterceptionContext.IsAsync" /> flag set to true.
        /// </summary>
        /// <returns>A new interception context associated with the async flag set.</returns>
        public new DbCommandTreeInterceptionContext AsAsync()
        {
            return (DbCommandTreeInterceptionContext)base.AsAsync();
        }

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString()
        {
            return base.ToString();
        }

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <inheritdoc />
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new Type GetType()
        {
            return base.GetType();
        }
    }
}
