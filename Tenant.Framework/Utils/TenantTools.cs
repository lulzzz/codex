﻿using Codex.Core.Exceptions;
using Codex.Tenants.Framework.Exceptions;
using Codex.Models.Tenants;
using Dapr.Client;
using Dapr.Client.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Codex.Core.Models;
using Codex.Core.Cache;

namespace Codex.Tenants.Framework.Utils
{
    public static class TenantTools
    {
        public static async Task<Tenant> SearchTenantByIdAsync(ILogger logger, CacheService<Tenant> tenantCacheService, DaprClient daprClient, string tenantId)
        {
            try
            {
                string cacheKey = $"{CacheConstant.Tenant_}{tenantId}";
                var tenant = await tenantCacheService.GetCacheAsync(daprClient, cacheKey);

                if (tenant == null)
                {
                    // TODO Add api key ADMIN from global tenant and add tenant Id to header
                    tenant = await daprClient.InvokeMethodAsync<Tenant>("tenantapi", $"Tenant/{tenantId}", new HTTPExtension() { Verb = HTTPVerb.Get });
                    await tenantCacheService.UpdateCacheAsync(daprClient, cacheKey, tenant);
                    return tenant;
                }
                else
                {
                    return tenant;
                }
            }
            catch (Exception exception)
            {
                if (exception is Grpc.Core.RpcException rpcException &&
                    rpcException.Status.StatusCode == Grpc.Core.StatusCode.NotFound)
                {
                    logger.LogInformation(rpcException, $"Tenant not found : '{tenantId}'");
                    throw new InvalidTenantIdException($"Tenant not found : '{tenantId}'", "TENANT_NOT_FOUND");
                }

                logger.LogError(exception, $"Unable to find Tenant {tenantId}");
                throw new TechnicalException($"Tenant not found : '{tenantId}'", "TENANT_NOT_FOUND");
            }
        }
    }
}
