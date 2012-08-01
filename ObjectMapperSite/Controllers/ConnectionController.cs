// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConnectionController.cs" company="Bryan Ross">
//   Copyright (c) Bryan Ross
// </copyright>
// <summary>
//   Defines the ConnectionController type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ObjectMapperSite.Controllers
{
    using System;
    using System.Configuration;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Web.Mvc;

    /// <summary>
    /// The connection controller.
    /// </summary>
    public abstract class ConnectionController : Controller
    {
        private readonly string connectionStringName;
        private readonly string connectionString;
        private readonly string providerName;

        private Lazy<IDbConnection> connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionController"/> class.
        /// </summary>
        protected ConnectionController()
            : this("default")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionController"/> class.
        /// </summary>
        /// <param name="connectionStringName">
        /// The connection string name.
        /// </param>
        protected ConnectionController(string connectionStringName)
        {
            this.connectionStringName = connectionStringName;
            var connString = ConfigurationManager.ConnectionStrings[this.connectionStringName];
            this.connectionString = connString.ConnectionString;
            this.providerName = connString.ProviderName;
            if (string.IsNullOrEmpty(this.providerName))
            {
                this.providerName = "System.Data.SqlClient";
            }

            this.connection = new Lazy<IDbConnection>(this.CreateConnection);
        }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        protected IDbConnection Connection
        {
            get
            {
                return this.connection.Value;
            }
        }

        /// <summary>
        /// The on result executed.
        /// </summary>
        /// <param name="filterContext">
        /// The filter context.
        /// </param>
        protected override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (this.connection.IsValueCreated && this.connection.Value.State != ConnectionState.Closed)
            {
                this.connection.Value.Close();
                Debug.WriteLine("Closing database connection.");
            }
        }

        private IDbConnection CreateConnection()
        {
            var provider = DbProviderFactories.GetFactory(this.providerName);
            var conn = provider.CreateConnection();
            if (conn == null)
            {
                throw new InvalidOperationException("Could not create connection for provider " + this.providerName);
            }

            Debug.WriteLine("Opening database connection.");
            conn.ConnectionString = this.connectionString;
            conn.Open();
            return conn;
        }
    }
}
