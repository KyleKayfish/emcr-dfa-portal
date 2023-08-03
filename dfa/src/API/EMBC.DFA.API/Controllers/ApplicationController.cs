using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using EMBC.DFA.API.ConfigurationModule.Models.Dynamics;
using EMBC.DFA.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace EMBC.DFA.API.Controllers
{
    [Route("api/applications")]
    [ApiController]
    [Authorize]
    public class ApplicationController : ControllerBase
    {
        private readonly IHostEnvironment env;
        private readonly IMapper mapper;
        private readonly IConfigurationHandler handler;

        public ApplicationController(
            IHostEnvironment env,
            IMapper mapper,
            IConfigurationHandler handler)
        {
            this.env = env;
            this.mapper = mapper;
            this.handler = handler;
        }

        private string currentUserId => User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        /// <summary>
        /// Create an application
        /// </summary>
        /// <param name="application">The application information</param>
        /// <returns>application id</returns>
        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<string>> AddApplication(DFAApplicationStart application)
        {
            if (application == null) return BadRequest("Application details cannot be empty.");
            var mappedApplication = mapper.Map<dfa_appapplicationstart_params>(application);

            var applicationId = await handler.HandleApplication(mappedApplication);

            // If no insurance, add signatures
            if (application.AppTypeInsurance.insuranceOption == InsuranceOption.No)
            {
                IEnumerable<dfa_signature> insuranceSignatures = Enumerable.Empty<dfa_signature>();
                if (application.AppTypeInsurance.applicantSignature != null && application.AppTypeInsurance.applicantSignature.signature != null)
                {
                    var primarySignature = new dfa_signature();
                    primarySignature.Content = Encoding.ASCII.GetBytes(application.AppTypeInsurance.applicantSignature.signature);
                    primarySignature.FileName = "primaryApplicantSignatureNoIns";
                    primarySignature.ContentType = "image/png";
                    primarySignature.Regarding = "dfa_appapplication";
                    primarySignature.id = applicationId;
                    insuranceSignatures = insuranceSignatures.Append(primarySignature);
                }

                if (application.AppTypeInsurance.secondaryApplicantSignature != null && application.AppTypeInsurance.secondaryApplicantSignature.signature != null)
                {
                    var secondarySignature = new dfa_signature();
                    secondarySignature.Content = Encoding.ASCII.GetBytes(application.AppTypeInsurance.secondaryApplicantSignature.signature);
                    secondarySignature.FileName = "secondaryApplicantSignatureNoIns";
                    secondarySignature.ContentType = "image/png";
                    secondarySignature.Regarding = "dfa_appapplication";
                    secondarySignature.id = applicationId;
                    insuranceSignatures = insuranceSignatures.Append(secondarySignature);
                }
                var result = await handler.HandleSignatures(insuranceSignatures);
            }
            return Ok(applicationId);
        }

        /// <summary>
        /// Update an application
        /// </summary>
        /// <param name="application">The application information</param>
        /// <returns>application id</returns>
        [HttpPut("update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<string>> UpdateApplication(DFAApplicationMain application)
        {
            if (application == null) return BadRequest("Application details cannot be empty.");
            var mappedApplication = mapper.Map<dfa_appapplicationmain_params>(application);

            var applicationId = await handler.HandleApplicationUpdate(mappedApplication);
            IEnumerable<dfa_signature> appSignatures = Enumerable.Empty<dfa_signature>();

            // Add signatures
            if (application.signAndSubmit?.applicantSignature?.signature != null &&
                application.deleteFlag == false)
            {
                var primarySignature = new dfa_signature();
                primarySignature.Content = Encoding.ASCII.GetBytes(application.signAndSubmit.applicantSignature.signature);
                primarySignature.FileName = "primaryApplicantSignature";
                primarySignature.ContentType = "image/png";
                primarySignature.Regarding = "dfa_appapplication";
                primarySignature.id = applicationId;
                appSignatures = appSignatures.Append(primarySignature);
            }

            if (application.signAndSubmit?.secondaryApplicantSignature?.signature != null &&
                application.deleteFlag == false)
            {
                var secondarySignature = new dfa_signature();
                secondarySignature.Content = Encoding.ASCII.GetBytes(application.signAndSubmit.secondaryApplicantSignature.signature);
                secondarySignature.FileName = "secondaryApplicantSignature";
                secondarySignature.ContentType = "image/png";
                secondarySignature.Regarding = applicationId;
                secondarySignature.id = applicationId;
                appSignatures = appSignatures.Append(secondarySignature);
            }

            if (appSignatures != null)
            {
                var result = await handler.HandleSignatures(appSignatures);
            }
            return Ok(applicationId);
        }

        /// <summary>
        /// Get an application by Id
        /// </summary>
        /// <returns> DFAApplicationStart</returns>
        /// <param name="applicationId">The application Id.</param>
        [HttpGet("appstart/byId")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DFAApplicationStart>> GetApplicationStart(
            [FromQuery]
            [Required]
            Guid applicationId)
        {
            var dfa_appapplication = await handler.GetApplicationStartAsync(applicationId);
            DFAApplicationStart dfaApplicationStart = new DFAApplicationStart();
            dfaApplicationStart.Id = applicationId;
            dfaApplicationStart.ProfileVerification = mapper.Map<ProfileVerification>(dfa_appapplication);
            dfaApplicationStart.Consent = mapper.Map<Consent>(dfa_appapplication);
            dfaApplicationStart.AppTypeInsurance = mapper.Map<AppTypeInsurance>(dfa_appapplication);
            return Ok(dfaApplicationStart);
        }

        /// <summary>
        /// Get an application main by Id
        /// </summary>
        /// <returns> DFAApplicationMain</returns>
        /// <param name="applicationId">The application Id.</param>
        [HttpGet("appmain/byId")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DFAApplicationStart>> GetApplicationMain(
            [FromQuery]
            [Required]
            Guid applicationId)
        {
            var dfa_appapplication = await handler.GetApplicationMainAsync(applicationId);
            DFAApplicationMain dfaApplicationMain = new DFAApplicationMain();
            dfaApplicationMain.Id = applicationId;
            dfaApplicationMain.damagedPropertyAddress = mapper.Map<DamagedPropertyAddress>(dfa_appapplication);
            dfaApplicationMain.propertyDamage = mapper.Map<PropertyDamage>(dfa_appapplication);
            dfaApplicationMain.signAndSubmit = mapper.Map<SignAndSubmit>(dfa_appapplication);
            dfaApplicationMain.cleanUpLog = mapper.Map<CleanUpLog>(dfa_appapplication);
            dfaApplicationMain.supportingDocuments = mapper.Map<SupportingDocuments>(dfa_appapplication);
            return Ok(dfaApplicationMain);
        }

        /// <summary>
        /// get dfa applications
        /// </summary>
        /// <returns>list of dfa applications</returns>
        [HttpGet("dfaapplication")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CurrentApplication>>> GetDFAApplications()
        {
            var userId = currentUserId;
            var profile = await handler.HandleGetUser(userId);
            if (profile == null) return NotFound(userId);
            var profileId = profile.Id;
            //var profileId = "d4e3d345-d106-4f32-85d8-4ee301683b5f";
            var lstApplications = await handler.HandleApplicationList(profileId);
            return Ok(lstApplications);
        }
    }

    /// <summary>
    /// Application
    /// </summary>
    public class DFAApplicationStart
    {
        public Guid? Id { get; set; }

        public Consent Consent { get; set; }

        public ProfileVerification ProfileVerification { get; set; }

        public AppTypeInsurance AppTypeInsurance { get; set; }
    }

    public class DFAApplicationMain
    {
        public Guid Id { get; set; }

        public DamagedPropertyAddress? damagedPropertyAddress { get; set; }

        public PropertyDamage? propertyDamage { get; set; }

        public CleanUpLog? cleanUpLog { get; set; }

        public SupportingDocuments? supportingDocuments { get; set; }

        public SignAndSubmit? signAndSubmit { get; set; }
        public bool deleteFlag { get; set; }
    }

    public class CurrentApplication
    {
        public string ApplicationId { get; set; }
        public string EventId { get; set; }
        public string ApplicationType { get; set; }
        public string DamagedAddress { get; set; }
        public string CaseNumber { get; set; }
        public string DateOfDamage { get; set; }
    }
}
