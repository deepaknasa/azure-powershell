// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using Microsoft.Azure.Commands.Common.Authentication;
using Microsoft.Azure.Commands.Common.Authentication.Abstractions;
using Microsoft.Azure.Commands.Common.Authentication.Models;
using Microsoft.Azure.Commands.Sql.Common;
using Microsoft.Azure.Management.Internal.Resources;
using Microsoft.Azure.Management.Sql;
using Microsoft.Azure.Management.Sql.LegacySdk;
using Microsoft.Azure.Management.Sql.LegacySdk.Models;
using Microsoft.WindowsAzure.Management.Storage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Commands.Sql.ReplicationLink.Services
{
    /// <summary>
    /// This class is responsible for all the REST communication with the audit REST endpoints
    /// </summary>
    public class AzureSqlDatabaseReplicationCommunicator
    {
        /// <summary>
        /// The Sql client to be used by this end points communicator
        /// </summary>
        private static Management.Sql.LegacySdk.SqlManagementClient LegacySqlClient { get; set; }

        /// <summary>
        /// Gets or set the Azure subscription
        /// </summary>
        private static IAzureSubscription Subscription { get; set; }

        /// <summary>
        /// Gets or sets the Azure profile
        /// </summary>
        public IAzureContext Context { get; set; }

        /// <summary>
        /// Creates a communicator for Azure SQL Databases
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="subscription"></param>
        public AzureSqlDatabaseReplicationCommunicator(IAzureContext context)
        {
            Context = context;
            if (context.Subscription != Subscription)
            {
                Subscription = context.Subscription;
                LegacySqlClient = null;
            }
        }

        /// <summary>
        /// Gets the specified Azure SQL Database replication link with Lagecy sdk
        /// </summary>
        public Management.Sql.LegacySdk.Models.ReplicationLink GetLink(string resourceGroupName, string serverName, string databaseName, Guid linkId)
        {
            return GetLegacySqlClient().DatabaseReplicationLinks.Get(resourceGroupName, serverName, databaseName, linkId.ToString()).ReplicationLink;
        }

        /// <summary>
        /// Gets the specified Azure Sql Database replication link with new Autorest sdk
        /// </summary>
        public Management.Sql.Models.ReplicationLink GetLinkWithNewSdk(string resourceGroupName, string serverName, string databaseName, Guid linkId)
        {
            return GetCurrentSqlClient().ReplicationLinks.Get(resourceGroupName, serverName, databaseName, linkId.ToString());
        }

        /// <summary>
        /// Lists Azure SQL Databases replication links with Legacy sdk
        /// </summary>
        public IList<Management.Sql.LegacySdk.Models.ReplicationLink> ListLinks(string resourceGroupName, string serverName, string databaseName)
        {
            return GetLegacySqlClient().DatabaseReplicationLinks.List(resourceGroupName, serverName, databaseName).ReplicationLinks;
        }

        /// <summary>
        /// List Azure Sql database replication links with new Autorest sdk
        /// </summary>
        public IList<Management.Sql.Models.ReplicationLink> ListLinksWithNewSdk(String resourceGroupName, string serverName, string databaseName)
        {
            return GetCurrentSqlClient().ReplicationLinks.ListByDatabase(resourceGroupName, serverName, databaseName).ToList();
        }

        /// <summary>
        /// Creates a copy of a Azure SQL Database with Legacy SDK model
        /// </summary>
        public Management.Sql.LegacySdk.Models.DatabaseCreateOrUpdateResponse CreateCopy(string resourceGroupName, string serverName, string databaseName, DatabaseCreateOrUpdateParameters parameters)
        {
            return GetLegacySqlClient().Databases.CreateOrUpdate(resourceGroupName, serverName, databaseName, parameters);
        }

        /// <summary>
        /// Creates a copy of a Azure SQL Database with new Autorest SDK
        /// </summary>
        public Management.Sql.Models.Database CreateCopy(string resourceGroupName, string serverName, string databaseName, Management.Sql.Models.Database parameters)
        {
            return GetCurrentSqlClient().Databases.CreateOrUpdate(resourceGroupName, serverName, databaseName, parameters);
        }

        /// <summary>
        /// Deletes a Replication Link
        /// </summary>
        public void RemoveLink(string resourceGroupName, string serverName, string databaseName, Guid linkId)
        {
            GetLegacySqlClient().DatabaseReplicationLinks.Delete(resourceGroupName, serverName, databaseName, linkId.ToString());
        }

        /// <summary>
        /// Deletes a Replication Link with new autorest sdk
        /// </summary>
        public void RemoveLinkWithNewSdk(string resourceGroupName, string serverName, string databaseName, Guid linkId)
        {
            GetCurrentSqlClient().ReplicationLinks.Delete(resourceGroupName, serverName, databaseName, linkId.ToString());
        }

        /// <summary>
        /// Fails over a Replication Link without data loss
        /// </summary>
        public void FailoverLink(string resourceGroupName, string serverName, string databaseName, Guid linkId)
        {
            GetLegacySqlClient().DatabaseReplicationLinks.Failover(resourceGroupName, serverName, databaseName, linkId.ToString());
        }

        /// <summary>
        /// Fails over a replication link without data loss using new autorest sdk
        /// </summary>
        public void FailoverLinkWithNewSdk(string resourceGroupName, string serverName, string databaseName, Guid linkId)
        {
            GetCurrentSqlClient().ReplicationLinks.Failover(resourceGroupName, serverName, databaseName, linkId.ToString());
        }

        /// <summary>
        /// Fails over a Replication Link with data loss
        /// </summary>
        public void FailoverLinkAllowDataLoss(string resourceGroupName, string serverName, string databaseName, Guid linkId)
        {
            GetLegacySqlClient().DatabaseReplicationLinks.FailoverAllowDataLoss(resourceGroupName, serverName, databaseName, linkId.ToString());
        }

        /// <summary>
        /// Fails over a replication link with data loss using new autorest sdk
        /// </summary>
        public void FailoverLinkAllowDatalossWithNewSdk(string resourceGroupName, string serverName, string databaseName, Guid linkId)
        {
            GetCurrentSqlClient().ReplicationLinks.FailoverAllowDataLoss(resourceGroupName, serverName, databaseName, linkId.ToString());
        }

        /// <summary>
        /// Retrieve the SQL Management client for the currently selected subscription, adding the session and request
        /// id tracing headers for the current cmdlet invocation.
        /// </summary>
        /// <returns>The SQL Management client for the currently selected subscription.</returns>
        private Management.Sql.SqlManagementClient GetCurrentSqlClient()
        {
            // Get the SQL management client for the current subscription
            // Note: client is not cached in static field because that causes ObjectDisposedException in functional tests
            var sqlClient = AzureSession.Instance.ClientFactory.CreateArmClient<Management.Sql.SqlManagementClient>(Context, AzureEnvironment.Endpoint.ResourceManager);

            return sqlClient;
        }

        /// <summary>
        /// Retrieve the SQL Management client for the currently selected subscription, adding the session and request
        /// id tracing headers for the current cmdlet invocation.
        /// </summary>
        /// <returns>The SQL Management client for the currently selected subscription.</returns>
        private Management.Sql.LegacySdk.SqlManagementClient GetLegacySqlClient()
        {
            // Get the SQL management client for the current subscription
            if (LegacySqlClient == null)
            {
                LegacySqlClient = AzureSession.Instance.ClientFactory.CreateClient<Management.Sql.LegacySdk.SqlManagementClient>(Context, AzureEnvironment.Endpoint.ResourceManager);
            }
            return LegacySqlClient;
        }
    }
}
