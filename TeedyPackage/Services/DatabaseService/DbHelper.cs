using Dapper;
using System.Data;
using System.Data.SqlClient;

namespace TeedyPackage.Services.DatabaseService
{
    public class DbHelper
    {
        private readonly string _connectionString;

        public DbHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<T> Query<T>(string sql, object parameters = null)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                return db.Query<T>(sql, parameters);
            }
        }

        public int Execute(string sql, object parameters = null)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                return db.Execute(sql, parameters);
            }
        }
    }
}
