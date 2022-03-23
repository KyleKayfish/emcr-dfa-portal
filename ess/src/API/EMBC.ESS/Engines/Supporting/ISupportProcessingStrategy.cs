﻿using System;
using System.Threading.Tasks;
using EMBC.ESS.Engines.Supporting.SupportProcessing;
using Microsoft.Extensions.DependencyInjection;

namespace EMBC.ESS.Engines.Supporting
{
    internal interface ISupportProcessingStrategy
    {
        Task<ProcessResponse> Handle(ProcessRequest request);

        Task<ValidationResponse> Handle(ValidationRequest request);
    }

    internal class SupportProcessingStrategyFactory
    {
        private IServiceProvider services;

        public SupportProcessingStrategyFactory(IServiceProvider services)
        {
            this.services = services;
        }

        public ISupportProcessingStrategy Create(SupportProcessingStrategyType type) => type switch
        {
            SupportProcessingStrategyType.Digital => services.GetRequiredService<DigitalSupportProcessingStrategy>(),
            SupportProcessingStrategyType.Paper => services.GetRequiredService<PaperSupportProcessingStrategy>(),

            _ => throw new NotImplementedException($"{type}")
        };
    }

    internal enum SupportProcessingStrategyType
    {
        Digital,
        Paper
    }
}
