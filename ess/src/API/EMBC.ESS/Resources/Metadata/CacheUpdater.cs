﻿using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using EMBC.ESS.Utilities.Dynamics;
using EMBC.Utilities.Caching;
using EMBC.Utilities.Hosting;
using Microsoft.Extensions.Logging;

namespace EMBC.ESS.Resources.Metadata
{
    public class CacheUpdater : IBackgroundTask
    {
        private readonly ILogger<CacheUpdater> logger;
        private readonly ICache cache;
        private readonly IMetadataRepository metadataRepository;

        public CacheUpdater(ILogger<CacheUpdater> logger, ICache cache, IEssContextFactory essContextFactory, IMapper mapper)
        {
            this.logger = logger;
            this.cache = cache;
            this.metadataRepository = new InternalMetadataRepository(essContextFactory, mapper);
        }

        public string Schedule => "* */15 * * * *";

        public int DegreeOfParallelism => 1;

        public TimeSpan InitialDelay => TimeSpan.FromSeconds(10);

        public TimeSpan InactivityTimeout => TimeSpan.FromMinutes(30);

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Start metadata cache refresh");
            await cache.Refresh(MetadataRepository.CommunitiesCacheKey, metadataRepository.GetCommunities, MetadataRepository.CacheEntryLifetime, cancellationToken);
            await cache.Refresh(MetadataRepository.CountriesCacheKey, metadataRepository.GetCountries, MetadataRepository.CacheEntryLifetime, cancellationToken);
            await cache.Refresh(MetadataRepository.StatesProvincesCacheKey, metadataRepository.GetStateProvinces, MetadataRepository.CacheEntryLifetime, cancellationToken);
            await cache.Refresh(MetadataRepository.SecurityQuestionsCacheKey, metadataRepository.GetSecurityQuestions, MetadataRepository.CacheEntryLifetime, cancellationToken);

            foreach (var portalType in Enum.GetValues<PortalType>())
            {
                var cacheKey = $"{MetadataRepository.PlannedOutagesCacheKey}:{portalType}";
                await cache.Refresh(cacheKey, () => metadataRepository.GetPlannedOutages(new OutageQuery
                {
                    PortalType = portalType,
                    DisplayDate = DateTime.UtcNow
                }), MetadataRepository.CacheEntryLifetime, cancellationToken);
            }
            logger.LogInformation("End metadata cache refresh");
        }
    }
}
