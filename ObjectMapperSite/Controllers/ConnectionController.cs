using System.Web.Mvc;
using System.Linq;
using System.Configuration;

namespace ObjectMapperSite.Controllers
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics;

    public abstract class ConnectionController : Controller
    {
        private readonly string connectionStringName;
        private readonly string connectionString;
        private readonly string providerName;

        private Lazy<IDbConnection> connection;

        protected ConnectionController()
            : this("default")
        {
        }

        protected ConnectionController(string connectionStringName)
        {
            this.connectionStringName = connectionStringName;
            var connString = ConfigurationManager.ConnectionStrings[this.connectionStringName];
            this.connectionString = connString.ConnectionString;
            this.providerName = connString.ProviderName;
            if (string.IsNullOrEmpty(this.providerName))
                this.providerName = "System.Data.SqlClient";

            connection = new Lazy<IDbConnection>(this.CreateConnection);
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

        protected IDbConnection Connection
        {
            get
            {
                return connection.Value;
            }
        }

        protected override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (connection.IsValueCreated && connection.Value.State != ConnectionState.Closed)
            {
                connection.Value.Close();
                Debug.WriteLine("Closing database connection.");
            }
        }
    }
}
